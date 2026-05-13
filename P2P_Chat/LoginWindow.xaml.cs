using System.Windows;

namespace P2P_Chat
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameInput.Text.Trim();

            if (!string.IsNullOrEmpty(username))
            {
                // tworzenie głównego okna i przekazanie mu wpisaną nazwę
                MainWindow mainChat = new MainWindow(username);
                mainChat.Show();

                this.Close();
            }
            else
            {
                MessageBox.Show("Proszę podać nazwę użytkownika!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}