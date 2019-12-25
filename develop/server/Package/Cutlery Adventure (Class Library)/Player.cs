using System;
using System.Net.Sockets;
using System.Net;

namespace Packet
{
    // player data
    public class Player
    {
        // player data vars
        public Guid Id { get; set; }        // unique player id
        public string Name { get; set; }    // player name
        

        // connection data
        public TcpClient TcpClient
        {
            get => TcpClient;
            set
            {
                TcpClient = value;
                SetPlayerEndPoint();
            }
        }       //client Tcp

        public EndPoint ClientEndPoint { get; set; }    // client EndPoint


        // private funcs
        // set the player endPoint from TcpClient provided
        private void SetPlayerEndPoint() => ClientEndPoint = TcpClient.Client.RemoteEndPoint;

    }
}
