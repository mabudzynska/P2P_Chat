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
        private UdpService udp;
        private DiscoveryService discovery;
        private PeerManager peerManager;
        private void OnUdpMessage(string msg, IPEndPoint endpoint)
        {
            Model? model = Parser.ParseJsonToModel(msg);

            if (model == null)
                return;

            if (model.Type == MessageType.HELLO)
            {
                discovery.HandleHello(model, endpoint);
            }
            else if (model.Type == MessageType.GOODBYE)
            {
                discovery.HandleBye(model, endpoint);
            }
        }
        public MainWindow()
        {
            InitializeComponent();

            udp = new UdpService(50000);
            peerManager = new PeerManager();
            discovery = new DiscoveryService(
                udp,
                "Marta",
                53241,
                peerManager
            );

            
            udp.OnMessageReceived += OnUdpMessage;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartNetworking();
        }

        private void StartNetworking()
        {
            // 📥 Listener (działa w tle)
            _ = Task.Run(() => udp.StartListening());

            // 📤 Broadcast loop (też w tle)
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await discovery.SendHello();
                    peerManager.ValidatePeers();
                    await Task.Delay(3000);
                }
            });
        }
    }
}