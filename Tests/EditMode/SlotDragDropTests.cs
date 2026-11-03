using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using PenguinPinball.Core;
using PenguinPinball.UI.ViewModels;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class SlotDragDropTests
    {
        private GameObject deckManagerObj;
        private GameObject inventoryManagerObj;

        private void CallAwake(MonoBehaviour mb)
        {
            var method = mb.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null) method.Invoke(mb, null);
        }

        [SetUp]
        public void SetUp()
        {
            inventoryManagerObj = new GameObject("ItemInventoryManager");
            var inv = inventoryManagerObj.AddComponent<ItemInventoryManager>();
            CallAwake(inv);

            deckManagerObj = new GameObject("PenguinDeckManager");
            var deck = deckManagerObj.AddComponent<PenguinDeckManager>();
            CallAwake(deck);
        }

        [TearDown]
        public void TearDown()
        {
            if (deckManagerObj != null) Object.DestroyImmediate(deckManagerObj);
            if (inventoryManagerObj != null) Object.DestroyImmediate(inventoryManagerObj);
        }

        private GameObject CreateTestPenguin(string name, PenguinDefinition def = null)
        {
            var obj = new GameObject(name, typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinLocationData), typeof(PenguinController));
            var controller = obj.GetComponent<PenguinController>();
            if (def != null) controller.Init(def);
            var locData = obj.GetComponent<PenguinLocationData>();
            locData.Location = PenguinLocation.Bench;
            return obj;
        }

        [Test]
        public void BenchSlot_SwapOrMove_MovesPenguinToEmptySlot()
        {
            var deckManager = PenguinDeckManager.Instance;
            var penguin = CreateTestPenguin("PenguinA");
            deckManager.RegisterPenguin(penguin);

            // 0번 슬롯에 들어갔는지 확인
            Assert.AreEqual(penguin, deckManager.GetPenguinAtBenchSlot(0));

            // 0번 -> 3번 슬롯으로 이동
            bool success = deckManager.SwapOrMoveBenchSlot(0, 3);
            Assert.IsTrue(success);

            // 0번은 null, 3번에 penguin이 있어야 함
            Assert.IsNull(deckManager.GetPenguinAtBenchSlot(0));
            Assert.AreEqual(penguin, deckManager.GetPenguinAtBenchSlot(3));

            var controller = penguin.GetComponent<PenguinController>();
            Assert.AreEqual(3, deckManager.GetBenchSlotIndex(controller));
            Assert.AreEqual(4, deckManager.GetBenchSlotNumber(controller));

            Object.DestroyImmediate(penguin);
        }

        [Test]
        public void BenchSlot_SwapOrMove_SwapsTwoPenguins()
        {
            var deckManager = PenguinDeckManager.Instance;
            var p1 = CreateTestPenguin("Penguin1");
            var p2 = CreateTestPenguin("Penguin2");

            deckManager.RegisterPenguin(p1);
            deckManager.RegisterPenguin(p2);

            Assert.AreEqual(p1, deckManager.GetPenguinAtBenchSlot(0));
            Assert.AreEqual(p2, deckManager.GetPenguinAtBenchSlot(1));

            // 슬롯 0과 1 스왑
            bool success = deckManager.SwapOrMoveBenchSlot(0, 1);
            Assert.IsTrue(success);

            Assert.AreEqual(p2, deckManager.GetPenguinAtBenchSlot(0));
            Assert.AreEqual(p1, deckManager.GetPenguinAtBenchSlot(1));

            Object.DestroyImmediate(p1);
            Object.DestroyImmediate(p2);
        }

        [Test]
        public void InventorySlot_SwapOrMove_MovesItemToEmptySlot()
        {
            var invManager = ItemInventoryManager.Instance;
            var equip = ScriptableObject.CreateInstance<EquipmentSO>();
            equip.name = "TestSword";

            invManager.AddEquipment(equip);

            // 0번에 들어갔는지 확인
            Assert.AreEqual(equip, invManager.GetItemAtSlot(0));

            // 0번 -> 4번 슬롯으로 이동
            bool success = invManager.SwapOrMoveInventorySlot(0, 4);
            Assert.IsTrue(success);

            Assert.IsNull(invManager.GetItemAtSlot(0));
            Assert.AreEqual(equip, invManager.GetItemAtSlot(4));

            Object.DestroyImmediate(equip);
        }

        [Test]
        public void InventorySlot_SwapOrMove_SwapsTwoItems()
        {
            var invManager = ItemInventoryManager.Instance;
            var equip1 = ScriptableObject.CreateInstance<EquipmentSO>();
            equip1.name = "Sword";
            var equip2 = ScriptableObject.CreateInstance<EquipmentSO>();
            equip2.name = "Shield";

            invManager.AddEquipment(equip1);
            invManager.AddEquipment(equip2);

            Assert.AreEqual(equip1, invManager.GetItemAtSlot(0));
            Assert.AreEqual(equip2, invManager.GetItemAtSlot(1));

            // 슬롯 0과 1 스왑
            bool success = invManager.SwapOrMoveInventorySlot(0, 1);
            Assert.IsTrue(success);

            Assert.AreEqual(equip2, invManager.GetItemAtSlot(0));
            Assert.AreEqual(equip1, invManager.GetItemAtSlot(1));

            Object.DestroyImmediate(equip1);
            Object.DestroyImmediate(equip2);
        }

        [Test]
        public void InventoryToBench_MovePenguin_PlacesInTargetBenchSlot()
        {
            var deckManager = PenguinDeckManager.Instance;
            var penguin = CreateTestPenguin("InventoryPenguin");

            // 인벤토리로 등록
            penguin.GetComponent<PenguinLocationData>().Location = PenguinLocation.Inventory;
            deckManager.RegisterPenguin(penguin);

            Assert.IsNull(deckManager.GetPenguinAtBenchSlot(2));

            // 인벤토리 -> 벤치 2번 슬롯으로 이동
            bool success = deckManager.MovePenguinToBenchSlot(penguin, 2);
            Assert.IsTrue(success);

            Assert.AreEqual(penguin, deckManager.GetPenguinAtBenchSlot(2));
            Assert.AreEqual(PenguinLocation.Bench, penguin.GetComponent<PenguinLocationData>().Location);

            Object.DestroyImmediate(penguin);
        }

        [Test]
        public void BenchToInventory_SwapPenguins_SwapsCorrectly()
        {
            var deckManager = PenguinDeckManager.Instance;
            var invManager = ItemInventoryManager.Instance;

            var benchPenguin = CreateTestPenguin("BenchPenguin");
            benchPenguin.GetComponent<PenguinLocationData>().Location = PenguinLocation.Bench;
            deckManager.RegisterPenguin(benchPenguin); // 벤치 0번 슬롯

            var invPenguin = CreateTestPenguin("InvPenguin");
            invPenguin.GetComponent<PenguinLocationData>().Location = PenguinLocation.Inventory;
            deckManager.RegisterPenguin(invPenguin); // 인벤토리 0번 슬롯

            Assert.AreEqual(benchPenguin, deckManager.GetPenguinAtBenchSlot(0));
            Assert.AreEqual(invPenguin, invManager.GetItemAtSlot(0));

            // 벤치 0번의 benchPenguin을 인벤토리 0번으로 이동 (스왑)
            bool success = deckManager.ChangePenguinLocation(benchPenguin, PenguinLocation.Inventory, 0);
            Assert.IsTrue(success);

            // 벤치 0번에 invPenguin, 인벤토리 0번에 benchPenguin이 들어갔는지 검증
            Assert.AreEqual(invPenguin, deckManager.GetPenguinAtBenchSlot(0));
            Assert.AreEqual(benchPenguin, invManager.GetItemAtSlot(0));
            Assert.AreEqual(PenguinLocation.Bench, invPenguin.GetComponent<PenguinLocationData>().Location);
            Assert.AreEqual(PenguinLocation.Inventory, benchPenguin.GetComponent<PenguinLocationData>().Location);

            Object.DestroyImmediate(benchPenguin);
            Object.DestroyImmediate(invPenguin);
        }

        [Test]
        public void InventoryToBench_WhenBenchFull_SwapsCorrectly()
        {
            var deckManager = PenguinDeckManager.Instance;
            var invManager = ItemInventoryManager.Instance;

            // 벤치 6칸 꽉 채우기
            var benchPenguins = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < deckManager.EffectiveBenchCapacity; i++)
            {
                var p = CreateTestPenguin($"BenchP_{i}");
                p.GetComponent<PenguinLocationData>().Location = PenguinLocation.Bench;
                deckManager.RegisterPenguin(p);
                benchPenguins.Add(p);
            }

            Assert.AreEqual(6, deckManager.EffectiveBenchCapacity);
            Assert.AreEqual(benchPenguins[2], deckManager.GetPenguinAtBenchSlot(2));

            // 인벤토리에 1마리 등록
            var invPenguin = CreateTestPenguin("InvPenguin");
            invPenguin.GetComponent<PenguinLocationData>().Location = PenguinLocation.Inventory;
            deckManager.RegisterPenguin(invPenguin);

            int invSlotIdx = Array.IndexOf(invManager.InventorySlots.ToArray(), invPenguin);
            Assert.IsTrue(invSlotIdx >= 0);

            // 벤치가 꽉 찬 상태에서 인벤토리 펭귄을 벤치 2번 슬롯으로 스왑 이동
            bool success = deckManager.MovePenguinToBenchSlot(invPenguin, 2);
            Assert.IsTrue(success, "벤치가 가득 차 있어도 기존 펭귄과의 1:1 맞교환(스왑)은 성공해야 합니다.");

            // 벤치 2번에 invPenguin, 인벤토리 원래 슬롯에 기존 benchPenguins[2]가 들어갔는지 검증
            Assert.AreEqual(invPenguin, deckManager.GetPenguinAtBenchSlot(2));
            Assert.AreEqual(benchPenguins[2], invManager.GetItemAtSlot(invSlotIdx));

            foreach (var p in benchPenguins) Object.DestroyImmediate(p);
            Object.DestroyImmediate(invPenguin);
         }

        [Test]
        public void Merge_WhenBothOnBench_SynthesizesOnBench()
        {
            var deckManager = PenguinDeckManager.Instance;
            var def = ScriptableObject.CreateInstance<PenguinDefinition>();
            def.name = "TestPenguin";

            var p1 = CreateTestPenguin("P1", def);
            var p2 = CreateTestPenguin("P2", def);

            deckManager.RegisterPenguin(p1);
            deckManager.RegisterPenguin(p2);

            Assert.AreEqual(1, deckManager.OwnedPenguins.Count, "두 펭귄이 합성되어 1마리가 남아야 합니다.");
            var mergedObj = deckManager.OwnedPenguins[0];
            var mergedController = mergedObj.GetComponent<PenguinController>();
            var mergedLoc = mergedObj.GetComponent<PenguinLocationData>();

            Assert.AreEqual(2, mergedController.Tier, "합성 결과는 2티어여야 합니다.");
            Assert.AreEqual(PenguinLocation.Bench, mergedLoc.Location, "벤치에 있던 펭귄은 벤치에서 합성되어야 합니다.");
            Assert.AreEqual(mergedObj, deckManager.GetPenguinAtBenchSlot(0), "합성된 펭귄은 벤치 0번 슬롯에 위치해야 합니다.");
            Assert.IsTrue(deckManager.ReadyPenguins.Contains(mergedObj), "벤치에 합성된 펭귄은 발사 대기 목록에 포함되어야 합니다.");

            Object.DestroyImmediate(def);
            Object.DestroyImmediate(mergedObj);
        }

        [Test]
        public void Merge_WhenOneOnBenchAndOneInInventory_SynthesizesOnBench()
        {
            var deckManager = PenguinDeckManager.Instance;
            var invManager = ItemInventoryManager.Instance;
            var def = ScriptableObject.CreateInstance<PenguinDefinition>();
            def.name = "TestPenguin";

            // p1은 벤치 0번에 배치
            var p1 = CreateTestPenguin("P1_Bench", def);
            p1.GetComponent<PenguinLocationData>().Location = PenguinLocation.Bench;
            deckManager.RegisterPenguin(p1);

            // p2는 인벤토리에 배치
            var p2 = CreateTestPenguin("P2_Inven", def);
            p2.GetComponent<PenguinLocationData>().Location = PenguinLocation.Inventory;
            deckManager.RegisterPenguin(p2);

            Assert.AreEqual(1, deckManager.OwnedPenguins.Count, "합성 후 1마리가 남아야 합니다.");
            var mergedObj = deckManager.OwnedPenguins[0];
            var mergedController = mergedObj.GetComponent<PenguinController>();
            var mergedLoc = mergedObj.GetComponent<PenguinLocationData>();

            Assert.AreEqual(2, mergedController.Tier, "합성 결과는 2티어여야 합니다.");
            Assert.AreEqual(PenguinLocation.Bench, mergedLoc.Location, "재료 중 벤치에 있던 펭귄이 있다면 벤치에서 합성되어야 합니다.");
            Assert.AreEqual(mergedObj, deckManager.GetPenguinAtBenchSlot(0), "합성된 펭귄은 벤치 0번 슬롯에 유지되어야 합니다.");
            Assert.IsNull(invManager.GetItemAtSlot(0), "인벤토리에 있던 재료 슬롯은 비워져야 합니다.");

            Object.DestroyImmediate(def);
            Object.DestroyImmediate(mergedObj);
        }

        [Test]
        public void Merge_WhenBothInInventory_SynthesizesInInventory()
        {
            var deckManager = PenguinDeckManager.Instance;
            var invManager = ItemInventoryManager.Instance;
            var def = ScriptableObject.CreateInstance<PenguinDefinition>();
            def.name = "TestPenguin";

            var p1 = CreateTestPenguin("P1_Inven", def);
            p1.GetComponent<PenguinLocationData>().Location = PenguinLocation.Inventory;
            deckManager.RegisterPenguin(p1);

            var p2 = CreateTestPenguin("P2_Inven", def);
            p2.GetComponent<PenguinLocationData>().Location = PenguinLocation.Inventory;
            deckManager.RegisterPenguin(p2);

            Assert.AreEqual(1, deckManager.OwnedPenguins.Count, "합성 후 1마리가 남아야 합니다.");
            var mergedObj = deckManager.OwnedPenguins[0];
            var mergedController = mergedObj.GetComponent<PenguinController>();
            var mergedLoc = mergedObj.GetComponent<PenguinLocationData>();

            Assert.AreEqual(2, mergedController.Tier, "합성 결과는 2티어여야 합니다.");
            Assert.AreEqual(PenguinLocation.Inventory, mergedLoc.Location, "둘 다 인벤토리에 있던 펭귄은 인벤토리에서 합성되어야 합니다.");
            Assert.AreEqual(mergedObj, invManager.GetItemAtSlot(0), "합성된 펭귄은 인벤토리 0번 슬롯에 위치해야 합니다.");
            Assert.IsNull(deckManager.GetPenguinAtBenchSlot(0), "벤치 0번 슬롯은 비어 있어야 합니다.");

            Object.DestroyImmediate(def);
            Object.DestroyImmediate(mergedObj);
        }
    }
}