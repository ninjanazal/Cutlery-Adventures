using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSettings : MonoBehaviour
{
    // target serser tickRate
    public int _targetTickRate = 0;

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

        QualitySettings.vSyncCount = 0;
        // set the target rate
        Application.targetFrameRate = _targetTickRate;

        // start network Controller
        _networkController = GetComponent<NetworkController>();
        _networkController.StartServer();
    }

    private void Update()
    {
        // rotate the image, if jumps is because the thread was blocked
        _checKThreadBlock.Rotate(new Vector3(0f, 0f, -40f) * Time.deltaTime);
    }
 
}

