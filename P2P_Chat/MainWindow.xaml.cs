using P2P_Chat.JsonParser;
// Moja bibliotek z plikami netowrk
using P2P_Chat.Network;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Linq;


namespace P2P_Chat
{
    public partial class MainWindow : Window
    {
        //zmienna nazwy użytkownika
        private string _userName;
        // Serwic do obsługi UDP
        private UdpService udp;
        // Serwic do obsługi TCP
        private TcpService tcp;

        // Obiekt do: Wykrywanie peerów, wysyłanie wiadomości typu HELLO / GOODBYE
        private DiscoveryService discovery;
        // Menadżer zarządzający peerami
        private PeerManager peerManager;
        private GroupManager groupManager;
        private string currentGroupId = "";
        public MainWindow(string userName)
        {
            InitializeComponent();
            //przypisanie nazwy z okna logowania
            _userName = userName;
            // Utworzenie clienta TCP na porcie 53241
            tcp = new TcpService(53241);
            // Utworzenie socketa UDP na porcie 50000
            udp = new UdpService(50000);
            // Konstruktor menadżera peerów
            peerManager = new PeerManager();
            // Konstruktor menadżera grup
            groupManager = new GroupManager();
            // Inicjalizacja serwisu zarządzania statusem w sieci.
            discovery = new DiscoveryService(
                udp,        // handler do serwisu UDP
                _userName,    // NAZWA UŻYTKOWNIKA
                53241,      // PORT TAKI SAM JAK W TCP!!!
                peerManager // handler do menadżera peerów
            );

            // Dodanie eventów do odbierania wiadomości po TCP i UDP
            tcp.OnMessageReceived += OnTcpMessage;
            udp.OnMessageReceived += OnUdpMessage;
            Loaded += MainWindow_Loaded;

            // Ustawienie nicku w polu tekstowym na starcie
            NickEditBox.Text = _userName;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartNetworking();
        }

        private void StartNetworking()
        {
            // Uruchomienie funkcji asynchronicznych do nasłuchiwania po TCP i UDP (w tle)
            _ = udp.StartListening();
            _ = tcp.StartListening();

            // Dodanie siebie na początek listy
            PeersListBox.Items.Add($"{_userName} (ja)");

            // Broadcast loop działający w tle. Podtrzymanie statusu w sieci oraz walidacja pozostałych peerów.
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await discovery.SendHello();
                    peerManager.ValidatePeers();
                    var activePeers = peerManager.GetPeers();

                    Dispatcher.Invoke(() =>
                    {
                        // Aktualizujemy "Ja" na samej górze
                        if (PeersListBox.Items.Count == 0)
                            PeersListBox.Items.Add($"{_userName} (ja)");
                        else
                            PeersListBox.Items[0] = $"{_userName} (ja)";

                        // Usuwamy nieaktywnych (od indeksu 1, żeby nie usunąć siebie)
                        for (int i = PeersListBox.Items.Count - 1; i >= 1; i--)
                        {
                            string item = PeersListBox.Items[i].ToString();
                            if (!activePeers.Any(p => $"{p.Name} ({p.IP})" == item))
                                PeersListBox.Items.RemoveAt(i);
                        }

                        // Dodajemy nowych
                        foreach (var peer in activePeers)
                        {
                            string entry = $"{peer.Name} ({peer.IP})";
                            if (!PeersListBox.Items.Cast<object>().Any(x => x.ToString() == entry))
                                PeersListBox.Items.Add(entry);
                        }
                    });
                    await Task.Delay(3000);
                }
            });
        }

        // Obsługa zmiany nicku w locie
        private void NickEditBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NickEditBox == null || discovery == null) return;

            // Aktualizacja zmiennej i serwisu discovery
            _userName = NickEditBox.Text;
            discovery.Name = _userName;

            // Natychmiastowe odświeżenie "Ja" na liście
            if (PeersListBox != null && PeersListBox.Items.Count > 0)
                PeersListBox.Items[0] = $"{_userName} (ja)";
        }

        // Calback dla btn click
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string msg = MessageInput.Text;

            if (string.IsNullOrEmpty(msg))
                return;

            if (!string.IsNullOrEmpty(currentGroupId))
            {
                var group = groupManager.GetGroup(currentGroupId);
                ChatBox.Items.Add($"[{group?.GroupName ?? "Group"}] {_userName}: {msg}");

                await SendGroupMessage(currentGroupId, msg);
            }
            else
            {
                ChatBox.Items.Add($"ME: {msg}");
                var peers = peerManager.GetPeers();

                foreach (var peer in peers)
                {
                    try
                    {
                        await tcp.SendMessage(
                            Parser.ParseModelToJson(new Model
                            {
                                Type = MessageType.MESSAGE,
                                Name = _userName, // Używamy aktualnego nicku
                                payload = msg
                            }),
                            peer.IP,
                            peer.Port
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Błąd wysyłki do " + peer.IP + ": " + ex.Message);
                    }
                }
            }
            MessageInput.Clear();
        }

        // Callback dla odebranej wiadomości po UDP
        private void OnUdpMessage(string msg, IPEndPoint endpoint)
        {

            Model? model = Parser.ParseJsonToModel(msg);

            // może być null ("?"), więc trzeba sprawdzić
            if (model == null)
                return;

            // Obsługa danych ze względu na typ otrzymanej ramki
            if (model.Type == MessageType.HELLO)
            {
                discovery.HandleHello(model, endpoint);
            }
            else if (model.Type == MessageType.GOODBYE)
            {
                discovery.HandleBye(model, endpoint);
                Dispatcher.Invoke(() =>
                {
                    ChatBox.Items.Add($"--- {model.Name} wyszedł z czatu ---");
                });
            }
        }

        // Callback dla odebranej wiadomości po TCP
        private async void OnTcpMessage(string msg, IPEndPoint endpoint)
        {
            // Parsowanie json stringa do obiektu klasy Model, żeby łatwiej było obsługiwać dane
            var model = Parser.ParseJsonToModel(msg);
            // obsłuż wyjątek
            if (model == null) return;

            // Tylko jeśli odebrana ramka jest typu MESSAGE
            if (model.Type == MessageType.MESSAGE)
            {
                // Musimy użyć Dispatchera, bo wiadomość przychodzi z innego wątku!
                Dispatcher.Invoke(() =>
                {
                    // printuj w czatboxie po nazwie.
                    ChatBox.Items.Add($"{model.Name}: {model.payload}");
                });
                // logging w terminalu na wszelki wypadek
                Console.WriteLine($"{model.Name}: {model.payload}");
            }
            // Obsługa odebranej wiadomości CREATE_GROUP.
            if (model.Type == MessageType.CREATE_GROUP)
            {
                // Utworzenie lokalnej kopii grupy w pamięci aplikacji.
                groupManager.CreateGroup(
                    model.GroupId,     // unikalny identyfikator grupy
                    model.GroupName,   // nazwa grupy widoczna w GUI
                    model.Name         // host/twórca grupy
                );

                // Dodanie hosta grupy jako pierwszego członka grupy.
                groupManager.AddMember(model.GroupId, model.Name);

                // Aktualizacja GUI musi zostać wykonana przez Dispatcher
                Dispatcher.Invoke(() =>
                {
                    // Informacja w oknie czatu o wykryciu nowej grupy.
                    ChatBox.Items.Add($"Group available: {model.GroupName}");

                    // Odświeżenie listy grup widocznych w GUI.
                    RefreshGroupList();
                });
            }

            // Obsługa wiadomości GROUP_JOIN.
            if (model.Type == MessageType.GROUP_JOIN)
            {
                // Dodanie nowego użytkownika do lokalnej listy członków grupy.
                groupManager.AddMember(model.GroupId, model.Name);

                // Jeśli to JA jestem hostem tej grupy, synchronizujemy listę członków
                var group = groupManager.GetGroup(model.GroupId);
                if (group != null && group.HostName == _userName)
                {
                    var syncModel = new Model
                    {
                        Type = MessageType.GROUP_INVITE,
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        Members = group.Members
                    };
                    string json = Parser.ParseModelToJson(syncModel);
                    await tcp.SendMessage(json, endpoint.Address.ToString(), model.Port);
                }

                Dispatcher.Invoke(() =>
                {
                    ChatBox.Items.Add($"{model.Name} joined group");
                });
            }
            if (model.Type == MessageType.GROUP_INVITE)
            {
                // Aktualizujemy lokalną wiedzę o grupie na podstawie danych od Hosta
                groupManager.CreateGroup(model.GroupId, model.GroupName, model.Name);
                foreach (var member in model.Members)
                {
                    groupManager.AddMember(model.GroupId, member);
                }

                Dispatcher.Invoke(() => {
                    ChatBox.Items.Add($"Synchronized group: {model.GroupName}");
                    RefreshGroupList();
                });
            }

            // Obsługa wiadomości GROUP_MESSAGE.
            if (model.Type == MessageType.GROUP_MESSAGE)
            {
                Dispatcher.Invoke(() =>
                {
                    ChatBox.Items.Add(
                        $"[{model.GroupName}] {model.Name}: {model.payload}"
                    );
                });
            }
        }

        // Metoda odpowiedzialna za utworzenie nowej grupy.
        private async Task CreateGroup(string groupName)
        {
            // Wygenerowanie unikalnego identyfikatora grupy.
            string groupId = Guid.NewGuid().ToString();

            // Utworzenie grupy lokalnie
            groupManager.CreateGroup(groupId, groupName, _userName);
            groupManager.AddMember(groupId, _userName);

            // Utworzenie modelu wiadomości CREATE_GROUP
            var model = new Model
            {
                Type = MessageType.CREATE_GROUP,
                Name = _userName,
                GroupId = groupId,
                GroupName = groupName
            };

            string json = Parser.ParseModelToJson(model);

            // Wysłanie informacji do wszystkich peerów
            foreach (var peer in peerManager.GetPeers())
            {
                await tcp.SendMessage(json, peer.IP, peer.Port);
            }

            ChatBox.Items.Add($"Group created: {groupName}");
            RefreshGroupList();
        }

        // Metoda odpowiedzialna za dołączenie użytkownika do istniejącej grupy.
        private async Task JoinGroup(string groupId)
        {
            var group = groupManager.GetGroup(groupId);

            if (group == null)
                return;

            groupManager.AddMember(groupId, _userName);

            var model = new Model
            {
                Type = MessageType.GROUP_JOIN,
                Name = _userName,
                GroupId = groupId,
                Port = 53241
            };

            string json = Parser.ParseModelToJson(model);

            foreach (var peer in peerManager.GetPeers())
            {
                if (peer.Name == group.HostName)
                {
                    await tcp.SendMessage(json, peer.IP, peer.Port);
                    break;
                }
            }
        }

        // Metoda odpowiedzialna za wysyłanie wiadomości grupowej.
        private async Task SendGroupMessage(string groupId, string message)
        {
            var group = groupManager.GetGroup(groupId);

            if (group == null)
                return;

            var model = new Model
            {
                Type = MessageType.GROUP_MESSAGE,
                Name = _userName,
                GroupId = groupId,
                GroupName = group.GroupName,
                payload = message
            };

            string json = Parser.ParseModelToJson(model);

            foreach (var member in group.Members)
            {
                if (member == _userName)
                    continue;

                foreach (var peer in peerManager.GetPeers())
                {
                    if (peer.Name == member)
                    {
                        await tcp.SendMessage(json, peer.IP, peer.Port);
                    }
                }
            }
        }

        // Callback wywoływany po kliknięciu przycisku "Create Group".
        private async void CreateGroup_Click(object sender, RoutedEventArgs e)
        {
            // Pobieramy nazwę z pola tekstowego, jeśli puste dajemy domyślną
            string gName = GroupNameEditBox.Text;
            if (string.IsNullOrWhiteSpace(gName)) gName = "Nowa Grupa";

            await CreateGroup(gName);
        }

        // Metoda odpowiedzialna za odświeżenie listy grup w GUI.
        private void RefreshGroupList()
        {
            GroupList.Items.Clear();
            foreach (var group in groupManager.GetGroups())
            {
                GroupList.Items.Add(group.GroupName);
            }
        }

        // Callback wywoływany po zmianie zaznaczenia grupy w GroupList.
        private void GroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupList.SelectedItem == null)
            {
                currentGroupId = "";
                return;
            }
            var selected = groupManager
                .GetGroups()
                .FirstOrDefault(g => g.GroupName == GroupList.SelectedItem?.ToString());

            if (selected != null)
            {
                currentGroupId = selected.GroupId;
            }
        }

        // Callback wywoływany po podwójnym kliknięciu grupy w GroupList.
        private async void GroupList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = groupManager
                .GetGroups()
                .FirstOrDefault(g => g.GroupName == GroupList.SelectedItem?.ToString());

            if (selected != null)
            {
                await JoinGroup(selected.GroupId);
                currentGroupId = selected.GroupId;
                ChatBox.Items.Add($"Joined group: {selected.GroupName}");
            }
        }
        //wyjscie z grupy
        private void LeaveGroup_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentGroupId))
            {
                MessageBox.Show("Nie jesteś w żadnej grupie!");
                return;
            }

            // Pobieramy nazwę grupy przed wyjściem, żeby wyświetlić info
            var group = groupManager.GetGroup(currentGroupId);
            string groupName = group?.GroupName ?? "grupy";

            // KLUCZOWY MOMENT: Czyścimy ID aktualnej grupy
            currentGroupId = "";

            // Resetujemy zaznaczenie na liście w GUI
            GroupList.SelectedItem = null;

            ChatBox.Items.Add($"--- Opuściłeś grupę: {groupName}. Powrót do czatu głównego. ---");
        }
    }
}