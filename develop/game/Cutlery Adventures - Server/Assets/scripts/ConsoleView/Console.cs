using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Console : MonoBehaviour
{

    private static Queue<String> _consoleLines;
    private Canvas _consoleCanvas;
    private static Text _consoleText;

    private static Vector2 _maxBounds;


    // Set references for GO and start the lineList
    private void Awake()
    {
        _consoleCanvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        _consoleText = _consoleCanvas.GetComponentInChildren<Text>();
        _consoleLines = new Queue<string>();

        // clean console on start
        _consoleLines.Enqueue("");
        _maxBounds = new Vector2(); //iniciate vector for Bounds


        CalculateConsoleBounds();
        UpdateConsole();
    }

    #region private
    //private functions
    private static void UpdateConsole()
    {
        // update text on Console
        _consoleText.text = string.Concat(_consoleLines.ToArray());
        // force canvas update for results on the current frame
        Canvas.ForceUpdateCanvases();

        // if the numbers of lines is bigger then the lines that can be displayed
        // dequeue the oldest entry
        while (_consoleText.cachedTextGenerator.lineCount > _maxBounds.y)
        {
            _consoleLines.Dequeue();
            Canvas.ForceUpdateCanvases();
        }
    }

    private void CalculateConsoleBounds()
    {
        //calculate max chars per line
        _maxBounds.x = (int)(_consoleCanvas.GetComponent<RectTransform>().rect.width * 134) / 803;

        //calculate max lines 
        _maxBounds.y = (int)(_consoleCanvas.GetComponent<RectTransform>().rect.height * 50) / 602;
        Debug.Log(_maxBounds);
    }

    #endregion

    #region public

    //public methods
    /// <summary>
    /// Write on console
    /// </summary>
    /// <param name="msg"></param> Message to write
    public static void Write(string msg)
    {
        string _strToWrited = msg + "\n";
        _consoleLines.Enqueue(_strToWrited);
        UpdateConsole();
    }

    /// <summary>
    /// Write to console with color
    /// </summary>
    /// <param name="msg"></param> Text to show
    /// <param name="color"></param> Custom Color
    public static void Write(string msg, Color color)
    {
        string _hexColor = ColorUtility.ToHtmlStringRGB(color);

        // to use color on a line need to add <color="hexCode">msg</color>
        _consoleLines.Enqueue($"<color=#{_hexColor}>{msg}</color>\n");

        UpdateConsole();
    }

    #endregion
}
