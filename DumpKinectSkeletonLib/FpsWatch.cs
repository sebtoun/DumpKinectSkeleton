using System;

namespace DumpKinectSkeletonLib
{
    internal class FpsWatch
    {
        private readonly double _updatePerdiod;

        public double Value { get; private set; }
        
        private int _frameCount;
        private DateTime _timeLastReset;

        public FpsWatch( double updatePerdiod )
        {
            _updatePerdiod = updatePerdiod;
        }

        public void Tick()
        {
            _frameCount++;
            var timeSinceLastReset = ( DateTime.Now - _timeLastReset ).TotalSeconds;
            if ( timeSinceLastReset >= _updatePerdiod )
            {
                Value = _frameCount / timeSinceLastReset;
                Reset();
            }
        }

        public void Reset()
        {
            _frameCount = 0;
            _timeLastReset = DateTime.Now;
        }        
    }
}
