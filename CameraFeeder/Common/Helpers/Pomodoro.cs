using System;

namespace Feeder.Common.Helpers
{
    public class Pomodoro
    {
        private bool _isRunning;
        private uint _seconds;
        private long _startTimeStamp;
        private long _totalElapsed;


        public Pomodoro()
        {
            _seconds = 15*60; //default 15 minutes
            Reset();
        }

        public bool Ding
        {
            get { return Elapsed.TotalSeconds >= _seconds; }
        }

        public TimeSpan Elapsed
        {
            get { return new TimeSpan(getRawTicks()); }
        }

        public void Start()
        {
            if (!_isRunning)
            {
                _startTimeStamp = GetTimeStamp();
                _isRunning = true;
            }
        }

        private long GetTimeStamp()
        {
            return DateTime.UtcNow.Ticks;
        }

        public void Reset()
        {
            _totalElapsed = 0;
            _isRunning = false;
            _startTimeStamp = 0;
        }

        private long getRawTicks()
        {
            long _timeElapsed = _totalElapsed;
            if (_isRunning)
            {
                long _currentTimeStamp = GetTimeStamp();
                long _elapsedUntilNow = _currentTimeStamp - _startTimeStamp;
                _timeElapsed += _elapsedUntilNow;
            }
            return _timeElapsed;
        }


        public void Set(int seconds)
        {
            if (seconds < 0)
                return;
            if (_isRunning)
            {
                Stop();
                Reset();
            }
            _seconds = (uint) seconds;
        }

        public void Stop()
        {
            if (_isRunning)
            {
                long _endTimestamp = GetTimeStamp();
                long _elapsed = _endTimestamp - _startTimeStamp;
                _totalElapsed += _elapsed;
                _isRunning = false;
            }
        }
    }
}
