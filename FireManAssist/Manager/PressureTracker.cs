using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireManAssist
{
    public enum Trend
    {
        Rising,
        Falling,
        Steady
    }
    public class PressureTracker
    {
        private float[] _pressureHistory = new float[5];
        private int _historyIndex = 0;
        public Trend UpdateAndCheckTrend(float pressure)
        {
            var lastPressure = GetLastPressure();
            _pressureHistory[_historyIndex] = pressure;
            _historyIndex++;
            if (_historyIndex == 5)
            {
                _historyIndex = 0;
            }
            var oldestPressure = getOldestPresure();
            if (oldestPressure == 0f)
            {
                return Trend.Steady;
            }
            return GetTrend(oldestPressure, lastPressure, pressure);
        }
        private float GetLastPressure()
        {
            var lastPressureIndex = _historyIndex - 1;
            if (lastPressureIndex < 0)
            {
                lastPressureIndex = 4;
            }
            return _pressureHistory[lastPressureIndex];
        }
        private float getOldestPresure()
        {
            return _pressureHistory[_historyIndex];
        }
        private Trend GetTrend(float oldestPressure, float lastPressure, float pressure)
        {
            var immediateTrend = GetTrendFromStates(lastPressure, pressure);
            var longTermTrend = GetTrendFromStates(oldestPressure, pressure);
            if (immediateTrend == longTermTrend)
            {
                return immediateTrend;
            }
            else
            {
                return Trend.Steady;
            }
        }

        private Trend GetTrendFromStates(float lastPressure, float pressure)
        {
            float difference = pressure - lastPressure;
            return DifferenceToTrend(difference);
        }

        private Trend DifferenceToTrend(float difference)
        {
            if (difference > 0)
            {
                return Trend.Rising;
            }
            else if (difference < 0)
            {
                return Trend.Falling;
            }
            else
            {
                return Trend.Steady;
            }
        }
    }
}
