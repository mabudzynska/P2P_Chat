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
//do obslugi kart sieciowych
using System.Net.NetworkInformation;


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

        // Wysyłanie danych na broadcast do wszystkich kart sieciowych
        public async Task SendBroadcast(string message)
        {
            Console.WriteLine("Sending on broadcast");
            byte[] data = Encoding.UTF8.GetBytes(message);

            bool sent = false;

            // Przejście przez wszystkie karty sieciowe w komputerze
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Interesują nas tylko te włączone, pomijając Loopback (localhost)
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork) // Tylko IPv4
                        {
                            // Wylicz adres broadcast dla tej konkretnej sieci (np. 192.168.1.255)
                            IPAddress broadcastIp = GetBroadcastAddress(ip.Address, ip.IPv4Mask);
                            if (broadcastIp != null)
                            {
                                IPEndPoint ep = new IPEndPoint(broadcastIp, port);
                                try
                                {
                                    await udpClient.SendAsync(data, data.Length, ep);
                                    sent = true;
                                }
                                catch (Exception ex)
                                {
                                    // Ignorujemy błędy wysyłania na konkretnej karcie, żeby nie zablokować pętli
                                    Console.WriteLine($"Błąd wysyłania UDP na {broadcastIp}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }

            // Fallback: jeśli z jakiegoś powodu nie znaleziono kart, wyślij klasycznie
            if (!sent)
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, port);
                await udpClient.SendAsync(data, data.Length, ep);
            }
        }

        // Funkcja pomocnicza: Oblicza adres broadcast na podstawie IP i maski podsieci
        private IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            if (subnetMask == null) return null;

            byte[] ipAddressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAddressBytes.Length != subnetMaskBytes.Length)
                return null;

            byte[] broadcastAddress = new byte[ipAddressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                // Operacja bitowa: Adres IP OR (NOT Maska)
                broadcastAddress[i] = (byte)(ipAddressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }
    }

}
