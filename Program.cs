using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace DumpKinectSkeleton
{
    class Program
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private static KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private static BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private static Body[] bodies = null;

        /// <summary>
        /// Output file stream
        /// </summary>
        private static StreamWriter outputStream = null;

        static void Main( string[] args )
        {
            // one sensor is currently supported
            kinectSensor = KinectSensor.GetDefault();
            if (kinectSensor == null)
            {
                Console.Error.WriteLine( "Error getting Kinect Sensor." );
                Close();
                return;
            }

            // open the reader for the body frames
            bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
            if (bodyFrameReader == null)
            {
                Console.Error.WriteLine("Error opening body frame reader.");
                Close();
                return;
            }
            
            // open file for output
            var outputFileName = args.Length > 0 ? args[0] : "kinect_output.csv";
            try
            {
                outputStream = new StreamWriter(outputFileName);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error opening output file: " + e.Message);
                Close();
                return;
            }

            // register to body frames
            bodyFrameReader.FrameArrived += FrameArrived;

            // open the sensor
            kinectSensor.Open();

            // wait for X, Q or Ctrl+C events
            Console.CancelKeyPress += new ConsoleCancelEventHandler( ConsoleHandler );
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
            Close();
        }

        /// <summary>
        /// Prevents ControlC event to be handled by system (don't kill the application)
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="args">event arguments</param>
        private static void ConsoleHandler(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private static void Close()
        {
            if (bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                bodyFrameReader.Dispose();
                bodyFrameReader = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }

            if (outputStream != null)
            {
                outputStream.Close();
                outputStream = null;
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
                    if (bodies == null)
                    {
                        bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData( bodies );
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                var body = bodies.FirstOrDefault( b => b.IsTracked );
                if (body != null)
                {
                    OutputBody(time, body);
                }
            }
        }

        /// <summary>
        /// Output skeleton data to output file with [timestamp, joint id, x, y, z].
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="body"></param>
        static void OutputBody(TimeSpan timestamp, Body body)
        {
            var joints = body.Joints;

            // see https://msdn.microsoft.com/en-us/library/microsoft.kinect.jointtype.aspx for jointType Description
            foreach (var jointType in joints.Keys)
            {
                var position = joints[jointType].Position;
                outputStream.WriteLine(string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0}, {1}, {2}, {3}, {4}", timestamp.TotalMilliseconds, (int)jointType, position.X, position.Y, position.Z));
            }
        }
    }
}
