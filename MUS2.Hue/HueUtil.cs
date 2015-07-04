using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MUS2.Hue {

  //
  // Summary:
  //     Utility class, which provides methods to
  //     register/initialize a hue application at a hue bridge.
  //
  // Authors:
  //     Florentina Grebe
  //     Sabine Winkler
  //
  // Since:
  //     2015-07-08
  // 
  //
  public static class HueUtil {

    private const string APP_NAME    = "mus";
    private const string APP_KEY     = "newdeveloper";
    private const int    TIMEOUT_SEC = 5; // timeout for locating the bridges


    //
    // Summary:
    //     Trys to locate the bridge and if a bridge was found,
    //     registers/initializes the application.
    // 
    //     Apps (their app key) have to be registered only once per bridge and LAN.
    //     Therefore, if the ip of the bridge changes, the app has to be registered again.
    // 
    // Parameters:
    //     register: true, if the app should be registered
    //               false, if the app is already registered and an init is enough
    //            
    // Returns:
    //     Registered/Initialized hue client.
    //   
    // Exceptions:
    //   HueException:
    //     one of the following errors occurred
    //      - No bridge was found.
    //      - Multiple bridges were found.
    //      - Registration of app failed, because the user hasn't pressed
    //        the link button on the bridge before running this method.
    //
    public static HueClient GetHueClient(bool register) {

      HueClient client = new HueClient(GetBridgeIp());

      if (register) {
        RegisterApp(client);
      } else {
        InitializeApp(client);
      }

      return client;
    }


    //
    // Summary:
    //     Registers the app.
    // 
    // Parameters:
    //     client: hue client on which the registration is performed
    //   
    // Exceptions:
    //   HueException:
    //     Registration of app failed, because the user hasn't pressed
    //     the link button on the bridge before running this method.
    //
    public static void RegisterApp(HueClient client) {

      Task<bool> registerTask = client.RegisterAsync(APP_NAME, APP_KEY);
      registerTask.Wait();
      bool isSuccess = registerTask.Result;

      if (!isSuccess) {
        string msg = String.Format(
            "Failed to register the app '{0}' with key '{1}'.\n"
          + "Please press the link button on the bridge, "
          + "and then try to connect to the bridge again.",
            APP_NAME, APP_KEY);
        throw new HueException(msg);
      }
    }


    //
    // Summary:
    //     Initializes the app.
    // 
    // Parameters:
    //     client: hue client on which the initialization of the app is performed
    //
    public static void InitializeApp(HueClient client) {
      client.Initialize(APP_KEY);
    }


    //
    // Summary:
    //     Returns the ip address of the bridge on the LAN.
    //     Exactly one bridge is necessary.
    // 
    // Returns:
    //     ip address of found bridge
    //   
    // Exceptions:
    //   HueException:
    //     one of the following errors occurred
    //       - No bridge was found.
    //       - Multiple bridges were found.
    //
    public static string GetBridgeIp() {

      TimeSpan locateBridgesTimeout = TimeSpan.FromSeconds(TIMEOUT_SEC);

      IBridgeLocator locator = new HttpBridgeLocator();
      Task<IEnumerable<string>> t = locator.LocateBridgesAsync(locateBridgesTimeout);
      t.Wait();
      IEnumerable<string> bridgeIPs = t.Result;

      if (!bridgeIPs.Any()) {
        throw new HueException("No bridges were found. Please connect one.");
      }

      if (bridgeIPs.Count() > 1) {
        throw new HueException("Multiple bridges were found. Please connect only one.");
      }

      return bridgeIPs.ElementAt(0);
    }

  }

}
