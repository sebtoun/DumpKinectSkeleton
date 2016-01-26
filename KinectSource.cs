using System;
using System.Collections.Generic;
using Microsoft.Kinect;

namespace DumpKinectSkeleton
{
    public class KinectSource
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor _kinectSensor;

        /// <summary>
        /// Reader for multi sources sync'ed frames
        /// </summary>
        private MultiSourceFrameReader _frameReader;

        /// <summary>
        /// Compute body capture rate.
        /// </summary>
        private readonly FpsWatch _bodySourceFpsWatcher = new FpsWatch( 1 );

        /// <summary>
        /// Get the skeleton stream frame rate.
        /// </summary>
        public double BodySourceFPS
        {
            get { return _bodySourceFpsWatcher.Value; }
        }

        /// <summary>
        /// Compute color capture rate.
        /// </summary>
        private readonly FpsWatch _colorSourceFpsWatcher = new FpsWatch( 1 );
        
        /// <summary>
        /// Get the color stream frame rate.
        /// </summary>
        public double ColorSourceFPS
        {
            get { return _colorSourceFpsWatcher.Value; }
        }

        public FrameDescription ColorFrameDescription
        {
            get { return _kinectSensor.ColorFrameSource.FrameDescription; }
        }

        /// <summary>
        /// Frame processing exception event handler delegate.
        /// </summary>
        /// <param name="exception"></param>
        public delegate void FrameProcessExceptionEventHandler( Exception exception );

        /// <summary>
        /// Event fired when an exception occured during frames processing.
        /// </summary>
        public event FrameProcessExceptionEventHandler FrameProcessExceptionEvent;
        
        public delegate void BodyFrameEventHandler( BodyFrame frame );
        public event BodyFrameEventHandler BodyFrameEvent;

        public delegate void ColorFrameEventHandler( ColorFrame frame );
        public event ColorFrameEventHandler ColorFrameEvent;

        /// <summary>
        /// Create a new kinect source, initialize Kinect sensor with one multi frame reader.
        /// </summary>
        public KinectSource()
        {
            _kinectSensor = KinectSensor.GetDefault();
            if ( _kinectSensor == null )
            {
                Close();
                throw new ApplicationException( "Error getting Kinect Sensor." );
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
                if ( BodyFrameEvent != null )
                {
                    using ( var bodyFrame = frame.BodyFrameReference.AcquireFrame() )
                    {
                        BodyFrameEvent( bodyFrame );
                    }
                }
                _bodySourceFpsWatcher.Tick();
                if ( ColorFrameEvent != null )
                {
                    using ( var colorFrame = frame.ColorFrameReference.AcquireFrame() )
                    {
                        ColorFrameEvent( colorFrame );
                    }
                }
                _colorSourceFpsWatcher.Tick();
            }
            catch ( Exception e )
            {
                if ( FrameProcessExceptionEvent != null )
                {
                    FrameProcessExceptionEvent( e );
                }
            }
        }

        /// <summary>
        /// Close opened reader and sensors.
        /// </summary>
        public void Close()
        {
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
        }

        /// <summary>
        /// Start capture.
        /// </summary>
        public void Start()
        {
            var features = FrameSourceTypes.None;
            if ( BodyFrameEvent != null )
            {
                features |= FrameSourceTypes.Body;
            }
            if ( ColorFrameEvent != null )
            {
                features |= FrameSourceTypes.Color;
            }

            // open the reader for the body frames
            _frameReader = _kinectSensor.OpenMultiSourceFrameReader( features );
            if ( _frameReader == null )
            {
                Close();
                throw new ApplicationException( "Error opening readers." );
            }

            // register to frames
            _frameReader.MultiSourceFrameArrived += FrameArrived;

            _kinectSensor.Open();
        }
    }
}
