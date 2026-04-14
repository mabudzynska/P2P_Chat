using P2P_Chat.JsonParser;
// Moja bibliotek z plikami netowrk
using P2P_Chat.Network;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
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
        // Serwic do obsługi UDP
        private UdpService udp;
        // Serwic do obsługi TCP
        private TcpService tcp;

        // Obiekt do: Wykrywanie peerów, wysyłanie wiadomości typu HELLO / GOODBYE
        private DiscoveryService discovery;
        // Menadżer zarządzający peerami
        private PeerManager peerManager;
        
        public MainWindow()
        {
            InitializeComponent();
            // Utworzenie clienta TCP na porcie 53241
            tcp = new TcpService(53241);
            // Utworzenie socketa UDP na porcie 50000
            udp = new UdpService(50000);
            // Konstruktor menadżera peerów
            peerManager = new PeerManager();
            // Inicjalizacja serwisu zarządzania statusem w sieci.
            discovery = new DiscoveryService(
                udp,        // handler do serwisu UDP
                "Marta",    // NAZWA UŻYTKOWNIKA - ustwić dopiero po jakimś okienku logowania / popupie
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
                    await Task.Delay(3000);
                }
            });
        }

        // Calback dla btn click
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // pobieranie wiadomości z pola
            string msg = MessageInput.Text;
            // wyślij tylko jeśli pole nie jest puste!
            if (!string.IsNullOrEmpty(msg))
            {
                // Konwertowanie wiadomości do formatu JSON
                var model = new Model
                {
                    Type = MessageType.MESSAGE,
                    Name = "Marta",
                    payload = msg
                };
                string json = Parser.ParseModelToJson(model);

                /* UWAGA! Na ten moment to działa troche jak broadcast (dla testu) - wysyła do każdego wykrytego peera w sieci
                 napisaną wiadomość - Prawdopodobnie wyśle też do samego siebie - nastąpi zapętlenie!!!!! Docelowo należy to zmienić
                , gdy zostanie wybrany użytkownik do którego piszemy, wysłać do konkretnego peera po kluczu. 
                
                 ! Klase Model możliwe że też trzeba zmienić żeby przechowywała Nazwę użytkownika. */
                foreach (var peer in peerManager.GetPeers())
                {
                    // wyślij na każdy dostępny peer
                    await tcp.SendMessage(json, peer.IP, peer.Port);
                }
                // wyświetl moją wiadomość
                ChatBox.Items.Add($"ME: {msg}");
                // wyczyść pole input
                MessageInput.Clear();
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