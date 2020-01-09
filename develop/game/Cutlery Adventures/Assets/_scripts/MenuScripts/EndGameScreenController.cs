using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameScreenController : MonoBehaviour
{
    // references to the objects
    private Image _endGameBackground;
    private Text _winnerPlayerName;
    private Text _fillText;
    private Button _exitBtn;

    // set references on awake
    private void Awake()
    {
        // reference to the background image
        _endGameBackground = GameObject.Find("EndGameBackground").GetComponent<Image>();
        // reference to the text that displays the winner name
        _winnerPlayerName = GameObject.Find("WinnerPlayerName").GetComponent<Text>();
        // reference to the fill text
        _fillText = GameObject.Find("WinText").GetComponent<Text>();
        // reference to the exit button
        _exitBtn = GameObject.Find("ExitMatchBtn").GetComponent<Button>();

        // disable all the componnents
        ChangeComponnentsTo(false);
    }

    // method to enable or disable the components
    private void ChangeComponnentsTo(bool state)
    {
        // change the background state
        _endGameBackground.gameObject.SetActive(state);
        // change the winner player name state
        _winnerPlayerName.gameObject.SetActive(state);
        // change the fill text state
        _fillText.gameObject.SetActive(state);
        //change the exit button state
        _exitBtn.gameObject.SetActive(state);
    }

    // public method called to displayer this screen
    public void DisplayerEndScreen(string winnerName)
    {
        // when this method is called, display all the information needed
        // set the player name
        _winnerPlayerName.text = winnerName;
        //show the UI
        ChangeComponnentsTo(true);
    }

    // exit button callback
    public void ExitButtonCallback()
    {
        // when pressed the button
        StartCoroutine(LoadMainMenuScene());
    }

    private IEnumerator LoadMainMenuScene()
    {
        // begin to load the scene
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        // disable the activation of this scene
        asyncOperation.allowSceneActivation = false;

        // is loaded bool
        bool isLoaded = false;

        // while the loading is not ready
        while (!isLoaded)
        {
            yield return null;

            float progress = asyncOperation.progress;
            // check if the progress is bigger than 0.9
            // if soo, define the isLoaded to true
            if (progress >= 0.9f)
                isLoaded = true;

        }
        // after async load, displayer the new scene
        asyncOperation.allowSceneActivation = true;
    }
}
