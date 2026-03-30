using System;
using System.Collections.Generic;
using System.Text;


// System.net for IPAddress and IPEndPoint
using System.Net;
// Socket for UDP communication
using System.Net.Sockets;


namespace P2P_Chat.Network
{
    class UdpBroadcaster
    {
        private UdpClient udpClient;
        int port;
        public UdpBroadcaster(int port)
        {
            this.port = port;
            udpClient = new UdpClient(port);
            udpClient.EnableBroadcast = true;
        }

        public void sendBroadcast(string message, int port)
        {
            // ep ma informacje o Adresie docelowym oraz porcie docelowym
            IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, 54321);

            // zamiana stringa na tablice bajtów (UDP wysyła ramkę bajtów). Dekoduje zgodznie z UTF8
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Wyślij ramkę danych po UDP do wszystkich urządzeń w sieci.
            udpClient.Send(data, data.Length, ep);


        }
    }
}
