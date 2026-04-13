using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

using P2P_Chat.JsonParser;


namespace P2P_Chat.Network
{
    class DiscoveryService
    {
        private readonly UdpService udpService;
        private readonly string Name;
        private readonly int TcpPort;
        private readonly PeerManager peerManager;

        public DiscoveryService(UdpService udpService, string Name, int TcpPort, PeerManager peerManager)
        {
            this.udpService = udpService;
            this.Name = Name;
            this.TcpPort = TcpPort;
            this.peerManager = peerManager;
        }

        public async Task SendHello()
        {
            var model = new Model
            {
                Name = Name,
                Type = MessageType.HELLO,
                Port = TcpPort,
            };

            string jsonMessage = Parser.ParseModelToJson(model);
            await udpService.SendBroadcast(jsonMessage);
        }

        public async Task SendBye()
        {
            var model = new Model
            {
                Name = Name,
                Type = MessageType.GOODBYE,
                Port = TcpPort,
            };

            string jsonMessage = Parser.ParseModelToJson(model);
            await udpService.SendBroadcast(jsonMessage);
        }

        public void HandleHello(Model model, IPEndPoint remote)
        {
            string ip = remote.Address.ToString();
            peerManager.addPeer(model.Name, ip, model.Port);
        }

        public void HandleBye(Model model, IPEndPoint remote)
        {
            string ip = remote.Address.ToString();
            peerManager.removePeer(ip, model.Port);
        }
    }
}
