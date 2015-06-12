using GestureFabric;
using GestureFabric.Config;
using GestureFabric.Core;
using GestureFabric.Persistence;
using KinectUtils.MovementRecorder;
using KinectUtils.MovementRecorder.GestureFabriceExport; // for Recognizer
using KinectUtils.OnlineFilter;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace MUS2
{
    public delegate void GestureRecognizedEventHandler(ResultList result);

    class KinectDataManager
    {
        #region Private Members
        private object locker = new Object();
        private bool workOffline;
        //this object is used for getting data from the Kinect
        private KinectSensor kinect;
        //record data
        private DataRecorder recorder = new DataRecorder(30);
        //recognize gestures
        private Recognizer recognizer;
        private List<GestureSet> gestureSets;
        #endregion
        #region Delegates
        private GestureRecognizedEventHandler gestureRecognized;

        public event GestureRecognizedEventHandler GestureRecognized
        {
            add
            {
                lock (locker)
                {
                    this.gestureRecognized += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    this.gestureRecognized -= value;
                }
            }
        }
        #endregion
        public KinectDataManager(bool workOffline = false) 
        {
            this.workOffline = workOffline;
            try {
               int n = KinectSensor.KinectSensors.Count;
               if (n > 0 ) {
                   kinect = KinectSensor.KinectSensors[0];
               }
            }
            catch {
                if (!workOffline)
                    throw new Exception("Couldn't initialize runtime. Make sure Kinect is plugged in.");
            }
            try
            {
                InitializeGestureRecognition();
            }
            catch
            {
                throw new Exception("Failed to initialize gesture recognition");
            }
        }
        public KinectSensor Kinect
        {
            get
            {
                return kinect;
            }
        }
        public DataRecorder Recorder
        {
            get
            {
                return recorder;
            }
        }
        public Recognizer Recognizer
        {
            get
            {
                return recognizer;
            }
        }
        #region recognition
        public List<GestureSet> GestureSets
        {
            get
            {
                return gestureSets;
            }
        }
        private void InitializeGestureRecognition()
        {
            Gesture triangleGesture = FileUtils.ReadGestureFromXml(@"../../GestureDefinitions/triangle_gesture.xml");
            Gesture circleGesture = FileUtils.ReadGestureFromXml(@"../../GestureDefinitions/circle_gesture.xml");
            Gesture arrowGesture = FileUtils.ReadGestureFromXml(@"../../GestureDefinitions/arrow_gesture.xml");
            Gesture lineGesture = FileUtils.ReadGestureFromXml(@"../../GestureDefinitions/SyntheticHLine.short.xml");

            GestureSet simpleSet = new GestureSet("SimpleGestureSet");
            simpleSet.Add(triangleGesture);
            simpleSet.Add(circleGesture);
            simpleSet.Add(arrowGesture);
            simpleSet.Add(lineGesture);

            // provide the list of gesture sets for later use (e.g. to be visualized)
            gestureSets = new List<GestureSet>();
            gestureSets.Add(simpleSet);

            Debug.WriteLine("#### gesture definition before applying the algorithm");
            ShowGestureDescription(simpleSet);  // here we have the full number of points (e.g. 102);

            Configuration config = new Configuration();
            config.AddGestureSet(simpleSet);
            config.AddAlgorithm("1Dollar", "GestureFabric.Algorithms.Dollar.DollarAlgorithm");
            config.AddAlgorithmGestureSetMapping("1Dollar", "SimpleGestureSet");
            recognizer = new Recognizer(config);

            Debug.WriteLine("#### gesture definition before applying the algorithm");
            ShowGestureDescription(simpleSet);  // here we have the full number of points (e.g. 64);    
        }
    // just for understanding the data structure
    private void ShowGestureDescription(GestureSet gestureSet) {
      foreach (Gesture gesture in gestureSet.Gestures)
            {
        Debug.WriteLine("*** ShowGestureDescription: gesture:" + gesture.Name);
                IList<IDescriptor> descriptors = gesture.Descriptors;
                foreach (IDescriptor d in descriptors)
                {
                    if (d is PointDescriptor)
                    {
                        PointDescriptor pd = (PointDescriptor) d;
            Debug.WriteLine("*** gesture:" + gesture.Name + " has " + pd.Count + " points");
            double minimumX = double.MaxValue;
            double maximumX = double.MinValue;
            foreach (PointD p in pd.Points)
            {
              //Debug.WriteLine("*** :" + "(" + p.X + "," + p.Y + ")");
              minimumX = Math.Min(minimumX, p.X);
              maximumX = Math.Max(maximumX, p.X);
            }
            //Debug.WriteLine("*** minimumX: " + minimumX);
            //Debug.WriteLine("*** maximumX: " + maximumX);
                    }
                }
            }
    }
        public void RecognizeRecordedGesture(JointType jointId)
        {
            var gesturePoints = recorder.GetGesturePoints(jointId, ProjectionPlane.XY_PLANE);
            if (gesturePoints.Count > 0)
            {
                var result = recognizer.Recognize(gesturePoints);
                if (gestureRecognized != null)
                {
                    gestureRecognized(result);
                }
            }
        }
        #endregion
        #region continuous recognition
        private OnlineLowpassFilter onlineLowpassFilter = new OnlineLowpassFilter(0.25);
        public OnlineLowpassFilter OnlineLowpassFilter
        {
            get
            {
                return onlineLowpassFilter;
            }
        }
        private JointVelocityMonitor monitor = new JointVelocityMonitor(0.3, 0.1)
        {
            StartMoveTriggerDurationMs = 100,
            StopMoveTriggerDurationMs = 200,
        };
        public JointVelocityMonitor JointVelocityMonitor
        {
            get
            {
                return monitor;
            }
        }
        #endregion
    }
}
