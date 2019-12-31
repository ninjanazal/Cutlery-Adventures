using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSettings : MonoBehaviour
{
    // target serser tickRate
    public int _targetTickRate = 32;

    // networkController
    private NetworkController _networkController;

    // reference to threadBlock image
    private RectTransform _checKThreadBlock;

    //define tickRate on Aplication
    private void Start()
    {
        // getting the componnent
        _checKThreadBlock = GameObject.Find("checkThreadBlock").
            GetComponent<RectTransform>();

        //setting the target rate
        TargetTickRate = _targetTickRate;

        // start network Controller
        _networkController = GetComponent<NetworkController>();
        _networkController.StartServer();
    }

    private void Update()
    {
        // rotate the image, if jumps is because the thread was blocked
        _checKThreadBlock.Rotate(new Vector3(0f, 0f, -40f) * Time.deltaTime);
    }
    //if server tick is changed, the tick rate is updated
    public int TargetTickRate
    {
        get => _targetTickRate;
        set
        {
            _targetTickRate = value;
            // Disable vSync on server
            QualitySettings.vSyncCount = 0;
            // set the target rate
            Application.targetFrameRate = _targetTickRate;
            Console.Write("Server TRate= " + Application.targetFrameRate);
        }

    }
}

