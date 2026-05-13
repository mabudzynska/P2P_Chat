using System;
using System.Collections.Generic;
using System.Text;

namespace P2P_Chat.Network
{
    internal class Group
    {
        public string GroupId { get; set; } = "";

        public string GroupName { get; set; } = "";

        public string HostName { get; set; } = "";

        public List<string> Members { get; set; } = new();
    }
}
