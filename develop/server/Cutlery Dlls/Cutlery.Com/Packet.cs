using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutlery.Com
{
    public class Packet
    {
        public PacketType PacketType { get; set; }       // type of packet
        public string Desc { get; set; }   // packet description
        // stamp for package sent
        // read only var
        public long GetSendStamp { get; private set; }

        #region SendedVars
        // packet can send information with all this components
        // packet type indicates what is sented
        // package vars
        public Guid PlayerGUID { get; set; }        // player GUID
        public string PlayerName { get; set; }      // player Name
        public CutleryColor ObjColor { get; set; }   // colors colors
        public GameState PlayerState { get; set; }  // player state
        public Position PlayerPosition { get; set; }    // position of the player
        public int PlayerScore { get; set; }        //player score
        public Position ObjPosition { get; set; }        // position for obj in game

        #endregion

        #region Funcs
        //packet internal funcs
        // set send stamp
        public void SetSendStamp() { GetSendStamp = DateTime.Now.Ticks; }

        #endregion
    }

}
