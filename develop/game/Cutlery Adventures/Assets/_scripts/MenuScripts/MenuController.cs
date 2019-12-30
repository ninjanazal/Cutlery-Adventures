using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    // private vars
    // reference to buttons
    private Button _playBtn, _exitBtn, _connectBtn, _cancelBtn;
    // reference to input filed
    private Text _playerInputField, _serverIpInput;
    // popUp to connect
    private GameObject _connectionPopUp;

    // private marker if trying to connect
    private bool _tryingToConnect;

    // connection
    //reference to newtwork controller
    private ClientNetworkController _netWorkController;
    // player name from connection
    private string _playerName;
    // server Ip
    private string _serverIP;
    private void Awake()
    {
        // set try to connect to false
        _tryingToConnect = false;

        // call function that manage all the references
        ManageReferences();
    }

    private void Update()
    {
        // check if the connection popup is oppend
        // if so enable the connect button when the name field
        // is not empty
        if (_connectionPopUp.activeSelf && !_tryingToConnect)
        {
            // if the player press escape whyle popup on
            // as the same behabiour as cancle button
            if (Input.GetKeyDown(KeyCode.Escape))
                CancelBtnCallback();

            // if inputfield has text
            if (_playerInputField.text.Length > 0 &&
                _serverIpInput.text.Length > 0)
                _connectBtn.enabled = true;
            else
                _connectBtn.enabled = false;
        }
    }

    // buttons callbacks
    #region BtnCallbacks
    // Play Button
    public void PlayBtnCallback()
    {
        // when button is pressed, popup apears
        // disable the buttons playBtn and exit
        _playBtn.enabled = false;
        _exitBtn.enabled = false;

        // show the popup
        _connectionPopUp.SetActive(true);
        //disable connect button
        _connectBtn.enabled = false;
        //focus the input filed
        _playerInputField.gameObject.GetComponentInParent<InputField>().Select();
    }

    // exit button pressed
    public void ExitBtnCallback()
    {
        // exit the aplication
        Application.Quit();
    }

    // POPUP buttons
    //pressed the connect button of the popUp
    public void ConnectBtnCallback()
    {
        // define that the player is trying to connect
        _tryingToConnect = true;

        // if so, save the player name
        _playerName = _playerInputField.text;
        // get server ip from input
        _serverIP = _serverIpInput.text;

        // starting the server 
        // passing the server ip and the player name
        _netWorkController.StartConnectionToServer(_serverIP, _playerName);

        //disableConnectBtn
        _connectBtn.enabled = false;
        _cancelBtn.enabled = false;
    }

    // cancel play button
    public void CancelBtnCallback()
    {
        // if cancel is pressed, remove the popup
        _connectionPopUp.SetActive(false);
        // reactivate play and exit button
        _playBtn.enabled = true;
        _exitBtn.enabled = true;
    }
    #endregion

    private void ManageReferences()
    {
        // get reference to the popUp GameObject
        _connectionPopUp = GameObject.Find("ConnectPopup");
        // set reference to play button
        _playBtn = GameObject.Find("Play Btn").GetComponent<Button>();
        // set reference to exit button
        _exitBtn = GameObject.Find("Exit Btn").GetComponent<Button>();
        // reference to the Connect button
        _connectBtn = GameObject.Find("ConnectBtn").GetComponent<Button>();
        // reference to the cancel button
        _cancelBtn = GameObject.Find("CancelBtn").GetComponent<Button>();
        // reference to InputField
        _playerInputField = GameObject.Find("inputFieldPlayerName").GetComponent<Text>();
        // reference to server ip inputField
        _serverIpInput = GameObject.Find("inputFieldServer").GetComponent<Text>();

        // disable PopUp
        _connectionPopUp.SetActive(false);

        // set buttons active 
        _playBtn.enabled = true;
        _exitBtn.enabled = true;

        // reference to the networkController
        _netWorkController =
            GameObject.Find("NetworkController").GetComponent<ClientNetworkController>();
    }
}

