using System;

namespace MUS2.Hue {

  //
  // Summary:
  //     Represents errors which occur in relation to hue
  //     (the hue bridge).
  //
  // Authors:
  //     Florentina Grebe
  //     Sabine Winkler
  //
  // Since:
  //     2015-07-08
  // 
  public class HueException : Exception {

    public HueException(string message) : base(message) {

    }

  }

}
