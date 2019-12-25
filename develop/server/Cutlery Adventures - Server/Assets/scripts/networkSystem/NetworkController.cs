using Packet;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;


public class NetworkController : MonoBehaviour
{
    // public Vars
    [Header("Insert server information")]
    public string serverIp = "127.0.0.1";       // server Ip (loopBacK)
    public int TcpPort = 7701, UdpPort = 7702;  // ports

    //server internal vars
    // server states
    private enum ServerState { AcceptingConnections, ServerFull }

    //connection
    private List<Player> _connectedPlayers; // list of players for the next match
    private TcpListener _tcpListener;       // listener for tcp Connection
    private IPAddress _ipAddress;           // IP adress

    private UdpClient _udpListener;         // listener for UDP Connection
    private IPEndPoint _remoteEndPoint;     // valid end points


    // Start Server Function
    public void StartServer()
    {
        // setting the ip as IPAddress
        _ipAddress = IPAddress.Parse(serverIp);


        // defining tcpListener
        _tcpListener = new TcpListener(_ipAddress, TcpPort);
        _tcpListener.Start();   // start tcpListener

        // defining udpListener
        // defining from where can server get data
        _remoteEndPoint = new IPEndPoint(IPAddress.Any, UdpPort);
        _udpListener = new UdpClient(_remoteEndPoint);

        Console.Write("Staring Server...", Color.green);
    }
}

/*  TODO
 *  - (maybe) wait line to play
 *  - max 2 players per match
 *  - Create server loop
 *  - seting comunication stream
 *  - Accept clients
 *  - (Maybe) notify the new connections
 *  - Setting up match
 *  - match loop
 *  - win/lose statement
 *  - reset server status
 *  - server ready for new match
 *  - refuse connections if game starter
 *  - accept disconnected player if match on-going
 */
