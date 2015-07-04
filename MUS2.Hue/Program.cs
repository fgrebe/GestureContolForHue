using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MUS2.Hue {

  //
  // Summary:
  //     Program to test some operations of the hue.
  //     Also see https://github.com/Q42/Q42.HueApi.
  //
  // Authors:
  //     Florentina Grebe
  //     Sabine Winkler
  //
  // Since:
  //     2015-07-08
  // 
  public class Program {

    private const int  TIME         = 2000;
    private const bool REGISTER_APP = false;


    public static void Main(string[] args) {

      Console.WriteLine("========== Test Philips Hue (Q42.HueApi) ==========");

      try {
        HueClient client = HueUtil.GetHueClient(REGISTER_APP);
      
        HueConnectorImpl hueManager = new HueConnectorImpl();

        Console.WriteLine("Switching on all");
        hueManager.SwitchOn(client);
        Thread.Sleep(TIME);

        Console.WriteLine("Switching off lamp 1");
        hueManager.SwitchOff(client, new List<string> { "1" });
        Thread.Sleep(TIME);

        Console.WriteLine("Switching on all");
        hueManager.SwitchOn(client);
        Thread.Sleep(TIME);

        Console.WriteLine("Changing color");
        hueManager.SetColor("ff270d", client);
        Thread.Sleep(TIME);

        hueManager.SetColor("080a67", client);
        Thread.Sleep(TIME);

        Console.WriteLine("Changing brightness");
        hueManager.SetBrightness(255, client);
        Thread.Sleep(TIME);

        hueManager.SetBrightness(50, client);
        Thread.Sleep(TIME);

        Console.WriteLine("Switching off all");
        hueManager.SwitchOff(client);
        Thread.Sleep(TIME);
      } catch (HueException e) {
        Console.WriteLine(e.Message);
      }

      Console.WriteLine("===================================================");
      Console.ReadLine();
    }
  }
}
