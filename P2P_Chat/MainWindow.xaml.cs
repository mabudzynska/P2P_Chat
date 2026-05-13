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

            // Broadcast loop działający w tle. Podtrzymanie statusu w sieci oraz walidacja pozostałych peerów.
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    // co 3 sekundy wyślij że jesteś aktywny, a potem sprawdź czy pozostałe peery nie zrobiły timeoutu.
                    await discovery.SendHello();
                    peerManager.ValidatePeers();
                    //Odświeżanie listy osób po prawej stronie
                    Dispatcher.Invoke(() =>
                    {
                        PeersListBox.Items.Clear();
                        foreach (var peer in peerManager.GetPeers())
                        {
                            // Wyświetlamy Nazwę i IP dla ułatwienia testów
                            PeersListBox.Items.Add($"{peer.Name} ({peer.IP})");
                        }
                    });
                    await Task.Delay(3000);
                }
            });
        }

        // Calback dla btn click
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string msg = MessageInput.Text;

            if (string.IsNullOrEmpty(msg))
                return;

            ChatBox.Items.Add($"ME: {msg}");

            MessageInput.Clear();

            if (!string.IsNullOrEmpty(currentGroupId))
            {
                await SendGroupMessage(currentGroupId, msg);
            }
            else
            {
                var peers = peerManager.GetPeers();

                foreach (var peer in peers)
                {
                    try
                    {
                        await tcp.SendMessage(
                            Parser.ParseModelToJson(new Model
                            {
                                Type = MessageType.MESSAGE,
                                Name = _userName,
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
            }
        }

        // Callback dla odebranej wiadomości po TCP
        private void OnTcpMessage(string msg, IPEndPoint endpoint)
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
            // Oznacza to, że inny peer w sieci utworzył nową grupę.
            if (model.Type == MessageType.CREATE_GROUP)
            {
                // Utworzenie lokalnej kopii grupy w pamięci aplikacji.
                // Każdy peer przechowuje własną listę grup.
                groupManager.CreateGroup(
                    model.GroupId,     // unikalny identyfikator grupy
                    model.GroupName,   // nazwa grupy widoczna w GUI
                    model.Name         // host/twórca grupy
                );

                // Dodanie hosta grupy jako pierwszego członka grupy.
                // Twórca grupy automatycznie należy do swojej grupy.
                groupManager.AddMember(model.GroupId, model.Name);

                // Aktualizacja GUI musi zostać wykonana przez Dispatcher,
                // ponieważ wiadomość TCP została odebrana w innym wątku.
                Dispatcher.Invoke(() =>
                {
                    // Informacja w oknie czatu o wykryciu nowej grupy.
                    ChatBox.Items.Add($"Group available: {model.GroupName}");

                    // Odświeżenie listy grup widocznych w GUI.
                    RefreshGroupList();
                });
            }

            // Obsługa wiadomości GROUP_JOIN.
            // Oznacza to, że użytkownik dołączył do istniejącej grupy.
            if (model.Type == MessageType.GROUP_JOIN)
            {
                // Dodanie nowego użytkownika do lokalnej listy członków grupy.
                // Każdy peer przechowuje własną kopię members list.
                groupManager.AddMember(model.GroupId, model.Name);

                // Aktualizacja GUI przez Dispatcher,
                // ponieważ wiadomość została odebrana w osobnym wątku TCP.
                Dispatcher.Invoke(() =>
                {
                    // Wyświetlenie informacji o dołączeniu użytkownika do grupy.
                    ChatBox.Items.Add($"{model.Name} joined group");
                });
            }

            // Obsługa wiadomości GROUP_MESSAGE.
            // Jest to zwykła wiadomość wysłana do grupy.
            if (model.Type == MessageType.GROUP_MESSAGE)
            {
                // Aktualizacja GUI w bezpieczny sposób z poziomu głównego wątku WPF.
                Dispatcher.Invoke(() =>
                {
                    // Wyświetlenie wiadomości grupowej w oknie czatu.
                    // Pokazywany jest identyfikator grupy, nadawca oraz treść wiadomości.
                    ChatBox.Items.Add(
                        $"[GROUP {model.GroupId}] {model.Name}: {model.payload}"
                    );
                });
            }
        }
        // Metoda odpowiedzialna za utworzenie nowej grupy.
        private async Task CreateGroup(string groupName)
        {
            // Wygenerowanie unikalnego identyfikatora grupy.
            // GUID pozwala jednoznacznie identyfikować grupę w całej sieci P2P.
            string groupId = Guid.NewGuid().ToString();

            // Utworzenie grupy lokalnie w pamięci aplikacji.
            // Każdy peer przechowuje własną listę grup.
            groupManager.CreateGroup(groupId, groupName, _userName);

            // Dodanie hosta (twórcy grupy) jako pierwszego członka grupy.
            groupManager.AddMember(groupId, _userName);

            // Utworzenie modelu wiadomości CREATE_GROUP,
            // który zostanie wysłany do pozostałych peerów w sieci.
            var model = new Model
            {
                Type = MessageType.CREATE_GROUP, // typ wiadomości sieciowej
                Name = _userName,                // nazwa twórcy grupy (hosta)
                GroupId = groupId,               // unikalny identyfikator grupy
                GroupName = groupName            // nazwa grupy widoczna w GUI
            };

            // Konwersja obiektu Model do formatu JSON.
            string json = Parser.ParseModelToJson(model);

            // Wysłanie informacji o nowej grupie do wszystkich wykrytych peerów.
            // Każdy peer po odebraniu CREATE_GROUP utworzy lokalną kopię grupy.
            foreach (var peer in peerManager.GetPeers())
            {
                await tcp.SendMessage(json, peer.IP, peer.Port);
            }

            // Informacja lokalna w oknie czatu.
            ChatBox.Items.Add($"Group created: {groupName}");

            // Odświeżenie listy grup w GUI.
            RefreshGroupList();
        }

        // Metoda odpowiedzialna za dołączenie użytkownika do istniejącej grupy.
        private async Task JoinGroup(string groupId)
        {
            // Pobranie grupy z lokalnego GroupManagera.
            // Jeśli grupa nie istnieje lokalnie, przerwij działanie funkcji.
            var group = groupManager.GetGroup(groupId);

            if (group == null)
                return;

            // Dodanie siebie lokalnie do members list.
            // Dzięki temu peer od razu wie,
            // że należy do tej grupy.
            groupManager.AddMember(groupId, _userName);

            // Utworzenie wiadomości GROUP_JOIN,
            // informującej hosta grupy o nowym użytkowniku.
            var model = new Model
            {
                Type = MessageType.GROUP_JOIN, // typ wiadomości sieciowej
                Name = _userName,              // użytkownik dołączający do grupy
                GroupId = groupId              // identyfikator grupy
            };

            // Konwersja modelu do JSON.
            string json = Parser.ParseModelToJson(model);

            // Wyszukanie hosta grupy na liście aktywnych peerów.
            foreach (var peer in peerManager.GetPeers())
            {
                // Jeśli znaleziono hosta grupy,
                // wyślij do niego wiadomość GROUP_JOIN.
                if (peer.Name == group.HostName)
                {
                    await tcp.SendMessage(json, peer.IP, peer.Port);

                    // Zakończ pętlę po wysłaniu wiadomości.
                    break;
                }
            }
        }

        // Metoda odpowiedzialna za wysyłanie wiadomości grupowej.
        private async Task SendGroupMessage(string groupId, string message)
        {
            // Pobranie grupy z lokalnego GroupManagera.
            // Jeśli grupa nie istnieje, przerwij działanie funkcji.
            var group = groupManager.GetGroup(groupId);

            if (group == null)
                return;

            // Utworzenie modelu wiadomości grupowej.
            // GROUP_MESSAGE zawiera:
            // - identyfikator grupy,
            // - nazwę nadawcy,
            // - treść wiadomości.
            var model = new Model
            {
                Type = MessageType.GROUP_MESSAGE, // typ wiadomości sieciowej
                Name = _userName,                 // nazwa nadawcy
                GroupId = groupId,                // identyfikator grupy
                payload = message                 // treść wiadomości
            };

            // Konwersja modelu do formatu JSON.
            string json = Parser.ParseModelToJson(model);

            // Iteracja po wszystkich członkach grupy.
            foreach (var member in group.Members)
            {
                // Pominięcie samego siebie.
                // Lokalna wiadomość została już wyświetlona w ChatBox.
                if (member == _userName)
                    continue;

                // Wyszukiwanie odpowiadającego peera
                // na liście aktywnych użytkowników.
                foreach (var peer in peerManager.GetPeers())
                {
                    // Jeśli znaleziono użytkownika należącego do grupy,
                    // wyślij do niego wiadomość TCP.
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
            // Utworzenie nowej grupy o nazwie "Test Group".
            // Aktualnie nazwa jest wpisana na stałe (hardcoded).
            // W przyszłości można dodać okno dialogowe lub TextBox
            // do wpisywania własnej nazwy grupy.
            await CreateGroup("Test Group");
        }

        // Metoda odpowiedzialna za odświeżenie listy grup w GUI.
        private void RefreshGroupList()
        {
            // Wyczyść aktualną zawartość listy grup.
            GroupList.Items.Clear();

            // Pobierz wszystkie grupy z lokalnego GroupManagera.
            foreach (var group in groupManager.GetGroups())
            {
                // Dodaj nazwę grupy do kontrolki ListBox.
                // Dzięki temu grupa pojawi się w interfejsie użytkownika.
                GroupList.Items.Add(group.GroupName);
            }
        }

        // Callback wywoływany po zmianie zaznaczenia grupy w GroupList.
        private void GroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Wyszukanie obiektu grupy odpowiadającego zaznaczonej nazwie w ListBox.
            var selected = groupManager
                .GetGroups()
                .FirstOrDefault(g => g.GroupName == GroupList.SelectedItem?.ToString());

            // Jeśli grupa została znaleziona,
            // ustaw ją jako aktualnie aktywną grupę czatu.
            if (selected != null)
            {
                // currentGroupId określa,
                // do której grupy będą wysyłane wiadomości.
                currentGroupId = selected.GroupId;
            }
        }

        // Callback wywoływany po podwójnym kliknięciu grupy w GroupList.
        private async void GroupList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Wyszukanie zaznaczonej grupy na podstawie nazwy wybranej w GUI.
            var selected = groupManager
                .GetGroups()
                .FirstOrDefault(g => g.GroupName == GroupList.SelectedItem?.ToString());

            // Jeśli grupa istnieje:
            if (selected != null)
            {
                // Wyślij wiadomość GROUP_JOIN do hosta grupy.
                // Powoduje to dołączenie użytkownika do members list.
                await JoinGroup(selected.GroupId);

                // Ustaw aktualnie wybraną grupę jako aktywną.
                // Wszystkie kolejne wiadomości będą wysyłane do tej grupy.
                currentGroupId = selected.GroupId;

                // Informacja lokalna w oknie czatu.
                ChatBox.Items.Add($"Joined group: {selected.GroupName}");
            }
        }
    }
}