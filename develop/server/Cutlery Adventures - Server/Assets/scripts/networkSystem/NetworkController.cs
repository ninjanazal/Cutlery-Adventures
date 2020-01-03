﻿using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using Cutlery.Com;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

public class NetworkController : MonoBehaviour
{
    // public Vars
    [Header("Insert server information")]
    public string serverIp = "127.0.0.1";       // server Ip (loopBacK)
    public int TcpPort = 7701, UdpPort = 7702;  // ports

    //server internal vars
    // server states
    private enum ServerState { ServerWaitingPlayers, ServerLoadingClients, ServerMatchRunning }
    private ServerState _serverState;

    //connection
    private List<Player> _connectedPlayers;     // array of players for the next match
    private TcpListener _tcpListener;       // listener for tcp Connection
    private IPAddress _ipAddress;           // IP adress

    private UdpClient _udpListener;         // listener for UDP Connection
    private IPEndPoint _remoteEndPoint;     // valid end points

    // async tasks handler
    private Queue<Action> _asyncActions;

    // gameSimulation elements
    // dictionary of connected players gambeObject
    private Dictionary<Guid, GameObject> _playersPrefsDict;

    [Header("Spawnable prefabs")]
    // map 
    // map gameObj
    public GameObject _selectedMap;
    // start positions
    private Transform[] _startPositions;

    // prefab for player
    public GameObject _playerPrefab;

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

            // start list of asyncActions
            _asyncActions = new Queue<Action>(2);
            // start Connected Player array
            _connectedPlayers = new List<Player>();

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
                        // todo func that close connection and remove the player from the list
                        break;
                    case GameState.Connecting:
                        // call handler for connecting state
                        Connecting(conPlayer);
                        break;
                    case GameState.Sync:
                        // sending the last layer info to the other player
                        SyncNewPlayer(conPlayer);
                        break;
                }
            }

            Debug.Log(_connectedPlayers.Count);
            // need to confirm if all players are waiting players
            // if soo and the player connected cout is 2, this means that server is ready
            // to start a match

            // if exists 2 players connected
            // and all players waiting player
            if (_connectedPlayers.Count == 2 && CheckAllPlayersState(GameState.WaitPlayer))
            {
                // Debug that all players are waiting for other player and there is
                // 2 players connect
                Debug.Log(_connectedPlayers.Count + " players connected and all synced");
                Console.Write(_connectedPlayers.Count + " players connected and synced", Color.yellow);

                // change the serve to server started the match
                _serverState = ServerState.ServerLoadingClients;
            }


        }
        // if server has started the match
        else if (_serverState == ServerState.ServerLoadingClients)
        {
            // this server state handles the match state of the server
            foreach (Player conPlayer in _connectedPlayers)
            {
                // to all players in waiting players
                // server is waiting for countdown packet, meaning all players have loaded the level
                switch (conPlayer.GameState)
                {
                    case GameState.Disconnected:
                        // disconnected state 
                        break;
                    case GameState.WaitPlayer:
                        // if the player is in this state , tell that match has started
                        WaitingPlayers(conPlayer);
                        break;
                    case GameState.CountDown:
                        // player on this state is waiting client sending msg of sceen loaded
                        // when all player recieved confirmation, change to waiting start
                        CountDownHandler(conPlayer);
                        break;
                }

            }

            // shoud check if all the players are waiting start
            // to continue on the server state, all the player need to be waiting start
            if (_connectedPlayers.Count == 2 && CheckAllPlayersState(GameState.WaitingStart))
            {
                //wait 2s for sent packet

                // debug to console, that match is starting
                Debug.Log("Match started");
                Console.Write("All players ready, starting match", Color.yellow);

                // change the server state to matchrunning
                _serverState = ServerState.ServerMatchRunning;

                // set packet to inform players that match has started
                Packet informationPacket = new Packet();
                // set packet type of start match
                informationPacket.PacketType = PacketType.StartMatch;

                // send to all players that match starter
                _connectedPlayers.ForEach(p =>
                {
                    // save the packet
                    p.PlayerPackets.Add(informationPacket);
                    //send the packet
                    p.SendPacket(informationPacket);
                    // change player state to gamerunning
                    p.GameState = GameState.GameRunning;

                    // debug to console
                    Console.Write($"{p.Name} is playeing!", Color.magenta);
                });

                // call method that creats and send packet to inicialize players
                SetSpawners();
            }
        }
        // if server is running a match
        else if (_serverState == ServerState.ServerMatchRunning)
        {

            //run loop to see if player has data to read
            // state that controlls all the data passing to the players
            // server will sent the position of each player every cicle
            foreach (Player conPlayer in _connectedPlayers)
            {
                switch (conPlayer.GameState)
                {
                    case GameState.Disconnected:
                        break;
                    case GameState.GameRunning:
                        break;
                    case GameState.GameEnded:
                        break;
                    default:
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
            if (_serverState != ServerState.ServerWaitingPlayers)
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

                // setting the package to send
                responsePacket.PacketType = PacketType.RequestPlayerInfo;
                // adding the player generated id
                responsePacket.PlayerGUID = acceptedPlayer.Id;

                // saving the packet to the packet list on player
                acceptedPlayer.PlayerPackets.Add(responsePacket);

                //queue actio to do
                // add the accepted player to the connectedPlayers list
                _asyncActions.Enqueue(() => { AddPlayer(acceptedPlayer); });

                // debug to server console that the client has been accepted
                _asyncActions.Enqueue(() => Console.Write("Client added to connected Players"));

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

    // this is needed because the way that unity handles with diferent threads
    // since the gEngine only accepts changes in local obj in the main thread
    // its needed to create functions to call on main thread
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
                {
                    // setting the player to sync with the existent one
                    _player.GameState = GameState.Sync;
                    // defining the player color
                    _player.PlayerColor =
                        new CutleryColor(Color.blue.r, Color.blue.g, Color.blue.b);
                }
                else
                {
                    // setting the player state to wait player
                    // this is the firt player that tryed to connect
                    _player.GameState = GameState.WaitPlayer;
                    // defining the color
                    _player.PlayerColor =
                        new CutleryColor(Color.red.r, Color.red.g, Color.red.b);
                }

                // send the confirmation of the successfull regist
                // create a new packet
                Packet _confirmPacket = new Packet();
                // set packet type of registationOk
                _confirmPacket.PacketType = PacketType.RegistationOK;
                // add packet description
                _confirmPacket.Desc = "Registration successful, wait other player";
                // add the player color to the packet
                _confirmPacket.ObjColor = _player.PlayerColor;

                // print the new information of the player
                // and the state the player is at
                Debug.Log(_player.Name);
                Console.Write($"{_player.Name} registed," +
                $" PlayerState in server: {_player.GameState.ToString()}", Color.green);

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
        // if the player is set to this method, there is allready another player connect
        // soo its needed to send to the connected player the data of the new one
        // and tell the new one about the existing player

        // debug that the server start the process of informing the existing player
        Debug.Log("Setting packet to informe the connected player");
        Console.Write("Seetting packet to send to the connected player", Color.magenta);

        // will sent to all the players setted as waiting player
        // and sent to the new one the information of the players allready connected
        _connectedPlayers.ForEach(p =>
        {
            // if the player id is diferent from the recent connected
            if (p.Id != _player.Id && p.GameState == GameState.WaitPlayer)
            {
                // debug of the player beeing informed
                Debug.Log($"Sending data about players: {p.Name} and {_player.Name}");
                Console.Write($"Sending data about players: {p.Name} and {_player.Name}",
                    Color.magenta);

                // defining the new packet
                Packet informativePacket = new Packet();
                // setting the packet type to new player
                informativePacket.PacketType = PacketType.NewPlayer;
                // adding player Id
                informativePacket.PlayerGUID = _player.Id;
                // adding the payer name to the packet
                informativePacket.PlayerName = _player.Name;
                // adding the player color 
                informativePacket.ObjColor = _player.PlayerColor;

                // will save the packet on his packet list
                p.PlayerPackets.Add(informativePacket);
                // informative packet will be sent for this client
                p.SendPacket(informativePacket);

                // now build the packet to inform the new player of the existing one
                // adding the allready connected player id
                informativePacket.PlayerGUID = p.Id;
                //adding the allready connected player name
                informativePacket.PlayerName = p.Name;
                // adding the allready connected player color
                informativePacket.ObjColor = p.PlayerColor;

                // save the packet on player packet list
                _player.PlayerPackets.Add(informativePacket);
                // server send the packet to the player
                _player.SendPacket(informativePacket);
            }
        });

        // since all players have beem informed about this player
        // change this player to waiting players
        // debug the changes
        Console.Write("Player synced!", Color.green);
        _player.GameState = GameState.WaitPlayer;
    }

    // handler for waitPlayers
    private void WaitingPlayers(Player _player)
    {
        //debug to console
        Console.Write($"Sending packet to {_player.Name}, to load map", Color.blue);

        // if this method is called and player is in this state
        // the server is in matchState
        // send a packet to the player saying that the game starter
        Packet sentPacket = new Packet();
        // setting the packet ty of gamestarted
        sentPacket.PacketType = PacketType.GameStart;
        // adding a description
        sentPacket.Desc = "Game started, load map";

        // add this packet to player packetList
        _player.PlayerPackets.Add(sentPacket);
        // send the packet to the player
        _player.SendPacket(sentPacket);

        // debug to console the state
        Console.Write("Player on CountDown state, waiting for loadingConfirmation");
        // change the player to countDownState
        _player.GameState = GameState.CountDown;
    }
    #endregion

    //handler for countdownState
    private void CountDownHandler(Player _player)
    {
        // server is waiting for client to send a countdown packet
        // confirming that he is ready and loaded
        if (_player.DataAvailabe())
        {
            // if player recieved data, read the data
            Packet recievedPacket = _player.ReadPacket();

            // check the packet type
            if (recievedPacket.PacketType == PacketType.CountDown)
            {
                // debug to console
                Console.Write($"Player {_player.Name} is ready and loaded");

                // save the packet in to the packets types
                _player.PlayerPackets.Add(recievedPacket);

                // change the player state to waitingStart
                _player.GameState = GameState.WaitingStart;
                // debug on console
                Console.Write("Player is now waiting to start the game");
            }
        }
    }

    // internal methods
    private bool CheckAllPlayersState(GameState state)
    {
        // define the var for hold if all players are waiting
        bool validator = false;
        // for each connected player
        foreach (Player p in _connectedPlayers)
        {
            // check if player is on wait state
            validator = (p.GameState == state) ? true : false;
            // if not in waiting state break the loop
            if (!validator) break;
        }
        // retur the value
        return validator;
    }

    // setting player spawns
    private void SetSpawners()
    {
        // debug to console the operation
        Console.Write("Spawning map on server");
        // set map vars on server side                
        // set the map on server side
        _selectedMap = Instantiate(_selectedMap);

        // load the start positions
        _startPositions = new Transform[2];
        _startPositions[0] = GameObject.Find("startPlayer1").GetComponent<Transform>();
        _startPositions[1] = GameObject.Find("startPlayer2").GetComponent<Transform>();

        // debug to console
        Console.Write("Creating Objects for each player", Color.cyan);

        // start the dictionar
        _playersPrefsDict = new Dictionary<Guid, GameObject>(2);
        // for each connected player set a preffab
        foreach (Player p in _connectedPlayers)
        {
            // add the instanciated prefab to dic with the player id as key
            // and the gameObject of the player, onn the corresponding position
            GameObject goToAdd = Instantiate(_playerPrefab,
                new Vector3(_startPositions[_connectedPlayers.IndexOf(p)].position.x,
                _startPositions[_connectedPlayers.IndexOf(p)].position.y),
                Quaternion.identity);

            // add to dictionary
            _playersPrefsDict.Add(p.Id, goToAdd);
            Debug.Log(_playersPrefsDict.Count);

            // after the player creation need to send the player spawn
            // to clients
            // creates a packet with this info
            Packet spawnPlayerPacket = new Packet();


            // set the packet type of set obj
            spawnPlayerPacket.PacketType = PacketType.SetObjPosition;
            // set desc of packet as player
            spawnPlayerPacket.Desc = "player";
            // adding the player id
            spawnPlayerPacket.PlayerGUID = p.Id;

            // add the player position on packet            
            Position pos = new Position();
            pos.X = goToAdd.transform.position.x;
            pos.Y = goToAdd.transform.position.y;
            spawnPlayerPacket.PlayerPosition = pos;

            // save the packet on player
            p.PlayerPackets.Add(spawnPlayerPacket);
            // send the packet for all the players
            _connectedPlayers.ForEach(player => player.SendPacket(spawnPlayerPacket));

            // debug 
            Console.Write($"Notifying all player about the GameObject of {p.Name};");
        }
        // debug the ending of spawning players prefs
        Console.Write("Players obj created, starting simulations", Color.green);
    }
}

/*  TODO
 *  - (maybe) wait line to play
 *  - Create server loop
 *  - (Maybe) notify the new connections
 *  - Setting up match
 *  - match loop
 *  - win/lose statement
 *  - reset server status
 *  - server ready for new match
 */
