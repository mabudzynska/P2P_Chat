using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// Moja bibliotek z plikami netowrk
using P2P_Chat.Network;
using System.Threading;
namespace P2P_Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UdpListener listener = new UdpListener(54321);
            Thread thread_UdpListener = new Thread(() => listener.StartListening(12345));


            UdpBroadcaster broadcaster = new UdpBroadcaster(12345);

            while (true)
            {
                string message = "DUPA 123";
                broadcaster.sendBroadcast(message, 54321);
                System.Threading.Thread.Sleep(1000);
            }
            
        }
    }
}