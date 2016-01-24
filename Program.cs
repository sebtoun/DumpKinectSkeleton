using System;
using System.Threading;
using Microsoft.Kinect;

namespace DumpKinectSkeleton
{
    internal class Program
    {
        private const string DefaultBodyDataOutputFile = "kinect_output.csv";
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
        /// Dump body to file.
        /// </summary>
        private BodyFrameDumper _bodyFrameDumper;

        /// <summary>
        /// Dump body to file.
        /// </summary>
        private ColorFrameDumper _colorFrameDumper;

        /// <summary>
        /// Compute capture rate.
        /// </summary>
        private FpsWatch _fpsWatch;

        /// <summary>
        /// Status display timer
        /// </summary>
        private Timer _timer;
        
        /// <summary>
        /// Wether to dump Body data.
        /// </summary>
        private bool _dumpBodies;

        /// <summary>
        /// Wether to dump Video (color) data.
        /// </summary>
        private bool _dumpVideo;

        public void Run( bool dumpBodies, bool dumpVideo, string[] args )
        {
            _dumpBodies = dumpBodies;
            _dumpVideo = dumpVideo;

            // one sensor is currently supported
            _kinectSensor = KinectSensor.GetDefault();
            if ( _kinectSensor == null )
            {
                Console.Error.WriteLine( "Error getting Kinect Sensor." );
                Close();
                return;
            }

            var features = FrameSourceTypes.None;
            if ( _dumpBodies )
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
            var bodyOutputFileName = args.Length > 0 ? args[ 0 ] : DefaultBodyDataOutputFile;
            try
            {
                _bodyFrameDumper = new BodyFrameDumper( bodyOutputFileName );
                _colorFrameDumper = new ColorFrameDumper( DefaultColorDataOutputFile );
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error preparing dumpers: " + e.Message );
                Close();
                return;
            }

            // register to body frames
            _frameReader.MultiSourceFrameArrived += FrameArrived;

            Console.WriteLine(
                $"{DateTime.Now:T}: Starting capture in file {bodyOutputFileName}. Capturing video @{_kinectSensor.ColorFrameSource.FrameDescription.Width}x{_kinectSensor.ColorFrameSource.FrameDescription.Height}. Press X, Q or Control+C to stop capture." );

            _fpsWatch = new FpsWatch();

            // write status in console
            _timer = new Timer( o =>
            {
                var status = $"Acquiring at {_fpsWatch.GetFPS( true ):F1} fps.";
                if ( _dumpBodies )
                {
                    status += $"Tracking {_bodyFrameDumper.BodyCount} body(ies).";
                }
                Console.Write(  status + "\r" );
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
                _frameReader.Dispose();
                _frameReader = null;
            }

            if ( _kinectSensor != null )
            {
                _kinectSensor.Close();
                _kinectSensor = null;
            }

            if ( _bodyFrameDumper != null )
            {
                _bodyFrameDumper.Close();
                _bodyFrameDumper = null;
            }

            if ( _colorFrameDumper != null )
            {
                _colorFrameDumper.Close();
                _colorFrameDumper = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void FrameArrived( object sender, MultiSourceFrameArrivedEventArgs e )
        {
            var frame = e.FrameReference.AcquireFrame();
            if ( frame == null ) return;

            try
            {
                if ( _dumpBodies )
                {
                    using ( var bodyFrame = frame.BodyFrameReference.AcquireFrame() )
                    {

                        _bodyFrameDumper.HandleBodyFrame( bodyFrame );
                    }
                }
                if ( _dumpVideo )
                {
                    using ( var colorFrame = frame.ColorFrameReference.AcquireFrame() )
                    {
                        _colorFrameDumper.HandleColorFrame( colorFrame );
                    }
                }
                _fpsWatch.Tick();
            }
            catch ( Exception )
            {
                // todo exit
            }
        }

        public static void Main( string[] args )
        {
            new Program().Run( true, true, args );
        }
    }
}
