using System;
using System.Collections.Generic;
using System.Text;

namespace P2P_Chat.JsonParser
{
    enum MessageType
    {
        HELLO,
        MESSAGE,
        GOODBYE
    }
    class Model
    {
        public string Name { get; set; } = "";
        public MessageType Type { get; set; } = MessageType.HELLO;
        public int Port { get; set; } = 0;

        public string payload { get; set; } = "";
    }
}
