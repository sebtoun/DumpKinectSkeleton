using System;
using Microsoft.Kinect;

namespace DumpKinectSkeletonLib
{
    public class KinectSource
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private readonly KinectSensor _kinectSensor;

        /// <summary>
        /// Reader for multi sources sync'ed frames
        /// </summary>
        private MultiSourceFrameReader _multiFrameReader;

        /// <summary>
        /// Reader for non sync'ed body frames
        /// </summary>
        private BodyFrameReader _bodyFrameReader;

        /// <summary>
        /// Reader for non sync'ed color frames
        /// </summary>
        private ColorFrameReader _colorFrameReader;

        public bool FrameSync { get; set; }

        /// <summary>
        /// Compute body stream frame rate.
        /// </summary>
        private readonly FpsWatch _bodySourceFpsWatcher = new FpsWatch( 1 );

        /// <summary>
        /// Get the skeleton stream frame rate.
        /// </summary>
        public double BodySourceFps
        {
            get { return _bodySourceFpsWatcher.Value; }
        }

        /// <summary>
        /// Compute color stream frame rate.
        /// </summary>
        private readonly FpsWatch _colorSourceFpsWatcher = new FpsWatch( 1 );

        /// <summary>
        /// Get the color stream frame rate.
        /// </summary>
        public double ColorSourceFps
        {
            get { return _colorSourceFpsWatcher.Value; }
        }

        public FrameDescription ColorFrameDescription
        {
            get { return _kinectSensor.ColorFrameSource.FrameDescription; }
        }

        /// <summary>
        /// First Frame RelativeTime event handler delegate.
        /// </summary>
        public delegate void FirstFrameRelativeTimeEventHandler( TimeSpan firstRelativeTime );

        private bool _firstFrameRelativeTimeEventFired;
        private bool _kinectUsedExternally = false;
        public event FirstFrameRelativeTimeEventHandler FirstFrameRelativeTimeEvent;

        /// <summary>
        /// Frame processing exception event handler delegate.
        /// </summary>
        /// <param name="exception"></param>
        public delegate void FrameProcessExceptionEventHandler( Exception exception );

        /// <summary>
        /// Event fired when an exception occured during frames processing.
        /// </summary>
        public event FrameProcessExceptionEventHandler FrameProcessExceptionEvent;

        /// <summary>
        /// Body frame processor event handler
        /// </summary>
        /// <param name="frame"></param>
        public delegate void BodyFrameEventHandler( BodyFrame frame );

        /// <summary>
        /// Event fired when a body frame is captured.
        /// </summary>
        public event BodyFrameEventHandler BodyFrameEvent;

        /// <summary>
        /// Color frame processor event handler
        /// </summary>
        /// <param name="frame"></param>
        public delegate void ColorFrameEventHandler( ColorFrame frame );

        /// <summary>
        /// Event fired when a color frame is captured.
        /// </summary>
        public event ColorFrameEventHandler ColorFrameEvent;

        /// <summary>
        /// Create a new kinect source, initialize Kinect sensor with one multi frame reader.
        /// </summary>
        public KinectSource()
        {
            _kinectSensor = KinectSensor.GetDefault();
            CheckSensor();
        }

        public KinectSource(KinectSensor sensor)
        {
            _kinectSensor = sensor;
            _kinectUsedExternally = true;
            CheckSensor();
        }

        private void CheckSensor()
        {
            if (_kinectSensor == null)
            {
                Close();
                throw new ApplicationException("Error getting Kinect Sensor.");
            }
        }
        
        private void OnFirstFrameRelativeTimeEvent( TimeSpan firstRelativeTime )
        {
            if ( FirstFrameRelativeTimeEvent != null )
            {
                FirstFrameRelativeTimeEvent( firstRelativeTime );
                _firstFrameRelativeTimeEventFired = true;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the multi frame reader.
        /// Dispatch each frames to corresponding processors and Dispose them.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="evt">event arguments</param>
        private void MultiFrameArrived( object sender, MultiSourceFrameArrivedEventArgs evt )
        {
            var frame = evt.FrameReference.AcquireFrame();
            if ( frame == null ) return;

            try
            {
                if ( BodyFrameEvent != null )
                {
                    using ( var bodyFrame = frame.BodyFrameReference.AcquireFrame() )
                    {
                        if ( !_firstFrameRelativeTimeEventFired )
                        {
                            OnFirstFrameRelativeTimeEvent( bodyFrame.RelativeTime );
                        }
                        BodyFrameEvent( bodyFrame );
                    }
                }
                _bodySourceFpsWatcher.Tick();
                if ( ColorFrameEvent != null )
                {
                    using ( var colorFrame = frame.ColorFrameReference.AcquireFrame() )
                    {
                        if ( !_firstFrameRelativeTimeEventFired )
                        {
                            OnFirstFrameRelativeTimeEvent( colorFrame.RelativeTime );
                        }
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
        /// Handle body frame arrived from dedicated Color frames reader. 
        /// Send frame to all registered processors and Dispose it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="evt"></param>
        private void BodyFrameArrived( object sender, BodyFrameArrivedEventArgs evt )
        {
            try
            {
                if ( BodyFrameEvent != null )
                {
                    using ( var bodyFrame = evt.FrameReference.AcquireFrame() )
                    {
                        if ( !_firstFrameRelativeTimeEventFired )
                        {
                            OnFirstFrameRelativeTimeEvent( bodyFrame.RelativeTime );
                        }
                        BodyFrameEvent( bodyFrame );
                    }
                }
                _bodySourceFpsWatcher.Tick();
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
        /// Handle color frame arrived from dedicated Color frames reader. 
        /// Send frame to all registered processors and Dispose it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="evt"></param>
        private void ColorFrameArrived( object sender, ColorFrameArrivedEventArgs evt )
        {
            try
            {
                if ( ColorFrameEvent != null )
                {
                    using ( var colorFrame = evt.FrameReference.AcquireFrame() )
                    {
                        if ( !_firstFrameRelativeTimeEventFired )
                        {
                            OnFirstFrameRelativeTimeEvent( colorFrame.RelativeTime );
                        }
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
        /// Start capture.
        /// </summary>
        public void Start()
        {
            if ( FrameSync )
            {
                // open streams using a synchronized readers, the frame rate is equal to the lowest of each separated stream.
                // select which streams to enable
                var features = FrameSourceTypes.None;
                if ( BodyFrameEvent != null )
                {
                    features |= FrameSourceTypes.Body;
                }
                if ( ColorFrameEvent != null )
                {
                    features |= FrameSourceTypes.Color;
                }
                if ( features == FrameSourceTypes.None )
                {
                    throw new ApplicationException( "No event processor registered." );
                }
                // check reader state
                if ( _multiFrameReader != null )
                {
                    throw new InvalidOperationException( "Kinect already started." );
                }
                // open the reader
                _multiFrameReader = _kinectSensor.OpenMultiSourceFrameReader( features );
                if ( _multiFrameReader == null )
                {
                    Close();
                    throw new ApplicationException( "Error opening readers." );
                }

                // register to frames
                _multiFrameReader.MultiSourceFrameArrived += MultiFrameArrived;
            }
            else
            {
                // open streams using separate readers, each one with the highest frame rate possible.
                // open body reader
                if ( BodyFrameEvent != null )
                {
                    if ( _bodyFrameReader != null )
                    {
                        throw new InvalidOperationException( "Kinect already started." );
                    }
                    _bodyFrameReader = _kinectSensor.BodyFrameSource.OpenReader();
                    if ( _bodyFrameReader == null )
                    {
                        Close();
                        throw new ApplicationException( "Error opening readers." );
                    }
                    _bodyFrameReader.FrameArrived += BodyFrameArrived;
                }
                // open color stream reader
                if ( ColorFrameEvent != null )
                {
                    if ( _colorFrameReader != null )
                    {
                        throw new InvalidOperationException( "Kinect already started." );
                    }
                    _colorFrameReader = _kinectSensor.ColorFrameSource.OpenReader();
                    if ( _colorFrameReader == null )
                    {
                        Close();
                        throw new ApplicationException( "Error opening readers." );
                    }
                    _colorFrameReader.FrameArrived += ColorFrameArrived;
                }
            }
            _firstFrameRelativeTimeEventFired = false;
            _kinectSensor.Open();
        }

        /// <summary>
        /// Close opened reader and sensors.
        /// </summary>
        public void Close()
        {
            if ( _multiFrameReader != null )
            {
                _multiFrameReader.MultiSourceFrameArrived -= MultiFrameArrived;
                _multiFrameReader.Dispose();
                _multiFrameReader = null;
            }

            if ( _colorFrameReader != null )
            {
                _colorFrameReader.FrameArrived -= ColorFrameArrived;
                _colorFrameReader.Dispose();
                _colorFrameReader = null;
            }

            if ( _bodyFrameReader != null )
            {
                _bodyFrameReader.FrameArrived -= BodyFrameArrived;
                _bodyFrameReader.Dispose();
                _bodyFrameReader = null;
            }

            if (!_kinectUsedExternally)
            {
                _kinectSensor?.Close();
            }
        }
    }
}
