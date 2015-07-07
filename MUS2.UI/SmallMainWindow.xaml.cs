using GestureFabric.Core;
using KinectUtils.MovementRecorder;
using KinectUtils.MovementRecorder.GestureFabriceExport;
using KinectUtils.RecorderTypeDefinitions;
using Microsoft.Kinect;
using MUS2.Speech;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace MUS2.UI {

  //
  // Summary:
  //     WPF ui for controlling the hue using Kinect-based gestures
  //     and Microsoft SAPI speech recognition.
  //
  // Authors:
  //     Werner Kurschl, Philipp Pendelin
  //     Adapted by Florentina Grebe and Sabine Winkler   
  //
  // Since:
  //     2015-07-08
  //
  public partial class SmallMainWindow : Window {

    private const string GRAMMAR_FILE = @"..\..\..\MUS2.Speech\Grammar\Grammar.xml";

    #region members

    KinectDataManager kinectDataMgr;
    SpeechRecognition speechRecognition;
    UIHelper uiHelper;

    #region color divisors for tinting depth pixels
    private static readonly int[] IntensityShiftByPlayerR = { 1, 2, 0, 2, 0, 0, 2, 0 };
    private static readonly int[] IntensityShiftByPlayerG = { 1, 2, 2, 0, 2, 0, 0, 1 };
    private static readonly int[] IntensityShiftByPlayerB = { 1, 0, 2, 2, 0, 2, 0, 2 };

    private const int RedIndex   = 2;
    private const int GreenIndex = 1;
    private const int BlueIndex  = 0;
    #endregion

    #region for processing of depth data
    private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
    const float MaxDepthDistance = 4095; // max value returned
    const float MinDepthDistance = 850;  // min value returned
    const float MaxDepthDistanceOffset = MaxDepthDistance - MinDepthDistance;
    #endregion

    #region joint connections
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
    #endregion

    #region recording stuff
    int pointsCount = 0;
    string recordedDataFilename = null;
    bool isLiveRecording = false;
    #endregion

    #endregion



    #region window
    public SmallMainWindow() {
      InitializeComponent();
      uiHelper = new UIHelper();
      InitializeSpeechRecognition();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      try {
        // if workOffLine is true, we can work without any Kinect device
        bool workOffLine = (KinectSensor.KinectSensors.Count == 0);
        kinectDataMgr = new KinectDataManager(workOffLine);

        if (!workOffLine) {
          
          kinectDataMgr.Kinect.Start();

          // position Kinect in a useful way
          kinectDataMgr.Kinect.ElevationAngle = 5;

          // depth stream
          kinectDataMgr.Kinect.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
          kinectDataMgr.Kinect.SkeletonStream.Enable(); // to identify players

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
        new RecorderSample(recorder_OnNewRecorderSample);

      // for gesture recognition
      InitializeGestureRecognition();

      //gesture recognition handler
      kinectDataMgr.GestureRecognized +=
        new GestureRecognizedEventHandler(kinectDataMgr_GestureRecognized);

      // for live recording and recognition
      kinectDataMgr.JointVelocityMonitor.StartMove +=
        new MovementEventHandler(JointVelocityMonitor_StartMove);
      kinectDataMgr.JointVelocityMonitor.StopMove +=
        new MovementEventHandler(JointVelocityMonitor_StopMove);
    }

    private void Window_Closed(object sender, EventArgs e) {
      kinectDataMgr.Kinect.ElevationAngle = 0;
      if (kinectDataMgr.Kinect.IsRunning) {
        kinectDataMgr.Kinect.Stop();
      }
    }
    #endregion
    
    

    #region depth data processing

    void Kinect_DepthFrameReady_Colored(object sender, DepthImageFrameReadyEventArgs e) {
      
      using (DepthImageFrame imageFrame = e.OpenDepthImageFrame()) {

        if (imageFrame != null) {

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
          
          // Converts a 16-bit grayscale depth frame which includes player indexes
          // into a 32-bit frame that displays different players in different colors
          byte[] convertedDepthBits =
            this.ConvertDepthFrame(pixelData, depthFrame32, ((KinectSensor)sender).DepthStream);
          
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

      for (int i16 = 0, i32 = 0;
               i16 < depthFrame.Length && i32 < depthFrame32.Length;
               i16++, i32 += 4) {
        
        int player = depthFrame[i16] & DepthImageFrame.PlayerIndexBitmask;
        int realDepth = depthFrame[i16] >> DepthImageFrame.PlayerIndexBitmaskWidth;

        // transform 13-bit depth information into an 8-bit intensity appropriate
        // for display (we disregard information in most significant bit)
        // The '~' operator is the bitwise negation operator.
        byte intensity = (byte)(~(realDepth >> 4));

        if (player == 0 && realDepth == 0) { // too near

          // white 
          depthFrame32[i32 + RedIndex]   = 255;
          depthFrame32[i32 + GreenIndex] = 255;
          depthFrame32[i32 + BlueIndex]  = 255;

        } else if (player == 0 && realDepth == tooFarDepth) { // too far
          
          // blue
          depthFrame32[i32 + RedIndex]   = 0;
          depthFrame32[i32 + GreenIndex] = 0;
          depthFrame32[i32 + BlueIndex]  = 255;

        } else if (player == 0 && realDepth == unknownDepth) { // unknown
                
          // red
          depthFrame32[i32 + RedIndex]   = 255;
          depthFrame32[i32 + GreenIndex] = 0;
          depthFrame32[i32 + BlueIndex]  = 0;

        } else {

          // tint the intensity by dividing by per-player values
          depthFrame32[i32 + RedIndex]   = (byte)(intensity >> IntensityShiftByPlayerR[player]);
          depthFrame32[i32 + GreenIndex] = (byte)(intensity >> IntensityShiftByPlayerG[player]);
          depthFrame32[i32 + BlueIndex]  = (byte)(intensity >> IntensityShiftByPlayerB[player]);
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

    void Kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e) {
      
      try {
      
        using (SkeletonFrame allSkeletons = e.OpenSkeletonFrame()) {
          if (allSkeletons == null) return;

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
            SetJointPosition(shoulderLeftEllipse,   skeleton.Joints[JointType.ShoulderLeft]);
            SetJointPosition(shoulderRightEllipse,  skeleton.Joints[JointType.ShoulderRight]);
            
            // wrist and hand
            SetJointPosition(wristLeftEllipse,  skeleton.Joints[JointType.WristLeft]);
            SetJointPosition(handLeftEllipse,   skeleton.Joints[JointType.HandLeft]);
            SetJointPosition(wristRightEllipse, skeleton.Joints[JointType.WristRight]);
            SetJointPosition(handRightEllipse,  skeleton.Joints[JointType.HandRight]);
            
            // elbow
            SetJointPosition(elbowLeftEllipse,  skeleton.Joints[JointType.ElbowLeft]);
            SetJointPosition(elbowRightEllipse, skeleton.Joints[JointType.ElbowRight]);
            SetJointPosition(spineEllipse,      skeleton.Joints[JointType.Spine]);
            
            // hip
            SetJointPosition(hipLeftEllipse,   skeleton.Joints[JointType.HipLeft]);
            SetJointPosition(hipRightEllipse,  skeleton.Joints[JointType.HipRight]);
            SetJointPosition(hipCenterEllipse, skeleton.Joints[JointType.HipCenter]);
            
            // knee
            SetJointPosition(kneeLeftEllipse,  skeleton.Joints[JointType.KneeLeft]);
            SetJointPosition(kneeRightEllipse, skeleton.Joints[JointType.KneeRight]);
            
            // ankle
            SetJointPosition(ankleLeftEllipse,  skeleton.Joints[JointType.AnkleLeft]);
            SetJointPosition(ankleRightEllipse, skeleton.Joints[JointType.AnkleRight]);
            
            // foot
            SetJointPosition(footLeftEllipse,  skeleton.Joints[JointType.FootLeft]);
            SetJointPosition(footRightEllipse, skeleton.Joints[JointType.FootRight]);

            // connect specific ellipses
            DrawSkeletonConnection(headEllipse, shoulderCenterEllipse, ref headShoulderCenterConn);
            
            // shoulder + elbow
            DrawSkeletonConnection(shoulderCenterEllipse, shoulderLeftEllipse,  ref shoulderCenterShoulderLeftConn);
            DrawSkeletonConnection(shoulderCenterEllipse, shoulderRightEllipse, ref shoulderCenterShoulderRightConn);
            DrawSkeletonConnection(elbowLeftEllipse,      shoulderLeftEllipse,  ref elbowLeftShoulderLeftConn);
            DrawSkeletonConnection(elbowRightEllipse,     shoulderRightEllipse, ref elbowRightShoulderRightConn);
            DrawSkeletonConnection(shoulderCenterEllipse, spineEllipse,         ref shoulderSpineCenterConn);
            
            // wrist + hands
            DrawSkeletonConnection(wristLeftEllipse,  elbowLeftEllipse,  ref wristLeftElbowLeftConn);
            DrawSkeletonConnection(wristRightEllipse, elbowRightEllipse, ref wristRightElbowRightConn);
            DrawSkeletonConnection(wristLeftEllipse,  handLeftEllipse,   ref handLeftWristLeftConn);
            DrawSkeletonConnection(wristRightEllipse, handRightEllipse,  ref handRightWristRightConn);
            
            // hips
            DrawSkeletonConnection(hipCenterEllipse, hipLeftEllipse,  ref hipCenterhHipLeftConn);
            DrawSkeletonConnection(hipCenterEllipse, hipRightEllipse, ref hipCenterHipRightConn);
            DrawSkeletonConnection(hipCenterEllipse, spineEllipse,    ref spineHipCenterConn);
            
            // hips + knees
            DrawSkeletonConnection(hipLeftEllipse,  kneeLeftEllipse,  ref hipLeftKneeLeftConn);
            DrawSkeletonConnection(hipRightEllipse, kneeRightEllipse, ref hipRightKneeRightConn);
            
            // knees + ankles
            DrawSkeletonConnection(kneeLeftEllipse,  ankleLeftEllipse,  ref kneeLeftAnkleLeftConn);
            DrawSkeletonConnection(kneeRightEllipse, ankleRightEllipse, ref kneeRightAnkleRightConn);
            
            // ankle + foot
            DrawSkeletonConnection(footLeftEllipse,  ankleLeftEllipse,  ref ankleLeftFootLeftConn);
            DrawSkeletonConnection(footRightEllipse, ankleRightEllipse, ref ankleRightFootRightConn);

            if (this.isLiveRecording) {
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

    private void DrawSkeletonConnection(FrameworkElement shape1, FrameworkElement shape2,
                                        ref Path connection) {
      
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

      path.Stroke = uiHelper.GetBlackBrush();
      path.StrokeThickness = 3;
      path.Fill = uiHelper.GetBlackBrush();

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



    #region recording
    
    private void DoLiveRecording(SkeletonFrame skeletonFrame) {
      
      Skeleton[] skeletonArray = new Skeleton[skeletonFrame.SkeletonArrayLength];
      skeletonFrame.CopySkeletonDataTo(skeletonArray);
      
      foreach (Skeleton data in skeletonArray) {
        
        // we only show data of tracked joints
        // if state is not tracked, then we do get lot of useless position data (e.g X/Y 0/0)
        if (SkeletonTrackingState.Tracked == data.TrackingState) {
          kinectDataMgr.Recorder.AddSample(data);

          var skeletonData = new RecordedSkeletonData(data);

          if (this.chkFilterInputGestureOnline.IsChecked.HasValue
                && this.chkFilterInputGestureOnline.IsChecked.Value) {
            
            // do online filtering here 
            skeletonData = kinectDataMgr.OnlineLowpassFilter.NextSample(skeletonData);

            // show live filtering results in filtering window
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate() {
              DrawJointInCanvas(this.canvasFilteredInput, skeletonData, uiHelper.GetBlackBrush());
            });
          }

          // insert data into the JointVelocityMonitor
          // (this data should be filtered, otherwise we get problems when deriving)
          kinectDataMgr.JointVelocityMonitor.AddSample(skeletonData.Joints[UIHelper.JOINT_TO_BE_DISPlAYED]);
          recorder_OnNewRecorderSample(new RecordedSkeletonData(data));
        }
      }
    }

    void recorder_OnNewRecorderSample(RecordedSkeletonData data) {
      
      this.pointsCount++;

      //synchronize to the GUI thread
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate() {
        DrawJointInCanvas(this.canvasRecordedGesture, data, uiHelper.GetBlueBrush());
        this.txtRecordedPoints.Text = this.pointsCount.ToString();
      });

    }

    private void DrawJointInCanvas(Canvas canvas, RecordedSkeletonData data, SolidColorBrush brush) {
      
      // Draw joints
      // we have all joints, but we just draw one
      RecordedJoint joint = data.Joints[UIHelper.JOINT_TO_BE_DISPlAYED];
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

    private RecordedJointsCollection ConvertToScreenCoordinates(
          RecordedJointsCollection joints, Canvas canvas) {

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
      
      List<SolidColorBrush>.Enumerator e = uiHelper.GetBrushes().GetEnumerator();
      
      foreach (GestureSet gestureSet in kinectDataMgr.GestureSets) {
        foreach (Gesture gesture in gestureSet.Gestures) {
          
          if (e.MoveNext() == false) {
          
            // we have reached the end of the colors so we start over
            e = uiHelper.GetBrushes().GetEnumerator();
            e.MoveNext();
          }
          SolidColorBrush b = e.Current;
          DrawPredefinedGesture(gesture, canvasPredefinedGestures, e.Current);
        }
      }
    }


// ##############################################################################################
// Speech recognition
// ##############################################################################################
    
    private void InitializeSpeechRecognition() {
      speechRecognition = new SpeechRecognition();

      // enables recognition and loads grammar file
      speechRecognition.EnableSpeech(GRAMMAR_FILE);

      // register a callback method, which is called when a speech command was correctly detected
      speechRecognition.SpeechCmdDetected += speechRecognition_SpeechCmdDetected;
    }

    private void speechRecognition_SpeechCmdDetected(string cmdText) {
      this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate() {
        ListBoxItem selectedItem = null;

        foreach (ListBoxItem item in this.listBoxSpeechCmd.Items) {
          if (((string)item.Content) == cmdText) {
            selectedItem = item;
          }
        }
        this.listBoxSpeechCmd.SelectedItem = selectedItem;
      });
    }

// ##############################################################################################


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
      
      findDrawingFactors(canvas, points,
                         ref minX, ref maxX, ref minY, ref maxY,
                         ref expandFactorX, ref expandFactorY);

      // define a polyline
      PointCollection pointsToDraw = new PointCollection();
      foreach (PointD p in points) {
        
        // add points to collection
        double newX = Math.Round(p.X, 2) - Math.Round(minX, 2);
        
        // mirror it, because 0 is for the X coordinates in the canvas on top, 
        // and for the recorded kinect coordinates on the bottom
        newX = canvas.Height - newX;

        double newY = Math.Round(p.Y, 2) - Math.Round(minY, 2);
        newX = newX * expandFactorX + 12;
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

    void kinectDataMgr_GestureRecognized(ResultList result) {
      
      RecognitionResult topResult = result.TopResult;
      double score       = topResult.Score;
      string gestureName = topResult.Name;

      GestureRecognizer.GetInstance().PerformHueAction(topResult);

      labelGestureValue.Content = gestureName + " (" +
          String.Format("{0:0.00}", score) + ")";
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
      isLiveRecording = (chkLiveRecording.IsChecked == true);
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
      var gesturePoints = kinectDataMgr.Recorder.GetGesturePoints(
        UIHelper.JOINT_TO_BE_DISPlAYED, UIHelper.PROJECTION_PLANE);

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
      this.txtFilterFactorInputData.IsEnabled
        = (chkFilterInputGestureOnline.IsChecked == true);
    }
    
    #endregion

  }
}
