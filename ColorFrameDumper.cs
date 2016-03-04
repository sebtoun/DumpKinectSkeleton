using System;
using System.IO;
using Microsoft.Kinect;

namespace DumpKinectSkeleton
{
    internal class ColorFrameDumper
    {
        /// <summary>
        /// Array storing latest color frame pixels.
        /// </summary>
        private byte[] _colorFrameBytes;

        /// <summary>
        /// Color frames output file stream.
        /// </summary>
        private Stream _colorOutputStream;

        public ColorFrameDumper( KinectSource kinectSource, string colorDataOutputFile )
        {
            // open file for output
            try
            {
                _colorOutputStream = new BufferedStream( new FileStream( colorDataOutputFile, FileMode.Create ) );
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error opening output file: " + e.Message );
                Close();
                throw;
            }
            kinectSource.ColorFrameEvent += HandleColorFrame;
        }

        /// <summary>
        /// Close subjacent output streams
        /// </summary>
        public void Close()
        {
            _colorOutputStream?.Close();
            _colorOutputStream = null;
        }

        /// <summary>
        /// Handle a ColorFrame. Dumps the frame in raw kinect YUY2 format.
        /// </summary>
        /// <param name="frame"></param>
        public void HandleColorFrame( ColorFrame frame )
        {
            // throw an error is dumper has been closed or output stream could not be opened or written to.
            if ( _colorOutputStream == null )
            {
                throw new InvalidOperationException( "ColorFrameDumper is closed." );
            }
            // lazy color frame buffer initialization
            if ( _colorFrameBytes == null )
            {
                _colorFrameBytes =
                    new byte[ frame.FrameDescription.LengthInPixels * frame.FrameDescription.BytesPerPixel ];
            }

            if ( frame.RawColorImageFormat != ColorImageFormat.Yuy2 )
            {
                frame.CopyConvertedFrameDataToArray( _colorFrameBytes, ColorImageFormat.Yuy2 );
            }
            else
            {
                frame.CopyRawFrameDataToArray( _colorFrameBytes );
            }

            try
            {
                _colorOutputStream.Write( _colorFrameBytes, 0, _colorFrameBytes.Length );
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error writing to output file(s): " + e.Message );
                Close();
                throw;
            }
        }        
    }
}
