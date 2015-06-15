using Q42.HueApi;
using System.Collections.Generic;
namespace MUS2.Hue {

  public class HueConnectorImpl : IHueConnector {

    public void SendCommandAsync(LightCommand command, HueClient client, List<string> lamps) {
      if (lamps != null)
        client.SendCommandAsync(command, lamps);
      else
        client.SendCommandAsync(command);
    }

    public void SwitchOff(HueClient client, List<string> lamps = null) {
      LightCommand command = new LightCommand();
      command.TurnOff();
      SendCommandAsync(command, client, lamps);
    }

    public void SwitchOn(HueClient client, List<string> lamps = null) {
      var command = new LightCommand();
      command.TurnOn();
      SendCommandAsync(command, client, lamps);
    }

    public void SetAColorAndBrightness(string color, int brightness, HueClient client, List<string> lamps = null) {
      var command = new LightCommand();
      command.SetColor(color);
      command.Brightness = (byte)brightness; 
      SendCommandAsync(command, client, lamps); 
    }

    public void SetColor(string color, HueClient client, List<string> lamps = null) {
      var command = new LightCommand();
      command.SetColor(color);
      SendCommandAsync(command, client, lamps);   
    }

    /*
     * intensity: of the colors and the brightness is at its maximum by setting the “sat” and “bri” resources to 255.
     * saturation: changes color intensits -> should be 255
     * brightness: changes brightness...
     * hue (a measure of color): runs from 0 to 65535  -> changes color
     */
    public void SetBrightness(int brightness, HueClient client, List<string> lamps = null) {
      var command = new LightCommand();
      command.Brightness = (byte)brightness; 
      SendCommandAsync(command, client, lamps); 
    }

    public void SetEffect(HueClient client, List<string> lamps = null) {
      var command = new LightCommand();
      //command.Effect = Effects.ColorLoop;
      SendCommandAsync(command, client, lamps);
    }
  }
}
