using GestureFabric.Core;
using GestureFabric.Persistence;
using KinectUtils.MovementRecorder;
using KinectUtils.MovementRecorder.GestureFabriceExport;
using KinectUtils.RecorderTypeDefinitions;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace MUS2.UI {

  public partial class SmallMainWindow : Window {

    #region members
    KinectDataManager kinectDataMgr;

    // color divisors for tinting depth pixels
    private static readonly int[] IntensityShiftByPlayerR = { 1, 2, 0, 2, 0, 0, 2, 0 };
    private static readonly int[] IntensityShiftByPlayerG = { 1, 2, 2, 0, 2, 0, 0, 1 };
    private static readonly int[] IntensityShiftByPlayerB = { 1, 0, 2, 2, 0, 2, 0, 2 };

    private const int RedIndex = 2;
    private const int GreenIndex = 1;
    private const int BlueIndex = 0;

    // for processing of depth data
    private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
    const float MaxDepthDistance = 4095; // max value returned
    const float MinDepthDistance = 850;  // min value returned
    const float MaxDepthDistanceOffset = MaxDepthDistance - MinDepthDistance;

    // joint connections
    Path wristLeftElbowLeftConn   = null;
    Path wristRightElbowRightConn = null;

    Path headShoulderCenterConn  = null;
    Path shoulderSpineCenterConn = null;

    Path elbowLeftShoulderLeftConn   = null;
    Path elbowRightShoulderRightConn = null;

    Path shoulderCenterShoulderLeftConn  = null;
    Path shoulderCenterShoulderRightConn = null;

    Path handLeftWristLeftConn   = null;
    Path handRightWristRightConn = null;

    Path spineHipCenterConn    = null;
    Path hipCenterhHipLeftConn = null;
    Path hipCenterHipRightConn = null;

    Path hipLeftKneeLeftConn   = null;
    Path hipRightKneeRightConn = null;

    Path kneeLeftAnkleLeftConn   = null;
    Path kneeRightAnkleRightConn = null;

    Path ankleLeftFootLeftConn   = null;
    Path ankleRightFootRightConn = null;

    SolidColorBrush blackBrush    = new SolidColorBrush();
    SolidColorBrush redBrush      = new SolidColorBrush();
    SolidColorBrush blueBrush     = new SolidColorBrush();
    List<SolidColorBrush> brushes = new List<SolidColorBrush>();

    // recording stuff
    int pointsCount = 0;
    bool isRecording = false;
    JointType jointToBeDisplayed = JointType.HandRight;
    string recordedDataFilename = null;

    // live recording stuff
    bool isLiveRecording = false;
    #endregion



    #region window
    public SmallMainWindow() {
      InitializeComponent();

      // for skeleton and gesture drawing
      blackBrush.Color = Colors.Black;
      redBrush.Color = Colors.Red;
      blueBrush.Color = Colors.Blue;

      // some brushes to show gestures in different colors
      SolidColorBrush b = new SolidColorBrush();
      b.Color = Colors.Red;
      this.brushes.Add(b);
      b = new SolidColorBrush();
      b.Color = Colors.Black;
      this.brushes.Add(b);
      b = new SolidColorBrush();
      b.Color = Colors.Blue;
      this.brushes.Add(b);
      b = new SolidColorBrush();
      b.Color = Colors.Aqua;
      this.brushes.Add(b);
      b.Color = Colors.Yellow;
      this.brushes.Add(b);
      b.Color = Colors.Green;
      this.brushes.Add(b);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      try {
        bool workOffLine = true; // workOffLine means that swe can work without any Kinect device
        // set this to true, if you do not have a Kinect
        if (KinectSensor.KinectSensors.Count == 0)
          workOffLine = true;
        else
          workOffLine = false;
        kinectDataMgr = new KinectDataManager(workOffLine);
        if (!workOffLine) {
          kinectDataMgr.Kinect.Start();
          // position Kinect in a useful way
          kinectDataMgr.Kinect.ElevationAngle = 5;
          // depth stream
          kinectDataMgr.Kinect.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
          kinectDataMgr.Kinect.SkeletonStream.Enable(); // To identify players
          // process and show raw depth data
          kinectDataMgr.Kinect.DepthFrameReady += Kinect_DepthFrameReady_Raw;
          // process and show depth stream data with color (color = distance)
          kinectDataMgr.Kinect.DepthFrameReady += Kinect_DepthFrameReady_Colored;
          // process and show skeleton data
          kinectDataMgr.Kinect.SkeletonFrameReady += Kinect_SkeletonFrameReady;
        }
      } catch (Exception ex) {
        // we do not have a connected Kinect device
        MessageBox.Show("Initialization failed. " + ex.Message);
        Application.Current.Shutdown(); ;
      }

      // for recording: show recorded data in view
      kinectDataMgr.Recorder.OnNewRecorderSample +=
        new KinectUtils.RecorderTypeDefinitions.RecorderSample(recorder_OnNewRecorderSample);

      // for gesture recognition
      InitializeGestureRecognition();
      //gesture recognition handler
      kinectDataMgr.GestureRecognized += new GestureRecognizedEventHandler(kinectDataMgr_GestureRecognized);

      // for live recording and recognition
      kinectDataMgr.JointVelocityMonitor.StartMove += new MovementEventHandler(JointVelocityMonitor_StartMove);
      kinectDataMgr.JointVelocityMonitor.StopMove += new MovementEventHandler(JointVelocityMonitor_StopMove);
    }

    private void Window_Closed(object sender, EventArgs e) {
      kinectDataMgr.Kinect.ElevationAngle = 0;
      if (kinectDataMgr.Kinect.IsRunning)
        kinectDataMgr.Kinect.Stop();
    }
    #endregion



    #region video data processing

    // ############### video data ##############
    void Kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e) {
      using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
        if (colorFrame == null) {
          return;
        }

        byte[] pixels = new byte[colorFrame.PixelDataLength];
        colorFrame.CopyPixelDataTo(pixels);
        int stride = colorFrame.Width * 4;
        /* imageRaw UI elements has been remove for screen resolution (1024x768) reasons
                imageRaw.Source =
                    BitmapSource.Create(colorFrame.Width, colorFrame.Height,
                    96, 96, PixelFormats.Bgr32, null, pixels, stride);
         * */
      }
    }

    #endregion
    
    
    
    #region depth data processing

    // ############### depth data ##############
    void Kinect_DepthFrameReady_Raw(object sender, DepthImageFrameReadyEventArgs e) {
      using (DepthImageFrame depthFrame = e.OpenDepthImageFrame()) {
        if (depthFrame == null) {
          return;
        }
        byte[] pixels = GenerateColoredBytes(depthFrame);
        //number of bytes per row width * 4 (B,G,R,Empty)
        int stride = depthFrame.Width * 4;
        //create image
        imageDepthRaw.Source =
          BitmapSource.Create(depthFrame.Width, depthFrame.Height,
          96, 96, PixelFormats.Bgr32, null, pixels, stride);
      }
    }

    private byte[] GenerateColoredBytes(DepthImageFrame depthFrame) {
      Int32 playerDistance = 0;
      //get the raw data from kinect with the depth for every pixel
      short[] rawDepthData = new short[depthFrame.PixelDataLength];
      depthFrame.CopyPixelDataTo(rawDepthData);

      //use depthFrame to create the image to display on-screen
      //depthFrame contains color information for all pixels in image
      //Height x Width x 4 (Red, Green, Blue, empty byte)
      Byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];

      //Bgr32  - Blue, Green, Red, empty byte
      //Bgra32 - Blue, Green, Red, transparency 
      //You must set transparency for Bgra as .NET defaults a byte to 0 = fully transparent

      //hardcoded locations to Blue, Green, Red (BGR) index positions       
      const int BlueIndex = 0;
      const int GreenIndex = 1;
      const int RedIndex = 2;


      //loop through all distances
      //pick a RGB color based on distance
      for (int depthIndex = 0, colorIndex = 0;
          depthIndex < rawDepthData.Length && colorIndex < pixels.Length;
          depthIndex++, colorIndex += 4) {
        //get the player (requires skeleton tracking enabled for values)
        int player = rawDepthData[depthIndex] & DepthImageFrame.PlayerIndexBitmask;

        //gets the depth value
        int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

        //.9M or 2.95'
        if (depth <= 900) {
          //we are very close
          pixels[colorIndex + BlueIndex] = 255;
          pixels[colorIndex + GreenIndex] = 0;
          pixels[colorIndex + RedIndex] = 0;
        }
        // .9M - 2M or 2.95' - 6.56'
        else if (depth > 900 && depth < 2000) {
          //we are a bit further away
          pixels[colorIndex + BlueIndex] = 0;
          pixels[colorIndex + GreenIndex] = 255;
          pixels[colorIndex + RedIndex] = 0;
        }
        // 2M+ or 6.56'+
        else if (depth > 2000) {
          //we are the farthest
          pixels[colorIndex + BlueIndex] = 0;
          pixels[colorIndex + GreenIndex] = 0;
          pixels[colorIndex + RedIndex] = 255;
        }

        //equal coloring for monochromatic histogram
        byte intensity = CalculateIntensityFromDepth(depth);
        pixels[colorIndex + BlueIndex] = intensity;
        pixels[colorIndex + GreenIndex] = intensity;
        pixels[colorIndex + RedIndex] = intensity;

        //Color all players "gold"
        if (player > 0 && chkPlayer.IsChecked == true) {
          pixels[colorIndex + BlueIndex] = Colors.Gold.B;
          pixels[colorIndex + GreenIndex] = Colors.Gold.G;
          pixels[colorIndex + RedIndex] = Colors.Gold.R;
          playerDistance = depth;
        }
      }
      // show player distance
      // we get mm and convert it to cm
      playerDistance = playerDistance / 100 * 10; // smooth the values down to centimeters to avoid any flickering
      txtDistance.Content = "Distance: " + playerDistance.ToString() + " cm";
      return pixels;
    }

    void Kinect_DepthFrameReady_Colored(object sender, DepthImageFrameReadyEventArgs e) {
      using (DepthImageFrame imageFrame = e.OpenDepthImageFrame()) {
        if (imageFrame != null) {

          // 
          WriteableBitmap outputBitmap = new WriteableBitmap(
              imageFrame.Width,
              imageFrame.Height,
              96,  // DpiX
              96,  // DpiY
              PixelFormats.Bgr32,
              null);

          short[] pixelData;
          pixelData = new short[imageFrame.PixelDataLength];
          imageFrame.CopyPixelDataTo(pixelData);

          byte[] depthFrame32;
          depthFrame32 = new byte[imageFrame.Width * imageFrame.Height * Bgr32BytesPerPixel];
          // Converts a 16-bit grayscale depth frame which includes player indexes into a 32-bit frame
          // that displays different players in different colors
          byte[] convertedDepthBits = this.ConvertDepthFrame(pixelData, depthFrame32, ((KinectSensor)sender).DepthStream);
          outputBitmap.WritePixels(
              new Int32Rect(0, 0, imageFrame.Width, imageFrame.Height),
              convertedDepthBits,
              imageFrame.Width * Bgr32BytesPerPixel,
              0);

          imageDepthColor.Source = outputBitmap;
        }
      }
    }

    // Converts a 16-bit grayscale depth frame which includes player indexes into a 32-bit frame
    // that displays different players in different colors
    private byte[] ConvertDepthFrame(short[] depthFrame, byte[] depthFrame32, DepthImageStream depthStream) {
      int tooNearDepth = depthStream.TooNearDepth;
      int tooFarDepth = depthStream.TooFarDepth;
      int unknownDepth = depthStream.UnknownDepth;

      for (int i16 = 0, i32 = 0; i16 < depthFrame.Length && i32 < depthFrame32.Length; i16++, i32 += 4) {
        int player = depthFrame[i16] & DepthImageFrame.PlayerIndexBitmask;
        int realDepth = depthFrame[i16] >> DepthImageFrame.PlayerIndexBitmaskWidth;

        // transform 13-bit depth information into an 8-bit intensity appropriate
        // for display (we disregard information in most significant bit)
        // The '~' operator is the bitwise negation operator.
        byte intensity = (byte)(~(realDepth >> 4));

        if (player == 0 && realDepth == 0) // too near
                {
          // white 
          depthFrame32[i32 + RedIndex] = 255;
          depthFrame32[i32 + GreenIndex] = 255;
          depthFrame32[i32 + BlueIndex] = 255;
        } else if (player == 0 && realDepth == tooFarDepth) // too far
                {
          // blue
          depthFrame32[i32 + RedIndex] = 0;
          depthFrame32[i32 + GreenIndex] = 0;
          depthFrame32[i32 + BlueIndex] = 255;
        } else if (player == 0 && realDepth == unknownDepth) // unknown
                {
          // red
          depthFrame32[i32 + RedIndex] = 255;
          depthFrame32[i32 + GreenIndex] = 0;
          depthFrame32[i32 + BlueIndex] = 0;
        } else {
          // tint the intensity by dividing by per-player values
          depthFrame32[i32 + RedIndex] = (byte)(intensity >> IntensityShiftByPlayerR[player]);
          depthFrame32[i32 + GreenIndex] = (byte)(intensity >> IntensityShiftByPlayerG[player]);
          depthFrame32[i32 + BlueIndex] = (byte)(intensity >> IntensityShiftByPlayerB[player]);
        }
      }
      return depthFrame32;
    }

    public static byte CalculateIntensityFromDepth(int distance) {
      //formula for calculating monochrome intensity for histogram
      return (byte)(255 - (255 * Math.Max(distance - MinDepthDistance, 0)
          / (MaxDepthDistanceOffset)));
    }
    #endregion
    
    
    
    #region skeleton tracking

    // ############### skeleton tracking ##############
    void Kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e) {
      try {
        using (SkeletonFrame allSkeletons = e.OpenSkeletonFrame()) {
          if (allSkeletons == null)
            return;
          // get the first tracked skeleton    
          // if state is not tracked, then we do get lot of useless position data (e.g X/Y 0/0)
          Skeleton[] skeletonArray = new Skeleton[allSkeletons.SkeletonArrayLength];
          allSkeletons.CopySkeletonDataTo(skeletonArray);
          Skeleton skeleton = (from s in skeletonArray
                               where s.TrackingState == SkeletonTrackingState.Tracked
                               select s).FirstOrDefault();

          if (skeleton != null && skeleton.TrackingState == SkeletonTrackingState.Tracked) {
            // move the ellipses in our canvas  to the location of a Joint
            SetJointPosition(headEllipse, skeleton.Joints[JointType.Head]);
            // shoulder
            SetJointPosition(shoulderCenterEllipse, skeleton.Joints[JointType.ShoulderCenter]);
            SetJointPosition(shoulderLeftEllipse, skeleton.Joints[JointType.ShoulderLeft]);
            SetJointPosition(shoulderRightEllipse, skeleton.Joints[JointType.ShoulderRight]);
            // wrist and hand
            SetJointPosition(wristLeftEllipse, skeleton.Joints[JointType.WristLeft]);
            SetJointPosition(handLeftEllipse, skeleton.Joints[JointType.HandLeft]);
            SetJointPosition(wristRightEllipse, skeleton.Joints[JointType.WristRight]);
            SetJointPosition(handRightEllipse, skeleton.Joints[JointType.HandRight]);
            // elbow
            SetJointPosition(elbowLeftEllipse, skeleton.Joints[JointType.ElbowLeft]);
            SetJointPosition(elbowRightEllipse, skeleton.Joints[JointType.ElbowRight]);
            SetJointPosition(spineEllipse, skeleton.Joints[JointType.Spine]);
            // hip
            SetJointPosition(hipLeftEllipse, skeleton.Joints[JointType.HipLeft]);
            SetJointPosition(hipRightEllipse, skeleton.Joints[JointType.HipRight]);
            SetJointPosition(hipCenterEllipse, skeleton.Joints[JointType.HipCenter]);
            // knee
            SetJointPosition(kneeLeftEllipse, skeleton.Joints[JointType.KneeLeft]);
            SetJointPosition(kneeRightEllipse, skeleton.Joints[JointType.KneeRight]);
            // ankle
            SetJointPosition(ankleLeftEllipse, skeleton.Joints[JointType.AnkleLeft]);
            SetJointPosition(ankleRightEllipse, skeleton.Joints[JointType.AnkleRight]);
            // foot
            SetJointPosition(footLeftEllipse, skeleton.Joints[JointType.FootLeft]);
            SetJointPosition(footRightEllipse, skeleton.Joints[JointType.FootRight]);

            // connect specific ellipses
            DrawSkeletonConnection(headEllipse, shoulderCenterEllipse, ref headShoulderCenterConn);
            // shoulder + elbow
            DrawSkeletonConnection(shoulderCenterEllipse, shoulderLeftEllipse, ref shoulderCenterShoulderLeftConn);
            DrawSkeletonConnection(shoulderCenterEllipse, shoulderRightEllipse, ref shoulderCenterShoulderRightConn);
            DrawSkeletonConnection(elbowLeftEllipse, shoulderLeftEllipse, ref elbowLeftShoulderLeftConn);
            DrawSkeletonConnection(elbowRightEllipse, shoulderRightEllipse, ref elbowRightShoulderRightConn);
            DrawSkeletonConnection(shoulderCenterEllipse, spineEllipse, ref shoulderSpineCenterConn);
            // wrist + hands
            DrawSkeletonConnection(wristLeftEllipse, elbowLeftEllipse, ref wristLeftElbowLeftConn);
            DrawSkeletonConnection(wristRightEllipse, elbowRightEllipse, ref wristRightElbowRightConn);
            DrawSkeletonConnection(wristLeftEllipse, handLeftEllipse, ref handLeftWristLeftConn);
            DrawSkeletonConnection(wristRightEllipse, handRightEllipse, ref handRightWristRightConn);
            // hips
            DrawSkeletonConnection(hipCenterEllipse, hipLeftEllipse, ref hipCenterhHipLeftConn);
            DrawSkeletonConnection(hipCenterEllipse, hipRightEllipse, ref hipCenterHipRightConn);
            DrawSkeletonConnection(hipCenterEllipse, spineEllipse, ref spineHipCenterConn);
            // hips + knees
            DrawSkeletonConnection(hipLeftEllipse, kneeLeftEllipse, ref hipLeftKneeLeftConn);
            DrawSkeletonConnection(hipRightEllipse, kneeRightEllipse, ref hipRightKneeRightConn);
            // knees + ankles
            DrawSkeletonConnection(kneeLeftEllipse, ankleLeftEllipse, ref kneeLeftAnkleLeftConn);
            DrawSkeletonConnection(kneeRightEllipse, ankleRightEllipse, ref kneeRightAnkleRightConn);
            // ankle + foot
            DrawSkeletonConnection(footLeftEllipse, ankleLeftEllipse, ref ankleLeftFootLeftConn);
            DrawSkeletonConnection(footRightEllipse, ankleRightEllipse, ref ankleRightFootRightConn);

            if (this.isRecording) {
              DoRecording(allSkeletons);
            } else if (this.isLiveRecording) {
              DoLiveRecording(allSkeletons);
            }
          }
        }
      } catch (Exception ex) {
        Debug.WriteLine(ex.Message);
      }
    }

    private void SetJointPosition(FrameworkElement ellipse, Joint joint) {
      //
      // A Joint position returns X,Y,Z values 
      // X = Horizontal position between –1 and +1 
      // Y = Vertical position between –1 and +1 
      // Z = Distance from Kinect measured in meters 
      Canvas.SetLeft(ellipse, (float)((joint.Position.X + 1) / 2.0 * canvasSkeleton.ActualWidth));
      Canvas.SetTop(ellipse, (float)((1 - joint.Position.Y) / 2.0 * canvasSkeleton.ActualHeight));
    }


    private void DrawSkeletonConnection(FrameworkElement shape1, FrameworkElement shape2, ref Path connection) {
      GeneralTransform transform1 = shape1.TransformToVisual(shape1.Parent as UIElement);
      GeneralTransform transform2 = shape2.TransformToVisual(shape2.Parent as UIElement);
      // define the geometry of a line
      LineGeometry lineGeometry = new LineGeometry() {
        StartPoint = transform1.Transform(new Point(shape1.ActualWidth / 2, shape1.ActualHeight / 2.0)),
        EndPoint = transform2.Transform(new Point(shape2.ActualWidth / 2.0, shape2.ActualHeight / 2.0))
      };
      // define the path data; which is in fact a line
      Path path = new Path() {
        Data = lineGeometry
      };

      path.Stroke = blackBrush;
      path.StrokeThickness = 3;
      path.Fill = blackBrush;

      if (connection == null) {
        connection = path;
        this.canvasSkeleton.Children.Add(path);
      } else {
        // update path with new data
        connection.Data = lineGeometry;
      }
      this.canvasSkeleton.InvalidateVisual();
    }
    #endregion
    
    
    
    #region basic control

    // ############### basic control ##############
    bool isElevationTaskOutstanding = false;
    int targetElevationAngle = 0;


    private void sdrElevationAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
      targetElevationAngle = Convert.ToInt32(e.NewValue.ToString());
      // just for safety, because we get called when GUI is loaded and no nui instance is available
      if ((kinectDataMgr != null) && (kinectDataMgr.Kinect.Status == KinectStatus.Connected) && (kinectDataMgr.Kinect.IsRunning)) {
        try {
          if (!isElevationTaskOutstanding) {
            StartElevationTask();
          }
        } catch (Exception ex) {
          Debug.WriteLine(ex.Message);
        }
      }
    }

    private void StartElevationTask() {
      KinectSensor sensor = kinectDataMgr.Kinect;
      int lastSetElevationAngle = int.MinValue;

      if (sensor != null) {
        isElevationTaskOutstanding = true;
        Task.Factory.StartNew(
          () => {
            int angleToSet = targetElevationAngle;
            // Keep adjusting the elevation angle until we "match".
            while ((lastSetElevationAngle != angleToSet) && sensor.IsRunning) {
              // Note: Change angle as few times (max 15 times every 20 sec.) as possible and
              // wait at least 1 sec. after a call. So wee wait 1350 ms.
              // see: http://msdn.microsoft.com/en-us/library/microsoft.kinect.kinectsensor.elevationangle.aspx
              sensor.ElevationAngle = angleToSet;  // set new angle
              lastSetElevationAngle = angleToSet;
              Thread.Sleep(1350);
              angleToSet = targetElevationAngle;
            }
          }).ContinueWith(
          results => {
            if (results.IsFaulted) {
              var exception = results.Exception;
              Debug.WriteLine("Set Elevation Task failed with exception: " + exception);
            }
            // In case more request to change the evelation angle appeared,
            // start the task again.
            this.Dispatcher.BeginInvoke((Action)(() => {
              if (targetElevationAngle != lastSetElevationAngle) {
                StartElevationTask();
              } else {
                isElevationTaskOutstanding = false;
              }
            }));
          });
      }
    }

    #endregion



    #region recording
    private void btnRecordingStart_Click(object sender, RoutedEventArgs e) {
      isRecording = true;
      pointsCount = 0;
      this.kinectDataMgr.Recorder.ClearData();
      this.canvasRecordedGesture.Children.Clear();
      this.btnRecordingStart.IsEnabled = false;
      this.btnRecordingStop.IsEnabled = true;
      this.btnRecordingSave.IsEnabled = false;
      this.btnRecordingLoad.IsEnabled = false;
    }

    private void btnRecordingStop_Click(object sender, RoutedEventArgs e) {
      isRecording = false;
      this.btnRecordingStart.IsEnabled = true;
      this.btnRecordingStop.IsEnabled = false; this.btnRecordingSave.IsEnabled = true;
      this.btnRecordingLoad.IsEnabled = true;
    }

    private void btnRecordingSave_Click(object sender, RoutedEventArgs e) {
      this.btnRecordingSave.IsEnabled = false;
      // http://msdn.microsoft.com/en-us/library/aa969773.aspx#Y722
      // Configure save file dialog box
      Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
      dlg.FileName = "RecordedGesture"; // Default file name
      dlg.DefaultExt = ".xml"; // Default file extension
      dlg.Filter = "XML documents (.xml)|*.xml"; // Filter files by extension
      dlg.InitialDirectory = @"C:\Users";

      // Show open file dialog box
      Nullable<bool> result = dlg.ShowDialog();
      // Process open file dialog box results
      if (result == true) {
        // Open document
        string filename = dlg.FileName;
        kinectDataMgr.Recorder.SaveDataToFile(filename);
        ExportRecordedPointsToGestureFabric(@"../../GestureDefinitions/GestureFabric.CurrentGestureSet.xml");
      }
    }

    // write xml document in GestureFabric format, so that we can use this data for our gesture description
    private void ExportRecordedPointsToGestureFabric(string filename) {
      Gesture ng = GestureFabriceExport.GetGesture(kinectDataMgr.Recorder, "CurrentGesture", jointToBeDisplayed, ProjectionPlane.XY_PLANE);
      GestureSet gSet = new GestureSet("CurrentGestureSet", ng);
      gSet.Save(filename);
    }

    private void btnRecordingLoad_Click(object sender, RoutedEventArgs e) {
      isRecording = false;
      // http://msdn.microsoft.com/en-us/library/aa969773.aspx#Y722
      // Configure open file dialog box
      Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
      dlg.FileName = "RecordedGesture"; // Default file name
      dlg.DefaultExt = ".xml"; // Default file extension
      dlg.Filter = "XML documents (.xml)|*.xml"; // Filter files by extension

      // Show open file dialog box
      Nullable<bool> result = dlg.ShowDialog();
      // Process open file dialog box results
      if (result == true) {
        // Open document
        recordedDataFilename = dlg.FileName; // store filename for later use 
        // (e.g. reloading data after  filter has been applied
        kinectDataMgr.Recorder.LoadDataFromFile(recordedDataFilename);
        this.btnRecordingPlay.IsEnabled = true;
        this.pointsCount = kinectDataMgr.Recorder.GetData().Count();
        this.txtRecordedPoints.Text = this.pointsCount.ToString();
      }
    }

    private void btnRecordingPlay_Click(object sender, RoutedEventArgs e) {
      isRecording = false;
      pointsCount = 0;
      this.canvasRecordedGesture.Children.Clear();
      this.canvasFilteredInput.Children.Clear();

      // apply filter to input data
      if (chkFilterInputGesture.IsChecked == true) {
        // filter
        float smoothingFactor = (float)Double.Parse(txtFilterFactorInputData.Text);
        Debug.WriteLine("smoothingFactor for input data " + smoothingFactor);
        kinectDataMgr.Recorder.ApplyFilter(new LowpassFilter(smoothingFactor));
        int no = kinectDataMgr.Recorder.GetData().Count();
        Debug.WriteLine("remaining data after filtering : " + no);
      }
      // show data
      kinectDataMgr.Recorder.Play();
    }

    private void cmbRecordingJoint_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      ComboBoxItem item = (ComboBoxItem)e.AddedItems[0];
      String value = (String)item.Content;
      if (value == "Left Hand") {
        jointToBeDisplayed = JointType.HandLeft;
      } else if (value == "Right Hand") {
        jointToBeDisplayed = JointType.HandRight;
      }
    }

    private void DoRecording(SkeletonFrame skeletonFrame) {
      Skeleton[] skeletonArray = new Skeleton[skeletonFrame.SkeletonArrayLength];
      skeletonFrame.CopySkeletonDataTo(skeletonArray);
      foreach (Skeleton data in skeletonArray) {
        // we only show data of tracked joints
        // if state is not tracked, then we do get lot of useless position data (e.g X/Y 0/0)
        if (SkeletonTrackingState.Tracked == data.TrackingState) {
          kinectDataMgr.Recorder.AddSample(data);
        }
        // draw new data in canvas
        recorder_OnNewRecorderSample(new RecordedSkeletonData(data));
      }
    }

    private void DoLiveRecording(SkeletonFrame skeletonFrame) {
      Skeleton[] skeletonArray = new Skeleton[skeletonFrame.SkeletonArrayLength];
      skeletonFrame.CopySkeletonDataTo(skeletonArray);
      foreach (Skeleton data in skeletonArray) {
        // we only show data of tracked joints
        // if state is not tracked, then we do get lot of useless position data (e.g X/Y 0/0)
        if (SkeletonTrackingState.Tracked == data.TrackingState) {
          kinectDataMgr.Recorder.AddSample(data);

          //new:philipp
          var skeletonData = new RecordedSkeletonData(data);

          if (this.chkFilterInputGestureOnline.IsChecked.HasValue && this.chkFilterInputGestureOnline.IsChecked.Value) {
            //new:philipp
            // do online filtering here 
            skeletonData = kinectDataMgr.OnlineLowpassFilter.NextSample(skeletonData);

            // show live filtering results in filtering window
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate() {
              DrawJointInCanvas(this.canvasFilteredInput, skeletonData, this.blackBrush);
            });
          }

          // insert data into the JointVelocityMonitor (this data should be filtered, otherwise we get problems when deriving)
          kinectDataMgr.JointVelocityMonitor.AddSample(skeletonData.Joints[jointToBeDisplayed]);
          recorder_OnNewRecorderSample(new RecordedSkeletonData(data));
        }
      }
    }

    void recorder_OnNewRecorderSample(RecordedSkeletonData data) {
      this.pointsCount++;
      //synchronize to the GUI thread
      this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate() {
        if (this.chkDrawFilteredInputGesture.IsChecked == true) {
          DrawJointInCanvas(this.canvasFilteredInput, data, this.blackBrush);
        } else {
          DrawJointInCanvas(this.canvasRecordedGesture, data, this.blueBrush);
        }
        this.txtRecordedPoints.Text = this.pointsCount.ToString();
      });

    }

    private void DrawJointInCanvas(Canvas canvas, KinectUtils.RecorderTypeDefinitions.RecordedSkeletonData data, SolidColorBrush brush) {
      // Draw joints
      // we have all joints, but we just draw one
      RecordedJoint joint = data.Joints[jointToBeDisplayed];
      Point jointPos;
      //Debug.WriteLine("      recJoint: " + joint.ID + " X/Y" + joint.Position.X + "/" + joint.Position.Y);
      RecordedJoint convertedJoint = ConvertToScreenCoordinates(joint, canvas);
      jointPos = new Point(convertedJoint.Position.X, convertedJoint.Position.Y);

      Line jointLine = new Line();
      jointLine.X1 = jointPos.X - 1;
      jointLine.X2 = jointLine.X1 + 1;
      jointLine.Y1 = jointLine.Y2 = jointPos.Y;
      jointLine.Stroke = brush;
      jointLine.StrokeThickness = 2;
      canvas.Children.Add(jointLine);
    }

    private RecordedJointsCollection ConvertToScreenCoordinates(RecordedJointsCollection joints, Canvas canvas) {
      RecordedJointsCollection convertedJoints = new RecordedJointsCollection();
      foreach (RecordedJoint joint in joints) {
        convertedJoints.Add(ConvertToScreenCoordinates(joint, canvas));
      }
      return convertedJoints;
    }

    //convert x and y coordinates to screen size
    private RecordedJoint ConvertToScreenCoordinates(RecordedJoint joint, Canvas canvas) {
      RecordedJoint convertedJoint = new RecordedJoint();
      convertedJoint.ID = joint.ID;
      convertedJoint.Position.X = (float)((joint.Position.X + 1) / 2.0 * canvas.ActualWidth);
      convertedJoint.Position.Y = (float)((1 - joint.Position.Y) / 2.0 * canvas.ActualHeight);
      //Debug.WriteLine("         Joint: X/Y" + joint.Position.X + "/" + joint.Position.Y);
      //Debug.WriteLine("convertedJoint: X/Y" + convertedJoint.Position.X + "/" + convertedJoint.Position.Y);
      convertedJoint.Position.Z = joint.Position.Z;
      return convertedJoint;
    }
    #endregion
    
    
    
    #region recognition

    private void InitializeGestureRecognition() {
      List<SolidColorBrush>.Enumerator e = brushes.GetEnumerator();
      foreach (GestureSet gestureSet in kinectDataMgr.GestureSets) {
        foreach (Gesture gesture in gestureSet.Gestures) {
          if (e.MoveNext() == false) {
            // we have reached the end of the colors so we start over
            e = brushes.GetEnumerator();
            e.MoveNext();
          }
          SolidColorBrush b = e.Current;
          DrawPredefinedGesture(gesture, canvasPredefinedGestures, e.Current);
        }
      }
    }

    private void DrawPredefinedGesture(Gesture gesture, Canvas canvas, SolidColorBrush brush) {
      Debug.WriteLine("*** DrawPredefinedGesture: gesture:" + gesture.Name);
      IList<IDescriptor> descriptors = gesture.Descriptors;
      foreach (IDescriptor d in descriptors) {
        if (d is PointDescriptor) {
          PointDescriptor pd = (PointDescriptor)d;
          Debug.WriteLine("*** gesture:" + gesture.Name + " has " + pd.Count + " points");
          DrawGestureInCanvas(this.canvasPredefinedGestures, pd.Points, brush);
        }
      }
    }

    private void DrawGestureInCanvas(Canvas canvas, IList<PointD> points, SolidColorBrush brush) {
      // we want to fit the gesture into the canvas, so we have to find factors for the drawing area
      double minX = 0, maxX = 0, minY = 0, maxY = 0;
      double expandFactorX = 1, expandFactorY = 1;
      findDrawingFactors(canvas, points, ref minX, ref maxX, ref minY, ref maxY, ref expandFactorX, ref expandFactorY);

      // define a polyline
      PointCollection pointsToDraw = new PointCollection();
      foreach (PointD p in points) {
        // add points to collection
        double newX = Math.Round(p.X, 2) - Math.Round(minX, 2);
        // mirror it, because 0 is for the X coordinates in the canvas on top, 
        // and for the recorded kinect coordinates on the bottom
        newX = canvas.Height - newX;

        double newY = Math.Round(p.Y, 2) - Math.Round(minY, 2);
        newX = newX * expandFactorX + 12; // why add 12 to get the needed shift to the center? (Werner)
        newY = newY * expandFactorY;

        pointsToDraw.Add(new Point(newX, newY));
      }
      Polyline line = new Polyline();
      line.Points = pointsToDraw;
      line.Stroke = brush;
      line.StrokeThickness = 1;

      canvas.Children.Add(line);
    }

    private void findDrawingFactors(Canvas canvas, IList<PointD> points, ref double minX, ref double maxX,
        ref double minY, ref double maxY, ref double expandFactorX, ref double expandFactorY) {
      // find maximum and minimum of X and Y
      minX = double.MaxValue;
      maxX = double.MinValue;
      minY = double.MaxValue;
      maxY = double.MinValue;
      foreach (PointD p in points) {
        // shift it to a positive value and integer range x
        double x = p.X;
        double y = p.Y;
        maxX = Math.Max(x, maxX);
        minX = Math.Min(x, minX);
        maxY = Math.Max(y, maxY);
        minY = Math.Min(y, minY);
      }
      //Debug.WriteLine("*** minX: " + minX);
      //Debug.WriteLine("*** maxX: " + maxX);
      double extentX = maxX - minX;
      double extentY = maxY - minY;
      expandFactorX = canvas.Width / extentX;
      expandFactorY = canvas.Height / extentY;
    }

    private void btRrecognize_Click(object sender, RoutedEventArgs e) {
      // apply filter to input data
      if (chkFilterInputGesture.IsChecked == true) {
        // filter
        float smoothingFactor = (float)Double.Parse(txtFilterFactorInputData.Text);
        this.kinectDataMgr.Recorder.ApplyFilter(new LowpassFilter(smoothingFactor));
      } else {
        // export data for use in Excel
        CsvExporter.Export(@"..\..\unfilteredInput.csv", this.kinectDataMgr.Recorder.GetData());
      }
      if (this.chkDrawFilteredInputGesture.IsChecked == true) {
        // show  data
        this.pointsCount = 0;
        this.canvasFilteredInput.Children.Clear();
        this.canvasRecordedGesture.Children.Clear();
        this.kinectDataMgr.Recorder.Play();
      }
      // recognize gesture
      TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
      kinectDataMgr.RecognizeRecordedGesture(jointToBeDisplayed);
      TimeSpan end = new TimeSpan(DateTime.Now.Ticks);
      TimeSpan duration = end.Subtract(start);
      txtRecDuration.Text = duration.Milliseconds.ToString();
    }

    void kinectDataMgr_GestureRecognized(ResultList result) {
      txtTopScore.Text = result.TopResult.Score.ToString();
      txtRecognizedGesture.Text = result.TopResult.Name;
      txtGesture.Content = "G: " + result.TopResult.Name + " (" +
          String.Format("{0:0.00}", result.TopResult.Score) + ")";
    }

    private void chkFilterInputGesture_Click(object sender, RoutedEventArgs e) {
      if (recordedDataFilename != null) {
        //kinectDataMgr.Recorder.ClearData();
        kinectDataMgr.Recorder.LoadDataFromFile(recordedDataFilename);
        Debug.WriteLine("Recorder.LoadDataFromFile" + recordedDataFilename + ")");
      }
      if (chkFilterInputGesture.IsChecked == true) {
        this.txtFilterFactorInputData.IsEnabled = true;
        this.chkDrawFilteredInputGesture.IsEnabled = true;
      } else {
        this.txtFilterFactorInputData.IsEnabled = false;
        this.chkDrawFilteredInputGesture.IsEnabled = false;
      }
    }

    private void txtFilterFactorInputData_TextChanged(object sender, TextChangedEventArgs e) {
      if (recordedDataFilename != null) {
        //kinectDataMgr.Recorder.ClearData();
        kinectDataMgr.Recorder.LoadDataFromFile(recordedDataFilename);
        Debug.WriteLine("Recorder.LoadDataFromFile" + recordedDataFilename + ")");
      }
    }
    #endregion
    
    
    
    #region continuous recording and recognition
    private void chkLiveRecording_Click(object sender, RoutedEventArgs e) {
      if (chkLiveRecording.IsChecked == true) {
        btnRecordingStart.IsEnabled = false;
        btnRecordingLoad.IsEnabled = false;
        btRrecognize.IsEnabled = false;
        chkFilterInputGesture.IsEnabled = false;
        chkFilterInputGestureOnline.IsEnabled = true;
        chkDrawFilteredInputGesture.IsEnabled = false;

        isLiveRecording = true;
      } else {
        btnRecordingStart.IsEnabled = true;
        btnRecordingLoad.IsEnabled = true;
        btRrecognize.IsEnabled = true;
        chkFilterInputGesture.IsEnabled = true;
        chkFilterInputGestureOnline.IsEnabled = false;
        chkDrawFilteredInputGesture.IsEnabled = true;

        isLiveRecording = false;
      }
    }

    void JointVelocityMonitor_StopMove(double actualSpeed) {
      Debug.WriteLine("JointVelocityMonitor_StopMove");
      // now we try to recognize the gesture
      RecognizeRecordedGesture();
      kinectDataMgr.JointVelocityMonitor.Clear();
    }

    void JointVelocityMonitor_StartMove(double actualSpeed) {
      Debug.WriteLine("JointVelocityMonitor_StartMove");
      // we only consider values from now on
      ClearRecordedDataAndViews();
    }

    public void RecognizeRecordedGesture() {
      if (this.chkFilterInputGestureOnline.IsChecked == true) {
        float smoothingFactor = (float)Double.Parse(txtFilterFactorInputData.Text);
        Debug.WriteLine("smoothingFactor for input data {0}", smoothingFactor);
        kinectDataMgr.Recorder.ApplyFilter(new LowpassFilter(smoothingFactor));
      }
      // we only consider one plane (e.g. XY)
      var gesturePoints = kinectDataMgr.Recorder.GetGesturePoints(jointToBeDisplayed, ProjectionPlane.XY_PLANE);
      if (gesturePoints.Count > 0) {
        var result = kinectDataMgr.Recognizer.Recognize(gesturePoints);
        kinectDataMgr_GestureRecognized(result);
      }
      ClearRecordedDataAndViews();
    }

    public void ClearRecordedDataAndViews() {
      kinectDataMgr.Recorder.ClearData();
      pointsCount = 0;
      canvasRecordedGesture.Children.Clear();
      canvasFilteredInput.Children.Clear();
    }

    private void chkFilterInputGestureOnline_Click(object sender, RoutedEventArgs e) {
      if (chkFilterInputGestureOnline.IsChecked == true) {

        this.txtFilterFactorInputData.IsEnabled = true;
      } else {
        this.txtFilterFactorInputData.IsEnabled = false;
      }
    }
    #endregion

  }
}
