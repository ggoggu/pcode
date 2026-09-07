using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class PenguinController : MonoBehaviour
    {
        #region Serialized Fields & Inspector Settings
        [SerializeField] PenguinDefinition definition;
        [Tooltip("Optional visual root. Renderer children are hidden while held or respawning.")]
        [SerializeField] GameObject visualRoot;

        [Header("Config Reference")]
        [SerializeField] private PinballPhysicsConfig physicsConfig;

        [Header("Penguin Tier Settings")]
        [SerializeField, Min(1)] private int tier = 1;
        #endregion

        #region Properties & Public Fields
        public PenguinDefinition Definition => definition;
        public Rigidbody Body { get; private set; }
        public Collider Collider { get; private set; }
        public int Combo { get; private set; }
        public bool IsOverloaded { get; private set; }
        public bool IsAvailable { get; private set; } = true;
        public bool IsInField { get; private set; } = false;
        public IPenguinAbility Ability => ability;
        public int Tier => tier;
        public Dictionary<StatType, Stat> Stats { get; private set; } = new();
        public PenguinEquipmentHandler EquipmentHandler { get; private set; }
        public float CurrentMaxSpeed
        {
            get
            {
                float baseMaxSpeed = Stats.TryGetValue(StatType.MaxSpeed, out var stat) ? stat.Value : (definition != null ? definition.maxSpeed : 15f);
                return IsOverloaded ? baseMaxSpeed * definition.overloadMaxSpeedMultiplier : baseMaxSpeed;
            }
        }
        #endregion

        #region Private Variables
        private Vector3 lastCheckedPosition;
        private float stuckCheckTimer;
        private float timeSinceLaunched;
        private Coroutine respawnCoroutine;
        private IPenguinAbility ability;
        private float lastComboAt;
        private int originalLayer;
        private bool isWaitingForLayerRestore = false;
        private readonly HashSet<Collider> currentOverlappingEnemyColliders = new();
        private readonly object tierModifierSource = new object();
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            Body=GetComponent<Rigidbody>();
            Collider=GetComponent<Collider>();

            EquipmentHandler = GetComponent<PenguinEquipmentHandler>();
            if (EquipmentHandler == null)
            {
                EquipmentHandler = gameObject.AddComponent<PenguinEquipmentHandler>();
            }

            int penguinLayer = LayerMask.NameToLayer("Penguin");
            originalLayer = (penguinLayer != -1) ? penguinLayer : gameObject.layer;

            if (definition != null)
            {
                Init(definition);
            }
        }

        private void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;

            if (GameManager.Instance != null)
            {
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;

            StopRespawnTimer();
        }

        void Update()
        {
            if (!IsInField)
                return;

            ability.Tick();

            CheckStuckCondition();

            if (Combo > 0 && Time.time - lastComboAt > CurrentComboDuration())
                ResetCombo();
        }

        private void FixedUpdate()
        {
            if (!IsInField) return;

            ClampSpeed();
        }

        void OnDestroy() => ability?.Dispose();
        #endregion

        #region Initialization & Setup
        // 펭귄 소환할 때 basepenguinprefab 만들고 init(penguin definition)으로 능력 할당하기
        public void Init(PenguinDefinition newDefinition, int initialTier = 1)
        {
            if (newDefinition == null)
            {
                Debug.LogError("Penguin Definition이 null입니다.", this);
                enabled = false;
                return;
            }

            definition = newDefinition;
            tier = Mathf.Max(1, initialTier);

            gameObject.name = $"{definition.DisplayName}_Tier{tier}";

            // 1. 외형 모델 생성
            SetupVisual();

            // 2. 스탯 초기화
            InitializeStats();

            // 3. 장비 핸들러 세팅
            EquipmentHandler.Initialize(this);

            // 4. 능력(Ability) 생성 및 초기화
            ability?.Dispose();
            ability = definition.CreateAbility();
            ability.Initialize(this);

            SetHeld(true);
        }

        private void SetupVisual()
        {
            if (visualRoot == null) return;

            // 1. visualRoot 하위 기존 오브젝트 청소
            foreach (Transform child in visualRoot.transform)
            {
                Destroy(child.gameObject);
            }

            // 2. Visual Handler 컴포넌트 확보 (없으면 자동 추가)
            if (!visualRoot.TryGetComponent<PenguinVisualHandler>(out var visualHandler))
            {
                visualHandler = visualRoot.AddComponent<PenguinVisualHandler>();
            }

            // 3. Definition의 Visual Prefab을 visualRoot 아래 동적 생성
            if (definition != null && definition.VisualPrefab != null)
            {
                GameObject visualInstance = Instantiate(definition.VisualPrefab, visualRoot.transform);

                // 4. VisualHandler 초기화 (Rigidbody 참조 및 생성된 비주얼 객체 전달)
                visualHandler.Initialize(Body, visualInstance);
            }
        }

        private void InitializeStats()
        {
            Stats[StatType.Attack] = new Stat(definition.attack);
            Stats[StatType.MaxSpeed] = new Stat(definition.maxSpeed);
            Stats[StatType.LinearDrag] = new Stat(definition.linearDrag);
            Stats[StatType.Mass] = new Stat(definition.mass);
            Stats[StatType.RespawnTime] = new Stat(definition.respawnSeconds);

            UpdateTierModifiers();
            ApplyStatsToPhysics();
        }

        public void SetTier(int newTier)
        {
            if (newTier < 1)
            {
                Debug.LogWarning($"[PenguinController] Invalid tier value: {newTier}. Tier must be at least 1.", this);
                return;
            }

            tier = newTier;
            UpdateTierModifiers();
            OnStatsChanged();
        }

        private void UpdateTierModifiers()
        {
            if (definition == null) return;

            // 1. 기존에 적용된 티어 관련 모디파이어 싹 비우기
            foreach (var stat in Stats.Values)
            {
                stat.RemoveAllModifiersFromSource(tierModifierSource);
            }

            int tierOffset = tier - 1;
            if (tierOffset <= 0) return; // 1티어는 추가 모디파이어 불필요

            // 2. 현재 티어에 맞는 모디파이어 추가 (Base 적용)
            Stats[StatType.Attack].AddModifier(new StatModifier(tierOffset * definition.attackGrowthPerTier, StatModType.BasePercentAdd, tierModifierSource));
            Stats[StatType.MaxSpeed].AddModifier(new StatModifier(tierOffset * definition.maxSpeedGrowthPerTier, StatModType.BasePercentAdd, tierModifierSource));
            Stats[StatType.Mass].AddModifier(new StatModifier(tierOffset * definition.massGrowthPerTier, StatModType.BaseFlat, tierModifierSource));
            Stats[StatType.RespawnTime].AddModifier(new StatModifier(-tierOffset * definition.respawnTimeGrowthPerTier, StatModType.BasePercentAdd, tierModifierSource));
        }
        #endregion

        #region Movement & Launch Logic
        public void PrepareAndLaunch(Vector3 position, Vector3 direction, float speed)
        {
            if (!IsAvailable || IsInField) return;

            transform.position = position;

            if (Body != null)
            {
                Body.position = position;
            }

            SetHeld(false);
            IsInField = true;

            lastCheckedPosition = position;
            stuckCheckTimer = 0f;
            timeSinceLaunched = 0f;

            Launch(direction, speed);
        }

        public void Launch(Vector3 direction, float speed)
        {
            if (Body == null) return;

            Body.linearVelocity = direction * speed;
        }

        public void LaunchTowards(Vector3 point, float speed)
        {
            var dir = (point - transform.position).normalized;
            Body.linearVelocity = dir * Mathf.Min(speed, CurrentMaxSpeed);
        }

        void ClampSpeed()
        {
            if (Body == null) return;
            float maxSpeed = CurrentMaxSpeed;

            float cap = maxSpeed * (IsOverloaded ? definition.overloadMaxSpeedMultiplier : 1f);
            if (Body.linearVelocity.sqrMagnitude > cap * cap)
                Body.linearVelocity = Body.linearVelocity.normalized * cap;
        }
        #endregion

        #region Combo & Overload System
        public void AddCombo(int value = 1)
        {
            if (!IsInField || value <= 0) return;

            Combo += value;
            lastComboAt = Time.time;
            GameEvents.RaiseComboChanged(this, Combo);

            if (!IsOverloaded && Combo >= definition.overloadThreshold)
            {
                IsOverloaded = true;
                Body.linearDamping = Stats[StatType.LinearDrag].Value * definition.overloadDragMultiplier; //과부화되었을 때 마찰력 조절
                ability.OnOverloadStarted();
                GameEvents.RaiseOverloadStarted(this);
            }
        }

        public void ResetCombo()
        {
            if (Combo == 0 && !IsOverloaded) return;

            Combo = 0;
            GameEvents.RaiseComboChanged(this, 0);

            if (IsOverloaded)
            {
                IsOverloaded = false;
                Body.linearDamping = Stats[StatType.LinearDrag].Value;
                ability.OnOverloadEnded();
                GameEvents.RaiseOverloadEnded(this);
            }
        }

        float CurrentComboDuration() => Mathf.Max(.25f, (definition.comboBaseDuration - Combo * definition.durationLossPerCombo) * definition.comboDurationMultiplier);
        #endregion

        #region Life Cycle (Drain & Respawn)
        public void Drain(float respawnMultiplier = 1f)
        {
            if (!IsInField) return;

            IsInField = false;
            IsAvailable = false;
            ResetCombo();
            SetHeld(true);

            float seconds = Mathf.Max(0.5f, Stats[StatType.RespawnTime].Value * respawnMultiplier);
            GameEvents.RaisePenguinDrained(this, seconds);

            StopRespawnTimer();
            respawnCoroutine = StartCoroutine(RespawnRoutine(seconds));
        }

        private IEnumerator RespawnRoutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.WaveInProgress)
            {
                respawnCoroutine = null;
                yield break;
            }

            CompleteRespawn();
            respawnCoroutine = null;
        }

        private void StopRespawnTimer()
        {
            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
            }
        }

        public void CompleteRespawn()
        {
            if (IsAvailable) return;
            IsAvailable = true;
            SetHeld(true);
            GameEvents.RaisePenguinRespawned(this);
        }

        public void ResetToHeld() // 펭귄을 발사 전 대기 상태로 강제 회수하고 초기화하는 함수
        {
            StopRespawnTimer();

            if (!IsInField) return;

            gameObject.layer = originalLayer;
            isWaitingForLayerRestore = false;
            currentOverlappingEnemyColliders.Clear();

            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            ResetCombo();

            stuckCheckTimer = 0f;
            timeSinceLaunched = 0f;

            IsInField = false;
            IsAvailable = true;
            SetHeld(true);
            GameEvents.RaisePenguinRespawned(this);
        }

        void SetHeld(bool held)
        {
            if (held)
            {
                if (!Body.isKinematic)
                {
                    Body.linearVelocity = Vector3.zero;
                    Body.angularVelocity = Vector3.zero;
                    Body.isKinematic = true;
                }
            }
            else
            {
                Body.isKinematic = false;
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }

            Collider.enabled = !held;
            if (visualRoot) visualRoot.SetActive(!held);
        }
        #endregion

        #region Stuck Detection
        private void CheckStuckCondition()
        {
            timeSinceLaunched += Time.deltaTime;

            // 발사 직후 유예 시간 동안은 검사 제외
            if (timeSinceLaunched < physicsConfig.launchGracePeriod) return;

            stuckCheckTimer += Time.deltaTime;

            if (stuckCheckTimer >= physicsConfig.stuckCheckInterval)
            {
                float distanceMoved = Vector3.Distance(transform.position, lastCheckedPosition);

                // 지정된 시간 동안 이동 거리가 기준치 미만이면 어딘가 낀 것으로 판단
                if (distanceMoved < physicsConfig.minMoveThreshold)
                {
                    stuckCheckTimer = 0f;
                    Drain();
                    return;
                }

                // 기준 위치 및 타이머 갱신
                lastCheckedPosition = transform.position;
                stuckCheckTimer = 0f;
            }
        }
        #endregion

        #region Collisions & Layer Control
        void OnCollisionEnter(Collision c)
        {
            if (!IsInField) return;

            GameEvents.RaisePenguinHit(this, c);

            if (c.collider.TryGetComponent<PinballBounceSurface>(out var bounceSurface)) return;

            if (c.collider.TryGetComponent<PenguinController>(out var other))
            {
                GameEvents.RaisePenguinsCollided(this, other);
                ability.OnPenguinCollision(other);
            }
            //else
            //{
            //    AddCombo();
            //    ability.OnEnvironmentCollision(c);
            //}
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsInField) return;

            // 적 부모/자식 오브젝트에서 Enemy 컴포넌트 탐색
            if (other.TryGetComponent<Enemy>(out var enemy) || other.GetComponentInParent<Enemy>() is { } parentEnemy && (enemy = parentEnemy) != null)
            {
                currentOverlappingEnemyColliders.Add(other);
                ability?.OnEnemyTriggerEnter(enemy, other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (currentOverlappingEnemyColliders.Remove(other))
            {
                // 과부화가 끝났으나 적 내부 탈출을 기다리던 상태였다면 안전하게 레이어 복구
                if (isWaitingForLayerRestore && currentOverlappingEnemyColliders.Count == 0)
                {
                    SetLayerRecursively(originalLayer);
                    isWaitingForLayerRestore = false;
                }
            }
        }

        public void RestoreDefaultLayer()
        {
            // 현재 적 몸 내부에 겹쳐있는 콜라이더가 없다면 즉시 원복
            if (currentOverlappingEnemyColliders.Count == 0)
            {
                SetLayerRecursively(originalLayer);
                isWaitingForLayerRestore = false;
            }
            else
            {
                // 몸체 내부에 갇혀 있다면, 완전히 탈출할 때까지 레이어 원복 유예
                isWaitingForLayerRestore = true;
            }
        }

        public void SetLayerRecursively(int newLayer)
        {
            SetLayerRecursivelyInternal(transform, newLayer);
        }

        private void SetLayerRecursivelyInternal(Transform trans, int newLayer)
        {
            trans.gameObject.layer = newLayer;
            foreach (Transform child in trans)
            {
                SetLayerRecursivelyInternal(child, newLayer);
            }
        }
        #endregion

        #region Stats & Physics Events
        public void OnStatsChanged()
        {
            ApplyStatsToPhysics();
        }

        private void ApplyStatsToPhysics()
        {
            if (Body == null) return;

            Body.mass = Stats[StatType.Mass].Value;

            float baseDrag = Stats[StatType.LinearDrag].Value;
            Body.linearDamping = IsOverloaded ? baseDrag * definition.overloadDragMultiplier : baseDrag;
        }

        private void HandleStateChanged(GameState newState)
        {
            if (newState != GameState.WaveInProgress)
            {
                ResetToHeld();
            }
        }
        #endregion
    }
}