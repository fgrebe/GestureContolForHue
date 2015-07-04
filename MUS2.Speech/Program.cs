using System.Threading;

namespace MUS2.Speech {

  //
  // Summary:
  //     Console application to initialize and start the speech recognition.
  //
  // Authors:
  //     Florentina Grebe
  //     Sabine Winkler
  //
  // Since:
  //     2015-07-08
  //
  public class Program {

    private static ManualResetEvent _completed = null;

    public static void Main(string[] args) {
      _completed = new ManualResetEvent(false);

      SpeechRecognition rec = new SpeechRecognition();
      rec.EnableSpeech();   // enables recognition and loads grammar file
      _completed.WaitOne(); // wait until speech recognition is completed
    }
  }
}
