using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Kinect;

namespace DumpKinectSkeleton
{
    internal class Program
    {
        private const string DefaultKinectDataOutputFile = "kinect_output.csv";
        private const string DefaultColorDataOutputFile = "color_output.yuv";

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor _kinectSensor;

        /// <summary>
        /// Reader for multi sources sync'ed frames
        /// </summary>
        private MultiSourceFrameReader _frameReader;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] _bodies;

        /// <summary>
        /// Array storing latest color frame pixels.
        /// </summary>
        private byte[] _colorFrameBytes;

        /// <summary>
        /// Body output file stream.
        /// </summary>
        private StreamWriter _bodyOutputStream;

        /// <summary>
        /// Color frames output file stream.
        /// </summary>
        private Stream _colorOutputStream;

        /// <summary>
        /// Status display timer
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// Count frames between status update (for fps count).
        /// </summary>
        private float _frameCount;

        /// <summary>
        /// Wether to dump Body data.
        /// </summary>
        private bool _dumpSkeleton = true;

        /// <summary>
        /// Wether to dump Video (color) data.
        /// </summary>
        private bool _dumpVideo = true;

        /// <summary>
        /// Status synchronization lock
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Body seen by kinect flag
        /// </summary>
        private bool _bodyCaptured;

        public void Run( string[] args )
        {
            // one sensor is currently supported
            _kinectSensor = KinectSensor.GetDefault();
            if ( _kinectSensor == null )
            {
                Console.Error.WriteLine( "Error getting Kinect Sensor." );
                Close();
                return;
            }

            var features = FrameSourceTypes.None;
            if ( _dumpSkeleton )
            {
                features |= FrameSourceTypes.Body;
            }
            if ( _dumpVideo )
            {
                features |= FrameSourceTypes.Color;
            }

            if ( features == FrameSourceTypes.None )
            {
                Console.Error.WriteLine( "No source selected." );
                Close();
                return;
            }

            // open the reader for the body frames
            _frameReader = _kinectSensor.OpenMultiSourceFrameReader( features );
            if ( _frameReader == null )
            {
                Console.Error.WriteLine( "Error opening body frame reader." );
                Close();
                return;
            }

            // open file for output
            var bodyOutputFileName = args.Length > 0 ? args[ 0 ] : DefaultKinectDataOutputFile;
            try
            {
                _bodyOutputStream = new StreamWriter( bodyOutputFileName );
                _colorOutputStream = new FileStream( DefaultColorDataOutputFile, FileMode.Create );

                // write header
                _bodyOutputStream.WriteLine(
                    "# timestamp, jointType, position.X, position.Y, position.Z, orientation.X, orientation.Y, orientation.Z, orientation.W, state" );
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error opening output file(s): " + e.Message );
                Close();
                return;
            }

            // register to body frames
            _frameReader.MultiSourceFrameArrived += FrameArrived;

            Console.WriteLine(
                $"{DateTime.Now:T}: Starting capture in file {bodyOutputFileName}. Capturing video @{_kinectSensor.ColorFrameSource.FrameDescription.Width}x{_kinectSensor.ColorFrameSource.FrameDescription.Height}. Press X, Q or Control+C to stop capture." );
            
            var timeLastStatus = DateTime.Now;
            
            // write status in console
            _timer = new Timer( o =>
            {
                double fps;
                var now = DateTime.Now;
                bool body;
                lock ( _lock )
                {
                    fps = _frameCount / ( now - timeLastStatus ).TotalSeconds;
                    _frameCount = 0;
                    timeLastStatus = now;
                    body = _bodyCaptured;
                }
                Console.Write( $"Acquiring at {fps:F1} fps. Tracking {( body ? 1 : 0 )} body.     \r" );
            }, null, 1000, 1000 );

            // open the sensor
            _kinectSensor.Open();

            // wait for X, Q or Ctrl+C events
            Console.CancelKeyPress += ConsoleHandler;
            while ( true )
            {
                // Start a console read operation. Do not display the input.
                var cki = Console.ReadKey( true );

                // Exit if the user pressed the 'X', 'Q' or ControlC key. 
                if ( cki.Key == ConsoleKey.X || cki.Key == ConsoleKey.Q )
                {
                    break;
                }
            }
            Console.WriteLine( Environment.NewLine + $"{DateTime.Now:T}: Stoping capture" );
            Close();
        }

        /// <summary>
        /// Prevents ControlC event to be handled by system (don't kill the application)
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="args">event arguments</param>
        private void ConsoleHandler( object sender, ConsoleCancelEventArgs args )
        {
            //args.Cancel = true;
            Console.WriteLine( Environment.NewLine + $"{DateTime.Now:T}: Stoping capture" );
            Close();
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        private void Close()
        {
            _timer?.Change( Timeout.Infinite, Timeout.Infinite );

            if ( _frameReader != null )
            {
                // BodyFrameReader is IDisposable
                _frameReader.Dispose();
                _frameReader = null;
            }

            if ( _kinectSensor != null )
            {
                _kinectSensor.Close();
                _kinectSensor = null;
            }

            if ( _bodyOutputStream != null )
            {
                _bodyOutputStream.Close();
                _bodyOutputStream = null;
            }

            if ( _colorOutputStream != null )
            {
                _colorOutputStream.Close();
                _colorOutputStream = null;
            }            
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void FrameArrived( object sender, MultiSourceFrameArrivedEventArgs e )
        {
            var dataReceived = false;
            var time = new TimeSpan();

            var frame = e.FrameReference.AcquireFrame();
            if ( frame != null )
            {
                if ( _dumpVideo )
                {
                    using ( var colorFrame = frame.ColorFrameReference.AcquireFrame() )
                    {
                        if ( _colorFrameBytes == null )
                        {
                            _colorFrameBytes =
                                new byte[
                                    colorFrame.FrameDescription.LengthInPixels * colorFrame.FrameDescription.BytesPerPixel ];
                        }

                        if ( colorFrame.RawColorImageFormat != ColorImageFormat.Yuy2 )
                        {
                            colorFrame.CopyConvertedFrameDataToArray( _colorFrameBytes, ColorImageFormat.Yuy2 );
                        }
                        else
                        {
                            colorFrame.CopyRawFrameDataToArray( _colorFrameBytes );
                        }
                        dataReceived = true;
                    }
                }
                if ( _dumpSkeleton )
                {
                    using ( var bodyFrame = frame.BodyFrameReference.AcquireFrame() )
                    {
                        time = bodyFrame.RelativeTime;
                        if ( _bodies == null )
                        {
                            _bodies = new Body[ bodyFrame.BodyCount ];
                        }
                        
                        // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                        // As long as those body objects are not disposed and not set to null in the array,
                        // those body objects will be re-used.
                        bodyFrame.GetAndRefreshBodyData( _bodies );
                        dataReceived = true;
                    }
                }
            }

            if ( dataReceived )
            {
                if ( _dumpSkeleton )
                {
                    var body = _bodies.FirstOrDefault( b => b.IsTracked );
                    _bodyCaptured = body != null;
                    if ( body != null )
                    {
                        OutputBody( time, body );
                        if ( _dumpVideo )
                        {
                            OutputColorFrame( _colorFrameBytes );                            
                        }
                    }
                }
                else if ( _dumpVideo )
                {
                    OutputColorFrame( _colorFrameBytes );
                }

                lock ( _lock )
                {
                    _frameCount++;
                }
            }
        }

        /// <summary>
        /// Output skeleton data to output file as [timestamp, jointType, position.X, position.Y, position.Z, orientation.X, orientation.Y, orientation.Z, orientation.W, state].
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="body"></param>
        private void OutputBody( TimeSpan timestamp, Body body )
        {
            try
            {
                var joints = body.Joints;
                var orientations = body.JointOrientations;

                // see https://msdn.microsoft.com/en-us/library/microsoft.kinect.jointtype.aspx for jointType Description
                foreach ( var jointType in joints.Keys )
                {
                    var position = joints[ jointType ].Position;
                    var orientation = orientations[ jointType ].Orientation;
                    _bodyOutputStream.WriteLine( string.Format( CultureInfo.InvariantCulture.NumberFormat,
                        "{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}",
                        timestamp.TotalMilliseconds,
                        (int) jointType,
                        position.X, position.Y, position.Z,
                        orientation.X, orientation.Y, orientation.Z, orientation.W,
                        (int) joints[ jointType ].TrackingState ) );
                }
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error writing to output file(s): " + e.Message );
                Close();
            }
        }

        /// <summary>
        /// Ouput color frame in yuy2 format in file.
        /// </summary>
        /// <param name="frame">the bytes representing the frame.</param>
        private void OutputColorFrame( byte[] frame )
        {
            try
            {                
                _colorOutputStream.Write( frame, 0, frame.Length );
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error writing to output file(s): " + e.Message );
                Close();
            }
        }

        public static void Main( string[] args )
        {
            new Program().Run( args );
        }
    }
}
