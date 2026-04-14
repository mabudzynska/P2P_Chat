using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace P2P_Chat.Network
{
    /* Klasa działająca na stosie TCP, odpowiadająca za komunikację P2P, wysyła dane w formacie Json na konkretny port, konkretnego adresu
     * ip. Odbiera Dane na tym samym porcie od pozostałych użytkowników w sieci. */
    internal class TcpService
    {
        // Handler do nasłuchiwania i wysyłania wiadomości TCP
        private TcpListener listener;
        // Port nadawczo-odbiorczy
        private int port;

        // Handler otrzymania wiadomości TCP
        public Action<string, IPEndPoint>? OnMessageReceived;

        // Inicjalizacja
        public TcpService(int port)
        {
            this.port = port;
            listener = new TcpListener(IPAddress.Any, port);
        }

        // Task asynchroniczny do nasłuchwiania wiadomości w sieci od pozostałych peerów.
        public async Task StartListening()
        {
            // Enable listening for incoming TCP connections
            listener.Start();
            // logging
            Console.WriteLine($"TCP listening on {port}");

            while (true)
            {
                // czekaj na wiadomość
                TcpClient client = await listener.AcceptTcpClientAsync();
                // obsłuż ją w tle
                _ = HandleClient(client);
            }
        }
        // Handler do obsługi wiadomości TCP
        private async Task HandleClient(TcpClient client)
        {
            // alokacja bufora (max 1kb danych)
            var stream = client.GetStream();
            byte [] buffer = new byte[1024];

            // kopiowanie danych do bufora i zapis odczytanych bajtów
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            IPEndPoint remote = (IPEndPoint)client.Client.RemoteEndPoint;

            OnMessageReceived?.Invoke(message, remote);
            client.Close();
        }

        public async Task SendMessage(string message, string ip, int port)
        {
            try
            {
                TcpClient client = new TcpClient();

                await client.ConnectAsync(IPAddress.Parse(ip), port);
                var stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending TCP message: {ex.Message}");
            }
        }
    }
}
