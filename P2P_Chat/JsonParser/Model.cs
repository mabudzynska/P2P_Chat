using System;
using System.Collections.Generic;
using System.Text;

namespace P2P_Chat.JsonParser
{
    /* Rodzaje wysyłanej wiadomości 
        - HELLO: Powitanie w sieci,
        - MESSAGE: Wiadomość,
        - GOODBYE: Pożegnanie,
    */
    enum MessageType
    {
        HELLO,
        MESSAGE,
        GOODBYE,

        CREATE_GROUP,
        GROUP_MESSAGE,
        GROUP_INVITE,
        GROUP_JOIN,
        GROUP_LEAVE
    }
    /* Struktura Wiadomości - stosowany format: JSON */
    class Model
    {
        public string Name { get; set; } = "";
        public MessageType Type { get; set; } = MessageType.HELLO;
        public int Port { get; set; } = 0;

        public string payload { get; set; } = "";

        public string GroupId { get; set; } = "";

        public string GroupName { get; set; } = "";

        public List<string> Members { get; set; } = new();
    }
}
