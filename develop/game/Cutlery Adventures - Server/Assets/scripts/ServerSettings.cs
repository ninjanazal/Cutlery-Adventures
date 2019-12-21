using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSettings : MonoBehaviour
{
    // target serser tickRate
    public int _targetTickRate = 32;

    //define tickRate on Aplication
    private void Start()
    {
        Application.targetFrameRate = _targetTickRate;
        Console.Write("Server TRate= " + _targetTickRate);
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

