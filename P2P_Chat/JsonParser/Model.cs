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
        GOODBYE
    }
    /* Struktura Wiadomości - stosowany format: JSON */
    class Model
    {
        public string Name { get; set; } = "";
        public MessageType Type { get; set; } = MessageType.HELLO;
        public int Port { get; set; } = 0;

        public string payload { get; set; } = "";
    }
}
