using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Kinect;

namespace DumpKinectSkeleton
{
    internal class BodyFrameDumper
    {
        /// <summary>
        /// Body output file stream.
        /// </summary>
        private StreamWriter _bodyOutputStream;

        /// <summary>
        /// Array buffer for tracked bodies.
        /// </summary>
        private Body[] _bodies;

        /// <summary>
        /// Number of currently tracked bodies.
        /// </summary>
        public int BodyCount { get; private set; }

        public TimeSpan InitialTime;

        /// <summary>
        /// Create a new body frame dumper that dumps first tracked Body data to a csv file.
        /// </summary>
        /// <param name="kinectSource"></param>
        /// <param name="outputFileName"></param>
        public BodyFrameDumper( KinectSource kinectSource, string outputFileName )
        {
            // open file for output
            try
            {
                _bodyOutputStream = new StreamWriter( outputFileName );

                // write header
                _bodyOutputStream.WriteLine(
                    "# timestamp, jointType, position.X, position.Y, position.Z, orientation.X, orientation.Y, orientation.Z, orientation.W, state" );
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error opening output file: " + e.Message );
                Close();
                throw;
            }
            kinectSource.BodyFrameEvent += HandleBodyFrame;
            kinectSource.FirstFrameRelativeTimeEvent += ts => InitialTime = ts;
        }

        /// <summary>
        /// Close subjacent output streams
        /// </summary>
        public void Close()
        {
            BodyCount = 0;
            _bodyOutputStream?.Close();
            _bodyOutputStream = null;
        }

        /// <summary>
        /// Handle a BodyFrame. Dumps first tracked Body to output file.
        /// </summary>
        /// <param name="frame"></param>
        public void HandleBodyFrame( BodyFrame frame )
        {
            // throw an error is dumper has been closed or output stream could not be opened or written to.
            if ( _bodyOutputStream == null )
            {
                throw new InvalidOperationException( "BodyFrameDumper is closed." );
            }
            var time = frame.RelativeTime;

            // lazy body buffer initialization
            if ( _bodies == null )
            {
                _bodies = new Body[ frame.BodyCount ];
            }
            
            // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
            // As long as those body objects are not disposed and not set to null in the array,
            // those body objects will be re-used.
            frame.GetAndRefreshBodyData( _bodies );

            // read the tracking state of bodies
            BodyCount = 0;
            Body firstBody = null;
            foreach ( var body in _bodies.Where( body => body.IsTracked ) )
            {
                BodyCount++;
                if ( firstBody == null )
                {
                    firstBody = body;
                }
            }

            // dump first tracked body
            if ( BodyCount > 0 )
            {
                try
                {
                    OutputBody( time - InitialTime, firstBody );
                }
                catch ( Exception e )
                {
                    Console.Error.WriteLine( "Error writing to output file(s): " + e.Message );
                    Close();
                    throw;
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
                    (int)jointType,
                    position.X, position.Y, position.Z,
                    orientation.X, orientation.Y, orientation.Z, orientation.W,
                    (int)joints[ jointType ].TrackingState ) );
            }
        }
    }
}
