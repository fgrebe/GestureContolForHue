using MUS2.Hue;
using Q42.HueApi;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Speech.Recognition;

namespace MUS2.Speech {
  public class SpeechRecognition {

    public delegate void SpeechHandler(SpeechRecognition s, SpeechRecognizedEventArgs e);
    public event SpeechHandler sh;

    private Color lampColor;
    private int brightness;

    private bool speechEnabled = true; // activated by checkbox
    private bool speechInitialized = false;
    private SpeechRecognizer recognizer;
    private Grammar grammar;
    private const bool REGISTER_APP = false;

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
      Debug.Assert(speechInitialized,"speech must be initialized in DisableSpeech");
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
        grammar = new Grammar(@"..\..\Grammar\Grammar.xml");
        // Set event handler
        grammar.SpeechRecognized += grammar_SpeechRecognized;

        recognizer.LoadGrammar(grammar);
        speechInitialized = true;
      }
      catch (Exception e) {
        Debug.WriteLine(
            "Exception caught when initializing SAPI."
            + " This application may not run correctly.\r\n\r\n"
            + e.ToString(),
            "Error");
      }
    }

    public void grammar_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
      Console.Write("I heard something...");
      HueClient client = HueUtil.GetHueClient(REGISTER_APP);
      HueConnectorImpl HueManager = new HueConnectorImpl();

      // show result on console
      //this.ShowRecognitionResult(e);
      // our grammar is so simple, that we only have to consider two elements
      RecognitionResult result = e.Result;
      RecognizedWordUnit[] unit = e.Result.Words.ToArray();
      RecognizedWordUnit elem0;
      RecognizedWordUnit elem1;

      // elem will be the property of the recognized phrase. 
      elem0 = unit[0];
      try {
        elem1 = unit[1];
      }
      catch (Exception) {
        elem1 = null;
      }
      // check, what has been said
      if (elem0 != null) {
        if (elem0.Text == "stop") {
          Console.Write("stop\n...Disabling speech...\n");
          this.DisableSpeech();
        }

        if (elem0.Text == "on") {
          Console.Write("on\n");
          HueManager.SwitchOn(client);
        }

        if (elem0.Text == "off") {
          Console.Write("off\n");
          HueManager.SwitchOff(client);
        }

        if (elem0.Text == "red") {
          Console.Write("red\n");
          HueManager.SetColor("ff0000", client);
        }

        if (elem0.Text == "green") {
          Console.Write("green\n");
          HueManager.SetColor("00cc00", client);
        }

        if (elem0.Text == "blue") {
          Console.Write("blue\n");
          HueManager.SetColor("0000ff", client);
        }

        if (elem0.Text == "lamp" && elem1 != null) {
          if (elem1.Text == "one") {
            Console.WriteLine("lamp one");
            HueManager.SetColor("ff270d", client);
          }

          else if (elem1.Text == "two") {
            Console.WriteLine("lamp two");
            HueManager.SetColor("ff270d", client);
          }
          else if (elem1.Text == "three") {
            Console.WriteLine("lamp three");
            HueManager.SetColor("ff270d", client);
          }
        }

        if (elem0.Text == "color" && elem1 != null) {
          if (elem1.Text == "red") {
            Console.WriteLine("color red");
          }
           
          else if (elem1.Text == "blue") {
            Console.WriteLine("color blue");
          }
          else if (elem1.Text == "green") {
            Console.WriteLine("color green");
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
