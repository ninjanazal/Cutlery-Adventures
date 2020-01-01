using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionScreen : MonoBehaviour
{
    // vars for ui elements
    // reference to canvas
    private Canvas _connectionDisplayerCanvas;

    // reference to local player elements
    private Text _playerName;
    private SpriteRenderer _playerChar;
    private Image _playerColorImage;

    // reference to local opponent elements
    private Text _opponentName;
    private SpriteRenderer _opponentChar;
    private Image _opponentColorImage;

    // reference to loading image
    private GameObject _loadIamgeGObj;
    private RectTransform _loadImageTransform;
    private Image _loadImage;

    // this method is called from the menu controller
    // after awake logic
    public void Awake() => ManageReferences();

    // reference manager
    private void ManageReferences()
    {
        // set references to the objectes
        // reference to canvas
        _connectionDisplayerCanvas = GameObject.Find("ConnectionDisplayer").GetComponent<Canvas>();
        // reference to local player vars
        //reference to local player name
        _playerName = GameObject.Find("PlayerName").GetComponent<Text>();
        // reference to local player sprite renderer
        _playerChar = GameObject.Find("playerChar").GetComponent<SpriteRenderer>();
        // reference to local player color
        _playerColorImage = GameObject.Find("playerColor").GetComponent<Image>();

        // reference to opponent elements
        // reference to opponent player name
        _opponentName = GameObject.Find("opponnentName").GetComponent<Text>();
        // reference to opponent sprite renderer
        _opponentChar = GameObject.Find("opponentPlayerImage").GetComponent<SpriteRenderer>();
        // reference to opponent color
        _opponentColorImage = GameObject.Find("opponnentColor").GetComponent<Image>();

        // reference to loadImage game object
        _loadIamgeGObj = GameObject.Find("loadingImage");
        // store Rect transform of loadImage
        _loadImageTransform = _loadIamgeGObj.GetComponent<RectTransform>();
        // reference to loadImage image
        _loadImage = _loadIamgeGObj.GetComponent<Image>();

        // disable elements of the opponent
        // disable name text
        _opponentName.enabled = false;
        // disable color displayer
        _opponentColorImage.enabled = false;
        // disable char image
        _opponentChar.enabled = false;

        // disable the elements of the local player
        //disable name text
        _playerName.enabled = false;
        //disable color displayer
        _playerColorImage.enabled = false;
        //disable char image
        _playerChar.enabled = false;

        // disable the canvas 
        _connectionDisplayerCanvas.enabled = false;

        // disable the load image gameObject
        _loadIamgeGObj.SetActive(false);
    }

    private void Update()
    {
        // just for visual style, the loading image will rotate if showned
        // if the loadImage is active
        if (_loadIamgeGObj.activeSelf)
            _loadImageTransform.Rotate(new Vector3(0f, 0f, 50f) * Time.deltaTime);
    }

    // public functions that react to player state
    // called when client is connected to the server
    public void LocalClientConnected(string name, Color color)
    {
        // func called when client is connected to the server
        // start showing the connection canvas overlay
        _connectionDisplayerCanvas.enabled = true;

        // enable local player name
        _playerName.enabled = true;
        // set the player name
        _playerName.text = name;

        // enable local player color
        _playerColorImage.enabled = true;
        // set the player color
        _playerColorImage.color = color;

        // enable the player char image
        _playerChar.enabled = true;

        // since the player is connected , 
        // show the waiting the other player
        _loadIamgeGObj.SetActive(true);
    }

    // called when client got an opponent
    public void OpponentClientConnected(string name, Color color)
    {
        // func called when client got an opponent
        // disabling the loading image
        _loadIamgeGObj.SetActive(false);

        // enable opponent player name
        _opponentName.enabled = true;
        // set the player name
        _opponentName.text = name;

        // enable the opponent player color
        _opponentColorImage.enabled = true;
        // set the opponent color
        _opponentColorImage.color = color;

        // enable the oponnent har image
        _opponentChar.enabled = true;
    }
}
