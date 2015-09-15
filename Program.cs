using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Kinect;

namespace DumpKinectSkeleton
{
    class Program
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private static KinectSensor _kinectSensor;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private static BodyFrameReader _bodyFrameReader;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private static Body[] _bodies;

        /// <summary>
        /// Output file stream
        /// </summary>
        private static StreamWriter _outputStream;

        /// <summary>
        /// Status display timer
        /// </summary>
        private static Timer _timer;

        /// <summary>
        /// Status synchronization lock
        /// </summary>
        private static readonly object Lock = new object();

        /// <summary>
        /// Frames counter
        /// </summary>
        private static int _frameCount;

        /// <summary>
        /// Last time status was displayed
        /// </summary>
        private static DateTime _timeLastFrame = DateTime.Now;

        /// <summary>
        /// Body seen by kinect flag
        /// </summary>
        private static bool _bodyCaptured;

        static void Main( string[] args )
        {
            // one sensor is currently supported
            _kinectSensor = KinectSensor.GetDefault();
            if (_kinectSensor == null)
            {
                Console.Error.WriteLine( "Error getting Kinect Sensor." );
                Close();
                return;
            }

            // open the reader for the body frames
            _bodyFrameReader = _kinectSensor.BodyFrameSource.OpenReader();
            if (_bodyFrameReader == null)
            {
                Console.Error.WriteLine("Error opening body frame reader.");
                Close();
                return;
            }
            
            // open file for output
            var outputFileName = args.Length > 0 ? args[0] : "kinect_output.csv";
            try
            {
                _outputStream = new StreamWriter(outputFileName);
                // write header
                _outputStream.WriteLine("# timestamp, jointType, position.X, position.Y, position.Z, orientation.X, orientation.Y, orientation.Z, orientation.W, state");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error opening output file: " + e.Message);
                Close();
                return;
            }

            // register to body frames
            _bodyFrameReader.FrameArrived += FrameArrived;

            Console.WriteLine($"{DateTime.Now:T}: Starting capture in file {outputFileName}");
            _timeLastFrame = DateTime.Now;

            // write status in console
            _timer = new Timer(o =>
            {
                double fps;
                var now = DateTime.Now;
                bool body;
                lock (Lock)
                {
                    fps = _frameCount / (now - _timeLastFrame).TotalSeconds;
                    _frameCount = 0;
                    _timeLastFrame = now;
                    body = _bodyCaptured;
                }
                Console.Write($"\rAcquiring at {fps:F1} fps. Tracking {(body ? 1 : 0)} body.");
            }, null, 1000, 1000);

            // open the sensor
            _kinectSensor.Open();

            // wait for X, Q or Ctrl+C events
            Console.CancelKeyPress += ConsoleHandler;
            while (true)
            {                
                // Start a console read operation. Do not display the input.
                var cki = Console.ReadKey( true );

                // Exit if the user pressed the 'X', 'Q' or ControlC key. 
                if (cki.Key == ConsoleKey.X || cki.Key == ConsoleKey.Q)
                {
                    break;
                }
            }
            Console.WriteLine(Environment.NewLine + $"{DateTime.Now:T}: Stoping capture");
            Close();
        }

        /// <summary>
        /// Prevents ControlC event to be handled by system (don't kill the application)
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="args">event arguments</param>
        private static void ConsoleHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine(Environment.NewLine + $"{DateTime.Now:T}: Stoping capture");
            Close();
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        private static void Close()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);

            if (_bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                _bodyFrameReader.Dispose();
                _bodyFrameReader = null;
            }

            if (_kinectSensor != null)
            {
                _kinectSensor.Close();
                _kinectSensor = null;
            }

            if (_outputStream != null)
            {
                _outputStream.Close();
                _outputStream = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        static void FrameArrived( object sender, BodyFrameArrivedEventArgs e )
        {
            var dataReceived = false;
            var time = new TimeSpan();

            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    time = bodyFrame.RelativeTime;
                    if (_bodies == null)
                    {
                        _bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData( _bodies );
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                var body = _bodies.FirstOrDefault( b => b.IsTracked );
                lock (Lock)
                {
                    _frameCount++;
                    _bodyCaptured = body != null;
                }
                if (body != null)
                {
                    OutputBody(time, body);
                }                                
            }
        }

        /// <summary>
        /// Output skeleton data to output file as [timestamp, jointType, position.X, position.Y, position.Z, orientation.X, orientation.Y, orientation.Z, orientation.W, state].
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="body"></param>
        static void OutputBody(TimeSpan timestamp, Body body)
        {
            var joints = body.Joints;
            var orientations = body.JointOrientations;

            // see https://msdn.microsoft.com/en-us/library/microsoft.kinect.jointtype.aspx for jointType Description
            foreach (var jointType in joints.Keys)
            {
                var position = joints[jointType].Position;
                var orientation = orientations[jointType].Orientation;
                _outputStream.WriteLine(string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", 
                    timestamp.TotalMilliseconds, 
                    (int)jointType, 
                    position.X, position.Y, position.Z,
                    orientation.X, orientation.Y, orientation.Z, orientation.W,
                    (int)joints[jointType].TrackingState));
            }
        }
    }
}
