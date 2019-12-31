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
    // script that controll all the network information
    // reference to output console
    private Text _outputText;

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

    // private name reference
    private string _playerName;

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
                    break;
                case GameState.Sync:
                    break;
                case GameState.WaitPlayer:
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
        // save the player name
        _playerName = playerName;
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

    // callback for the async beging connect
    private void ConnectionCallback(IAsyncResult ar)
    {
        // retrieve the tcp client from the async callback
        TcpClient client = (TcpClient)ar.AsyncState;
        // ends a pending asynchronous connection attempt
        client.EndConnect(ar);

        // debug to log
        Debug.Log("Async connection attempt stoped");
        // debug to text
        _outputText.text += "\n-> Async connection attempt stoped";

        // if the connection is established
        if (client.Connected)
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

    #region StateHandlers
    // handler for connecting state
    public void PlayerConnecting()
    {
        // checking if player has data to read
        if (_player.DataAvailabe())
        {
            //debug to log that has data to read
            Debug.Log("Player has data to read in connecting state");
            // debug to text
            _outputText.text += "\n-> Data available: Connecting state";
        }
    }

    #endregion
}

