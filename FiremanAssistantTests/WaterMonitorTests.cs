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
            Assert.AreEqual(1.0f, WaterMonitor.CalculateInjectorTargetCurve(0.75f, WaterCurve.Default));
            Assert.AreEqual(0.0f, WaterMonitor.CalculateInjectorTargetCurve(0.81667f, WaterCurve.Default));
            Assert.AreEqual(0.1f, WaterMonitor.CalculateInjectorTargetCurve(0.804f, WaterCurve.Default));
            Assert.AreEqual(0.9f, WaterMonitor.CalculateInjectorTargetCurve(0.75066f, WaterCurve.Default));
            Assert.AreEqual(0.5f, WaterMonitor.CalculateInjectorTargetCurve(0.76667f, WaterCurve.Default));
        }

        [TestMethod()]
        public void CalculateInjectorTargetTest_HighPressure()
        {
            Assert.AreEqual(1.0f, WaterMonitor.CalculateInjectorTargetCurve(0.81f, WaterCurve.HighPressure));
            Assert.AreEqual(0.0f, WaterMonitor.CalculateInjectorTargetCurve(0.85f, WaterCurve.HighPressure));
            Assert.AreEqual(0.1f, WaterMonitor.CalculateInjectorTargetCurve(0.849f, WaterCurve.HighPressure));
            Assert.AreEqual(0.9f, WaterMonitor.CalculateInjectorTargetCurve(0.82f, WaterCurve.HighPressure));
            Assert.AreEqual(0.5f, WaterMonitor.CalculateInjectorTargetCurve(0.84f, WaterCurve.HighPressure));
        }

        [TestMethod()]
        public void CalculateInjectorTargetTest_LowPressure()
        {
            Assert.AreEqual(1.0f, WaterMonitor.CalculateInjectorTargetCurve(0.75f, WaterCurve.LowPressure));
            Assert.AreEqual(0.0f, WaterMonitor.CalculateInjectorTargetCurve(0.8f, WaterCurve.LowPressure));
            Assert.AreEqual(0.1f, WaterMonitor.CalculateInjectorTargetCurve(0.79f, WaterCurve.LowPressure));
            Assert.AreEqual(0.9f, WaterMonitor.CalculateInjectorTargetCurve(0.75001f, WaterCurve.LowPressure));
            Assert.AreEqual(0.5f, WaterMonitor.CalculateInjectorTargetCurve(0.754f, WaterCurve.LowPressure));
        }
    }
}