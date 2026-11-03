using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class MarkAmplificationTests
    {
        private class TestEnemy : Enemy
        {
            public void SetHp(float hp)
            {
                var maxHpProp = typeof(Enemy).GetProperty("MaxHealth");
                if (maxHpProp != null) maxHpProp.SetValue(this, hp, null);

                var curHpProp = typeof(Enemy).GetProperty("CurrentHealth");
                if (curHpProp != null) curHpProp.SetValue(this, hp, null);

                var isDeadProp = typeof(Enemy).GetProperty("IsDead");
                if (isDeadProp != null) isDeadProp.SetValue(this, false, null);

                gameObject.SetActive(true);
            }
        }

        private class CustomTestMark : MonoBehaviour, ICombatMark
        {
            public bool isMarked = true;
            public string markType = "CustomMark";

            public bool IsMarked => isMarked;
            public string MarkType => markType;
        }

        private GameObject enemyObj;
        private TestEnemy enemy;
        private GameObject sourceObj;
        private PenguinController sourcePenguin;

        [SetUp]
        public void SetUp()
        {
            CombatResolver.ClearDamageModifiers();

            enemyObj = new GameObject("TestEnemy", typeof(BoxCollider), typeof(TestEnemy));
            enemy = enemyObj.GetComponent<TestEnemy>();
            enemy.SetHp(1000f);

            sourceObj = new GameObject("TestSourcePenguin", typeof(SphereCollider), typeof(Rigidbody), typeof(PenguinController));
            sourcePenguin = sourceObj.GetComponent<PenguinController>();
        }

        [TearDown]
        public void TearDown()
        {
            CombatResolver.ClearDamageModifiers();

            if (enemyObj != null) Object.DestroyImmediate(enemyObj);
            if (sourceObj != null) Object.DestroyImmediate(sourceObj);
        }

        [Test]
        public void DistinctMarkCount_WhenNoMarks_ReturnsZero()
        {
            int count = CombatResolver.GetDistinctMarkCount(enemy);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void DistinctMarkCount_WhenSingleMark_ReturnsOne()
        {
            var vulnMark = enemyObj.AddComponent<VulnerabilityMark>();
            vulnMark.ApplyMark(sourcePenguin);

            int count = CombatResolver.GetDistinctMarkCount(enemy);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void DistinctMarkCount_WhenMultipleMarksOfSameType_ReturnsOne()
        {
            // Requirement: "Even if multiple marks of the same type are attached, they are treated as 1."
            var mark1 = enemyObj.AddComponent<VulnerabilityMark>();
            mark1.ApplyMark(sourcePenguin);

            var mark2 = enemyObj.AddComponent<VulnerabilityMark>();
            mark2.ApplyMark(sourcePenguin);

            int count = CombatResolver.GetDistinctMarkCount(enemy);
            Assert.AreEqual(1, count, "Multiple marks of the same type must only count as 1.");
        }

        [Test]
        public void DistinctMarkCount_WhenDifferentMarks_ReturnsCountOfDistinctTypes()
        {
            var vulnMark = enemyObj.AddComponent<VulnerabilityMark>();
            vulnMark.ApplyMark(sourcePenguin);

            var paydayMark = enemyObj.AddComponent<PaydayMark>();
            paydayMark.Initialize(sourcePenguin);

            int count = CombatResolver.GetDistinctMarkCount(enemy);
            Assert.AreEqual(2, count, "Two distinct mark types must result in count of 2.");
        }

        [Test]
        public void DistinctMarkCount_WhenMarkInactive_ReturnsZero()
        {
            var vulnMark = enemyObj.AddComponent<VulnerabilityMark>();
            vulnMark.ApplyMark(sourcePenguin);
            vulnMark.RemoveMark(); // Inactive

            int count = CombatResolver.GetDistinctMarkCount(enemy);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void DistinctMarkCount_WithCustomICombatMark_CountsCorrectly()
        {
            var custom1 = enemyObj.AddComponent<CustomTestMark>();
            custom1.markType = "BurnMark";
            custom1.isMarked = true;

            var custom2 = enemyObj.AddComponent<CustomTestMark>();
            custom2.markType = "BurnMark"; // Duplicate type
            custom2.isMarked = true;

            var custom3 = enemyObj.AddComponent<CustomTestMark>();
            custom3.markType = "FreezeMark"; // Distinct type
            custom3.isMarked = true;

            int count = CombatResolver.GetDistinctMarkCount(enemy);
            Assert.AreEqual(2, count, "Two BurnMarks and one FreezeMark must result in count of 2.");
        }

        [Test]
        public void Effect_MarkAmplification_IncreasesCollisionDamageProportionalToMarks()
        {
            var effect = ScriptableObject.CreateInstance<Effect_MarkAmplification>();
            effect.DamageMultiplierPerMark = 0.25f; // +25% per mark type
            effect.CollisionOnly = true;
            effect.ApplyEffect();

            // 1. When no marks are present
            enemy.SetHp(1000f);
            float hpBefore0 = enemy.CurrentHealth;
            CombatResolver.DealDamage(sourcePenguin, enemy, 100f, isCollision: true);
            float damageTaken0 = hpBefore0 - enemy.CurrentHealth;
            Assert.AreEqual(100f, damageTaken0, 0.01f);

            // 2. When 1 mark type is present (+25% -> 125 damage)
            enemy.SetHp(1000f);
            var paydayMark = enemyObj.AddComponent<PaydayMark>();
            paydayMark.Initialize(sourcePenguin);

            float hpBefore1 = enemy.CurrentHealth;
            CombatResolver.DealDamage(sourcePenguin, enemy, 100f, isCollision: true);
            float damageTaken1 = hpBefore1 - enemy.CurrentHealth;
            Assert.AreEqual(125f, damageTaken1, 0.01f);

            effect.RemoveEffect();
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Effect_MarkAmplification_MultipleDistinctMarks_StacksProportionally()
        {
            var effect = ScriptableObject.CreateInstance<Effect_MarkAmplification>();
            effect.DamageMultiplierPerMark = 0.25f;
            effect.ApplyEffect();

            var paydayMark = enemyObj.AddComponent<PaydayMark>();
            paydayMark.Initialize(sourcePenguin);

            var customMark = enemyObj.AddComponent<CustomTestMark>();
            customMark.markType = "ShockMark";
            customMark.isMarked = true;

            // 2 distinct types -> 100 * (1 + 2 * 0.25) = 150
            enemy.SetHp(1000f);
            float hpBefore = enemy.CurrentHealth;
            CombatResolver.DealDamage(sourcePenguin, enemy, 100f, isCollision: true);
            float damageTaken = hpBefore - enemy.CurrentHealth;

            Assert.AreEqual(150f, damageTaken, 0.01f);

            effect.RemoveEffect();
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Effect_MarkAmplification_DuplicateMarksOfSameType_OnlyCountAsOne()
        {
            var effect = ScriptableObject.CreateInstance<Effect_MarkAmplification>();
            effect.DamageMultiplierPerMark = 0.25f;
            effect.ApplyEffect();

            // 3 duplicate marks of "Poison"
            var m1 = enemyObj.AddComponent<CustomTestMark>();
            m1.markType = "Poison";
            var m2 = enemyObj.AddComponent<CustomTestMark>();
            m2.markType = "Poison";
            var m3 = enemyObj.AddComponent<CustomTestMark>();
            m3.markType = "Poison";

            // 3 duplicates treated as 1 type -> 100 * (1 + 1 * 0.25) = 125
            enemy.SetHp(1000f);
            float hpBefore = enemy.CurrentHealth;
            CombatResolver.DealDamage(sourcePenguin, enemy, 100f, isCollision: true);
            float damageTaken = hpBefore - enemy.CurrentHealth;

            Assert.AreEqual(125f, damageTaken, 0.01f);

            effect.RemoveEffect();
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Effect_MarkAmplification_NonCollision_NotAmplifiedWhenCollisionOnly()
        {
            var effect = ScriptableObject.CreateInstance<Effect_MarkAmplification>();
            effect.DamageMultiplierPerMark = 0.25f;
            effect.CollisionOnly = true;
            effect.ApplyEffect();

            var paydayMark = enemyObj.AddComponent<PaydayMark>();
            paydayMark.Initialize(sourcePenguin);

            // isCollision = false -> should not be amplified
            enemy.SetHp(1000f);
            float hpBefore = enemy.CurrentHealth;
            CombatResolver.DealDamage(sourcePenguin, enemy, 100f, isCollision: false);
            float damageTaken = hpBefore - enemy.CurrentHealth;

            Assert.AreEqual(100f, damageTaken, 0.01f);

            effect.RemoveEffect();
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Effect_MarkAmplification_WhenRemoved_RestoresOriginalDamage()
        {
            var effect = ScriptableObject.CreateInstance<Effect_MarkAmplification>();
            effect.DamageMultiplierPerMark = 0.25f;
            effect.ApplyEffect();

            var paydayMark = enemyObj.AddComponent<PaydayMark>();
            paydayMark.Initialize(sourcePenguin);

            // Remove effect
            effect.RemoveEffect();

            enemy.SetHp(1000f);
            float hpBefore = enemy.CurrentHealth;
            CombatResolver.DealDamage(sourcePenguin, enemy, 100f, isCollision: true);
            float damageTaken = hpBefore - enemy.CurrentHealth;

            Assert.AreEqual(100f, damageTaken, 0.01f);

            Object.DestroyImmediate(effect);
        }
    }
}
