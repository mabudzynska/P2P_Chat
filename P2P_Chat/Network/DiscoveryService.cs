using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

using P2P_Chat.JsonParser;


namespace P2P_Chat.Network
{
    /* Serwis Do obsługi komunikacji UDP. Wyższa warstwa niż obsługa UDP. Służy do wysyłania / odbierania wiadomości po UDP */
    class DiscoveryService
    {
        // Obiekt do wysyłania / odbierania danych po UDP.
        private readonly UdpService udpService;
        // Nazwa Zalogowanej Osoby
        private readonly string Name;
        // Port komunikacyjny TCP
        private readonly int TcpPort;
        // Menadżer peerów
        private readonly PeerManager peerManager;

        // Przy tworzeniu obiektu, ustawienie na stałe zbindowanego wcześniej portu TCP, nazwy użytkownika oraz obiektu do obsługi UDP
        public DiscoveryService(UdpService udpService, string Name, int TcpPort, PeerManager peerManager)
        {
            this.udpService = udpService;
            this.Name = Name;
            this.TcpPort = TcpPort;
            this.peerManager = peerManager;
        }
        /* Wysyłanie na broadcast powitania */
        public async Task SendHello()
        {
            // Obiekt z danymi do wysłania
            var model = new Model
            {
                Name = Name,
                Type = MessageType.HELLO,
                Port = TcpPort,
            };
            // Utworzenie JSON Stringa
            string jsonMessage = Parser.ParseModelToJson(model);
            // Wyślij po UDP
            await udpService.SendBroadcast(jsonMessage);
        }

        public async Task SendBye()
        {
            // Obiekt z danymi do wysłania
            var model = new Model
            {
                Name = Name,
                Type = MessageType.GOODBYE,
                Port = TcpPort,
            };
            // Utworzenie JSON Stringa
            string jsonMessage = Parser.ParseModelToJson(model);
            // Wyślij po UDP
            await udpService.SendBroadcast(jsonMessage);
        }

        public void HandleHello(Model model, IPEndPoint remote)
        {
            // Zobacz z jakiego adresu UDP przyszła wiadomość
            string ip = remote.Address.ToString();
            // Przekaż dane do menadżera peerów (Nazwa, adres IP (uniq) oraz port z którego to przyszło (TCP))
            peerManager.addPeer(model.Name, ip, model.Port);
        }

        public void HandleBye(Model model, IPEndPoint remote)
        {
            // Zobacz z jakiego adresu UDP przyszła wiadomość
            string ip = remote.Address.ToString();
            // Przekaż dane do menadżera peerów (Nazwa, adres IP (uniq) oraz port z którego to przyszło (TCP))
            peerManager.removePeer(ip, model.Port);
        }
    }
}
