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

    // on awake
    private void Awake()
    {
        // keep the NetworkController when load sceens
        // since the scene will change, need to keep information
        // from the menu, all the connection state will be handle here
        UnityEngine.Object.DontDestroyOnLoad(this);

        //inicialize the list of actions
        _actions = new List<Action>();

        // reference to outputText
        _outputText = GetComponentInChildren<Text>();
        _outputText.text = "This is a text, im working";

        // setting up the players
        // local player
        _player = new Player();
        // opponent player
        _opponentPlayer = new Player();

        //define the player to a disconnected State
        _player.GameState = GameState.Disconnected;
        // define the internal TcpClient
        _player.TcpClient = new TcpClient();
    }

    //update method
    private void Update()
    {
        // if the connection thread from the client queued actions to clear
        // do all the actions
        if (_actions.Count > 0)
            DoQueuedActions();
    }

    //Coroutine
    // Coroutine for clear actions from the other thread
    private void DoQueuedActions()
    {
        // create a new list of actions to do
        // used to not blocking the main queue whyle crear the actions
        List<Action> actionsTODO;
        //lock the used _actions

        // copy all the queued actions
        actionsTODO = new List<Action>(_actions);
        // clear the queued actions
        _actions.Clear();

        // execute all actions
        foreach (Action action in actionsTODO) { action(); Debug.Log("Cleared task"); }
    }

    // method called when the player wants to connect to the server
    // when the connect button is pressed
    public void StartConnectionToServer(string serverIp, string playerName)
    {
        //Write to outputView
        _outputText.text = $"->Try connection to: {serverIp}:{_serverTcpPort}";

        // store the ip from string to IPAdress var
        _IpAdress = IPAddress.Parse(serverIp);
        // saving the player name
        _playerName = playerName;
        // call the func begingConnect to start async connection
        // try to start connection

        _player.TcpClient.BeginConnect(_IpAdress, _serverTcpPort, BeginConnectionToServer,
            _player.TcpClient);
        // write to outputView that client started connecting
        _outputText.text += "\n-> Begin Connect";

    }
    // async method for setting a connection
    private void BeginConnectionToServer(IAsyncResult async)
    {
        // stores the tcp client
        TcpClient client = (TcpClient)async.AsyncState;
        // stop the pending async connection
        client.EndConnect(async);

        // check if the player is connected to the server
        if (client.Connected)
        {
            // debug to file
            Debug.Log("Connected");
            // write to outputView

            _outputText.text += "\n-> Client Connected, wait request...";
            _outputText.text += "\n-> Saved tcpclient";

            // define reader of player
            _player.PlayerReader = new BinaryReader(client.GetStream());
            // define write of the player
            _player.PlayerWriter = new BinaryWriter(client.GetStream());

            // change the player state to connectiong
            _player.GameState = GameState.Connecting;
            _outputText.text += "\n-> Change player state";

            // iniciate the packet list
            _player.PlayerPackets = new List<Packet>();
            _outputText.text += "\n-> Start packet list";

            // print that the tcp is connected
            _outputText.text += "\n ->PlayerConnected: " + _player.TcpClient.Connected.ToString();

            Debug.Log("Got connection data");
            // call client async loop started
            // add client loop start to actions queue
            ClientLoopAsync();

        }
        else
        { _outputText.text = "-> Connection refused!"; }
    }

    private async void ClientLoopAsync()
    {
        // print that clientLoop will start
        _outputText.text += "\n Client async loop starting.";

        // this func will run on a new thread
        await Task.Run(ClientNetworkAsync);
    }

    // async client connection handler
    private void ClientNetworkAsync()
    {
        // debug that the async method has been called
        _outputText.text += "\n-> Async method called";

        // output to text that the async client has started
        _outputText.text += "\n -> AsyncNetwok started...";

        // this loop will run whyle the client is connected
        while (_player.TcpClient.Connected)
        {
            // for each connected state
            switch (_player.GameState)
            {
                case GameState.Connecting:
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

}
