using NUnit.Framework;
using UnityEngine;
using TMPro;
using Game.ObjectSystem;
using PenguinPinball.Core;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class ObjectSystemTests
    {
        private GameObject storageManagerObj;
        private GameObject dropManagerObj;
        private GameObject placementManagerObj;

        private void CallAwake(MonoBehaviour mb)
        {
            var method = mb.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null) method.Invoke(mb, null);
        }

        [SetUp]
        public void SetUp()
        {
            storageManagerObj = new GameObject("ObjectStorageManager");
            var storage = storageManagerObj.AddComponent<ObjectStorageManager>();
            CallAwake(storage);

            dropManagerObj = new GameObject("ObjectDropManager");
            var drop = dropManagerObj.AddComponent<ObjectDropManager>();
            CallAwake(drop);

            placementManagerObj = new GameObject("PlacementManager");
            var placement = placementManagerObj.AddComponent<PlacementManager>();
            CallAwake(placement);
        }

        [TearDown]
        public void TearDown()
        {
            if (placementManagerObj != null) Object.DestroyImmediate(placementManagerObj);
            if (dropManagerObj != null) Object.DestroyImmediate(dropManagerObj);
            if (storageManagerObj != null) Object.DestroyImmediate(storageManagerObj);
        }

        [Test]
        public void DropManager_InitializeWaveGoals_SetsCorrectGoalsAndProbability()
        {
            var dropManager = ObjectDropManager.Instance;
            Assert.IsNotNull(dropManager, "ObjectDropManager instance should be initialized");

            dropManager.InitializeWaveGoals(20, 4);

            Assert.AreEqual(20, dropManager.TotalEnemiesInWave);
            Assert.AreEqual(4, dropManager.TargetDropCount);
            Assert.AreEqual(0, dropManager.CurrentDroppedCount);
            Assert.AreEqual(0, dropManager.KilledEnemyCount);

            // 기획 §2: 총 20명 중 4개 목표 -> 개체당 기댓값 확률 0.2 (20%)
            Assert.AreEqual(0.2f, dropManager.CurrentDropProbability, 0.001f);
        }

        [Test]
        public void DropManager_DespawnedEnemies_DoNotIncreaseDropProbability_MaintainsDeficit()
        {
            var dropManager = ObjectDropManager.Instance;
            Assert.IsNotNull(dropManager);

            dropManager.InitializeWaveGoals(10, 3);

            float initialProb = dropManager.CurrentDropProbability;
            Assert.AreEqual(0.3f, initialProb, 0.001f);

            // 5마리의 적이 베이스에 도달하여 퇴장 (소멸)
            for (int i = 0; i < 5; i++)
            {
                dropManager.OnEnemyReachedBaseOrDespawned();
            }

            Assert.AreEqual(5, dropManager.KilledEnemyCount);
            // 기획서 §2: "소멸한 개체의 몫은 다른 개체에 재배분하지 않으며, 그만큼 실제 습득량은 미달로 끝난다"
            // 남은 적에게 드랍 확률이 폭등(100% 강제 보정)하지 않고 기본 확률 유지
            Assert.AreEqual(0.3f, dropManager.CurrentDropProbability, 0.001f);
        }

        [Test]
        public void StorageManager_NonStackable_UniqueGuidInstances()
        {
            var storageManager = ObjectStorageManager.Instance;
            Assert.IsNotNull(storageManager, "ObjectStorageManager instance should be initialized");

            storageManager.ClearAllStorage();

            var item1 = new ObjectItem(ObjectType.Bumper);
            var item2 = new ObjectItem(ObjectType.Bumper);

            storageManager.AddObject(item1);
            storageManager.AddObject(item2);

            var stored = storageManager.GetStoredObjects();
            Assert.AreEqual(2, stored.Count, "수량 누적이 아닌 2개의 개별 인스턴스로 보관되어야 합니다.");
            Assert.AreNotEqual(item1.InstanceId, item2.InstanceId, "각각 고유한 GUID를 가져야 합니다.");
        }

        [Test]
        public void StorageManager_ClearOnMapTransition_PurgesStorageAndField()
        {
            var storageManager = ObjectStorageManager.Instance;
            Assert.IsNotNull(storageManager);

            storageManager.AddObject(new ObjectItem(ObjectType.Bumper));
            storageManager.AddObject(new ObjectItem(ObjectType.Slingshot));
            Assert.AreEqual(2, storageManager.GetStoredObjects().Count);

            // 맵 전환 시 일괄 소멸 (§5)
            storageManager.ClearOnMapTransition();

            Assert.AreEqual(0, storageManager.GetStoredObjects().Count, "보관소 잔여분은 전량 소멸되어야 합니다.");
        }

        [Test]
        public void StorageManager_InitializeTestObjects_AddsOneOfEachType()
        {
            var storageManager = ObjectStorageManager.Instance;
            Assert.IsNotNull(storageManager, "ObjectStorageManager instance should be initialized");
            storageManager.ClearAllStorage();

            storageManager.InitializeTestObjects();

            var stored = storageManager.GetStoredObjects();
            Assert.AreEqual(2, stored.Count, "종류별로 1개씩 총 2개의 오브젝트가 추가되어야 합니다.");

            bool hasBumper = false;
            bool hasSlingshot = false;
            for (int i = 0; i < stored.Count; i++)
            {
                if (stored[i].Type == ObjectType.Bumper) hasBumper = true;
                if (stored[i].Type == ObjectType.Slingshot) hasSlingshot = true;
            }

            Assert.IsTrue(hasBumper, "보관소에 범퍼가 1개 포함되어야 합니다.");
            Assert.IsTrue(hasSlingshot, "보관소에 슬링샷이 1개 포함되어야 합니다.");
        }

        [Test]
        public void PlacementManager_BufferSettings_MaintainsTargetSpecificValues()
        {
            var pm = PlacementManager.Instance;
            Assert.IsNotNull(pm, "PlacementManager instance should be initialized");

            var settings = new PlacementManager.PlacementBufferSettings(0.25f, 0.2f, 0.35f);
            pm.BufferSettings = settings;

            Assert.AreEqual(0.25f, pm.BufferSettings.objectBuffer);
            Assert.AreEqual(0.2f, pm.BufferSettings.terrainBuffer);
            Assert.AreEqual(0.35f, pm.BufferSettings.enemyPathBuffer);
            Assert.AreEqual(0.35f, pm.BufferSettings.MaxBuffer);
        }

        [Test]
        public void HUDView_EnsureHUDVisible_EnsuresElementsAndParentActive_OnPlacementStarted()
        {
            // Canvas -> Area -> Gold (with TMP_Text)
            var canvasObj = new GameObject("Canvas", typeof(Canvas));
            var hudView = canvasObj.AddComponent<HUDView>();

            var areaObj = new GameObject("Area", typeof(RectTransform));
            areaObj.transform.SetParent(canvasObj.transform, false);

            var goldObj = new GameObject("Gold", typeof(RectTransform));
            goldObj.transform.SetParent(areaObj.transform, false);
            var goldText = goldObj.AddComponent<TextMeshProUGUI>();

            // Reflection으로 private goldText 필드 할당
            var goldField = typeof(HUDView).GetField("goldText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(goldField, "goldText field should exist in HUDView");
            goldField.SetValue(hudView, goldText);

            // 초기 상태에서 Area나 Gold를 비활성화
            areaObj.SetActive(false);
            goldObj.SetActive(false);
            goldText.enabled = false;

            // EnsureHUDVisible 호출 검증
            hudView.EnsureHUDVisible();

            Assert.IsTrue(areaObj.activeSelf, "Area 컨테이너가 활성화되어야 합니다.");
            Assert.IsTrue(goldObj.activeSelf, "골드 오브젝트가 활성화되어야 합니다.");
            Assert.IsTrue(goldText.enabled, "골드 TMP 컴포넌트가 활성화되어야 합니다.");

            // 계층 순서: Area가 Canvas의 최상단(마지막 자식)으로 이동되었는지 검증
            Assert.AreEqual(canvasObj.transform.childCount - 1, areaObj.transform.GetSiblingIndex(), "Area가 Canvas의 LastSibling이어야 합니다.");

            // 오브젝트 정리
            Object.DestroyImmediate(canvasObj);
        }
    }
}
