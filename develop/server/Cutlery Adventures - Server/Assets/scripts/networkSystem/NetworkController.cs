using System.Collections.Generic;
using System.Collections;
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
    private ServerState _serverState;
    // async callback execution
    // since unity only allow methods be called from the main thread
    // async will queue orders to do, and in the main thread they will be executed
    private List<Action> _actions;

    //connection
    private List<Player> _connectedPlayers;     // array of players for the next match
    private TcpListener _tcpListener;       // listener for tcp Connection
    private IPAddress _ipAddress;           // IP adress

    private UdpClient _udpListener;         // listener for UDP Connection
    private IPEndPoint _remoteEndPoint;     // valid end points


    // Start Server Function
    public void StartServer()
    {
        // try to start the server
        try
        {
            // setting the ip as IPAddress
            _ipAddress = IPAddress.Parse(serverIp);

            // debug message for server starting
            Console.Write("Staring Server...", Color.green);

            // more information about the server
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

            //server Loop 
            //async method, not using unity update cycle

            Console.Write("GOING FULL ASYNC SERVER", Color.magenta);
            ServerLoopAsync();
        }
        catch (Exception ex) { Console.Write($"Error ocurred: {ex}", Color.red); }

    }

    //server unity Update
    private void Update()
    {
        // if there is actions queued
        if (_actions.Count > 0)
            StartCoroutine(DoQueuedActions());
    }

    // private server Functions
    // server loop async
    private async void ServerLoopAsync()
    {
        // Console print
        // displaing server start 
        Console.Write("-> Server Loop Started!", Color.gray);

        //start the list of actions to do
        _actions = new List<Action>();

        // async await, server loop
        // this func will run on a new thread
        await Task.Run(InternalLoopAsync);
    }

    //intern server Loop
    // in async func on unity , u cant call methods from other threads, soo need to
    // queue actions from the other threads to run on main
    private void InternalLoopAsync()
    {
        // for catching exeptions
        try
        {
            // queue call on main thread
            _actions.Add(() => Console.Write("Waiting Requestes"));

            while (true)
            {
                // listen for new players if is pending connection
                if (_tcpListener.Pending())
                {
                    // queue executions from this async task to run on main thread
                    _actions.Add(()
                        => Console.Write("New Contact waiting...", Color.white));

                    // start accepting tcpClient async
                    // call AsyncAcceptClient method
                    _tcpListener.BeginAcceptTcpClient(new AsyncCallback(AsyncAcceptClient),
                        _tcpListener);
                }

                foreach (Player _conPlayer in _connectedPlayers)
                {
                    switch (_conPlayer.GameState)
                    {
                        // when player is disconnected
                        case GameState.Disconnected:
                            Disconnected(_conPlayer);
                            break;
                        // when player is connecting to the server
                        case GameState.Connecting:
                            Connecting(_conPlayer);
                            break;
                        //when player is connected
                        case GameState.Connected:
                            Connected(_conPlayer);
                            break;
                        //when player is syncing
                        case GameState.Sync:
                            Syncing(_conPlayer);
                            break;
                        //when player is waiting for start
                        case GameState.WaitingStart:
                            WaitForStart(_conPlayer);
                            break;
                        // when player is on countDown
                        case GameState.CountDown:
                            CountDown(_conPlayer);
                            break;
                        // when player is in game
                        case GameState.GameStarted:
                            GameStarted(_conPlayer);
                            break;
                        //when player finished the game
                        case GameState.GameEnded:
                            EndGame(_conPlayer);
                            break;
                    }
                }
            }
        }
        // if server cant run the try portion, exception will tell the problem
        catch (Exception ex) { _actions.Add(() => Console.Write($"Server error: {ex.Message}")); }
    }

    // AsyncAcceptClient
    // Func for async tcp client first contact
    private void AsyncAcceptClient(IAsyncResult iasync)
    {
        // to the main listener not stop listen, use the async listener to
        // terminate the lookup and retrieve the client
        TcpListener listener = (TcpListener)iasync.AsyncState;
        // determinate the tcpClient from the connection
        TcpClient client = listener.EndAcceptTcpClient(iasync);

        //max match size is 2 players, for now we are accepting connections if
        // there is less then 2 players connected
        // confirm if the client is connected
        if (_connectedPlayers.Count < 2 && client.Connected)
        {
            //print the new state on console
            // add call to queue of actions
            _actions.Add(() =>
            Console.Write("New connection onGoing!", Color.green));

            // register the connection data on server
            // Filling new player entrance
            Player player = new Player();
            // inicialize the list that stores all the packets recieved and sent
            // to the player
            player.PlayerPackets = new List<Packet>();
            player.Id = Guid.NewGuid();     // setting the new Player an Id
            player.TcpClient = client;      // saving the TcpClient
            player.GameState = GameState.Connecting;    // set player to connecting state

            _connectedPlayers.Add(player);  // add player to list

            //setting package to send to the client
            Packet packet = new Packet();
            // define the packet type,
            // this type is sent when the registration is in progress
            // adicional info is needed
            packet.PacketType = PacketType.RequestPlayerInfo;

            // build packet with information give by the server
            packet.PlayerGUID = player.Id;
            packet.PlayerState = player.GameState;

            // add the sented packet to the list
            player.PlayerPackets.Add(packet);
            // store connected player in list
            _connectedPlayers.Add(player);

            // send packet to the player with the information
            player.SendPacket(packet);

            // Server console write
            _actions.Add(() =>
            Console.Write("Registing player, waiting remain data", Color.yellow));
        }
        // if server is full , all the new player will get a message from server
        // telling that
        else
        {
            //creates a player for this connecntion
            Player refusedPlayer = new Player();
            // extracts the tcpLink
            refusedPlayer.TcpClient = client;

            //create a packet to inform the player why the connection will drop
            Packet packet = new Packet();
            // set the type of the packet and the description
            packet.PacketType = PacketType.ConnectionRefused;
            packet.Desc = "Server is full";
            // send the packet to the player
            refusedPlayer.SendPacket(packet);
            // drop the connection
            refusedPlayer.CloseConnection();
        }
    }

    // methods that handle the player states
    #region playerState Handlers
    // handle if the player is disconnected
    private void Disconnected(Player _player)
    {
        //TODO
    }

    //handle if the player is Connecting
    private void Connecting(Player _player)
    {
        // printing waiting for remaining info
        _actions.Add(() =>
        Console.Write($"Waiting requested data from {_player.Id}", Color.yellow));

        // check if the player has new data Available
        if (_player.DataAvailabe())
        {
            // read the new data
            Packet _recieved = _player.ReadPacket();

            // save the recieved packet
            _player.PlayerPackets.Add(_recieved);

            // if is any data available, check if the package 
            // is the requested info
            if (_recieved.PacketType == PacketType.PlayerInfo)
            {
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
                _actions.Add(() =>
                Console.Write($"{_player.Name} registed," +
                $" PlayerState in serve{_player.GameState.ToString()}", Color.green));

                // add packet to playerPackets
                _player.PlayerPackets.Add(_confirmPacket);
                // send packet
                _player.SendPacket(_confirmPacket);
            }
        }
    }

    //handle if the player is connected
    private void Connected(Player _player)
    {
        // TODO
    }

    // handle if the player is Syncing with other
    private void Syncing(Player _player)
    {
        // setting up the packet for syncing this player with the other one
        // printing that server start to notiffy the other player
        _actions.Add(() => Console.Write($"Syncing {_player.Name} to the other player",
            Color.yellow));

        // preparing the packet to inform the connected player
        Packet _syncPacket = new Packet();
        // set the packet type as newPlayer
        // sending the player Id and name
        _syncPacket.PacketType = PacketType.NewPlayer;
        _syncPacket.PlayerGUID = _player.Id;
        _syncPacket.PlayerName = _player.Name;

        // sending to player who is waiting for the new player
        _connectedPlayers.ForEach(_p =>
        {
            // diferent from the actual player and waiting for the new player
            if (_p.Id != _player.Id && _p.GameState == GameState.WaitPlayer)
            {
                // print that server is notifying the existing player
                _actions.Add(() => Console.Write($"Notifying {_p.Name} " +
                    $"that a new player has connected", Color.green));

                // send to player the packet to notify the new player
                // save the packet sent
                _p.PlayerPackets.Add(_syncPacket);
                // send the packet to the player waiting
                _p.SendPacket(_syncPacket);

                // since all the players are connected we can change the state of
                // this to waiting game start
                _p.GameState = GameState.WaitingStart;

                // printing that the existing player has been notifyed and now is read
                _actions.Add(() => Console.Write($"Player {_p.Name}, has been notifyed," +
                    $"and now is read to player", Color.red));

                // setting the packet to notify the new player the existing players
                // creating the packet to send
                Packet _notifyExisting = new Packet();
                // add info from the existing connection
                // set the packet to new player type
                _notifyExisting.PacketType = PacketType.NewPlayer;
                // adding the id and the name of the existing player
                _notifyExisting.PlayerGUID = _p.Id;
                _notifyExisting.PlayerName = _p.Name;

                // add packet to packetList of the player
                _player.PlayerPackets.Add(_notifyExisting);
                // send the packet to the player
                _player.SendPacket(_notifyExisting);

                // priting that the new player was notifyed about the connected player
                _actions.Add(() => Console.Write($"Player {_player.Name} know about" +
                    $"{_p.Name}", Color.red));
            }
        });
        // since the new player as known of the existing players and they know about the new
        // one, we can change the state of this player to waitingStart
        _player.GameState = GameState.WaitingStart;

        //printing that the new player is ready to play
        _actions.Add(() => Console.Write($"The player {_player.Name} is now ready to player",
            Color.green));
    }

    //handle if the player is waiting for start
    private void WaitForStart(Player _player)
    {
        //TODO
    }

    //handle if player is countingDown
    private void CountDown(Player _player)
    {
        // TODO
    }

    //handle if the player is in the game
    private void GameStarted(Player _player)
    {
        // TODO
    }

    // handle if the player is in endGame state
    private void EndGame(Player _player)
    {
        //TODO
    }
    #endregion

    #region Coroutines
    //Coroutine
    //coroutine for clear tasks from async 
    private IEnumerator DoQueuedActions()
    {
        // locks the list of actions
        // this is used to block changes during this copy
        // creates a list to store the queued actions
        List<Action> actionsToDo;
        // lock used var _actions
        lock (_actions)
        {
            // copy all the queued actions
            actionsToDo = new List<Action>(_actions);
            // clear the line
            _actions.Clear();
        }
        // for each action, executes
        foreach (Action item in actionsToDo) { item(); }
        yield return null;

    }
    #endregion
}

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
