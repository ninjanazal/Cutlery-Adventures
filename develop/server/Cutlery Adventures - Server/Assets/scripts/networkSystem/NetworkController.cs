using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using Cutlery.Com;
using System.Threading.Tasks;

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
    private List<Player> _connectedPlayers;     // array of players for the next match
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

        // start Connected Player array
        _connectedPlayers = new List<Player>();


        Console.Write("Staring Server...", Color.green);

        //server Loop
        Task.Run(() => ServerLoop());
    }
    // private server Functions
    private void ServerLoop()
    {
        // Console print
        Console.Write("-> Server Loop Started!", Color.gray);
        Console.Write("Waiting Requestes");

        while (true)
        {
            // listen for new players if is pending connection
            if (_tcpListener.Pending())
            {
                Console.Write("New Contact waiting...", Color.white);
                // start accepting tcpClient async
                // call AsyncAcceptClient method
                _tcpListener.BeginAcceptTcpClient(new AsyncCallback(AsyncAcceptClient),
                    _tcpListener);
            }

        }
    }

    // AsyncAcceptClient
    // Func for async tcp client first contact
    private void AsyncAcceptClient(IAsyncResult iasync)
    {
        //max match size is 2 players, for now we are accepting connections if
        // there is less then 2 players connected
        if (_connectedPlayers.Count < 2)
        {
            // to the main listener not stop listen, use the async listener to
            // terminate the lookup and retrieve the client
            TcpListener listener = (TcpListener)iasync.AsyncState;
            // determinate the tcpClient from the connection
            TcpClient client = listener.EndAcceptTcpClient(iasync);

            // confirm if the client is connected
            if (client.Connected)
            {
                //print the new state on console
                Console.Write("New connection onGoing!", Color.green);

                // register the connection data on server
                // Filling new player entrance
                Player player = new Player();
                player.Id = Guid.NewGuid();     // setting the new Player an Id
                player.TcpClient = client;      // saving the TcpClient
                player.GameState = GameState.Connecting;    // set player to connecting state

                _connectedPlayers.Add(player);  // add player to list

                //setting package to send to the client
                Packet packet = new Packet();


                // Server console write
                Console.Write("Registing player, waiting remain data", Color.yellow);
            }
        }
    }
}


// doing , line 101 setting up packet for request complementary information
// from the player

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
