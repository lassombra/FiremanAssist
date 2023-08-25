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
        public void CalculateInjectorTargetTest_Default()
        {
            // target should range from 1.0f at 0.75f input to 0.0f at 0.85f input
            // and should be linear
            Assert.AreEqual(1.0f, WaterMonitor.CalculateInjectorTarget(0.75f, 2.0f, 0.75f, 0.81667f));
            Assert.AreEqual(0.0f, WaterMonitor.CalculateInjectorTarget(0.81667f, 2.0f, 0.75f, 0.81667f));
            Assert.AreEqual(0.1f, WaterMonitor.CalculateInjectorTarget(0.804f, 2.0f, 0.75f, 0.81667f));
            Assert.AreEqual(0.9f, WaterMonitor.CalculateInjectorTarget(0.75066f, 2.0f, 0.75f, 0.81667f));
            Assert.AreEqual(0.5f, WaterMonitor.CalculateInjectorTarget(0.76667f, 2.0f, 0.75f, 0.81667f));
        }

        [TestMethod()]
        public void CalculateInjectorTargetTest_HighPressure()
        {
            Assert.AreEqual(1.0f, WaterMonitor.CalculateInjectorTarget(0.81f, 1 / 3.0f, 0.8f, 0.85f));
            Assert.AreEqual(0.0f, WaterMonitor.CalculateInjectorTarget(0.85f, 1 / 3.0f, 0.8f, 0.85f));
            Assert.AreEqual(0.1f, WaterMonitor.CalculateInjectorTarget(0.849f, 1 / 3.0f, 0.8f, 0.85f));
            Assert.AreEqual(0.9f, WaterMonitor.CalculateInjectorTarget(0.82f, 1 / 3.0f, 0.8f, 0.85f));
            Assert.AreEqual(0.5f, WaterMonitor.CalculateInjectorTarget(0.84f, 1 / 3.0f, 0.8f, 0.85f));
        }

        [TestMethod()]
        public void CalculateInjectorTargetTest_LowPressure()
        {
            Assert.AreEqual(1.0f, WaterMonitor.CalculateInjectorTarget(0.75f, 4.0f, 0.75f, 0.8f));
            Assert.AreEqual(0.0f, WaterMonitor.CalculateInjectorTarget(0.8f, 4.0f, 0.75f, 0.8f));
            Assert.AreEqual(0.1f, WaterMonitor.CalculateInjectorTarget(0.79f, 4.0f, 0.75f, 0.8f));
            Assert.AreEqual(0.9f, WaterMonitor.CalculateInjectorTarget(0.75001f, 4.0f, 0.75f, 0.8f));
            Assert.AreEqual(0.5f, WaterMonitor.CalculateInjectorTarget(0.754f, 4.0f, 0.75f, 0.8f));
        }
    }
}