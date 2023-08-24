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
    public class WaterMonitorTests
    {
        [TestMethod()]
        public void CalculateWaterTargetTest()
        {
            // target should range from 1.0f at 0.75f input to 0.0f at 0.85f input
            // and should be linear
            Assert.AreEqual(1.0f, WaterMonitor.CalculateInjectorTarget(0.75f));
            Assert.AreEqual(0.0f, WaterMonitor.CalculateInjectorTarget(0.81667f));
            Assert.AreEqual(0.1f, WaterMonitor.CalculateInjectorTarget(0.804f));
            Assert.AreEqual(0.9f, WaterMonitor.CalculateInjectorTarget(0.75066f));
            Assert.AreEqual(0.5f, WaterMonitor.CalculateInjectorTarget(0.76667f));
        }
    }
}