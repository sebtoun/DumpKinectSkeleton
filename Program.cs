using System;
using System.Threading;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using Timer = System.Threading.Timer;

namespace DumpKinectSkeleton
{
    internal class Program
    {
        private const string BodyDataOutputFileSuffix = "_body.csv";
        private const string ColorDataOutputFileSuffix = "_color.yuy2";

        private KinectSource _kinectSource;

        /// <summary>
        /// Dump body to file.
        /// </summary>
        private BodyFrameDumper _bodyFrameDumper;

        /// <summary>
        /// Dump body to file.
        /// </summary>
        private ColorFrameDumper _colorFrameDumper;
        
        /// <summary>
        /// Status display timer
        /// </summary>
        private Timer _timer;

        [Option( 'v', "video", HelpText = "Dump color video stream data as a yuy2 raw format." )]
        public bool DumpVideo { get; set; }

        [Option( 's', "synchronize", HelpText = "Synchronize streams." )]
        public bool Synchronize { get; set; }

        [Option( "prefix", DefaultValue = "output", MetaValue = "PREFIX", HelpText = "Output files prefix." )]
        public string BaseOutputFile { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild( this, current => HelpText.DefaultParsingErrorsHandler( this, current ) );
        }

        public void Run()
        {
            try
            {
                _kinectSource = new KinectSource();
                _kinectSource.FrameSync = Synchronize;
                _kinectSource.FrameProcessExceptionEvent += e =>
                {
                    Console.Error.WriteLine( "Error: " + e.Message );
                    Terminate();
                };                
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error initializing Kinect: " + e.Message );
                Cleanup();
                return;
            }

            // initialize dumpers
            try
            {
                _bodyFrameDumper = new BodyFrameDumper( _kinectSource, BaseOutputFile + BodyDataOutputFileSuffix );
                if ( DumpVideo )
                {
                    _colorFrameDumper = new ColorFrameDumper( _kinectSource, BaseOutputFile + ColorDataOutputFileSuffix );
                }
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error preparing dumpers: " + e.Message );
                Cleanup();
                return;
            }

            Console.WriteLine( "Starting capture" );
            Console.WriteLine( $"Ouput skeleton data in file {BaseOutputFile + BodyDataOutputFileSuffix}" );
            if ( DumpVideo )
            {
                Console.WriteLine( $"Video stream @{_kinectSource.ColorFrameDescription.Width}x{_kinectSource.ColorFrameDescription.Height} outputed in file {BaseOutputFile + ColorDataOutputFileSuffix}" );
            }
            Console.WriteLine( "Press X, Q or Control + C to stop capture" );
            Console.WriteLine();

            Console.WriteLine( "Capture rate(s):" );
            // write status in console every seconds
            _timer = new Timer( o =>
            {
                Console.Write( $"{_bodyFrameDumper.BodyCount} Skeleton(s) @ { _kinectSource.BodySourceFps:F1} Fps" );
                if ( DumpVideo )
                {
                    Console.Write( $" - Color Frames @ { _kinectSource.ColorSourceFps:F1} Fps" );
                }
                Console.Write( "\r" );
            }, null, 1000, 1000 );

            // start capture
            _kinectSource.Start();
            
            // wait for X, Q or Ctrl+C events to exit
            Console.CancelKeyPress += (sender, args) => Cleanup();
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
            Cleanup();
        }

        private void Cleanup()
        {
            Console.WriteLine( Environment.NewLine + $"Stopping capture" );
            Close();
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        private void Close()
        {
            _timer?.Change( Timeout.Infinite, Timeout.Infinite );

            if ( _kinectSource != null )
            {
                _kinectSource.Close();
                _kinectSource = null;
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

        private void Terminate()
        {
            SendKeys.SendWait( "Q" );
        }

        public static void Main( string[] args )
        {
            var main = new Program();
            if ( Parser.Default.ParseArguments( args, main ) )
            {
                main.Run();
            }
        }
    }
}
