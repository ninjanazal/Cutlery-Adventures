using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class Console : MonoBehaviour
{
    public static Queue<String> _consoleLines;
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
        _consoleLines.Clear();

        // call the method to calculate the text bounds
        CalculateConsoleBounds();

        // start async task for output to console text
        Task cleanerLines = ConsoleLineCleaner();

    }

    private void Update()
    {
        _consoleText.text = string.Concat(_consoleLines.ToArray());
    }

    #region private

    // async task for line number controll
    private async Task<bool> ConsoleLineCleaner()
    {
        // runs all the time
        while (true)
        {
            // if the numbers of lines is bigger then the lines that can be displayed
            // dequeue the oldest entry        
            while (_consoleLines.Count > _maxVisibleLines)
            {
                // dequeue the oldest line
                _consoleLines.Dequeue();
            }
            // runs the loop 1 time per x ms
            await Task.Delay(250);
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
        Write("-Console Size: " + _consoleCanvas.GetComponent<RectTransform>().rect.width
            + "*" + _consoleCanvas.GetComponent<RectTransform>().rect.height +
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
        // line to add to console
        _consoleLines.Enqueue($"{DateTime.Now.Ticks.ToString()}: {msg}\n");
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
        _consoleLines.Enqueue($"<color=#{_hexColor}>{DateTime.Now.Ticks.ToString()}" +
            $": {msg}</color>\n");
    }

    /// <summary>
    /// Clears console text
    /// </summary>
    public static void Clear()
    {
        _consoleLines.Clear();
    }
    #endregion

}
