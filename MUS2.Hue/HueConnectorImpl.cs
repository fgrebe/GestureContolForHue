using Q42.HueApi;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MUS2.Hue {

  //
  // Summary:
  //     Implementation of the hue operations.
  //
  // Authors:
  //     Florentina Grebe
  //     Sabine Winkler
  //
  // Since:
  //     2015-07-08
  //
  public class HueConnectorImpl : IHueConnector {

    private HueClient client;
    private bool isOn;
    private bool isAlertOn;
    private bool isChaserLightOn;
    private int currentBrightness;

    private const int MAX_BRIGHTNESS = 255;
    private const int MIN_BRIGHTNESS = 0;

    private Task chaserLight;
    private const int CHASER_LIGHT_TIMEOUT = 1000; // 1 second

    public HueConnectorImpl(bool registerApp) {
      client = HueUtil.GetHueClient(registerApp);
    }

    private void SendCommandAsync(LightCommand command, List<string> lamps) {
      if (lamps != null)
        client.SendCommandAsync(command, lamps);
      else
        client.SendCommandAsync(command);
    }

    public bool IsOn() {
      return this.isOn;
    }

    public bool IsAlertOn() {
      return this.isAlertOn;
    }

    public bool IsChaserLightOn() {
      return this.isChaserLightOn;
    }

    public void SwitchOff(List<string> lamps = null) {
      isOn = false;
      LightCommand command = new LightCommand();
      command.TurnOff();
      SendCommandAsync(command, lamps);
    }

    public void SwitchOn(List<string> lamps = null) {
      isOn = true;
      var command = new LightCommand();
      command.TurnOn();
      SendCommandAsync(command, lamps);
    }

    public void SetAColorAndBrightness(string color, int brightness, List<string> lamps = null) {
      var command = new LightCommand();
      command.SetColor(color);
      command.Brightness = (byte)brightness; 
      SendCommandAsync(command, lamps); 
    }

    public void SetColor(string color, List<string> lamps = null) {
      var command = new LightCommand();
      command.SetColor(color);
      SendCommandAsync(command, lamps);   
    }

    /*
     * intensity: of the colors and the brightness is at its maximum by setting the “sat” and “bri” resources to 255.
     * saturation: changes color intensits -> should be 255. Reducing the saturation takes this hue and moves 
     *             it in a straight line towards the white point. So "sat":255 always gives the most saturated 
     *             colors and reducing it to "sat":200 makes them less intense and more white.
     * brightness: This is the brightness of a light from its minimum brightness 0 to its maximum brightness 255 
     *             (note minimum brightness is not off). 
     * hue (a measure of color): runs from 0 to 65535  -> changes color
     */
    public void SetBrightness(int brightness, List<string> lamps = null) {
      
      if (brightness >= MIN_BRIGHTNESS
          && brightness <= MAX_BRIGHTNESS) {
        currentBrightness = brightness;
        var command = new LightCommand();
        command.Brightness = (byte)brightness;
        SendCommandAsync(command, lamps); 
      }
    }

    public int GetCurrentBrightness() {
      return this.currentBrightness;
    }

    public int GetMaxBrightness() {
      return MAX_BRIGHTNESS;
    }

    public void SetAlertOn(List<string> lamps = null) {
      isAlertOn = true;
      SetAlert(Alert.Multiple, lamps);
    }

    public void SetAlertOff(List<string> lamps = null) {
      isAlertOn = false;
      SetAlert(Alert.None, lamps);
    }

    public void SetChaserLightOn() {
      SetChaserLight(true);
    }

    public void SetChaserLightOff() {
      SetChaserLight(false);
    }

    private void SetChaserLight(bool toOn) {
      if (toOn) {
        chaserLight = new Task(() => {
          int lampIndex = 1;
          isChaserLightOn = true;
          List<string> lamps = new List<string>();

          while (isChaserLightOn) {
            lamps.Clear();
            lamps.Add(lampIndex.ToString());
            SwitchOn(lamps);
            Thread.Sleep(CHASER_LIGHT_TIMEOUT);
            SwitchOff(lamps);
            Thread.Sleep(CHASER_LIGHT_TIMEOUT);
            lampIndex = lampIndex % 4;
            lampIndex++;
          }
        });

        chaserLight.Start();
      } else {
        isChaserLightOn = false;
      }
    }

    private void SetAlert(Alert alert, List<string> lamps = null) {
      var command = new LightCommand();
      command.Alert = alert;
      SendCommandAsync(command, lamps);
    }
  }
}
