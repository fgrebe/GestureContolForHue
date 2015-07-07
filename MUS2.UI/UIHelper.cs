using KinectUtils.MovementRecorder.GestureFabriceExport;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Windows.Media;

namespace MUS2.UI {

  public class UIHelper {
    
    private List<SolidColorBrush> brushes;
    private SolidColorBrush blackBrush;
    private SolidColorBrush redBrush;
    private SolidColorBrush blueBrush;

    public const JointType JOINT_TO_BE_DISPlAYED  = JointType.HandRight;
    public const ProjectionPlane PROJECTION_PLANE = ProjectionPlane.XY_PLANE;


    public UIHelper() {
      CreateBrushes();

      // for skeleton and gesture drawing
      blackBrush = new SolidColorBrush();
      redBrush   = new SolidColorBrush();
      blueBrush  = new SolidColorBrush();

      blackBrush.Color = Colors.Black;
      redBrush.Color   = Colors.Red;
      blueBrush.Color  = Colors.Blue;
    }

    private void CreateBrushes() {
      brushes = new List<SolidColorBrush>();

      // some brushes to show gestures in different colors
      AddBrush(Colors.Red);
      AddBrush(Colors.Black);
      AddBrush(Colors.Blue);
      AddBrush(Colors.Aqua);
      AddBrush(Colors.Yellow);
      AddBrush(Colors.Green);
    }


    private void AddBrush(Color color) {
      SolidColorBrush brush = new SolidColorBrush();
      brush.Color = color;
      this.brushes.Add(brush);
    }


    public List<SolidColorBrush> GetBrushes() {
      return this.brushes;
    }

    public SolidColorBrush GetBlackBrush() {
      return this.blackBrush;
    }

    public SolidColorBrush GetRedBrush() {
      return this.redBrush;
    }

    public SolidColorBrush GetBlueBrush() {
      return this.blueBrush;
    }
  }
}
