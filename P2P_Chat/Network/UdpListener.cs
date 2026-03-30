using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Sockets;

namespace P2P_Chat.Network
{
    class UdpListener
    {
        // port bindowania, na który nasłuchujemy ramek UDP
        private int port;
        // klient UDP
        private UdpClient udpClient;

        // Konstruktor klasy Słuchacza UDP, inicjalizujący klienta UDP oraz definiujący port nasłuchiwania.
         public UdpListener(int port)
        {
            this.port = port;
            udpClient = new UdpClient(port);
        }
        // funkcja BLOKUJĄCA, która nasłuchuje dane z portu UDP. Argument wskazuje na port z którego będą pochodzić dane (filtracja)
        public void StartListening(int port)
        {
            Console.WriteLine("Nasłuchwianie na porcie " + this.port);
            
            
            while (true)
            {
                // zdalny punkt dostępu, z którego nadchodza ramki danyuch. Można w petli bo Garbage Collector zwolni pamięc po każdej iteracji
                IPEndPoint rep = new IPEndPoint(IPAddress.Any, port);

                // Odbierz ramke z broadcasta. BLOKUJĄCE!
                byte[] data = udpClient.Receive(ref rep);

                // Parsowanie danych z surowych bajtów do stringa
                string message = Encoding.UTF8.GetString(data);

                Console.WriteLine(message);
            }
        }
    }
}
