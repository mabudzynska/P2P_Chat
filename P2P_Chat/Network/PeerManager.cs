using System;
using System.Collections.Generic;
using System.Text;

namespace P2P_Chat.Network
{
    /* Klasa odpowiedzialna za zarządzanie zdalnymi peerami. */
    internal class PeerManager
    {
        // Timeout po którym przkazana będzie informacja do GUI o braku aktywności peera - Timeout i disconnect.
        private TimeSpan timeout;
        // słownik z peerami (dostęp po kluczu unikatowym ip:port)
        private readonly Dictionary<string, Peer> peers = new();

        /* Konstruktora menadżera peerów, ustawienie timeoutu peera na 15 s*/
        public PeerManager() { timeout = TimeSpan.FromSeconds(15); }
        // Unikalny klucz dla każdego peer, Adres IP oraz port.
        private string GetKey(string ip, int port) => $"{ip}:{port}";


        /*Dodanie peera - Obsługa przychodzącej wiadomości HELLO */
        public void addPeer(string name, string ip, int port)
        {
            // pobierz klucz
            string key = GetKey(ip, port);
            
            if (!peers.ContainsKey(key))
            {
                // Klucz nie został wykryty w bazie. Dodaj go i zaktualizuj czas PINGu
                peers[key] = new Peer { Name = name, IP = ip, Port = port, LastSeen = DateTime.Now };
                Console.WriteLine("Peer added: " + name + " (" + ip + ":" + port + ")");
            }
            else
            {
                // Jeżeli ten klucz już znajduję się w naszej bazie, zaktualizuj czas PINGU - oddalenie timeoutu
                peers[key].LastSeen = DateTime.Now;
            }
        }
        /* Usunięcie peera - Obsługa przychodzącej wiadomości GOODBYE lub timeout */
        public void removePeer(string ip, int port)
        {
            // Weź klucz
            string key = GetKey(ip, port);
            if (peers.ContainsKey(key))
            {
                // Jeżeli klucz jest w słowniku, usuń go
                peers.Remove(key);
                Console.WriteLine("Peer removed: " + peers[key].Name + " (" + ip + ":" + port + ")");
          
            }
            else
            {
                // Jeżeli klucza nie znaleziono, daj info do terminala.
                Console.WriteLine("Peer not found: " + ip + ":" + port);
            }
        }

        public List<Peer> GetPeers()
        {
            // Listowanie wykrytych peerów.
            return peers.Values.ToList();
        }

        /* Walidacja timeoutów dla każdego peera */
        public void ValidatePeers()
        {
            // Pobierz aktualny timestamp
            var now = DateTime.Now;
            // Lista peerów po kluczach do usunięcia
            var toRemove = new List<string>(); // lub typ klucza

            foreach (var peer in peers)
            {
                // Sprawdź czy nie ma timeoutu w każdym peerze.
                if (now - peer.Value.LastSeen > timeout)
                {
                    Console.WriteLine("Peer timed out: " + peer.Value.Name + " (" + peer.Value.IP + ":" + peer.Value.Port + ")");
                    toRemove.Add(peer.Key);
                }
            }

            // Usuń każdy timeoutowy peer
            foreach (var key in toRemove)
            {
                peers.Remove(key);
            }
        }
    }
}
