using P2P_Chat.JsonParser;
using System;
using System.Collections.Generic;
// System.net for IPAddress and IPEndPoint
using System.Net;
// Socket for UDP communication
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Interop;


namespace P2P_Chat.Network
{
    class UdpService
    {
        private UdpClient udpClient;
        private int port;
        // Callback na przychodzacą wiadomość
        public Action<string, IPEndPoint>? OnMessageReceived;
        
        // Konfiguracja UDP
        public UdpService(int port)
        {
            this.port = port;
            udpClient = new UdpClient(port);
            // Włączenie trybu broadcast
            udpClient.EnableBroadcast = true;

            //udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        // Nasłuchiwanie na zadanym porcie UDP.
        public async Task StartListening()
        {
            Console.WriteLine("Listening on " + this.port + " port");
            while (true)
            {
                // Czekaj aż przyjdzie ramka
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                // dekodowanie zgodnie z UTF 8
                string message = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine(message);
                // Wywołaj Callback od: przyszła wiadomość
                OnMessageReceived?.Invoke(message, result.RemoteEndPoint);
            }
        }

        // Wysyłanie danych na broadcast
        public async Task SendBroadcast(string message)
        {
            Console.WriteLine("Sending on broadcast");
            // Kodowanie stringa do bajtów zgodnie z UTF8 
            byte[] data = Encoding.UTF8.GetBytes(message);
            // Stworzenie Endpointu
            IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, port);
            // Ślij dane
            await udpClient.SendAsync(data, data.Length, ep);
        }
    }

}
