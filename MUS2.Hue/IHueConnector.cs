using System.Collections.Generic;

namespace MUS2.Hue {

  //
  // Summary:
  //     Interface, which provides methods to perform operations on
  //     the hue (switching off/on lights, setting color and brightness
  //     of lights, ...).
  //
  // Authors:
  //     Florentina Grebe
  //     Sabine Winkler
  //
  // Since:
  //     2015-07-08
  // 
  public interface IHueConnector {

    bool IsOn();
    bool IsAlertOn();
    bool IsChaserLightOn();
    int  GetCurrentBrightness();
    int  GetMaxBrightness();

    // Switch on/off specific lamps or all if none are specified
    void SwitchOff(List<string> lamps = null);
    void SwitchOn(List<string> lamps = null);

    // Set color and brightness of specific lamps or all if none are specified
    void SetAColorAndBrightness(string color, int brightness, List<string> lights = null);
    // Set color of specific lamps or all if none are specified
    void SetColor(string color, List<string> lamps = null);
    // Set brightness of specific lamps or all if none are specified
    void SetBrightness(int brightness, List<string> lights = null);

    void SetAlertOn(List<string> lamps = null);
    void SetAlertOff(List<string> lamps = null);

    void SetChaserLightOn();
    void SetChaserLightOff();
  }
}
