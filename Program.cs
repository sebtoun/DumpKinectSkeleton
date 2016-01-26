using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Kinect;
using CommandLine;
using CommandLine.Text;
using Timer = System.Threading.Timer;

namespace DumpKinectSkeleton
{
    internal class Program
    {
        private const string BodyDataOutputFileSuffix = "_body.csv";
        private const string ColorDataOutputFileSuffix = "_color.yuy2";

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

        [Option( 'v', "video", HelpText = "Dump color video stream data as a yuy2 raw format." )]
        public bool DumpVideo { get; set; }

        [Option( "prefix", DefaultValue = "output", MetaValue = "PREFIX", HelpText = "Output files prefix." )]
        public string BaseOutputFile { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild( this, ( HelpText current ) => HelpText.DefaultParsingErrorsHandler( this, current ) );
        }

        public void Run()
        {
            if ( !InitializeKinect( DumpVideo ) )
            {
                Close();
                return;
            }

            // initialize dumpers
            try
            {
                _bodyFrameDumper = new BodyFrameDumper( BaseOutputFile + BodyDataOutputFileSuffix );
                if ( DumpVideo )
                {
                    _colorFrameDumper = new ColorFrameDumper( BaseOutputFile + ColorDataOutputFileSuffix );
                }
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error preparing dumpers: " + e.Message );
                Close();
                return;
            }

            Console.WriteLine( "Starting capture." );
            Console.WriteLine( $"Ouput skeleton data in file {BaseOutputFile + BodyDataOutputFileSuffix}." );
            if ( DumpVideo )
            {
                Console.WriteLine( $"Video stream @{_kinectSensor.ColorFrameSource.FrameDescription.Width}x{_kinectSensor.ColorFrameSource.FrameDescription.Height} outputed in file {BaseOutputFile + ColorDataOutputFileSuffix}." );
            }
            Console.WriteLine( "Press X, Q or Control + C to stop capture." );
            Console.WriteLine();

            // write status in console every seconds
            _fpsWatch = new FpsWatch();
            _timer = new Timer( o =>
            {
                Console.Write( $"Acquiring at {_fpsWatch.GetFPS( true ):F1} fps." );
                Console.Write( $" Tracking {_bodyFrameDumper.BodyCount} body(ies)." );
                Console.Write( "\r" );
            }, null, 1000, 1000 );

            // open the sensor
            _kinectSensor.Open();

            // wait for X, Q or Ctrl+C events to exit
            Console.CancelKeyPress += (sender, args) => Terminate();
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
            Terminate();
        }

        private void Terminate()
        {
            Console.WriteLine( Environment.NewLine + $"{DateTime.Now:T}: Stopping capture" );
            Close();
        }

        /// <summary>
        /// Initialize Kinect sensor with one multi frame reader.
        /// </summary>
        /// <param name="dumpBodies"></param>
        /// <param name="dumpVideo"></param>
        /// <returns></returns>
        private bool InitializeKinect( bool dumpVideo )
        {
            _kinectSensor = KinectSensor.GetDefault();
            if ( _kinectSensor == null )
            {
                Console.Error.WriteLine( "Error getting Kinect Sensor." );
                return false;
            }

            var features = FrameSourceTypes.Body;
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
        /// <param name="evt">event arguments</param>
        private void FrameArrived( object sender, MultiSourceFrameArrivedEventArgs evt )
        {
            var frame = evt.FrameReference.AcquireFrame();
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
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error: " + e.Message );
                SendKeys.SendWait( "Q" );
            }
        }

        public static void Main( string[] args )
        {
            var main = new Program();
            if ( CommandLine.Parser.Default.ParseArguments( args, main ) )
            {
                main.Run();
            }
        }
    }
}
