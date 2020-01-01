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

public class ClientNetworkController : MonoBehaviour
{
    // reference to menuController script
    private MenuController _menu;

    // script that controll all the network information
    // reference to output console
    private Text _outputText;

    // async tasks handler
    private Queue<Action> _asyncActions;

    // private vars
    // players 
    private Player _player;
    private Player _opponentPlayer;

    // since unity doesnt allow unity functions been called from other threads
    private List<Action> _actions;

    // vars for connection 
    // tcpClient and UDPclient
    private UdpClient _udpClient;

    // IpAddress var
    private IPAddress _IpAdress;

    // ports for connection
    private int _serverTcpPort = 7701, _serverUdpPort = 7702;

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

        // setting the players
        // local player
        _player = new Player();
        // opponent player
        _opponentPlayer = new Player();

        // setting player to a disconnected state
        _player.GameState = GameState.Disconnected;

        // debug into text
        _outputText.text = "Player networkController is awaked";
        // after start the player connection state is disconnected
    }

    // network update
    private void Update()
    {
        // if the player was connecting or trying
        if (_player.GameState != GameState.Disconnected)
        {
            // check server recieved data
            PlayerConnectionLoop();
        }

        // after all logic, execut the actions queued by the asyunc method
        AsyncActionsClear();
    }

    // function that handles the player connection part while connected
    private void PlayerConnectionLoop()
    {
        // if the player is connected
        if (_player.TcpClient.Connected)
        {
            Debug.Log("Player Is connected");
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
                    break;
                case GameState.CountDown:
                    break;
                case GameState.GameStarted:
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

    // execute queued asyncAction
    private void AsyncActionsClear()
    {
        //if exists any queued action 
        Debug.Log("AsyncActions Queued: " + _asyncActions.Count);
        while (_asyncActions.Count > 0)
        {
            // execute the oldest action in queue
            _asyncActions.Dequeue()();
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
                Debug.Log(recievedPacket.ObjColor.B);

                // show the packet desc
                _outputText.text += "\n-> " + recievedPacket.Desc;

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

    #endregion
}

