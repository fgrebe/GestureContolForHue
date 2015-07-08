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
        IHueConnector hueConnector = HueConnectorFactory.GetHueConnector(REGISTER_APP);

        Console.WriteLine("Switching on all");
        hueConnector.SwitchOn();
        Thread.Sleep(TIME);

        Console.WriteLine("Switching off lamp 1");
        hueConnector.SwitchOff(new List<string> { "1" });
        Thread.Sleep(TIME);

        Console.WriteLine("Switching on all");
        hueConnector.SwitchOn();
        Thread.Sleep(TIME);

        Console.WriteLine("Changing color");
        hueConnector.SetColor("ff270d");
        Thread.Sleep(TIME);

        hueConnector.SetColor("080a67");
        Thread.Sleep(TIME);

        Console.WriteLine("Changing brightness");
        hueConnector.SetBrightness(255);
        Thread.Sleep(TIME);

        hueConnector.SetBrightness(50);
        Thread.Sleep(TIME);

        Console.WriteLine("Switching off all");
        hueConnector.SwitchOff();
        Thread.Sleep(TIME);
      } catch (HueException e) {
        Console.WriteLine(e.Message);
      }

      Console.WriteLine("===================================================");
      Console.ReadLine();
    }
  }
}
