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

    private static int _maxVisibleLines;

    // Set references for GO and start the lineList
    private void Awake()
    {
        _consoleCanvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        _consoleText = _consoleCanvas.GetComponentInChildren<Text>();
        _consoleLines = new Queue<string>();

        // clean console on start
        _consoleLines.Enqueue("");

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

        while (_consoleText.cachedTextGenerator.lineCount > _maxVisibleLines)
        {
            _consoleLines.Dequeue();
            Canvas.ForceUpdateCanvases();
        }
    }

    private void CalculateConsoleBounds()
    {
        //calculate max lines         
        _maxVisibleLines = (int)(_consoleCanvas.GetComponent<RectTransform>().rect.height /
            (_consoleText.font.lineHeight));

        // Console Debug Writing
        // write console specs 
        // write max visible lines
        Write("-Console Size: " + _consoleCanvas.GetComponent<RectTransform>().rect.height +
            " -Font size: " + _consoleText.fontSize + " -Line Height: " + _consoleText.font.lineHeight);
        Write("-visible Lines: " + _maxVisibleLines, Color.gray);
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

    /// <summary>
    /// Clears console text
    /// </summary>
    public static void Clear()
    {
        _consoleLines.Clear();
        UpdateConsole();
    }
    #endregion
}
