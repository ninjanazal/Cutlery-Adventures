using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerController : MonoBehaviour
{
    // reference to the trigger of the GameObject
    private BoxCollider2D _objColiider;

    // list of objs that the obj is colliding with
    private List<GameObject> _collidingObjs;
    private void Awake()
    {
        // inicialize the list as empty
        _collidingObjs = new List<GameObject>();
    }

    // when something enter the trigger
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // add that obj to the list
        _collidingObjs.Add(collision.gameObject);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        // when exits, remove from the list
        _collidingObjs.Remove(collision.gameObject);
    }

    // public methods to get the list of colliding objs
    public List<GameObject> GetObjOnTrigger()
    { return _collidingObjs; }
}
