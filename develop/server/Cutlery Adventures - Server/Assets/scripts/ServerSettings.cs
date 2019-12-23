using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSettings : MonoBehaviour
{
    // target serser tickRate
    public int _targetTickRate = 32;

    // networkController
    private NetworkController _networkController;

    //define tickRate on Aplication
    private void Start()
    {
        Application.targetFrameRate = _targetTickRate;
        Console.Write("Server TRate= " + _targetTickRate);

        // start network Controller
        _networkController = GameObject.Find("networkController").GetComponent<NetworkController>();
        _networkController.StartServer();
    }

    //if server tick is changed, the tick rate is updated
    public int TargetTickRate
    {
        get => _targetTickRate;
        set
        {
            _targetTickRate = value;
            Application.targetFrameRate = _targetTickRate;
            Console.Write("Server TRate= " + _targetTickRate);
        }

    }
}

