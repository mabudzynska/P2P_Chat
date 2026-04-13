using System;
using System.Collections.Generic;
using System.Text;

namespace P2P_Chat.Network
{
    internal class PeerManager
    {
        private TimeSpan timeout;
        private readonly Dictionary<string, Peer> peers = new();
        public PeerManager() { timeout = TimeSpan.FromSeconds(15); }
        // Unikalny klucz dla każdego peer, Adres IP oraz port.
        private string GetKey(string ip, int port) => $"{ip}:{port}";

        public void addPeer(string name, string ip, int port)
        {
            string key = GetKey(ip, port);
            if (peers.ContainsKey(key))
            {
                peers[key] = new Peer { Name = name, IP = ip, Port = port, LastSeen = DateTime.Now };
                Console.WriteLine("Peer added: " + name + " (" + ip + ":" + port + ")");
            }
            else
            {
                peers[key].LastSeen = DateTime.Now;
            }
        }

        public void removePeer(string ip, int port)
        {
            string key = GetKey(ip, port);
            if (peers.ContainsKey(key))
            {
                peers.Remove(key);
                Console.WriteLine("Peer removed: " + peers[key].Name + " (" + ip + ":" + port + ")");
          
            }
            else
            {
                Console.WriteLine("Peer not found: " + ip + ":" + port);
            }
        }

        public List<Peer> GetPeers()
        {
            return peers.Values.ToList();
        }

        public void ValidatePeers()
        {
            var now = DateTime.Now;
            var toRemove = new List<string>(); // lub typ klucza

            foreach (var peer in peers)
            {
                if (now - peer.Value.LastSeen > timeout)
                {
                    Console.WriteLine("Peer timed out: " + peer.Value.Name + " (" + peer.Value.IP + ":" + peer.Value.Port + ")");
                    toRemove.Add(peer.Key);
                }
            }

            foreach (var key in toRemove)
            {
                peers.Remove(key);
            }
        }
    }
}
