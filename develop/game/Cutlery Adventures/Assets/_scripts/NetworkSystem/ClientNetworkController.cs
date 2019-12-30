using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using Cutlery.Com;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;

public class ClientNetworkController : MonoBehaviour
{
    // script that controll all the network information
    // reference to output console
    private Text _outputText;

    // private vars
    // players 
    private Player _player;
    private Player _opponentPlayer;

    //reference to the network Task
    private Task _asyncNetworkTask;
    // since unity doesnt allow unity functions been called from other threads
    private List<Action> _actions;

    // vars for connection 
    // tcpClient and UDPclient
    private TcpClient _tcpClient;
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
        _tcpClient = new TcpClient();
    }

    //update method
    private void Update()
    {
        // if the connection thread from the client queued actions to clear
        // do all the actions
        if (_actions.Count > 0)
            StartCoroutine(DoQueuedActions());
    }

    #region Coroutines
    //Coroutine
    // Coroutine for clear actions from the other thread
    private IEnumerator DoQueuedActions()
    {
        // create a new list of actions to do
        // used to not blocking the main queue whyle crear the actions
        List<Action> actionsTODO;
        //lock the used _actions
        lock (_actions)
        {
            // copy all the queued actions
            actionsTODO = new List<Action>(_actions);
            // clear the queued actions
            _actions.Clear();
        }
        // execute all actions
        actionsTODO.ForEach(action => action());
        yield return null;
    }
    #endregion

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
        try
        {
            _tcpClient.BeginConnect(_IpAdress, _serverTcpPort, BeginConnectionToServer, _tcpClient);
            // write to outputView that client started connecting
            _outputText.text += "\n-> Begin Connect";
        }
        catch (Exception ex) { _outputText.text = ex.ToString(); }
    }
    // async method for setting a connection
    private void BeginConnectionToServer(IAsyncResult async)
    {
        //write to outputView
        _actions.Add(() => _outputText.text += "\n-> Connecting..");

        // stores the tcp client
        TcpClient tcpClient = (TcpClient)async.AsyncState;
        // stop the pending async connection
        tcpClient.EndConnect(async);

        // check if the player is connected to the server
        if (tcpClient.Connected)
        {
            // write to outputView
            _actions.Add(() => _outputText.text += "\n-> Client Connected, wait request...");

            // extract the tcpClient from the async var
            // store on the player tcpVar
            _player.TcpClient = tcpClient;
            // change the player state to connectiong
            _player.GameState = GameState.Connecting;
            // iniciate the packet list
            _player.PlayerPackets = new List<Packet>();

            // call that handles the network side
            ClientNetworkAsync();
        }
        else
        { _actions.Add(() => _outputText.text = "-> Connection refused!"); }
    }

    // async client connection handler
    private async void ClientNetworkAsync()
    {
        // run on a new therad the connection state
        await Task.Run(() =>
        {
            // output to text that the async client has started
            _actions.Add(() => _outputText.text += "\n -> AsyncNetwok started...");

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
                    default:
                        break;
                }
            }
        });
    }

}
