using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using Cutlery.Com;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.SceneManagement;
using System.Text;
using Newtonsoft.Json;

public class ClientNetworkController : MonoBehaviour
{
    // reference to menuController script
    private MenuController _menu;

    // script that controll all the network information
    // reference to output console
    private Text _outputText;

    // async tasks handler
    private Queue<Action> _asyncActions;
    // async var for loading scenes
    private AsyncOperation _loadAsyncOperation;

    // private bool for isLoadingSceen operation
    private bool _isLoadingSceenOperation;

    // private vars
    // players 
    private Player _player;
    private Player _opponentPlayer;

    // reference to spawned objs
    // gameobject dict controller
    private Dictionary<Guid, PlayerScript> _spawnedPlayers;
    // cutlery in game
    private GameObject _inGameCutlery;

    // vars for connection    
    // IpAddress var
    private IPAddress _IpAdress;
    private UdpClient _UdpListener;

    // ports for connection
    private int _serverTcpPort = 7701, _serverUdpPort = 7702,
        _playerUdpListenPort = 7703;

    // UdpConnection
    // state of the udp Listener
    private bool _recievingUdpDatagrams;

    // gamePlayer vars
    // public references
    [Header("References to prefabs")]
    // reference to spawnables gameObjs
    // public gameObj
    public GameObject playerGO;
    // reference to the sprites of the cutlerObj
    public Sprite[] CutlerySprites;

    [Space(5)]
    // reference for the cutler Prefab
    public GameObject CutlerPrefab;


    // script awake
    private void Awake()
    {
        // keep the networkController gameobject when load 
        // sceens, since the scene will change, need to keep
        // the information from the menu and the connection 
        // all the connection part will be maneged by this script
        UnityEngine.Object.DontDestroyOnLoad(this);

        // reference to output text
        _outputText = GetComponentInChildren<Text>();
        _menu = GameObject.Find("MenuController").GetComponent<MenuController>();

        // start queue of asyncActions
        _asyncActions = new Queue<Action>();
        // set the _isloadingOperation to false
        _isLoadingSceenOperation = false;
        //set the loadasyncAction to null
        _loadAsyncOperation = null;

        //defining the udp wont recieve data
        _recievingUdpDatagrams = false;

        // setting the players
        // local player
        _player = new Player();
        // opponent player
        _opponentPlayer = new Player();
        _player.LastPacketStamp = 0;

        // setting player to a disconnected state
        _player.GameState = GameState.Disconnected;

        // debug into text
        _outputText.text = "Player networkController is awaked";
        // after start the player connection state is disconnected
    }

    // network update
    private void Update()
    {
        // after all logic, execut the actions queued by the asyunc method
        AsyncActionsClear();

        // if the player was connecting or trying
        if (_player.GameState != GameState.Disconnected)
        {
            // check server recieved data
            PlayerConnectionLoop();
        }
    }

    // function that handles the player connection part while connected
    private void PlayerConnectionLoop()
    {
        // if the player is connected
        if (_player.TcpClient.Connected)
        {
            // switch for the player connection state
            switch (_player.GameState)
            {
                case GameState.Connecting:
                    // call handler for this state
                    PlayerConnecting();
                    break;
                case GameState.Connected:
                    //  player is connected to server but still missing information
                    PlayerConnected();
                    break;
                case GameState.WaitPlayer:
                    // player in this state is waiting the server to send
                    // the opponent info
                    WaitingOpponentPlayer();
                    break;
                case GameState.WaitingStart:
                    // player on this state is waiting the server to send a start msg
                    // with the map number (for now, since only one map added, this is not needed)
                    WaitingMatchStart();
                    break;
                case GameState.CountDown:
                    // in this sate player will load the scene of the game
                    // and when is done send to the server the confirmation
                    CountDownState();
                    break;
                case GameState.GameStarted:
                    // player in this state is loaded and waiting for server to start the match
                    GameStartedState();
                    break;
                case GameState.GameRunning:
                    // in this state the game is running , expecting spawn packets, 
                    //position and states
                    GameRunningState();
                    break;
                case GameState.GameEnded:
                    break;
            }
        }
    }

    // function that is called when the player press the connect button
    public void StartConnection(string serverIp, string playerName)
    {
        // save the information passed
        // save the ip as a IpAddress var
        _IpAdress = IPAddress.Parse(serverIp);

        // setting the name on local player var
        _player.Name = playerName;

        // defining the local player tcpVar
        _player.TcpClient = new TcpClient();

        // defining the local player udpVar

        // debug to text
        _outputText.text = $"-> Trying to connected to {serverIp}:{_serverTcpPort}";

        // start the beggin connection
        _player.TcpClient.BeginConnect(_IpAdress, _serverTcpPort,
            new AsyncCallback(ConnectionCallback), _player.TcpClient);

        //func will wait here for results
        Debug.Log("Begin connect started");
        // debug to text
        _outputText.text += "\n-> Begin connect started\n -> Player seted to Connecting";
        _player.GameState = GameState.Connecting;
    }

    #region AsyncMethods
    // callback for the async beging connect
    private void ConnectionCallback(IAsyncResult ar)
    {
        // retrieve the tcp client from the async callback
        TcpClient client = (TcpClient)ar.AsyncState;
        // ends a pending asynchronous connection attempt
        client.EndConnect(ar);

        // queue actions
        _asyncActions.Enqueue(() =>
        {
            // debug to log
            Debug.Log("Async connection attempt stoped");
            // debug to text
            _outputText.text += "\n-> Async connection attempt stoped";
        });

        // if the connection is established
        if (client.Connected)
        {
            // queued this actions
            _asyncActions.Enqueue(() =>
            {
                // debug to log
                Debug.Log("Connection granted");
                // debug to text
                _outputText.text += "\n-> Connection granted, creating writer and reader";

                // creating the binaryReader using the connection stream
                _player.PlayerReader = new BinaryReader(client.GetStream());
                // creating the binaryWriter using the connection stream
                _player.PlayerWriter = new BinaryWriter(client.GetStream());
                // start the list of packets
                _player.PlayerPackets = new List<Packet>();

                // output to text
                _outputText.text += "\n-> Setting up Udp definition";

                // setting the Server ipendpoint
                _player.ClientEndPoint = new IPEndPoint(_IpAdress, _serverUdpPort);
                // defining the player udp
                _player.UdpCLient = new UdpClient();
                //defining the player udpListener

                // defining the udp client for recieving data
                _UdpListener = new UdpClient();

            });
        }
        else
        {
            // if not connected 
            // debug to log
            Debug.Log("Connection Refused");
            // debug to text
            _outputText.text += "\n -> connection refused";
        }
    }

    // function that loads sceen async
    // and reports to the server that player is ready to play
    private async Task<bool> LoadSceanAsync(string sceneName)
    {
        // debug telling that loadSceneAsync has started
        _asyncActions.Enqueue(() =>
        {
            // set loadingOperation bool to true
            _isLoadingSceenOperation = true;
            Debug.Log("Start loading the scene");
            _outputText.text += "\n-> Start loading the scene";
        });

        // begin to load the scene
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        // disable the activation of this scene
        asyncOperation.allowSceneActivation = false;

        // is loaded bool
        bool isLoaded = false;

        // while the loading is not ready
        while (!isLoaded)
        {
            // if hasnt ended loading the scene return null;
            await Task.Delay(500);

            // print the progress of the load
            _asyncActions.Enqueue(() =>
            {
                float progress = asyncOperation.progress;
                _outputText.text += $"\n-> Loading progress: {progress * 100}%";
                // check if the progress is bigger than 0.9
                // if soo, define the isLoaded to true
                if (progress >= 0.9f)
                    isLoaded = true;
            });
        }
        _asyncActions.Enqueue(() =>
        {
            // queue the finished asyncOperation
            _asyncActions.Enqueue(() => _loadAsyncOperation = asyncOperation);
        });
        Debug.Log("finished loading");
        return true;
    }

    // execute queued asyncAction
    private void AsyncActionsClear()
    {
        // run if any async action was queued
        while (_asyncActions.Count > 0)
        {
            try
            {
                // execute the oldest action in queue
                _asyncActions.Dequeue()();
            }
            catch (Exception ex) { Debug.Log(ex); }
        }


    }

    // async callback for reading packets sent using udp
    private void RecievingDatagramCallback(IAsyncResult asyncResult)
    {
        //queue the async results
        _asyncActions.Enqueue(() =>
        {
            // debug to outPut
            _outputText.text = "\n-> Udp packet recieved";

            // if so, start listen for new data
            if (_recievingUdpDatagrams)
                _UdpListener.BeginReceive(new AsyncCallback(RecievingDatagramCallback),
                    _UdpListener);
        });

        // retriving the client
        UdpClient client = (UdpClient)asyncResult.AsyncState;
        // creating the endPoint
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, _playerUdpListenPort);

        //debug to coonsole
        _asyncActions.Enqueue(() => Debug.Log("Extracting data from the datagram"));

        // save the data
        byte[] recievedData = client.EndReceive(asyncResult, ref endPoint);
        //decoding the byte to a string
        string recievedJsonString = Encoding.ASCII.GetString(recievedData);
        //save the data as a packet
        Packet recievedUdpPacket = JsonConvert.DeserializeObject<Packet>(recievedJsonString);

        // treat the reaceved data
        // if is a packet for player position
        if (recievedUdpPacket.PacketType == PacketType.PlayerPosition)
        {
            // if is a packet of movement
            // check if the packet is to this player
            // on udp connection player can only recieve packets for the mov of the other player
            // this check for all the players connected because the udp packet can reatch the client before the tcp one
            if (recievedUdpPacket.PlayerGUID == _opponentPlayer.Id &&
                _spawnedPlayers.ContainsKey(_opponentPlayer.Id))
            {
                // check if this is the newer packet
                if (recievedUdpPacket.GetSendStamp > _player.LastPacketStamp)
                {
                    // add to queue
                    _asyncActions.Enqueue(() =>
                    {
                        // if soo, add this packet to list
                        _player.PlayerPackets.Add(recievedUdpPacket);
                        // add the stamp
                        _player.LastPacketStamp = recievedUdpPacket.GetSendStamp;

                        // call method to player the player at the recieved pos
                        // set transform recieves xand y , and rotation y
                        _spawnedPlayers[_opponentPlayer.Id].
                            SetTransform(recievedUdpPacket.PlayerPosition.X,
                            recievedUdpPacket.PlayerPosition.Y,
                            recievedUdpPacket.ObjRotation.Y);
                    });


                }
            }
        }
        // if is a packet for obj Update
        else if (recievedUdpPacket.PacketType == PacketType.SetObjPosition)
        {
            // check if the obj is spawned
            if (_inGameCutlery != null)
                // confirm if is the latest packet recieved
                if (recievedUdpPacket.GetSendStamp > _player.LastPacketStamp)
                {
                    // add to async queue
                    _asyncActions.Enqueue(() =>
                    {
                        // add this packet to the player packets
                        _player.PlayerPackets.Add(recievedUdpPacket);
                        // update the last packet stamp
                        _player.LastPacketStamp = recievedUdpPacket.GetSendStamp;

                        // update the transform of the obj
                        _inGameCutlery.transform.position =
                            new Vector3(recievedUdpPacket.ObjPosition.X,
                            recievedUdpPacket.ObjPosition.Y, 0f);

                        // update the position
                        _inGameCutlery.transform.rotation =
                        Quaternion.Euler(0f, 0f, recievedUdpPacket.ObjRotation.Y);

                    });
                }
        }
    }
    #endregion

    #region StateHandlers
    // handler for connecting state
    private void PlayerConnecting()
    {
        // checking if player has data to read
        if (_player.DataAvailabe())
        {
            try
            {
                //debug to log that has data to read
                Debug.Log("Player has data to read in connecting state");
                // debug to text
                _outputText.text += "\n-> Data available: Connecting state";

                // set recieved packet
                // and store the packet data
                Packet recievedPacket = _player.ReadPacket();

                // at this point only 2 types of packet types are expected
                // requesting more info or connectionRefused

                if (recievedPacket.PacketType == PacketType.RequestPlayerInfo)
                {
                    // debug that player has recieve a request packet
                    Debug.Log("Player recieved a request Packet");
                    _outputText.text += "\n-> Player recieved a request Packet";

                    // if the packet is a request packet
                    // client should sent the adicional information

                    // extract the information sented by the server
                    // save the server player id
                    _player.Id = recievedPacket.PlayerGUID;

                    // debug the recieved id
                    _outputText.text += "\n-> playerId: " + _player.Id;

                    // add packet to the player packet list
                    _player.PlayerPackets.Add(recievedPacket);

                    // set the playerInformationRequest packet
                    Packet playerRequestedInfo = new Packet
                    {
                        // add the player name to the packet
                        PlayerName = _player.Name,
                        // define the packet type
                        PacketType = PacketType.PlayerInfo
                    };
                    // debug that player sent a responce
                    Debug.Log("Sent requested data");
                    _outputText.text += "\n-> Sent requested data";

                    // sending the requested info to server
                    _player.SendPacket(playerRequestedInfo);

                    // save the packet on playerPackets
                    _player.PlayerPackets.Add(playerRequestedInfo);

                    // change player state to awaiting the other player
                    Debug.Log("Player waiting opponent");
                    _outputText.text += "\n-> Player waiting opponent";

                    _player.GameState = GameState.Connected;
                }
                else if (recievedPacket.PacketType == PacketType.ConnectionRefused)
                {
                    // if packet is a connection refused on
                    // player will know that the server is full
                    // so he should drop the connection 

                    // debugging the description
                    Debug.Log(recievedPacket.Desc);
                    _outputText.text = recievedPacket.Desc;

                    // closing connection
                    _player.TcpClient.Close();
                    // clear the var
                    _player = new Player();
                    // set player to disconnected
                    _player.GameState = GameState.Disconnected;

                    // call the cancel button action
                    _menu.CancelBtnCallback();
                }
                else
                {
                    // debug if a diferent packet was recieved
                    Debug.Log(recievedPacket.PacketType.ToString());
                    // display on text outpu the type of the packet
                    _outputText.text = recievedPacket.PacketType.ToString();
                }
            }
            // debug the excetption if happend
            catch (Exception ex) { _outputText.text = ex.Message; Debug.Log(ex.Message); }
        }
    }

    // handler for connected state
    private void PlayerConnected()
    {
        // check if player has data available
        if (_player.DataAvailabe())
        {
            // debug that player has data to recieved
            Debug.Log("Data to recieve in connected state");
            _outputText.text += "\n-> Data available in Connected state";


            // save the recieved packet
            Packet recievedPacket = _player.ReadPacket();

            // player waiting for a registationOk packet
            if (recievedPacket.PacketType == PacketType.RegistationOK)
            {
                // save packet on player packets
                _player.PlayerPackets.Add(recievedPacket);

                // debug that player has recieved a registationOK packet
                Debug.Log("Registation Ok recieved");
                _outputText.text += "\n-> RegistationOk packet recieved";

                // extract data from the packet
                // save color set from the server to the player
                _player.PlayerColor = recievedPacket.ObjColor;

                // show the packet desc
                _outputText.text += "\n-> " + recievedPacket.Desc;

                //Save the player number 
                _player.PlayerNumber = recievedPacket.PlayerNumber;

                //define the udp listener on base of player number
                // with this remove the problems for debug
                _UdpListener =
                    new UdpClient(new IPEndPoint(_IpAdress,
                    _playerUdpListenPort + _player.PlayerNumber));

                //show the player number
                _outputText.text += "\n-> player Number: " + _player.PlayerNumber;

                // since the registation was succeeded the player will wait for the other player
                _player.GameState = GameState.WaitPlayer;

                // debug that player is waiting for opponent
                _outputText.text += "\n-> Waiting for player to appear";

                // call function that display all the player information on screen
                _menu.DisplayPlayerConnected(_player.Name, _player.PlayerColor);
            }


        }
    }

    // handler for waiting player state
    private void WaitingOpponentPlayer()
    {
        // check if player has data available
        if (_player.DataAvailabe())
        {
            // if has data to read, lets read it
            // debug that exists data available
            Debug.Log("Data available in waitplayer state");
            _outputText.text += "\n-> Data available in waitPlayer state";

            // save the recieved packet on local var
            Packet recievedpacket = _player.ReadPacket();

            // player is waiting for a new player packet
            if (recievedpacket.PacketType == PacketType.NewPlayer)
            {
                // if is the expected packet, store the packet
                _player.PlayerPackets.Add(recievedpacket);

                // debug that new player packet was recieved
                Debug.Log("NewPlayer packet recieved");
                _outputText.text += "New player packet recieved";

                // the new player is the opponent player
                // save the opponent data into the opponent var
                // save the id
                _opponentPlayer.Id = recievedpacket.PlayerGUID;
                // save the name
                _opponentPlayer.Name = recievedpacket.PlayerName;
                // save the opponent color
                _opponentPlayer.PlayerColor = recievedpacket.ObjColor;
                //saving the opponent player number
                _opponentPlayer.PlayerNumber = recievedpacket.PlayerNumber;

                // since the opponent data has been stored, 
                // server will be starting the game soo

                // debug that player is read to play
                Debug.Log(_opponentPlayer.Name + " Player is synced and ready to play");
                _outputText.text += "\n->" + _opponentPlayer.Name + " Player ready to play";

                // call menu method to display this information
                _menu.DisplayerConnectedOpponent(_opponentPlayer.Name, _opponentPlayer.PlayerColor);

                // change player state to waitstartMatch
                _player.GameState = GameState.WaitingStart;
            }
        }
    }

    // handle for wait match start
    private void WaitingMatchStart()
    {
        // client will wait for a packet of type match started
        if (_player.DataAvailabe())
        {
            // debug this state
            Debug.Log("Data available on waiting start state");
            _outputText.text += "\n-> Data available on waiting start state";

            // read the pending data
            Packet recievedPacket = _player.ReadPacket();

            // check what packet type is
            if (recievedPacket.PacketType == PacketType.GameStart)
            {
                // if passes this if
                // save the packet on player packet
                _player.PlayerPackets.Add(recievedPacket);

                // change the player state to countdown
                _player.GameState = GameState.CountDown;
            }
        }
    }

    // handler for countDown state
    private async void CountDownState()
    {
        // if an async func is onGOing
        if (!_isLoadingSceenOperation)
        {
            // set var to null before waiting the new one
            _loadAsyncOperation = null;

            // if player enter this state, should start to load the scene
            Task<bool> loading = LoadSceanAsync("trainStationMap");
            // wait for the result
            bool result = await loading;
        }

        // check if the load scene has ended
        if (_loadAsyncOperation != null)
        {
            // the operation has ended
            Debug.Log("finished loading the scene");
            _outputText.text += "\n-> Map loaded, wait server to start";

            //send packet to server saying that player is loaded
            Packet loadedPacket = new Packet();
            // set the packet type of countdown
            loadedPacket.PacketType = PacketType.CountDown;
            // save the packet on player packets
            _player.PlayerPackets.Add(loadedPacket);

            // sent packet to server
            _player.SendPacket(loadedPacket);
            // debug to text
            Debug.Log("Packet informing server of loaded state sented");
            _outputText.text += "\n-> Packet informing of load sented";

            // change client to gameStarted state
            _player.GameState = GameState.GameStarted;
        }
    }

    // handler for gameStarted State
    private void GameStartedState()
    {
        // check for available data
        if (_player.DataAvailabe())
        {
            // if data is available in this state
            // read the data
            Packet recievedPacket = _player.ReadPacket();

            // player waiting for packet of type start match
            if (recievedPacket.PacketType == PacketType.StartMatch)
            {
                // save the recieved packet
                _player.PlayerPackets.Add(recievedPacket);

                // debug to text that match as started
                _outputText.text += "\n-> match started, change scenes";

                // change the player state to matchrunning
                _player.GameState = GameState.GameRunning;

                // reset the vars needed
                _isLoadingSceenOperation = false;
                _loadAsyncOperation.allowSceneActivation = true;

                // start a start the spawnedGO dic
                _spawnedPlayers = new Dictionary<Guid, PlayerScript>(2);

                // start reading packets from the udp connection
            }
        }
    }

    // handler for GameRunning State
    private void GameRunningState()
    {
        //if the udp is not recieving data from the server
        if (!_recievingUdpDatagrams)
        {
            // debug to console
            Debug.Log("Starting listen for udp datagrams");
            //debug to text
            _outputText.text += "\n-> Stated listen for udp datagrams";

            // since this will run in a internal async loop, not needed to be called again
            _recievingUdpDatagrams = true;
            //starting receiving datagrams async
            _UdpListener.BeginReceive(new AsyncCallback(RecievingDatagramCallback),
                _UdpListener);
        }

        // tcp data
        // if player has data available here
        if (_player.DataAvailabe())
        {
            // debug to text
            _outputText.text = "data available on TCP stream";
            // multiple types of packets should be loaded here
            // setObj packet, controls the instanciation of gameObjects

            // read the data
            Packet recievedPacket = _player.ReadPacket();
            // if the packet is a setobjpacket, the desc tells what obj should be spawned
            if (recievedPacket.PacketType == PacketType.SetObjPosition)
            {
                // debug to text
                _outputText.text += "\n-> new spawn recieved";

                // save the packet
                _player.PlayerPackets.Add(recievedPacket);
                // if the recieve packet has is des player
                if (recievedPacket.Desc == "player")
                {
                    // debug to text
                    _outputText.text += "\n-> Spawning new player";

                    // client should spawn a player game Obj
                    //add the game obj to the dictionary of spawnable players
                    _spawnedPlayers.Add(recievedPacket.PlayerGUID,
                        Instantiate(playerGO).GetComponent<PlayerScript>());

                    // check if the obj the local player
                    // for setting the player color, and name on the obj
                    if (recievedPacket.PlayerGUID == _player.Id)
                    {
                        //set the spawned player info
                        _spawnedPlayers[_player.Id].OnPlayerSpanw(_player.Id,
                            _player.Name, _player.PlayerNumber,
                            _player.PlayerColor, true, this);

                        // set the position of the spawned local player
                        _spawnedPlayers[recievedPacket.PlayerGUID].SetTransform(
                            recievedPacket.PlayerPosition.X, recievedPacket.PlayerPosition.Y, 0f);
                    }
                    // if not, then the spawned player is the opponent
                    else if (recievedPacket.PlayerGUID == _opponentPlayer.Id)
                    {
                        // set the spawned player info
                        _spawnedPlayers[_opponentPlayer.Id].OnPlayerSpanw(_opponentPlayer.Id,
                            _opponentPlayer.Name, _opponentPlayer.PlayerNumber,
                            _opponentPlayer.PlayerColor,
                            false, this);

                        // set the position of the spawned local player
                        _spawnedPlayers[recievedPacket.PlayerGUID].SetTransform(
                            recievedPacket.PlayerPosition.X, recievedPacket.PlayerPosition.Y, -180f);
                    }

                }

            }
            // if is as packet for spawn obj
            else if (recievedPacket.PacketType == PacketType.SpawnObj)
            {
                // add the recieved packet to the packet list
                _player.PlayerPackets.Add(recievedPacket);

                // if is a packet for spawning a obj
                // instanciate the new obj with the properties recied from the server
                _inGameCutlery = Instantiate(CutlerPrefab,
                    new Vector3(recievedPacket.ObjPosition.X, recievedPacket.ObjPosition.Y, 0f),
                    Quaternion.identity);

                // change the sprite to the value from the server
                _inGameCutlery.GetComponent<SpriteRenderer>().sprite =
                    CutlerySprites[recievedPacket.ObjSprite];
            }
            // if is as player socre packet
            else if (recievedPacket.PacketType == PacketType.PlayerScore)
            {
                // check the id of the player who scored
                if (recievedPacket.PlayerGUID == _player.Id)
                {
                    // save the packet
                    _player.PlayerPackets.Add(recievedPacket);
                    // save the socre localy
                    _player.AddPlayerScorePoint();
                    // update the score to the player displayer
                    _spawnedPlayers[_player.Id].UpdateScore(_player.PlayerScore);
                }
                else if (recievedPacket.PlayerGUID == _opponentPlayer.Id)
                {
                    // save the packet to the packet list
                    _player.PlayerPackets.Add(recievedPacket);
                    // save the score localy
                    _opponentPlayer.AddPlayerScorePoint();
                    // update the score to the player displayer
                    _spawnedPlayers[_opponentPlayer.Id].
                        UpdateScore(_opponentPlayer.PlayerScore);
                }
            }
            // if is a reset player position 
            else if (recievedPacket.PacketType == PacketType.ResetPlayerPosition)
            {
                //  if the packet is a reset position
                // saven the packet to the player packet list
                _player.PlayerPackets.Add(recievedPacket);

                // set the player to te reset position
                // setting the position
                Vector3 playerNewPosition =
                    new Vector3(recievedPacket.PlayerPosition.X,
                    recievedPacket.PlayerPosition.Y, 0f);
                // set the position to the new one
                _spawnedPlayers[_player.Id].transform.position = playerNewPosition;
            }
            // if a detroy obj packet
            else if (recievedPacket.PacketType == PacketType.DestroyObj)
            {
                // if is a packet to destroy the obj
                //save the packet to the packet player list
                _player.PlayerPackets.Add(recievedPacket);
                //destroy obj in game
                Destroy(_inGameCutlery);
                // set the var to null
                _inGameCutlery = null;
            }

        }
    }
    #endregion

    #region Network Player Actions


    // method called by local player to send the new position
    public void SendPlayerPosUdp(float x, float y, float rotY)
    {
        // send the new player position to the server via udp
        // debug
        _outputText.text = "-> Sending player position using Udp";

        // setting the packet to send
        Packet updatePosPacket = new Packet();
        // set the packet type to player position
        updatePosPacket.PacketType = PacketType.PlayerPosition;

        // seting the new values on packet information
        Position pos = new Position();
        pos.X = x;
        pos.Y = y;
        //adding to the packet
        updatePosPacket.PlayerPosition = pos;

        //setting the values for player rotation
        Rotation rot = new Rotation(0f, rotY, 0f);
        // add the rotation to the packet
        updatePosPacket.ObjRotation = rot;

        // adding the player id to the packet, its allways the local player since this method will only be called
        // if is local
        updatePosPacket.PlayerGUID = _player.Id;

        // send packet using upd
        _player.SendPacketUdp(updatePosPacket);

        // adding packet to player packetlist
        _player.PlayerPackets.Add(updatePosPacket);

        // debug to text
        _outputText.text += "\n-> Packet sented using udp";
    }

    //method called by local to preform a action
    public void RequestPlayerAction()
    {
        // setting up the packet to send
        Packet requestActionPacket = new Packet();
        // setting the packet type
        requestActionPacket.PacketType = PacketType.PlayerAction;

        // saving the packet on player actions
        _player.PlayerPackets.Add(requestActionPacket);

        // send to the server
        _player.SendPacket(requestActionPacket);

        // output to the console and output text
        Debug.Log("Player action");
        _outputText.text += "\n-> Player action requested";

    }
    #endregion


}

