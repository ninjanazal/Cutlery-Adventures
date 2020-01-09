using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using Cutlery.Com;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Text;
using Newtonsoft.Json;

public class NetworkController : MonoBehaviour
{
    // public Vars
    [Header("Insert server information")]
    public string serverIp = "127.0.0.1";       // server Ip (loopBacK)
    public int TcpPort = 7701, UdpPort = 7702, UdpClientPort = 7703;  // ports

    //server internal vars
    // server states
    private enum ServerState { ServerWaitingPlayers, ServerLoadingClients, ServerMatchRunning }
    private ServerState _serverState;

    // bool for track if the game is running
    private bool _isMatchRunning;

    //connection
    private List<Player> _connectedPlayers;     // array of players for the next match
    private TcpListener _tcpListener;       // listener for tcp Connection
    private IPAddress _ipAddress;           // IP adress

    private UdpClient _udpListener;         // listener for UDP Connection
    private IPEndPoint _remoteEndPoint;     // valid end points
    private bool _recievingUpdDatagrams;    // state of the udp listener

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
    // prefab for the spawned obj
    public GameObject obJPrefab;

    // referenc to the obj in game
    // only one obj will be in game each time

    private GameObject _objInGame;

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
            _udpListener = new UdpClient(UdpPort);
            // defining that udp wont recieve data
            _recievingUpdDatagrams = false;
            // seting the is match runing to false
            _isMatchRunning = false;

            // start list of asyncActions
            _asyncActions = new Queue<Action>();
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
        if (_asyncActions.Count > 0)
            StartCoroutine(AsyncActionsClear());

        // internal loop for acepting players
        // listen for new players , looking for pending connections
        if (_tcpListener.Pending())
            PendingConnectionsResolver();


        // if server is waiting players
        if (_serverState == ServerState.ServerWaitingPlayers)
            WaitingPlayersStateResolver();                   // call resolver for waiting players
        // if server has started the match
        else if (_serverState == ServerState.ServerLoadingClients)
            LoadingClientsStateResolver();                  // call resolver for loading players
        // if server is running a match
        else if (_serverState == ServerState.ServerMatchRunning)
            MatchRunningStateResolver();                    // call resolver for match running state
    }

    #region ServerStates
    //resolv pending connections requests
    private void PendingConnectionsResolver()
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

    // resolver for waiting players server state
    private void WaitingPlayersStateResolver()
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

    // resolver for loading clients state
    private void LoadingClientsStateResolver()
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
            // set that match is running
            _isMatchRunning = true;

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

    // resolver for match running sttate
    private void MatchRunningStateResolver()
    {
        // if not running, start the udpListener recieving the data
        if (!_recievingUpdDatagrams && _isMatchRunning)
        {
            //debug to console
            Console.Write("Start Recieving UdpDatagrams", Color.blue);

            // since the method will run in a internal asynca calling
            _recievingUpdDatagrams = true;
            // start reciving datagrams async
            _udpListener.BeginReceive(new AsyncCallback(RecievingDatagramCallback), _udpListener);
        }
        // Udp listener will queue the requested actions
        // state that controlls all the data passing to the players
        // server will sent the position of each player every cicle
        foreach (Player conPlayer in _connectedPlayers)
        {
            switch (conPlayer.GameState)
            {
                case GameState.GameRunning:
                    // at this state the client can send actions using tcp
                    GameRunningHandler(conPlayer);
                    break;
            }
        }

        // run match logic 
        MatchLocgic();
    }
    #endregion

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
            // setting the last packet stamp as 0
            acceptedPlayer.LastPacketStamp = 0;
            // setting the Udp params
            // udpClient
            acceptedPlayer.UdpCLient = new UdpClient();
            // seting the IpEndPoint
            // this is the endPoint of the connected player
            acceptedPlayer.ClientEndPoint =
                new IPEndPoint(((IPEndPoint)client.Client.RemoteEndPoint).Address,
                UdpClientPort);

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

    // async callback for reading packets sent using udp
    private void RecievingDatagramCallback(IAsyncResult asyncResult)
    {
        // retriving the udpClient
        UdpClient client = (UdpClient)asyncResult.AsyncState;
        // creating the IpEndpoint
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, UdpPort);

        // recieved data
        byte[] recievedData = client.EndReceive(asyncResult, ref endPoint);

        // decoding the byte to a string
        string recievedJsonString = Encoding.ASCII.GetString(recievedData);

        // saving this data as a packet
        Packet recievedUdpPacket = JsonConvert.DeserializeObject<Packet>(recievedJsonString);

        // since this is a async method we need to queue action to the main thread
        _asyncActions.Enqueue(() =>
        {
            // before treat the recieve packet, start recieving packets again if _recieving updadatagrams is true
            if (_recievingUpdDatagrams)
                _udpListener.BeginReceive(new AsyncCallback(RecievingDatagramCallback), _udpListener);
        });

        // treating the data
        if (recievedUdpPacket.PacketType == PacketType.PlayerPosition)
        {
            // if is a position update from client
            // loock for the player who send int
            foreach (Player player in _connectedPlayers)
            {
                // confirm the id
                if (recievedUdpPacket.PlayerGUID == player.Id)
                {
                    // confim if this packet is newer
                    if (recievedUdpPacket.GetSendStamp > player.LastPacketStamp)
                    {
                        //debug that is a valid packet

                        // saving the recieved packet stamp
                        player.LastPacketStamp = recievedUdpPacket.GetSendStamp;
                        // and storing the packet
                        player.PlayerPackets.Add(recievedUdpPacket);

                        // if soo, Update the player Position on server
                        // its a async method soo add to actions
                        _asyncActions.Enqueue(() =>
                        {
                            // change the player transform on server
                            _playersPrefsDict[player.Id].transform.position =
                                new Vector3(recievedUdpPacket.PlayerPosition.X,
                                recievedUdpPacket.PlayerPosition.Y, 0f);
                        });
                        // send this to the other player
                        foreach (Player p in _connectedPlayers)
                        {
                            // if the player id os not equal to the pmoved player
                            if (player.Id != p.Id)
                            {
                                // saving the packet sented for the player
                                p.PlayerPackets.Add(recievedUdpPacket);
                                // send the packet to the player
                                p.SendPacketUdp(recievedUdpPacket);
                                // debug to log

                            }
                        }
                        Debug.Log("Position Recieved and sented to the other");
                    }
                }
            }
        }
    }

    // execute asyncMethods
    private IEnumerator AsyncActionsClear()
    {
        // if exists any queued action queued
        // lock the var for execut
        lock (_asyncActions)
        {
            while (_asyncActions.Count > 0)
            {
                // execute the oldest action
                _asyncActions.Dequeue()();
            }
            yield return null;
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
                    //defining player number
                    _player.PlayerNumber = 2;
                    // setting the udp endpoint from the player num
                    _player.ClientEndPoint.Port = UdpClientPort + _player.PlayerNumber;
                }
                else
                {
                    // setting the player state to wait player
                    // this is the firt player that tryed to connect
                    _player.GameState = GameState.WaitPlayer;
                    // defining the color
                    _player.PlayerColor =
                        new CutleryColor(Color.red.r, Color.red.g, Color.red.b);
                    //defining the player number
                    _player.PlayerNumber = 1;
                    // setting the udp endpoint from the player num
                    _player.ClientEndPoint.Port = UdpClientPort + _player.PlayerNumber;
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
                //adding the player number to the packet
                _confirmPacket.PlayerNumber = _player.PlayerNumber;

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
                //adding the player number
                informativePacket.PlayerNumber = _player.PlayerNumber;

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
                // adding the allready connected player number
                informativePacket.PlayerNumber = p.PlayerNumber;

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

    // handler for gameRunning
    private void GameRunningHandler(Player _player)
    {
        // check if the player has data available
        if (_player.DataAvailabe())
        {
            // read the packet from the stream
            Packet recievedPacket = _player.ReadPacket();

            //check the packet type
            // if is a action packet, 
            if (recievedPacket.PacketType == PacketType.PlayerAction)
            {
                // debug to log
                Debug.Log("Player action recieved");

                // lets check if the player obj is colliding with any obj
                List<GameObject> collidingObjs = new List<GameObject>();

                collidingObjs = _playersPrefsDict[_player.Id].
                    GetComponent<TriggerController>().GetObjOnTrigger();
                // check what type of obj is on the trigger
                if (collidingObjs.Count > 0)
                    collidingObjs.ForEach(obj =>
                    {
                        // if is a cutlery obj
                        if (obj.CompareTag("Cutlery"))
                        {
                            // calculate the direction of the force
                            Vector3 resultantForce =
                                _objInGame.transform.position -
                                _playersPrefsDict[_player.Id].transform.position;

                            //normalize the vector
                            resultantForce.Normalize();
                            // add the force to the resultante Vector
                            resultantForce *= 250f;

                            // add the force to the obj
                            _objInGame.GetComponent<ObjController>().AddForce(
                                    new Vector2(resultantForce.x, resultantForce.y));
                        }
                    });
            }
        }
    }
    #endregion

    #region Internal Methods
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
                new Vector3(_startPositions[p.PlayerNumber - 1].position.x,
                _startPositions[p.PlayerNumber - 1].position.y),
                Quaternion.identity);

            // add to dictionary
            _playersPrefsDict.Add(p.Id, goToAdd);
            Debug.Log("_players obj added to dictionary " + _playersPrefsDict.Count);

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

    // internal  match logic
    private void MatchLocgic()
    {
        // run the match logic
        // need to check if there is any matchObj allready spawned
        if (_objInGame == null)
        {
            // if this var is null, there is no obj in game
            _objInGame = Instantiate(obJPrefab, Vector3.zero, Quaternion.identity);

            // after spawning a obj, server needs to inform all the players about the new spawn
            // this elements are made using Tcp
            // preparing the packet
            Packet spawnObjPacket = new Packet();
            // setting the packet type
            spawnObjPacket.PacketType = PacketType.SpawnObj;
            //defining the sprite of the ob
            // since there is only 3 sprites
            spawnObjPacket.ObjSprite = new System.Random().Next(0, 3);
            // setting the obj position
            Position objPos = new Position();
            objPos.X = _objInGame.transform.position.x;
            objPos.Y = _objInGame.transform.position.y;
            // adding the position to the packet
            spawnObjPacket.ObjPosition = objPos;

            // debug on console
            Console.Write("Spawning obj", Color.yellow);

            // sending this information to all players
            _connectedPlayers.ForEach(p =>
            {
                // add packet to player packet list
                p.PlayerPackets.Add(spawnObjPacket);
                // send the packet to the player
                p.SendPacket(spawnObjPacket);
            });
        }
    }

    #endregion

    #region public methods
    // method called by the in game obj
    public void UpdateObjPosition(float xPos, float yPos, float yRot)
    {
        // setting the packet for update in game Obj
        Packet updateObjPacket = new Packet();

        // setting the packet type
        updateObjPacket.PacketType = PacketType.SetObjPosition;
        // adding the position of the obj
        Position objPos = new Position();
        objPos.X = xPos;
        objPos.Y = yPos;
        // adding the position to the packet
        updateObjPacket.ObjPosition = objPos;
        // setting the obj rotation
        Rotation rot = new Rotation(0f, yRot, 0);
        //adding the rot to the packet
        updateObjPacket.ObjRotation = rot;

        // debug on console
        Debug.Log("Updating obj pos");

        // send this packet to all the players
        _connectedPlayers.ForEach(p =>
        {
            // add the packet to the player packet
            p.PlayerPackets.Add(updateObjPacket);
            // sending this packet using udp
            p.SendPacketUdp(updateObjPacket);
        });
    }

    // method called when a player scores
    public void PlayerScore(int pNum)
    {
        // check what player score
        foreach (Player _player in _connectedPlayers)
        {
            // confirm the player number
            if (_player.PlayerNumber == pNum)
            {
                // ifsoo, add the point to the player
                _player.AddPlayerScorePoint();

                // build the packet to inform the players about this score
                Packet informScorePointPacket = new Packet();
                // packet type
                informScorePointPacket.PacketType = PacketType.PlayerScore;
                // add the id of the player that scored
                informScorePointPacket.PlayerGUID = _player.Id;
                // add the player score to the packet
                informScorePointPacket.PlayerScore = _player.PlayerScore;

                // send this to all the player
                _connectedPlayers.ForEach(player =>
                {
                    // save the packet on player packet
                    player.PlayerPackets.Add(informScorePointPacket);

                    // send the information using tcp
                    player.SendPacket(informScorePointPacket);

                    // send other packet to reset the player position
                    Packet resetPlayerPosition = new Packet();
                    // setting the packet type
                    resetPlayerPosition.PacketType = PacketType.ResetPlayerPosition;

                    // setting the position
                    Position position = new Position();
                    position.X = _startPositions[player.PlayerNumber - 1].position.x;
                    position.Y = _startPositions[player.PlayerNumber - 1].position.y;
                    // adding the position to the packet
                    resetPlayerPosition.PlayerPosition = position;

                    //save the packet to the player packet list
                    player.PlayerPackets.Add(resetPlayerPosition);
                    // send the packet
                    player.SendPacket(resetPlayerPosition);

                    //after the packet about the reset position, inform about the obj
                    //destruction
                    // build packet to inform about the obj destruction
                    Packet objDestructionPacket = new Packet();
                    // set the packet type
                    objDestructionPacket.PacketType = PacketType.DestroyObj;

                    //save the packet on the player packet list
                    player.PlayerPackets.Add(objDestructionPacket);
                    //send the packet to the client
                    player.SendPacket(objDestructionPacket);
                });
                // when found , break
                break;
            }
        }
        // destroy the obj on server
        Destroy(_objInGame);
        // set the var to null for respawn
        _objInGame = null;
    }
    #endregion
}