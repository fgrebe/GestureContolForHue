using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MUS2.Speech {
  class Program {
    static ManualResetEvent _completed = null;

    static void Main(string[] args) {
      _completed = new ManualResetEvent(false);

      SpeechRecognition rec = new SpeechRecognition();
      rec.EnableSpeech(); // enables recognition and loads grammar file
      _completed.WaitOne(); // wait until speech recognition is completed
    }
  }
}
