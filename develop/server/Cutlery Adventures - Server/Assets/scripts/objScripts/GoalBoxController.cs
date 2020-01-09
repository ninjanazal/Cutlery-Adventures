using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalBoxController : MonoBehaviour
{
    // reference to the NetworkController
    private NetworkController _netController;
    // number of the player
    private int _playerNum;

    private void Awake()
    {
        // find the reference to the networkController
        _netController = GameObject.Find("ServerController").GetComponent<NetworkController>();
        // get the player num by the name of the game obj
        if (this.gameObject.name == "Player1Goal")
            _playerNum = 1;
        else if (this.gameObject.name == "Player2Goal")
            _playerNum = 2;

    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        // check if is the cutlery obj
        if (collision.tag.Contains("Cutlery"))
        {
            // debug
            Debug.Log("Cutlery on goal " + _playerNum);
            // if so, the obj is in the player goal
            _netController.PlayerScore(_playerNum);
        }
    }
}
