using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MUS2.Hue {

  public static class HueUtil {

    private const string BRIDGE_IP = "192.168.1.52";
    private const string APP_NAME = "mus";
    private const string APP_KEY = "newdeveloper";
    private const int TIME = 2000;

    public static HueClient GetHueClient() {
      // Find bridge and register application.
      IBridgeLocator locator = new HttpBridgeLocator();
      HueClient client = new HueClient(BRIDGE_IP);
      client.RegisterAsync(APP_NAME, APP_KEY);
      client.Initialize(APP_KEY);
      return client;
    }

  }
}
