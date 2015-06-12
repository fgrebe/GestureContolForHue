using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace MUS_Projekt {
  class SpeechRecognition {

    private Color lampColor;
    private int brightness;

    private bool speechEnabled = false;
    private bool speechInitialized = false;
    private SpeechRecognizer recognizer;
    private Grammar grammar;

    private bool EnableSpeech() {
      Debug.WriteLine("enabling speech ...");
      Debug.Assert(speechEnabled, "speechEnabled must be true in EnableSpeech");

      // START_WERNER_1
      if (speechInitialized == false) {
        InitializeSpeechWithGrammarFile();
      }
      recognizer.Enabled = true;
      Debug.WriteLine("Recognition state is now: {0} ", recognizer.State);
      // END_WERNER_1
      return true;
    }

    /// <summary>
    ///     This is a private function that stops speech recognition.
    /// </summary>
    /// <returns></returns>
    private bool DisableSpeech() {
      Debug.Assert(speechInitialized,
                   "speech must be initialized in DisableSpeech");
      // START_WERNER_1
      if (speechInitialized) {
        // Putting the recognition context to disabled state will 
        // stop speech recognition. Changing the state to enabled 
        // will start recognition again.

        recognizer.Enabled = false;
        Debug.WriteLine("Recognition state is now: {0} ", recognizer.State);
        Debug.WriteLine("disabling speech ...");
      }
      // END_WERNER_1
      return true;
    }

    private void InitializeSpeechWithGrammarFile() {
      Debug.WriteLine("Initializing SAPI objects...");
      try {
        recognizer = new SpeechRecognizer();
        grammar = new Grammar(@"..\..\Grammar\Grammar.xml");
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

    void grammar_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
      // START_WERNER_1
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
        if (elem0.Text == "quit") {
          //this.Dispose( // cross-thread exception
          //System.Windows.Forms.Application.Exit();
        }
        if (elem0.Text == "color" && elem1 != null) {
          if (elem1.Text == "red") {
            //do stuff
          }
            
          else if (elem1.Text == "blue") {

          }
          else if (elem1.Text == "green") {

          }           
        }
      }
    }
    private void ShowRecognitionResult(SpeechRecognizedEventArgs e) {
      Debug.WriteLine("--- START recognition results ---");
      // START_WERNER_1
      RecognitionResult result = e.Result;
      Debug.WriteLine("confidence: {0}", result.Confidence);
      foreach (RecognizedWordUnit word in result.Words) {
        Debug.WriteLine("word: {0} confidence: {1}", word.Text, word.Confidence);
      }
      // END_WERNER_1
      Debug.WriteLine("--- END   recognition results ---");
    }
  }
}
