using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjController : MonoBehaviour
{
    // reference to the NetworkController
    private NetworkController _netController;

    // internal var for looking for updates positions
    private Vector2 _oldPosition;

    private void Awake()
    {
        // find the references
        _netController = GameObject.Find("ServerController").GetComponent<NetworkController>();
    }

    private void Update()
    {
        // will check if the obj has moved
        Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
        if (_oldPosition != currentPos)
        {
            // call method to send the new position
            _netController.UpdateObjPosition(currentPos.x, currentPos.y,
                transform.rotation.eulerAngles.z);
            _oldPosition = currentPos;
        }

    }

    public void AddForce(Vector2 force)
    {
        // add the passed force to the obj
        GetComponent<Rigidbody2D>().AddRelativeForce(force);
        GetComponent<Rigidbody2D>().AddForce(Vector2.up * 50);
        // adding rotation force for more rotation
        if (force.x > 0)
            GetComponent<Rigidbody2D>().AddTorque(400f);
        else
            GetComponent<Rigidbody2D>().AddTorque(-400f);
    }
}
