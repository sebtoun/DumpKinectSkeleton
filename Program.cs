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
        
        public void Run( bool dumpBodies, bool dumpVideo, string[] args )
        {
            if ( !InitializeKinect( dumpBodies, dumpVideo ) )
            {
                Close();
                return;
            }

            // initialize dumpers
            var bodyOutputFileName = args.Length > 0 ? args[ 0 ] : DefaultBodyDataOutputFile;
            try
            {
                if ( dumpBodies )
                {
                    _bodyFrameDumper = new BodyFrameDumper( bodyOutputFileName );
                }
                if ( dumpVideo )
                {
                    _colorFrameDumper = new ColorFrameDumper( DefaultColorDataOutputFile );
                }
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error preparing dumpers: " + e.Message );
                Close();
                return;
            }

            Console.WriteLine(
                $"{DateTime.Now:T}: Starting capture in file {bodyOutputFileName}. Capturing video @{_kinectSensor.ColorFrameSource.FrameDescription.Width}x{_kinectSensor.ColorFrameSource.FrameDescription.Height}. Press X, Q or Control+C to stop capture." );

            // write status in console every seconds
            _fpsWatch = new FpsWatch();
            _timer = new Timer( o =>
            {
                var status = $"Acquiring at {_fpsWatch.GetFPS( true ):F1} fps.";
                if ( dumpBodies )
                {
                    status += $"Tracking {_bodyFrameDumper.BodyCount} body(ies).";
                }
                Console.Write(  status + "\r" );
            }, null, 1000, 1000 );

            // open the sensor
            _kinectSensor.Open();

            // wait for X, Q or Ctrl+C events to exit
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
        /// Initialize Kinect sensor with one multi frame reader.
        /// </summary>
        /// <param name="dumpBodies"></param>
        /// <param name="dumpVideo"></param>
        /// <returns></returns>
        private bool InitializeKinect( bool dumpBodies, bool dumpVideo )
        {
            _kinectSensor = KinectSensor.GetDefault();
            if ( _kinectSensor == null )
            {
                Console.Error.WriteLine( "Error getting Kinect Sensor." );
                return false;
            }

            var features = FrameSourceTypes.None;
            if ( dumpBodies )
            {
                features |= FrameSourceTypes.Body;
            }
            if ( dumpVideo )
            {
                features |= FrameSourceTypes.Color;
            }

            if ( features == FrameSourceTypes.None )
            {
                Console.Error.WriteLine( "No source selected." );
                return false;
            }

            // open the reader for the body frames
            _frameReader = _kinectSensor.OpenMultiSourceFrameReader( features );
            if ( _frameReader == null )
            {
                Console.Error.WriteLine( "Error opening body frame reader." );
                return false;
            }

            // register to frames
            _frameReader.MultiSourceFrameArrived += FrameArrived;
            return true;
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
                if ( _bodyFrameDumper != null )
                {
                    using ( var bodyFrame = frame.BodyFrameReference.AcquireFrame() )
                    {
                        _bodyFrameDumper.HandleBodyFrame( bodyFrame );
                    }
                }
                if ( _colorFrameDumper != null )
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
