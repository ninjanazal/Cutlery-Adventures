﻿using System;
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

    // func is called when a new player is spawned
    public void OnPlayerSpanw(Guid pId, string pName, CutleryColor pColor, bool isLocal, ClientNetworkController clientNetwork)
    {
        // save on func vars the id, name and color of the spawned player
        _playerId = pId;
        _playerName = pName;
        _playerColor = new Color(pColor.R, pColor.G, pColor.B);

        //the network manager defines if this player is the local player
        _isLocal = isLocal;

        // set the components to the info passed
        _playernameText.text = _playerName;
        _playerColorImage.color = _playerColor;

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
        }
    }


    #region SetVars
    // func called when a new position is defined
    public void SetTransform(float x, float y, float rotY)
    {
        // if this method is called on the local player
        if (_isLocal)
        {
            // set the position
            _playerTransform.position = new Vector3(x, y, 0.0f);
            // seting the new sprite rotation
            _spriteTransform.rotation = new Quaternion(0f, rotY, 0f, 0f);
        }
    }

    #endregion


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
                // debug  that a new position will be sent
                Debug.Log("New position, sending new data via udp");
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


            Debug.Log(_isGrounded);
            // check for jump state
            if (_isGrounded && Input.GetAxis("Jump") != 0)
            {
                // calculate the force to add when jump
                Vector2 jumpVec = Vector2.up * jumpInpulse;
                // add force to rigidBody
                _playerRigidbody.AddForce(jumpVec);
            }
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
