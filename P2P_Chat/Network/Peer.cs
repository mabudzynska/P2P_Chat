using System;
using System.Collections.Generic;
using System.Text;

namespace P2P_Chat.Network
{
    /* Klasa przechowująca informacje o wykrytym peerze w sieci. */
    internal class Peer
    {
        public string Name { get; set; } = "";
        public string IP { get; set; } = "";
        public int Port { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
    }
}
