using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using Cutlery.Com;
using System.Threading.Tasks;
using System.IO;

public class NetworkController : MonoBehaviour
{
    // public Vars
    [Header("Insert server information")]
    public string serverIp = "127.0.0.1";       // server Ip (loopBacK)
    public int TcpPort = 7701, UdpPort = 7702;  // ports

    //server internal vars
    // server states
    private enum ServerState { ServerWaitingPlayers, ServerStartedMatch }
    private ServerState _serverState;

    //connection
    private List<Player> _connectedPlayers;     // array of players for the next match
    private TcpListener _tcpListener;       // listener for tcp Connection
    private IPAddress _ipAddress;           // IP adress

    private UdpClient _udpListener;         // listener for UDP Connection
    private IPEndPoint _remoteEndPoint;     // valid end points

    // async tasks handler
    private Queue<Action> _asyncActions;

    // Start Server Function
    public void StartServer()
    {
        // try to start the server
        try
        {
            // setting the ip as IPAddress
            _ipAddress = IPAddress.Parse(serverIp);

            // debug message for server starting
            Debug.Log("Staring Server...");
            Console.Write("Staring Server...", Color.green);

            // more information about the server
            Debug.Log($"Server Ip: {_ipAddress}, port: {TcpPort}");
            Console.Write($"Server Ip: {_ipAddress}, port: {TcpPort}");

            // defining tcpListener
            _tcpListener = new TcpListener(_ipAddress, TcpPort);
            _tcpListener.Start();   // start tcpListener

            // defining udpListener
            // defining from where can server get data
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, UdpPort);
            _udpListener = new UdpClient(_remoteEndPoint);

            // start Connected Player array
            _connectedPlayers = new List<Player>();

            // start list of asyncActions
            _asyncActions = new Queue<Action>();

            //server Loop 
            //async method, not using unity update cycle
            Debug.Log("Server started");
            Console.Write("-> Server Started!", Color.magenta);
            // set the server state to ServerWaitingPlayers
            _serverState = ServerState.ServerWaitingPlayers;
        }
        catch (Exception ex) { Console.Write($"Error ocurred: {ex}", Color.red); Debug.Log(ex); }

    }

    //server unity Update
    private void Update()
    {
        // after all logic, resolve the actions queue
        AsyncActionsClear();

        // internal loop for acepting players
        // listen for new players , looking for pending connections
        if (_tcpListener.Pending())
        {
            // Debug to log
            Debug.Log("New pending connection");
            // debug to console
            Console.Write("New Pending Connection found", Color.green);

            //start a async accepting the client
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptingConnectionCallback),
                _tcpListener);

            // debug to log
            Debug.Log("BeginAcceptStarted");
        }

        // if server is waiting players
        if (_serverState == ServerState.ServerWaitingPlayers)
        {
            foreach (Player conPlayer in _connectedPlayers)
            {
                switch (conPlayer.GameState)
                {
                    case GameState.Disconnected:
                        // handle if a player is in disconneted state
                        // fornow if this happens the game ends and the connections are droped
                        break;
                    case GameState.Connecting:
                        // call handler for connecting state
                        Connecting(conPlayer);
                        break;
                    case GameState.Sync:
                        // sending the last layer info to the other player
                        break;
                    case GameState.WaitPlayer:
                        // just waits for confirmation of data recieved from the client
                        break;
                    case GameState.WaitingStart:
                        // waiting server start the game
                        break;
                }
            }
        }
    }

    #region AsyncMethods and Resolvers
    // async callback for accepting new players
    private void AcceptingConnectionCallback(IAsyncResult asyncResult)
    {
        // extract the listener from the result of the async beging accept
        TcpListener listener = (TcpListener)asyncResult.AsyncState;
        // end accepting tcpClient, extracting the tcpclient
        TcpClient client = listener.EndAcceptTcpClient(asyncResult);

        // if the client is connected
        if (client.Connected)
        {
            // queue actions
            _asyncActions.Enqueue(() =>
            {
                // debug to log
                Debug.Log("New connecting accepted");
                // write to server console
                Console.Write("New Connecting accepted", Color.green);
            });

            // regist the connection data on server memory
            Player acceptedPlayer = new Player();
            // inicialize the packet list for the player
            acceptedPlayer.PlayerPackets = new List<Packet>();
            // set the player unique id
            acceptedPlayer.Id = Guid.NewGuid();
            // save the tcpClient of the player
            acceptedPlayer.TcpClient = client;
            // set the player binaryread and binarywriter
            acceptedPlayer.PlayerReader = new BinaryReader(client.GetStream());
            acceptedPlayer.PlayerWriter = new BinaryWriter(client.GetStream());

            // queue actions
            _asyncActions.Enqueue(() =>
            {
                //debug to log
                Debug.Log("All data from connection saved");
                Console.Write("All data from connection saved", Color.green);
            });

            // defining the packet to send
            Packet responsePacket = new Packet();

            // checking if the match has started
            if (_serverState == ServerState.ServerStartedMatch)
            {
                //queue actions
                _asyncActions.Enqueue(() =>
                {
                    // display in console that a player has been refused
                    Debug.Log("Player refuse due to full server");
                    Console.Write("Player refuse due to full server", Color.red);
                });

                // the player wont be added to the connected players
                // will recieve a packet telling that he wout be accepted
                // set the packet type of connectionRefused
                responsePacket.PacketType = PacketType.ConnectionRefused;

                // adding some description to the packet
                responsePacket.Desc = "Server full and match has started," +
                    " connection droped";

                // send the packet to the client
                acceptedPlayer.SendPacket(responsePacket);
                // close connection with this client
                client.Close();
            }
            else
            {
                // queue actions
                _asyncActions.Enqueue(() =>
                {
                    // debug that client has been accepted and registed
                    Debug.Log($"Client accepted with the id: {acceptedPlayer.Id}");
                    Console.Write($"Client accepted with the id: {acceptedPlayer.Id}", Color.green);
                });

                //setting the player state on server
                acceptedPlayer.GameState = GameState.Connecting;

                // debug to server console that the client has been accepted
                _asyncActions.Enqueue(() => Console.Write("Client added to connected Players"));

                // setting the package to send
                responsePacket.PacketType = PacketType.RequestPlayerInfo;
                // adding the player generated id
                responsePacket.PlayerGUID = acceptedPlayer.Id;

                // saving the packet to the packet list on player
                acceptedPlayer.PlayerPackets.Add(responsePacket);

                //queue actio to do
                // add the accepted player to the connectedPlayers list
                _asyncActions.Enqueue(() => { AddPlayer(acceptedPlayer); });

                // sending the response packet
                acceptedPlayer.SendPacket(responsePacket);

                // enque actions
                _asyncActions.Enqueue(() =>
                {
                    // debug to log
                    Debug.Log("Response packet sented, waiting more data");
                    Console.Write("Response packet sented, waiting more data");
                });
            }

        }
        else
        {
            //enqueue action
            _asyncActions.Enqueue(() =>
            {
                // if player is not connected
                // debug that player was refused
                Debug.Log("Connection refuse");
                Console.Write("Connection refused", Color.blue);
            });
        }
    }

    // execute asyncMethods
    private void AsyncActionsClear()
    {
        // if exists any queued action queued
        Debug.Log("AsyncActions queued: " + _asyncActions.Count);
        while (_asyncActions.Count > 0)
        {
            // execute the oldest action
            _asyncActions.Dequeue()();
        }
    }

    // method that queue players to add
    private void AddPlayer(Player connectedPlayer)
    { _connectedPlayers.Add(connectedPlayer); }
    #endregion

    #region PlayerStateHandlers

    //handle if the player is Connecting
    private void Connecting(Player _player)
    {
        // check if the player has new data Available
        if (_player.DataAvailabe())
        {
            // Debug that player has data available
            Debug.Log($"{_player.Id} has data available");
            Console.Write($"{_player.Id} has data available", Color.cyan);

            // read the new data
            Packet _recieved = _player.ReadPacket();

            // save the recieved packet
            _player.PlayerPackets.Add(_recieved);

            // if is any data available, check if the package 
            // is the requested info
            if (_recieved.PacketType == PacketType.PlayerInfo)
            {
                // debug that server recieved packet
                Console.Write("Server recieved packet with requested info");

                // if soo, complete player information
                _player.Name = _recieved.PlayerName;

                // if the other player is ready to play
                // change this player to sync with
                // else set the player to wait other player
                if (_connectedPlayers.Count == 2)
                    _player.GameState = GameState.Sync;
                else
                    _player.GameState = GameState.WaitPlayer;

                // send the confirmation of the successfull regist
                // create a new packet
                Packet _confirmPacket = new Packet();
                // set packet type of registationOk
                _confirmPacket.PacketType = PacketType.RegistationOK;
                // add packet description
                _confirmPacket.Desc = "Registration successful, wait other player";

                // print the new information of the player
                // and the state the player is at

                Console.Write($"{_player.Name} registed," +
                $" PlayerState in serve{_player.GameState.ToString()}", Color.green);

                // add packet to playerPackets
                _player.PlayerPackets.Add(_confirmPacket);
                // send packet
                _player.SendPacket(_confirmPacket);
            }
        }
    }

    //handler for the player syncState
    private void SyncNewPlayer(Player _player)
    {

    }

    #endregion
}






//// handle if the player is Syncing with other
//private void Syncing(Player _player)
//{
//    // setting up the packet for syncing this player with the other one
//    // printing that server start to notiffy the other player
//    _actions.Add(() => Console.Write($"Syncing {_player.Name} to the other player",
//        Color.yellow));

//    // preparing the packet to inform the connected player
//    Packet _syncPacket = new Packet();
//    // set the packet type as newPlayer
//    // sending the player Id and name
//    _syncPacket.PacketType = PacketType.NewPlayer;
//    _syncPacket.PlayerGUID = _player.Id;
//    _syncPacket.PlayerName = _player.Name;

//    // sending to player who is waiting for the new player
//    _connectedPlayers.ForEach(_p =>
//    {
//        // diferent from the actual player and waiting for the new player
//        if (_p.Id != _player.Id && _p.GameState == GameState.WaitPlayer)
//        {
//            // print that server is notifying the existing player
//            _actions.Add(() => Console.Write($"Notifying {_p.Name} " +
//                $"that a new player has connected", Color.green));

//            // send to player the packet to notify the new player
//            // save the packet sent
//            _p.PlayerPackets.Add(_syncPacket);
//            // send the packet to the player waiting
//            _p.SendPacket(_syncPacket);

//            // since all the players are connected we can change the state of
//            // this to waiting game start
//            _p.GameState = GameState.WaitingStart;

//            // printing that the existing player has been notifyed and now is read
//            _actions.Add(() => Console.Write($"Player {_p.Name}, has been notifyed," +
//                $"and now is read to player", Color.red));

//            // setting the packet to notify the new player the existing players
//            // creating the packet to send
//            Packet _notifyExisting = new Packet();
//            // add info from the existing connection
//            // set the packet to new player type
//            _notifyExisting.PacketType = PacketType.NewPlayer;
//            // adding the id and the name of the existing player
//            _notifyExisting.PlayerGUID = _p.Id;
//            _notifyExisting.PlayerName = _p.Name;

//            // add packet to packetList of the player
//            _player.PlayerPackets.Add(_notifyExisting);
//            // send the packet to the player
//            _player.SendPacket(_notifyExisting);

//            // priting that the new player was notifyed about the connected player
//            _actions.Add(() => Console.Write($"Player {_player.Name} know about" +
//                $"{_p.Name}", Color.red));
//        }
//    });
//    // since the new player as known of the existing players and they know about the new
//    // one, we can change the state of this player to waitingStart
//    _player.GameState = GameState.WaitingStart;

//    //printing that the new player is ready to play
//    _actions.Add(() => Console.Write($"The player {_player.Name} is now ready to player",
//        Color.green));
//}

////handle if the player is waiting for start
//private void WaitForStart(Player _player)
//{
//    //TODO
//}

////handle if player is countingDown
//private void CountDown(Player _player)
//{
//    // TODO
//}

////handle if the player is in the game
//private void GameStarted(Player _player)
//{
//    // TODO
//}

//// handle if the player is in endGame state
//private void EndGame(Player _player)
//{
//    //TODO
//}
//#endregion




//TO DO 


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
