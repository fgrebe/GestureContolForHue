﻿using Q42.HueApi;
using System.Collections.Generic;
namespace MUS2.Hue {

  public interface IHueConnector {
    // Switch on/off specific lamps or all if none are specified
    void SwitchOff(HueClient client, List<string> lamps = null);
    void SwitchOn(HueClient client, List<string> lamps = null);

    // Set color and brightness of specific lamps or all if none are specified
    void SetAColorAndBrightness(string color, int brightness, HueClient client, List<string> lights = null);
    // Set color of specific lamps or all if none are specified
    void SetColor(string color, HueClient client, List<string> lamps);
    // Set brightness of specific lamps or all if none are specified
    void SetBrightness(int brightness, HueClient client, List<string> lights = null);

    void SetEffect(HueClient client, List<string> lamps = null);
  }
}
