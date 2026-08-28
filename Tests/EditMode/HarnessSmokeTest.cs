using NUnit.Framework;
using PenguinPinball.Core;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class HarnessSmokeTest
    {
        [Test]
        public void AutonomousHarness_BasicMathAndExecution_Passes()
        {
            int a = 2 + 2;
            Assert.AreEqual(4, a, "Basic arithmetic sanity check failed.");
        }

        [Test]
        public void AutonomousHarness_DomainPOCOInstantiable_Passes()
        {
            var stat = new Stat(50f);
            Assert.IsNotNull(stat, "Domain POCO Stat could not be instantiated.");
            Assert.AreEqual(50f, stat.Value, "Initial Stat value does not match expected base value.");
        }

        [Test]
        public void AutonomousHarness_StatModifierPipeline_CalculatesCorrectly()
        {
            var stat = new Stat(100f);
            var flatMod = new StatModifier(20f, StatModType.Flat);
            var pctMod = new StatModifier(0.1f, StatModType.PercentAdd);

            stat.AddModifier(flatMod);
            stat.AddModifier(pctMod);

            // (100 + 20) * 1.1 = 132
            Assert.AreEqual(132f, stat.Value, 0.0001f, "Stat modifier calculation mismatch.");
        }
    }
}
