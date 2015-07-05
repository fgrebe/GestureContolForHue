
namespace MUS2.Hue {

  //
  // Summary:
  //     Factory class which returns an instance of HueConnectorImpl.
  //
  // Authors:
  //     Florentina Grebe
  //     Sabine Winkler
  //
  // Since:
  //     2015-07-08
  //
  public static class HueConnectorFactory {

    public static IHueConnector GetHueConnector(bool registerApp) {
      return new HueConnectorImpl(registerApp);
    }
  }
}
