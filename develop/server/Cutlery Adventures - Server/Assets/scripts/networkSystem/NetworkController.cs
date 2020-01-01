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
            if (_connectedPlayers.Count == 2 && CheckAllPlayersWaiting())
            {
                // Debug that all players are waiting for other player and there is
                // 2 players connect
                Debug.Log( _connectedPlayers.Count +" players connected and all synced");
                Console.Write(_connectedPlayers.Count + " players connected and synced", Color.yellow);

                // change the serve to server started the match
                _serverState = ServerState.ServerStartedMatch;
            }


        }
        // if server has started the match
        else
        {

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

    #endregion

    // internal methods
    private bool CheckAllPlayersWaiting()
    {
        // define the var for hold if all players are waiting
        bool validator = false;
        // for each connected player
        foreach (Player p in _connectedPlayers)
        {
            // check if player is on wait state
            validator = (p.GameState == GameState.WaitPlayer) ? true : false;
            // if not in waiting state break the loop
            if (!validator) break;
        }
        // retur the value
        return validator;
    }
}







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
