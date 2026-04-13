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
        public Action<string, IPEndPoint>? OnMessageReceived;
        
        public UdpService(int port)
        {
            this.port = port;
            udpClient = new UdpClient(port);
            udpClient.EnableBroadcast = true;

            //udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        public async Task StartListening()
        {
            Console.WriteLine("Listening on " + this.port + " port");
            while (true)
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string message = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine(message);
                OnMessageReceived?.Invoke(message, result.RemoteEndPoint);
            }
        }

        public async Task SendBroadcast(string message)
        {
            Console.WriteLine("Sending on broadcast");
            byte[] data = Encoding.UTF8.GetBytes(message);
            IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, port);
            await udpClient.SendAsync(data, data.Length, ep);
        }
    }

}
