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
            if (string.IsNullOrEmpty(msg)) return;

            // Najpierw dodaj do siebie, żeby widzieć, że przycisk działa
            ChatBox.Items.Add($"ME: {msg}");
            MessageInput.Clear();

            var peers = peerManager.GetPeers();
            // Debug: sprawdź ilu peerów widzi Twój program
            Console.WriteLine($"Próba wysyłki do {peers.Count} osób");

            foreach (var peer in peers)
            {
                try
                {
                    await tcp.SendMessage(Parser.ParseModelToJson(new Model
                    {
                        Type = MessageType.MESSAGE,
                        Name = _userName,
                        payload = msg
                    }), peer.IP, peer.Port);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Błąd wysyłki do " + peer.IP + ": " + ex.Message);
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
        }
    }
}