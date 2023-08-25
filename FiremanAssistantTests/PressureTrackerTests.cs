using Microsoft.VisualStudio.TestTools.UnitTesting;
using FireManAssist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireManAssist.Tests
{
    [TestClass()]
    public class PressureTrackerTests
    {
        [TestMethod()]
        public void IntialTrendIsNeutralUntil5ReceivedTest()
        {
            var tracker = new PressureTracker();
            Assert.AreEqual(Trend.Steady, tracker.UpdateAndCheckTrend(13f));
            Assert.AreEqual(Trend.Steady, tracker.UpdateAndCheckTrend(13f));
            Assert.AreEqual(Trend.Steady, tracker.UpdateAndCheckTrend(13f));
            Assert.AreEqual(Trend.Steady, tracker.UpdateAndCheckTrend(13f));
            Assert.AreNotEqual(Trend.Steady, tracker.UpdateAndCheckTrend(14f));
        }
        [TestMethod()]
        public void ImmediateTrendAndLongTermTrendDifferenceIsNeutralTest()
        {
            var tracker = new PressureTracker();
            tracker.UpdateAndCheckTrend(12f);
            tracker.UpdateAndCheckTrend(13f);
            tracker.UpdateAndCheckTrend(14f);
            tracker.UpdateAndCheckTrend(13.5f);
            Assert.AreEqual(Trend.Steady, tracker.UpdateAndCheckTrend(13f));

            Assert.AreEqual(Trend.Steady, tracker.UpdateAndCheckTrend(13f));
        }

        [TestMethod()]
        public void ImmediateAndLongTermRiseTest()
        {
            var tracker = new PressureTracker();
            tracker.UpdateAndCheckTrend(12f);
            tracker.UpdateAndCheckTrend(12.5f);
            tracker.UpdateAndCheckTrend(13f);
            tracker.UpdateAndCheckTrend(13.5f);
            Assert.AreEqual(Trend.Rising, tracker.UpdateAndCheckTrend(14f));
            Assert.AreEqual(Trend.Steady, tracker.UpdateAndCheckTrend(13.5f));
        }

        [TestMethod()]
        public void ImmediateAndLongTermFallTest()
        {
            var tracker = new PressureTracker();
            tracker.UpdateAndCheckTrend(14f);
            tracker.UpdateAndCheckTrend(13.8f);
            tracker.UpdateAndCheckTrend(13.7f);
            tracker.UpdateAndCheckTrend(13.2f);
            Assert.AreEqual(Trend.Falling, tracker.UpdateAndCheckTrend(13f));
            Assert.AreEqual(Trend.Steady, tracker.UpdateAndCheckTrend(13.1f));
        }

        [TestMethod()]
        public void ImmedateEqualIsSteadyTest()
        {
            var tracker = new PressureTracker();
            tracker.UpdateAndCheckTrend(14f);
            tracker.UpdateAndCheckTrend(13.8f);
            tracker.UpdateAndCheckTrend(13.7f);
            tracker.UpdateAndCheckTrend(13.2f);
            Assert.AreEqual(Trend.Steady, tracker.UpdateAndCheckTrend(13.2f));
            Assert.AreEqual(Trend.Falling, tracker.UpdateAndCheckTrend(13f));
            Assert.AreEqual(Trend.Steady, tracker.UpdateAndCheckTrend(13.7f));
        }
    }
}