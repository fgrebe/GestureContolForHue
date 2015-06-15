using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MUS2.Hue {

  public class Program {

    private const string BRIDGE_IP = "192.168.1.52";
    private const string APP_NAME  = "mus";
    private const string APP_KEY = "newdeveloper";
    private const int TIME = 2000;

    public static void Main(string[] args) {

      // See https://github.com/Q42/Q42.HueApi
      

      Console.WriteLine("========== Test Philips Hue (Q42.HueApi) ==========");

      // Find bridge and register application.
      IBridgeLocator locator = new HttpBridgeLocator();
      HueClient client = new HueClient(BRIDGE_IP);
      client.RegisterAsync(APP_NAME, APP_KEY);
      client.Initialize(APP_KEY);

      HueConnectorImpl HueManager = new HueConnectorImpl();

      Console.WriteLine("Switching on all");
      HueManager.SwitchOn(client);
      Thread.Sleep(TIME);

      Console.WriteLine("Switching off lamp 1");
      HueManager.SwitchOff(client, new List<string> { "1" });
      Thread.Sleep(TIME);

      Console.WriteLine("Switching on all");
      HueManager.SwitchOn(client);
      Thread.Sleep(TIME);

      Console.WriteLine("Changing color");
      HueManager.SetColor("ff270d", client);
      Thread.Sleep(TIME);

      HueManager.SetColor("080a67", client);
      Thread.Sleep(TIME);

      Console.WriteLine("Changing brightness");
      HueManager.SetBrightness(255, client);
      Thread.Sleep(TIME);

      HueManager.SetBrightness(50, client);
      Thread.Sleep(TIME);

      Console.WriteLine("Switching off all");
      HueManager.SwitchOff(client);
      Thread.Sleep(TIME);

      Console.WriteLine("===================================================");
      //Console.ReadLine();
    }
  }
}
