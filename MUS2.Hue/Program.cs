using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;

namespace MUS2.Hue {

  public class Program {

    private const string BRIDGE_IP = "";
    private const string APP_NAME  = "mus";
    private const string APP_KEY = "mus";


    public static void Main(string[] args) {

      // See https://github.com/Q42/Q42.HueApi
      

      Console.WriteLine("========== Test Philips Hue (Q42.HueApi) ==========");


      // Find bridge and register application.
      IBridgeLocator locator = new HttpBridgeLocator();
      HueClient client = new HueClient(BRIDGE_IP);
      client.RegisterAsync(APP_NAME, APP_KEY);


      Console.WriteLine("===================================================");
    }
  }
}
