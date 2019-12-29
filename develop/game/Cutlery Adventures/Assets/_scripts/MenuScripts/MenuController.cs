using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    // private vars
    // reference to buttons
    public Button _playBtn, _exitBtn, _connectBtn, _cancelBtn;
    // reference to input filed
    private Text _playerInputField;
    // popUp to connect
    private GameObject _connectionPopUp;
    // player name from connection
    private string _playerName;
    private void Awake()
    {
        // call function that manage all the references
        ManageReferences();
    }

    private void Update()
    {
        // check if the connection popup is oppend
        // if so enable the connect button when the name field
        // is not empty
        if (_connectionPopUp.activeSelf)
            if (_playerInputField.text.Length > 0)  // if inputfield has text
                _connectBtn.enabled = true;
            else
                _connectBtn.enabled = false;
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

        //disable connect button
        _connectBtn.enabled = false;

        // show the popup
        _connectionPopUp.SetActive(true);
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
        // if so, save the player name
        _playerName = _playerInputField.text;
    }
    // cancel play button
    public void CancelBtnCallback()
    {
        // if cancel is pressed, remove the popup
        _connectionPopUp.SetActive(false);
    }
    #endregion

    private void ManageReferences()
    {
        // get reference to the popUp GameObject
        _connectionPopUp = GameObject.Find("ConnectPopup");
        // disable PopUp
        _connectionPopUp.SetActive(false);
        // set reference to play button
        _playBtn = GameObject.Find("Play Btn").GetComponent<Button>();
        // set reference to exit button
        _exitBtn = GameObject.Find("Exit Btn").GetComponent<Button>();
        // reference to the Connect button
        _connectBtn = GameObject.Find("ConnectBtn").GetComponent<Button>();
        // reference to the cancel button
        _cancelBtn = GameObject.Find("CancelBtn").GetComponent<Button>();
        // reference to InputField
        _playerInputField = GameObject.Find("inputField").GetComponent<Text>();

        // set buttons active 
        _playBtn.enabled = true;
        _exitBtn.enabled = true;
    }
}
