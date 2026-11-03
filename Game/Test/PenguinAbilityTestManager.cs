using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    /// <summary>
    /// 기획자 및 개발자용 펭귄 능력 통합 테스트 매니저.
    /// 펭귄 스폰/교체, 정밀 발사/회수, 과부하(Overload) 치트, 복수 샌드백 관리, 고정 파트너 펭귄 스폰, 시간 배속 등을 제어합니다.
    /// </summary>
    public class PenguinAbilityTestManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("기본 펭귄 프리팹 (BasicPenguin)")]
        [SerializeField] private GameObject basicPenguinPrefab;

        [Tooltip("메인 테스트 펭귄 스폰 위치")]
        [SerializeField] private Transform launchSpawnPoint;

        [Tooltip("발사대 참조 (선택 사항)")]
        [SerializeField] private PenguinLauncher launcher;

        [Header("Target Dummies Settings")]
        [Tooltip("샌드백 더미 프리팹 (없을 경우 기본 실린더로 생성)")]
        [SerializeField] private GameObject dummyPrefab;

        [Tooltip("더미 1 스폰 위치")]
        [SerializeField] private Transform dummySpawnPoint1;

        [Tooltip("더미 2 스폰 위치")]
        [SerializeField] private Transform dummySpawnPoint2;

        [Header("Partner Penguin Settings")]
        [Tooltip("보조 펭귄 전용 프리팹 (비워둘 경우 기본 펭귄에서 CollisionHandler를 자동 제거하여 사용)")]
        [SerializeField] private GameObject partnerPenguinPrefab;

        [Tooltip("보조 펭귄(충돌 테스트용) 스폰 위치")]
        [SerializeField] private Transform partnerSpawnPoint;

        [Tooltip("보조 펭귄을 제자리에 고정(넉백 및 사라짐 방지)할지 여부")]
        [SerializeField] private bool freezePartnerPenguin = true;

        [Header("Penguin Selection")]
        [SerializeField] private PenguinDefinition selectedDefinition;
        [SerializeField, Range(1, 3)] private int selectedTier = 1;
        [SerializeField] private List<PenguinDefinition> registeredDefinitions = new();

        [Header("Launch Options")]
        [Tooltip("발사 기준 전방 벡터 (PenguinLauncher 기본값은 0, 0, -1)")]
        [SerializeField] private Vector3 localAimDirection = new Vector3(0f, 0f, -1f);

        [SerializeField, Range(-45f, 45f)] private float testLaunchAngle = 0f;
        [SerializeField, Range(1f, 30f)] private float testLaunchSpeed = 12f;

        [Header("Active Entities (Runtime)")]
        [SerializeField] private PenguinController activePenguin;
        [SerializeField] private DamageTestDummy dummy1;
        [SerializeField] private DamageTestDummy dummy2;
        [SerializeField] private PenguinController partnerPenguin;

        public PenguinController ActivePenguin => activePenguin;
        public DamageTestDummy Dummy1 => dummy1;
        public DamageTestDummy Dummy2 => dummy2;
        public PenguinController PartnerPenguin => partnerPenguin;
        public PenguinDefinition SelectedDefinition
        {
            get => selectedDefinition;
            set => selectedDefinition = value;
        }
        public int SelectedTier
        {
            get => selectedTier;
            set => selectedTier = Mathf.Clamp(value, 1, 3);
        }
        public List<PenguinDefinition> RegisteredDefinitions => registeredDefinitions;
        public float TestLaunchAngle
        {
            get => testLaunchAngle;
            set => testLaunchAngle = value;
        }
        public float TestLaunchSpeed
        {
            get => testLaunchSpeed;
            set => testLaunchSpeed = value;
        }

        private float initialFixedDeltaTime = 0.02f;

        private void Awake()
        {
            initialFixedDeltaTime = Time.fixedDeltaTime;

            if (launcher == null)
            {
                launcher = FindFirstObjectByType<PenguinLauncher>();
            }

            if (launchSpawnPoint == null && launcher != null)
            {
                launchSpawnPoint = launcher.LaunchPoint;
            }

            if (launchSpawnPoint == null)
            {
                launchSpawnPoint = transform;
            }
        }

        private void Start()
        {
            if (activePenguin == null)
            {
                activePenguin = FindFirstObjectByType<PenguinController>();
            }
        }

        #region Penguin Spawning & Setup
        public void SpawnOrReplacePenguin()
        {
            if (selectedDefinition == null)
            {
                Debug.LogWarning("[PenguinAbilityTestManager] PenguinDefinition이 선택되지 않았습니다.", this);
                return;
            }

            Vector3 spawnPos = launchSpawnPoint != null ? launchSpawnPoint.position : Vector3.zero;

            if (activePenguin == null)
            {
                if (basicPenguinPrefab == null)
                {
                    Debug.LogError("[PenguinAbilityTestManager] Basic Penguin Prefab이 설정되지 않았습니다.", this);
                    return;
                }

                GameObject penguinObj = Instantiate(basicPenguinPrefab, spawnPos, Quaternion.identity);
                activePenguin = penguinObj.GetComponent<PenguinController>();
            }
            else
            {
                activePenguin.transform.position = spawnPos;
            }

            activePenguin.Init(selectedDefinition, selectedTier);
            activePenguin.ResetToHeld();

            Debug.Log($"[PenguinAbilityTestManager] 펭귄 장착 완료: {selectedDefinition.DisplayName} (Tier {selectedTier})");
        }

        public void SetPenguinTier(int tier)
        {
            selectedTier = Mathf.Clamp(tier, 1, 3);
            if (activePenguin != null)
            {
                activePenguin.SetTier(selectedTier);
                Debug.Log($"[PenguinAbilityTestManager] {activePenguin.Definition?.DisplayName} 티어 변경 -> Tier {selectedTier}");
            }
        }

        public void RecallPenguin()
        {
            if (activePenguin == null) return;

            Vector3 spawnPos = launchSpawnPoint != null ? launchSpawnPoint.position : Vector3.zero;
            activePenguin.transform.position = spawnPos;
            activePenguin.ResetToHeld();

            Debug.Log("[PenguinAbilityTestManager] 펭귄을 발사대로 회수했습니다.");
        }
        #endregion

        #region Launch Control
        public void LaunchPenguin()
        {
            if (activePenguin == null)
            {
                Debug.LogWarning("[PenguinAbilityTestManager] 발사할 펭귄이 없습니다. 먼저 펭귄을 스폰해주세요.");
                return;
            }

            if (activePenguin.IsInField)
            {
                RecallPenguin();
            }

            Transform refPoint = launchSpawnPoint != null ? launchSpawnPoint : (launcher != null ? launcher.LaunchPoint : transform);
            Vector3 spawnPos = refPoint.position;
            Vector3 upAxis = refPoint.up;

            // PenguinLauncher와 동일하게 localAimDirection(기본 0, 0, -1)을 기준으로 각도 회전
            Vector3 forwardDir = refPoint.TransformDirection(localAimDirection.normalized);
            Vector3 launchDirection = (Quaternion.AngleAxis(testLaunchAngle, upAxis) * forwardDir).normalized;

            activePenguin.PrepareAndLaunch(spawnPos, launchDirection, testLaunchSpeed);
            GameEvents.RaiseOnPenguinLaunched(activePenguin);

            Debug.Log($"[PenguinAbilityTestManager] 발사! 각도: {testLaunchAngle}°, 속도: {testLaunchSpeed}, 방향: {launchDirection}");
        }
        #endregion

        #region Combo & Overload Controls
        public void AddCombo(int amount = 1)
        {
            if (activePenguin == null) return;
            activePenguin.AddCombo(amount);
            Debug.Log($"[PenguinAbilityTestManager] Combo +{amount} -> {activePenguin.Combo}/{activePenguin.OverloadThreshold}");
        }

        public void TriggerOverload()
        {
            if (activePenguin == null) return;

            int safety = 1000;
            while (!activePenguin.IsOverloaded && safety-- > 0)
            {
                activePenguin.AddCombo(1);
            }

            Debug.Log($"[PenguinAbilityTestManager] 과부하(Overload) 강제 발동 완료! (Combo: {activePenguin.Combo})");
        }

        public void ResetCombo()
        {
            if (activePenguin == null) return;
            activePenguin.ResetCombo();
            Debug.Log("[PenguinAbilityTestManager] 콤보 및 과부하 리셋.");
        }
        #endregion

        #region Dummy 1 & 2 Controls
        public void SpawnOrResetDummy1()
        {
            Vector3 spawnPos = dummySpawnPoint1 != null 
                ? dummySpawnPoint1.position 
                : (launchSpawnPoint != null ? launchSpawnPoint.position + launchSpawnPoint.TransformDirection(localAimDirection) * 5f : new Vector3(0f, 0f, 5f));

            dummy1 = CreateOrResetDummyInternal(dummy1, spawnPos, "DamageTestDummy_1");
            Debug.Log("[PenguinAbilityTestManager] 더미 1 배치/리셋 완료.");
        }

        public void SpawnOrResetDummy2()
        {
            Vector3 spawnPos = dummySpawnPoint2 != null 
                ? dummySpawnPoint2.position 
                : (launchSpawnPoint != null ? launchSpawnPoint.position + launchSpawnPoint.TransformDirection(localAimDirection) * 7f + Vector3.right * 2f : new Vector3(2f, 0f, 7f));

            dummy2 = CreateOrResetDummyInternal(dummy2, spawnPos, "DamageTestDummy_2");
            Debug.Log("[PenguinAbilityTestManager] 더미 2 배치/리셋 완료.");
        }

        public void SpawnOrResetAllDummies()
        {
            SpawnOrResetDummy1();
            SpawnOrResetDummy2();
        }

        private DamageTestDummy CreateOrResetDummyInternal(DamageTestDummy targetDummy, Vector3 spawnPos, string dummyName)
        {
            if (targetDummy == null)
            {
                if (dummyPrefab != null)
                {
                    GameObject dummyObj = Instantiate(dummyPrefab, spawnPos, Quaternion.identity);
                    dummyObj.name = dummyName;
                    targetDummy = dummyObj.GetComponent<DamageTestDummy>();
                    if (targetDummy == null) targetDummy = dummyObj.AddComponent<DamageTestDummy>();
                }
                else
                {
                    GameObject dummyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    dummyObj.name = dummyName;
                    dummyObj.transform.position = spawnPos;
                    dummyObj.transform.localScale = new Vector3(1f, 1f, 1f);
                    targetDummy = dummyObj.AddComponent<DamageTestDummy>();
                }
            }
            else
            {
                targetDummy.transform.position = spawnPos;
                targetDummy.ResetStats();
            }

            return targetDummy;
        }
        #endregion

        #region Partner Penguin Controls (고정 보조 펭귄)
        public void SpawnPartnerPenguin(PenguinDefinition partnerDef = null)
        {
            if (partnerPenguin != null)
            {
                Destroy(partnerPenguin.gameObject);
                partnerPenguin = null;
            }

            GameObject prefabToSpawn = partnerPenguinPrefab != null ? partnerPenguinPrefab : basicPenguinPrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogError("[PenguinAbilityTestManager] 펭귄 프리팹이 설정되지 않았습니다 (Basic/Partner Penguin Prefab).");
                return;
            }

            Vector3 spawnPos = partnerSpawnPoint != null 
                ? partnerSpawnPoint.position 
                : (launchSpawnPoint != null ? launchSpawnPoint.position + launchSpawnPoint.TransformDirection(localAimDirection) * 3f : new Vector3(0f, 0f, 3f));

            GameObject partnerObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            partnerObj.name = "PartnerPenguin_Stationary";

            // 보조 펭귄 자체의 충돌 핸들러 제거 (메인 펭귄 충돌 반발 로직 간섭 방지)
            if (partnerObj.TryGetComponent<PenguinCollisionHandler>(out var collisionHandler))
            {
                Destroy(collisionHandler);
            }

            partnerPenguin = partnerObj.GetComponent<PenguinController>();

            PenguinDefinition defToUse = partnerDef != null ? partnerDef : (selectedDefinition != null ? selectedDefinition : null);
            if (defToUse != null && partnerPenguin != null)
            {
                partnerPenguin.Init(defToUse, 1);
            }

            // 고정 옵션 처리: 콜라이더와 비주얼은 켜두고 Rigidbody를 Kinematic으로 고정하여 튕겨나가지 않게 함
            if (freezePartnerPenguin && partnerPenguin != null)
            {
                var col = partnerPenguin.GetComponent<Collider>();
                if (col != null) col.enabled = true;

                // 하위 Visual 활성화
                foreach (Transform child in partnerPenguin.transform)
                {
                    child.gameObject.SetActive(true);
                }

                if (partnerPenguin.Body != null)
                {
                    partnerPenguin.Body.isKinematic = true;
                    partnerPenguin.Body.constraints = RigidbodyConstraints.FreezeAll;
                    partnerPenguin.Body.linearVelocity = Vector3.zero;
                    partnerPenguin.Body.angularVelocity = Vector3.zero;
                }

                // Update 루프를 꺼서 멈춰있을 때 Stuck 감지(Drain)로 사라지지 않게 방지
                partnerPenguin.enabled = false;
            }
            else if (partnerPenguin != null)
            {
                partnerPenguin.PrepareAndLaunch(spawnPos, Vector3.zero, 0f);
            }

            Debug.Log("[PenguinAbilityTestManager] 보조 펭귄(고정 상태 & CollisionHandler 제거) 배치 완료.");
        }

        public void ClearAllEntities()
        {
            RecallPenguin();

            if (partnerPenguin != null)
            {
                Destroy(partnerPenguin.gameObject);
                partnerPenguin = null;
            }

            if (dummy1 != null) dummy1.ResetStats();
            if (dummy2 != null) dummy2.ResetStats();

            Debug.Log("[PenguinAbilityTestManager] 필드 엔티티 정리 완료.");
        }
        #endregion

        #region Time Scale Controls
        public void SetTimeScale(float scale)
        {
            Time.timeScale = Mathf.Max(0f, scale);
            Time.fixedDeltaTime = initialFixedDeltaTime * Time.timeScale;
            Debug.Log($"[PenguinAbilityTestManager] Time Scale: {Time.timeScale:F2}x");
        }
        #endregion

        #region Game State Controls (Wave & Maintenance)
        public void StartWaveState()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameState.WaveInProgress);
                Debug.Log("[PenguinAbilityTestManager] 게임 상태 전환 -> WaveInProgress (전투)");
            }
            else
            {
                Debug.LogWarning("[PenguinAbilityTestManager] 씬에 GameManager가 존재하지 않습니다.");
            }
        }

        public void StartMaintenanceState()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameState.MaintenanceTime);
                Debug.Log("[PenguinAbilityTestManager] 게임 상태 전환 -> MaintenanceTime (정비)");
            }
            else
            {
                Debug.LogWarning("[PenguinAbilityTestManager] 씬에 GameManager가 존재하지 않습니다.");
            }
        }

        public void ToggleGameState()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePhase();
                Debug.Log($"[PenguinAbilityTestManager] 게임 상태 토글 -> {GameManager.Instance.CurrentState}");
            }
            else
            {
                Debug.LogWarning("[PenguinAbilityTestManager] 씬에 GameManager가 존재하지 않습니다.");
            }
        }
        #endregion
    }
}


