using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cutlery.Com;
using UnityEngine.UI;


public class PlayerScript : MonoBehaviour
{
    // vars inside each spawned player on client
    // player Guid
    private Guid _playerId;
    // player name
    private string _playerName;
    // player color
    private Color _playerColor;
    //player Number
    private int _playerNumber;
    // bool that indicates if is a local player
    private bool _isLocal;

    [Header("References for player")]
    // references for transform, rigidBody, text
    public Transform _playerTransform;          //player transfor
    public Transform _spriteTransform;          //sprite transform
    public Rigidbody2D _playerRigidbody;        // local player rigidbody
    public Text _playernameText;                // player name text
    public RawImage _playerColorImage;          // player color
    public SpriteRenderer _playerRenderer;      // player sprite renderer

    private Text _ScoreDisplayer;       // reference to goals objs
    private Text _scoreNameDisplayer;   // reference to the goal player name

    // reference to the endScreen Obj
    private EndGameScreenController _endScreenController;

    // reference to the networkController
    private ClientNetworkController _clientNet;

    [Header("Values for movement")]
    // public vars for movement
    public float accelaration = 10f;
    public float jumpInpulse = 10f;

    // player jump state
    private bool _isGrounded;

    // old value of postion
    private Vector2 _oldPosition;

    // latest jump button state
    private float _lateJumpState;


    // func is called when a new player is spawned
    public void OnPlayerSpanw(Guid pId, string pName, int pNum, CutleryColor pColor,
        bool isLocal, ClientNetworkController clientNetwork)
    {
        // save on func vars the id, name and color of the spawned player
        _playerId = pId;
        _playerName = pName;
        _playerColor = new Color(pColor.R, pColor.G, pColor.B);
        _playerNumber = pNum;

        //the network manager defines if this player is the local player
        _isLocal = isLocal;


        // set the components to the info passed
        _playernameText.text = _playerName;
        _playerColorImage.color = _playerColor;

        // load reference for the player score
        if (_playerNumber == 1)
        {
            // if player is the number 1 player
            // find and save the reference to the score displayer
            _ScoreDisplayer = GameObject.Find("Player1 scoreText").GetComponent<Text>();
            // find and save the reference to the score name displayer
            _scoreNameDisplayer = GameObject.Find("Player1ScoreName").GetComponent<Text>();
        }
        // if is the player 2
        else if (_playerNumber == 2)
        {
            Debug.Log("Player 2 setting goal box");
            //if player is the number 2 player
            // find and save the reference to the score displayer
            _ScoreDisplayer = GameObject.Find("Player2 scoreText").GetComponent<Text>();
            // find and save the reference to the score name displayer
            _scoreNameDisplayer = GameObject.Find("Player2ScoreName").GetComponent<Text>();
        }
        // set the player name and score
        _ScoreDisplayer.text = "0";
        _scoreNameDisplayer.text = _playerName;

        // if is not a local , all the positions are controlled from the server
        if (!_isLocal)
        {
            // remove the rigidbody and the boxColider
            Destroy(_playerRigidbody);
            Destroy(GetComponent<BoxCollider2D>());

            // change the transparency of the nonLocal player
            _playerRenderer.color = new Color(_playerRenderer.color.r,
                _playerRenderer.color.g, _playerRenderer.color.b, 0.5f) * _playerColor;

        }
        else
        {
            //save the controller
            _clientNet = clientNetwork;
            // saving the old states
            _oldPosition = _playerTransform.position;
            // setting the jump state
            _lateJumpState = 0;
            // get the reference to the endGameScreenController
            _endScreenController = GameObject.Find("EndGameScreen").GetComponent<EndGameScreenController>();

        }
    }


    #region SetVars
    // func called when a new position is defined
    public void SetTransform(float x, float y, float rotY)
    {

        // set the position
        _playerTransform.position = new Vector3(x, y, 0.0f);
        // seting the new sprite rotation
        _spriteTransform.rotation = new Quaternion(0f, rotY, 0f, 0f);

    }

    // func called when the score changes
    public void UpdateScore(int score)
    {
        // change the score count abouve the goal
        _ScoreDisplayer.text = score.ToString();
    }
    #endregion

    // method called when a player wins
    public void PlayerWonCall(string winnerName) => _endScreenController.DisplayerEndScreen(winnerName);
    private void Update()
    {
        // check if the position is diferent from the old one
        // check if this is the local player
        if (_isLocal)
        {
            // set the player position on a vector 2
            // for evaluation
            Vector2 playerPos = new Vector2(_playerTransform.position.x,
                _playerTransform.position.y);

            // check if the player moved
            if (playerPos != _oldPosition)
            {
                // send new position to the server
                // call the method to send data over udp
                _clientNet.SendPlayerPosUdp(playerPos.x,
                    playerPos.y, _spriteTransform.rotation.y);

                // set the old position equal to the new one
                _oldPosition = playerPos;
            }
        }
    }

    private void FixedUpdate()
    {
        // this will handle the local player input
        if (_isLocal)
        {
            // move check
            // if this value is diferent from 0 then the player is pressing the move keys
            Vector2 moveVec = Vector2.right * Input.GetAxis("Horizontal") *
                accelaration;

            // aply the force to the rigidBody
            _playerRigidbody.AddForce(moveVec);

            // set the player spriteRotation with base on the moveVec
            if (moveVec.x > 0)  // if movevec x is bigger than 0
                // rotate the sprite to the right
                _spriteTransform.rotation = new Quaternion(0f, 0f, 0f, 0f);
            else if (moveVec.x < 0)
                // rotate the sprite to the right
                _spriteTransform.rotation = new Quaternion(0f, -180f, 0f, 0f);


            // check for jump state
            if (_isGrounded && Input.GetAxis("Vertical") > 0)
            {
                // calculate the force to add when jump
                Vector2 jumpVec = Vector2.up * jumpInpulse;
                // add force to rigidBody
                _playerRigidbody.AddForce(jumpVec);
            }

            // check if the player pressed the action key
            if (_lateJumpState == 0 && Input.GetAxis("Jump") > 0)
            {
                //debug the call
                Debug.Log("Action button call");
                // prefor the action request
                _clientNet.RequestPlayerAction();
            }

            // save the jumpState value
            _lateJumpState = Input.GetAxis("Jump");
        }

    }


    #region COllisionChecks
    // check collisions
    // when entering
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // if player is colliding with the ground
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && _isLocal)
            _isGrounded = true;
    }
    //when exiting
    private void OnTriggerExit2D(Collider2D collision)
    {// if player is colliding with the ground
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && _isLocal)
            _isGrounded = false;

    }
    #endregion
}
