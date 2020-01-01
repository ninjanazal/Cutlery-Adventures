using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Cutlery.Com
{
    // player data
    public class Player
    {
        //private vars
        private JsonSerializerSettings _jsonSettings;

        // player data vars
        public Guid Id { get; set; }                // unique player id
        public GameState GameState { get; set; }    // player state
        public string Name { get; set; }            // player name
        public CutleryColor PlayerColor { get; set; }      // player color
        public Position PlayerPos { get; set; }     // player position
        public int PlayerScore { get; private set; }    // player score (read only var)

        // connection data
        public List<Packet> PlayerPackets { get; set; } //packets sented/recieved 

        //TCP connection
        //client Tcp
        public TcpClient TcpClient { get; set; }

        // binary reader (read only var)
        public BinaryWriter PlayerWriter { get; set; }
        // binary writer (read only var)
        public BinaryReader PlayerReader { get; set; }

        // UDP connection
        public EndPoint ClientEndPoint { get; set; }    // client EndPoint


        // player const
        public Player()
        {
            // ignore the null field when serialize to jason
            _jsonSettings = new JsonSerializerSettings
            { NullValueHandling = NullValueHandling.Ignore };

            //inicialize Score
            PlayerScore = 0;
        }


        //publc funcs
        /// <summary>
        /// Function serialize data on opened connection Stream
        /// </summary>
        /// <param name="packet">Packet to send</param>
        public void SendPacket(Packet packet)
        {
            // creates a string with information from the packet, on a json format
            string jsonPacket = JsonConvert.SerializeObject(packet, _jsonSettings);
            // writes the string to the opend stream of connection
            PlayerWriter.Write(jsonPacket);
        }

        /// <summary>
        /// Function read data from the opened stream and return a packet
        /// </summary>
        /// <returns>Return packet read from stream</returns>
        public Packet ReadPacket()
        {
            // read string from the opend stream
            string jsonPacket = PlayerReader.ReadString();
            //convert the string to a packet through json deserialization
            return JsonConvert.DeserializeObject<Packet>(jsonPacket, _jsonSettings);
        }

        // regist score point
        public void AddPlayerScorePoint()
        {
            // add point to player score
            PlayerScore++;

        }

        //check if player sent data to server
        public bool DataAvailabe() { return TcpClient.GetStream().DataAvailable; }

        // Close connection with the player
        public void CloseConnection() { TcpClient.Close(); }


        // private funcs
        // set the player endPoint from TcpClient provided
        private void SetPlayerEndPoint() => ClientEndPoint = TcpClient.Client.RemoteEndPoint;

    }
}
