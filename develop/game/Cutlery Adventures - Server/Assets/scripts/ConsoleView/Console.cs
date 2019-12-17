using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class Console : MonoBehaviour
{

    private static Stack<String> _consoleLines;
    private Canvas _consoleCanvas;
    private Text _consoleText;

    private static int _maxConsoleLines;

    // Set references for GO and start the lineList
    private void Awake()
    {
        _consoleCanvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        _consoleLines = new Stack<string>();
        _consoleText = _consoleCanvas.GetComponentInChildren<Text>();
    }

    private static void UpdateConsole()
    {
        // if the numbers of lines is bigger then the lines that can be displayed
        // pop the oldest entry
        if (_consoleLines.Count > _maxConsoleLines)
        { _consoleLines.Pop(); }
    }

    /// <summary>
    /// Write on console
    /// </summary>
    /// <param name="msg"></param> Message to write
    public static void Write(string msg) { _consoleLines.Push(msg + "/n"); UpdateConsole(); }

    /// <summary>
    /// Write to console with color
    /// </summary>
    /// <param name="msg"></param> Text to show
    /// <param name="color"></param> Custom Color
    public static void Write(string msg, Color color)
    {
        // to use color on a line need to add <color="hexCode">msg</color>
        _consoleLines.Push($"<color=#" +
            $"{color.r.ToString("x")}{color.g.ToString("x")}{color.b.ToString("x")}>" +
            $"{msg}</color>/n");

        UpdateConsole();
    }

}
