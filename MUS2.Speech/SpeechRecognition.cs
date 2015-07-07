using MUS2.Hue;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Speech.Recognition;

namespace MUS2.Speech {

  public delegate void SpeechCmdDetectedHandler(string cmdText);

  //
  // Summary:
  //     Enables to control the hue using the speech recognition API (SAPI).
  //
  // Authors:
  //     Florentina Grebe
  //     Sabine Winkler
  //
  // Since:
  //     2015-07-08
  //
  public class SpeechRecognition {


    private SpeechCmdDetectedHandler speechCmdDetected;

    public event SpeechCmdDetectedHandler SpeechCmdDetected {
      add    { lock (this) speechCmdDetected += value; }
      remove { lock (this) speechCmdDetected -= value; }
    }

    private Color lampColor;
    private int brightness;

    private bool speechInitialized = false;
    private SpeechRecognizer recognizer;
    private Grammar grammar;

    private const bool   REGISTER_APP = false;
    private const string GRAMMAR_FILE = @"..\..\Grammar\Grammar.xml";

    #region color constants
    private const string RED   = "ff0000";
    private const string GREEN = "00cc00";
    private const string BLUE  = "0000ff";
    private const string LAMP  = "ff270d";
    #endregion 
    
    #region command constants
    private const string CMD_STOP  = "stop";
    private const string CMD_ON    = "on";
    private const string CMD_OFF   = "off";
    private const string CMD_RED   = "red";
    private const string CMD_GREEN = "green";
    private const string CMD_BLUE  = "blue";
    private const string CMD_LAMP  = "lamp";
    private const string CMD_ONE   = "one";
    private const string CMD_TWO   = "two";
    private const string CMD_THREE = "three";
    private const string CMD_COLOR = "color";
    #endregion


    // default constructor
    public SpeechRecognition() {

    }


    public bool EnableSpeech() {
      Debug.WriteLine("enabling speech ...");
      
      if (speechInitialized == false) {
        InitializeSpeechWithGrammarFile();
      }
      recognizer.Enabled = true;
      Debug.WriteLine("Recognition state is now: {0} ", recognizer.State);
      return true;
    }


    public bool DisableSpeech() {
      Debug.Assert(speechInitialized, "speech must be initialized in DisableSpeech");
      if (speechInitialized) {
        // Putting the recognition context to disabled state will 
        // stop speech recognition. Changing the state to enabled 
        // will start recognition again.
        recognizer.Enabled = false;
        Debug.WriteLine("Recognition state is now: {0} ", recognizer.State);
        Debug.WriteLine("disabling speech ...");
      }
      return true;
    }


    // Called during EnableSpeech()
    private void InitializeSpeechWithGrammarFile() {
      Debug.WriteLine("Initializing SAPI objects...");
      try {
        recognizer = new SpeechRecognizer();
        grammar = new Grammar(GRAMMAR_FILE);

        // Set event handler
        grammar.SpeechRecognized += grammar_SpeechRecognized;

        recognizer.LoadGrammar(grammar);
        speechInitialized = true;
      } catch (Exception e) {
        Debug.WriteLine(
            "Exception caught when initializing SAPI."
            + " This application may not run correctly.\r\n\r\n"
            + e.ToString(),
            "Error");
      }
    }

    private void FireSpeechCmdDetected(string cmdText) {
      if (speechCmdDetected != null) {
        speechCmdDetected(cmdText);
      }
    }

    public void grammar_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {

      Console.Write("I heard something...");
      IHueConnector hueConnector = HueConnectorFactory.GetHueConnector(REGISTER_APP);

      // show result on console
      this.ShowRecognitionResult(e);

      // our grammar is so simple, that we only have to consider two elements
      RecognitionResult result = e.Result;
      RecognizedWordUnit[] unit = e.Result.Words.ToArray();
      RecognizedWordUnit firstTerm;
      RecognizedWordUnit secondTerm;

      // ...Term will be the property of the recognized phrase. 
      firstTerm = unit[0];
      try {
        secondTerm = unit[1];
      } catch (Exception) {
        secondTerm = null;
      }

      string cmdText = "";

      // check, what has been said

      if (firstTerm != null) {

        switch (firstTerm.Text) {
          
          case CMD_STOP: {
            cmdText = CMD_STOP;
            Console.WriteLine(CMD_STOP + "\n...Disabling speech...");
            this.DisableSpeech();
            FireSpeechCmdDetected(cmdText);
            break;
          }
          case CMD_ON: {
            cmdText = CMD_ON;
            Console.WriteLine(cmdText);
            hueConnector.SwitchOn();
            FireSpeechCmdDetected(cmdText);
            break;
          }
          case CMD_OFF: {
            cmdText = CMD_OFF;
            Console.WriteLine(cmdText);
            hueConnector.SwitchOff();
            FireSpeechCmdDetected(cmdText);
            break;
          }
          case CMD_RED: {
            cmdText = CMD_RED;
            Console.WriteLine(cmdText);
            hueConnector.SetColor(RED);
            FireSpeechCmdDetected(cmdText);
            break;
          }
          case CMD_GREEN: {
            cmdText = CMD_GREEN;
            Console.WriteLine(cmdText);
            hueConnector.SetColor(GREEN);
            FireSpeechCmdDetected(cmdText);
            break;
          }
          case CMD_BLUE: {
            cmdText = CMD_BLUE;
            Console.WriteLine(cmdText);
            hueConnector.SetColor(BLUE);
            FireSpeechCmdDetected(cmdText);
            break;
          }
        } // switch

        // lamp [one | two | three]
        if (firstTerm.Text == CMD_LAMP && secondTerm != null) {
          
          switch (secondTerm.Text) {
          
            case CMD_ONE: {
              cmdText = CMD_LAMP + " " + CMD_ONE;
              Console.WriteLine(cmdText);
              hueConnector.SetColor(LAMP);
              FireSpeechCmdDetected(cmdText);
              break;
            }
            case CMD_TWO: {
              cmdText = CMD_LAMP + " " + CMD_TWO;
              Console.WriteLine(cmdText);
              hueConnector.SetColor(LAMP);
              FireSpeechCmdDetected(cmdText);
              break;
            }
            case CMD_THREE: {
              cmdText = CMD_LAMP + " " + CMD_THREE;
              Console.WriteLine(cmdText);
              hueConnector.SetColor(LAMP);
              FireSpeechCmdDetected(cmdText);
              break;
            }
          } // switch
        }

        // color [red | green | blue]
        if (firstTerm.Text == CMD_COLOR && secondTerm != null) {
          
          switch (secondTerm.Text) {
          
            case CMD_RED: {
              cmdText = CMD_COLOR + " " + CMD_RED;
              Console.WriteLine(cmdText);
              hueConnector.SetColor(RED);
              FireSpeechCmdDetected(cmdText);
              break;
            }
            case CMD_GREEN: {
              cmdText = CMD_COLOR + " " + CMD_GREEN;
              Console.WriteLine(cmdText);
              hueConnector.SetColor(GREEN);
              FireSpeechCmdDetected(cmdText);
              break;
            }
            case CMD_BLUE: {
              cmdText = CMD_COLOR + " " + CMD_BLUE;
              Console.WriteLine(cmdText);
              hueConnector.SetColor(BLUE);
              FireSpeechCmdDetected(cmdText);
              break;
            }
          } // switch
        }

      }

    }


    private void ShowRecognitionResult(SpeechRecognizedEventArgs e) {
      Debug.WriteLine("--- START recognition results ---");

      RecognitionResult result = e.Result;
      Debug.WriteLine("confidence: {0}", result.Confidence);
      foreach (RecognizedWordUnit word in result.Words) {
        Debug.WriteLine("word: {0} confidence: {1}", word.Text, word.Confidence);
      }

      Debug.WriteLine("--- END   recognition results ---");
    }
  }
}
