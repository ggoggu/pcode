using NUnit.Framework;
using PenguinPinball.Core;

namespace PenguinShot.Tests
{
    public class StatTests
    {
        [Test]
        public void Stat_BaseValue_ReturnsInitialValue()
        {
            var stat = new Stat(100f);
            Assert.AreEqual(100f, stat.Value);
            Assert.AreEqual(100f, stat.BaseValue);
        }

        [Test]
        public void Stat_FlatModifier_AddsToTotal()
        {
            var stat = new Stat(100f);
            var mod = new StatModifier(25f, StatModType.Flat, this);
            stat.AddModifier(mod);

            Assert.AreEqual(125f, stat.Value);
        }

        [Test]
        public void Stat_PercentAddModifier_ScalesTotal()
        {
            var stat = new Stat(100f);
            var mod = new StatModifier(0.5f, StatModType.PercentAdd, this); // +50%
            stat.AddModifier(mod);

            Assert.AreEqual(150f, stat.Value);
        }

        [Test]
        public void Stat_FlatAndPercentModifiers_CalculatedCorrectly()
        {
            var stat = new Stat(100f);
            var flatMod = new StatModifier(50f, StatModType.Flat, this); // (100 + 50) = 150
            var percentMod = new StatModifier(0.2f, StatModType.PercentAdd, this); // 150 * (1 + 0.2) = 180
            stat.AddModifier(flatMod);
            stat.AddModifier(percentMod);

            Assert.AreEqual(180f, stat.Value);
        }

        [Test]
        public void Stat_RemoveAllModifiersFromSource_RemovesSourceModifiersOnly()
        {
            var stat = new Stat(100f);
            object sourceA = new object();
            object sourceB = new object();

            var modA = new StatModifier(20f, StatModType.Flat, sourceA);
            var modB = new StatModifier(30f, StatModType.Flat, sourceB);

            stat.AddModifier(modA);
            stat.AddModifier(modB);
            Assert.AreEqual(150f, stat.Value);

            stat.RemoveAllModifiersFromSource(sourceA);
            Assert.AreEqual(130f, stat.Value);
        }
    }
}
