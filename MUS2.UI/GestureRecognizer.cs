using GestureFabric.Core;
using MUS2.Hue;

namespace MUS2 {

  //
  // Summary:
  //     Gesture recognizer, which calls a hue operation depending on
  //     the gesture of the user.
  //
  // Authors:
  //     Florentina Grebe
  //     Sabine Winkler
  //
  // Since:
  //     2015-07-08
  //
  public class GestureRecognizer {

    private const string GESTURE_CIRCLE_CW  = "Circle(CW)";
    private const string GESTURE_DELETE     = "Delete";
    private const string GESTURE_LINE       = "Line";
    private const string GESTURE_CARET      = "Caret";
    private const string GESTURE_V          = "V";

    private const bool REGISTER_APP   = false;
    private const int  BRIGHTNESS_INC = 51; // inc. / dec. brightness by 51 per gesture 

    private IHueConnector hueConnector;
    private static GestureRecognizer instance = null;
    
    private GestureRecognizer() {
      hueConnector = HueConnectorFactory.GetHueConnector(REGISTER_APP);
    }

    public static GestureRecognizer GetInstance() {

      if (instance == null) {
        instance = new GestureRecognizer();
      }

      return instance;
    }

    public void InitializeHue() {
      hueConnector.SwitchOn();
      hueConnector.SetBrightness(hueConnector.GetMaxBrightness());
    }

    public void PerformHueAction(RecognitionResult recognizedGesture) {
      string gestureName = recognizedGesture.Name;

      switch (gestureName) {

        // start/stop chaser light from left to right
        case GESTURE_CIRCLE_CW: {
          if (hueConnector.IsChaserLightOn()) {
            hueConnector.SetChaserLightOff();
          } else {
            hueConnector.SetChaserLightOn();
          }
          break;
        }

        // switch on/off all lamps
        case GESTURE_DELETE: {
          hueConnector.SetChaserLightOff();
          if (hueConnector.IsOn()) {
            hueConnector.SwitchOff();
          } else {
            hueConnector.SwitchOn();
          }
          break;
        }

        // start/stop alert for all lamps
        case GESTURE_LINE: {
          if (hueConnector.IsAlertOn()) {
            hueConnector.SetAlertOff();
          } else {
            hueConnector.SetAlertOn();
          }
          break;
        }

        // increase brightness for all lamps
        case GESTURE_CARET: {
          int newBrightness = hueConnector.GetCurrentBrightness() + BRIGHTNESS_INC;
          hueConnector.SetBrightness(newBrightness);
          break;
        }

        // decrease brightness for all lamps
        case GESTURE_V: {
          int newBrightness = hueConnector.GetCurrentBrightness() - BRIGHTNESS_INC;
          hueConnector.SetBrightness(newBrightness);
          break;
        }
      }
    }
  }
}
