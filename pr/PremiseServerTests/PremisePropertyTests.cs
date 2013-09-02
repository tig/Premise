using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Premise;

namespace PremiseServerTests
{
    [TestClass]
    public class PremisePropertyTests
    {
        [TestMethod]
        public void Boolean() {
            var prop = new PremiseProperty("test", Premise.PremiseProperty.PremiseType.TypeBoolean);

            prop.Value = -1; // true
            Assert.AreEqual(prop.Value, (bool)true);

            prop.Value = 1; // true
            Assert.AreEqual(prop.Value, (bool)true);

            prop.Value = "True"; // true
            Assert.AreEqual(prop.Value, (bool)true);

            prop.Value = "On"; // true
            Assert.AreEqual(prop.Value, (bool)true);

            prop.Value = "Yes"; // true
            Assert.AreEqual(prop.Value, (bool)true);

            prop.Value = 0; // false
            Assert.AreEqual(prop.Value, (bool)false);

            prop.Value = "False"; // false
            Assert.AreEqual(prop.Value, (bool)false);

            prop.Value = "Off"; // false
            Assert.AreEqual(prop.Value, (bool)false);

            prop.Value = "No"; // false
            Assert.AreEqual(prop.Value, (bool)false);
        }

        [TestMethod]
        public void Percent()
        {
            var prop = new PremiseProperty("test", Premise.PremiseProperty.PremiseType.TypePercent);

            prop.Value = 1; 
            Assert.AreEqual(prop.Value, 1.0);

            prop.Value = .99;
            Assert.AreEqual(prop.Value, .99);

            prop.Value = "99%"; 
            Assert.AreEqual(prop.Value, .99);

            prop.Value = ".99"; 
            Assert.AreEqual(prop.Value, .99);

            prop.Value = 0.0;
            Assert.AreEqual(prop.Value, 0.0);

            prop.Value = 0; 
            Assert.AreEqual(prop.Value, 0.0);

        }
    }
}
