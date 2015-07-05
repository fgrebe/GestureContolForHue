using MUS2.Hue;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Speech.Recognition;

namespace MUS2.Speech {

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

    public delegate void SpeechHandler(SpeechRecognition s, SpeechRecognizedEventArgs e);
    public event SpeechHandler sh;

    private Color lampColor;
    private int brightness;

    private bool speechEnabled = true; // activated by checkbox
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
      Debug.Assert(speechEnabled, "speechEnabled must be true in EnableSpeech");

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


    public void grammar_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
      Console.Write("I heard something...");
      IHueConnector hueConnector = HueConnectorFactory.GetHueConnector(REGISTER_APP);

      // show result on console
      this.ShowRecognitionResult(e);

      // our grammar is so simple, that we only have to consider two elements
      RecognitionResult result = e.Result;
      RecognizedWordUnit[] unit = e.Result.Words.ToArray();
      RecognizedWordUnit elem0;
      RecognizedWordUnit elem1;

      // elem will be the property of the recognized phrase. 
      elem0 = unit[0];
      try {
        elem1 = unit[1];
      } catch (Exception) {
        elem1 = null;
      }

      // check, what has been said
      if (elem0 != null) {
        if (elem0.Text == CMD_STOP) {
          Console.WriteLine(CMD_STOP + "\n...Disabling speech...");
          this.DisableSpeech();
        }

        if (elem0.Text == CMD_ON) {
          Console.WriteLine(CMD_ON);
          hueConnector.SwitchOn();
        }

        if (elem0.Text == CMD_OFF) {
          Console.WriteLine(CMD_OFF);
          hueConnector.SwitchOff();
        }

        if (elem0.Text == CMD_RED) {
          Console.WriteLine(CMD_RED);
          hueConnector.SetColor(RED);
        }

        if (elem0.Text == CMD_GREEN) {
          Console.WriteLine(CMD_GREEN);
          hueConnector.SetColor(GREEN);
        }

        if (elem0.Text == CMD_BLUE) {
          Console.WriteLine(CMD_BLUE);
          hueConnector.SetColor(BLUE);
        }

        // lamp [one | two | three]
        if (elem0.Text == CMD_LAMP && elem1 != null) {
          if (elem1.Text == CMD_ONE) {
            Console.WriteLine(CMD_LAMP + " " + CMD_ONE);
            hueConnector.SetColor(LAMP);
          } else if (elem1.Text == CMD_TWO) {
            Console.WriteLine(CMD_LAMP + " " + CMD_TWO);
            hueConnector.SetColor(LAMP);
          } else if (elem1.Text == CMD_THREE) {
            Console.WriteLine(CMD_LAMP + " " + CMD_THREE);
            hueConnector.SetColor(LAMP);
          }
        }

        // color [red | green | blue]
        if (elem0.Text == CMD_COLOR && elem1 != null) {
          if (elem1.Text == CMD_RED) {
            Console.WriteLine(CMD_COLOR + " " + CMD_RED);
          } else if (elem1.Text == CMD_BLUE) {
            Console.WriteLine(CMD_COLOR + " " + CMD_BLUE);
          } else if (elem1.Text == CMD_GREEN) {
            Console.WriteLine(CMD_COLOR + " " + CMD_GREEN);
          }
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
