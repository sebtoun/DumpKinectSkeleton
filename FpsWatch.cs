using System;

namespace DumpKinectSkeleton
{
    internal class FpsWatch
    {
        private int _frameCount;
        private DateTime _timeLastReset;

        public void Tick()
        {
            _frameCount++;
        }

        public void Reset()
        {
            _frameCount = 0;
            _timeLastReset = DateTime.Now;
        }

        public double GetFPS( bool reset )
        {
            var value = _frameCount / ( DateTime.Now - _timeLastReset ).TotalSeconds;
            if ( reset ) Reset();
            return value;
        }
    }
}
