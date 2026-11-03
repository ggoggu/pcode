using System.Collections.Generic;
using PenguinPinball.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "New Counter Wave Effect", menuName = "Charms/Effects/Counter Wave")]
public class Effect_CounterWave : EffectBase
{
    [Header("반격 파동 설정")]
    [Tooltip("반격 파동으로 반경 내 모든 적에게 가할 피해량")]
    [SerializeField] private float waveDamage = 40f;

    [Tooltip("베이스 중심 파동 공격 반경 (m)")]
    [SerializeField] private float waveRadius = 6.0f;

    [Tooltip("반격 파동 발동 쿨타임 (초)")]
    [SerializeField] private float cooldown = 5.0f;

    [Tooltip("파동 발동 시 베이스 위치에 생성할 시각 효과 프리팹 (선택)")]
    [SerializeField] private GameObject waveVfxPrefab;

    // 런타임 상태
    private float lastTriggerTime = -999f;

    public float WaveDamage
    {
        get => waveDamage;
        set => waveDamage = value;
    }

    public float WaveRadius
    {
        get => waveRadius;
        set => waveRadius = value;
    }

    public float Cooldown
    {
        get => cooldown;
        set => cooldown = value;
    }

    public float LastTriggerTime
    {
        get => lastTriggerTime;
        set => lastTriggerTime = value;
    }

    public GameObject WaveVfxPrefab
    {
        get => waveVfxPrefab;
        set => waveVfxPrefab = value;
    }

    public bool IsOnCooldown => (Time.time - lastTriggerTime) < cooldown;
    public float RemainingCooldown => Mathf.Max(0f, cooldown - (Time.time - lastTriggerTime));

    public override void ApplyEffect(GameObject origin = null)
    {
        lastTriggerTime = -cooldown; // 장착 즉시 첫 공격에 발동 가능하도록 초기화
        GameEvents.BaseAttacked += HandleBaseAttacked;
        Debug.Log($"[Effect_CounterWave] 반격 파동 효과 활성화 (피해량: {waveDamage}, 반경: {waveRadius}m, 쿨타임: {cooldown}초)");
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        GameEvents.BaseAttacked -= HandleBaseAttacked;
        Debug.Log("[Effect_CounterWave] 반격 파동 효과 해제");
    }

    private void HandleBaseAttacked(Vector3 basePosition)
    {
        // 쿨타임 검사
        if (IsOnCooldown)
        {
            Debug.Log($"[Effect_CounterWave] 반격 파동 쿨타임 중 (남은 시간: {RemainingCooldown:F2}초)");
            return;
        }

        lastTriggerTime = Time.time;

        // 베이스 중심 좌표 결정: 씬 내 BaseHealth 컴포넌트 우선, 없으면 전달된 basePosition 사용
        Vector3 center = BaseHealth.Instance != null ? BaseHealth.Instance.transform.position : basePosition;

        ExecuteCounterWave(center);
    }

    /// <summary>
    /// 지정된 중심 좌표를 기준으로 반경 내 모든 활성 적에게 범위 공격을 실행합니다.
    /// </summary>
    /// <param name="center">공격 중심 위치</param>
    /// <returns>피해를 입힌 적의 수</returns>
    public int ExecuteCounterWave(Vector3 center)
    {
        HashSet<Enemy> targetEnemies = new HashSet<Enemy>();

        // 1. 물리 OverlapSphere로 반경 내 콜라이더 탐색 (Trigger 포함 모든 레이어)
        Collider[] colliders = Physics.OverlapSphere(center, waveRadius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;
            var enemy = colliders[i].GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                targetEnemies.Add(enemy);
            }
        }

        // 2. 테스트 환경 또는 비물리 적 보완: 씬 내 활성 적 거리 기반 탐색 (중복 방지)
        if (targetEnemies.Count == 0)
        {
            var allEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            for (int i = 0; i < allEnemies.Length; i++)
            {
                var enemy = allEnemies[i];
                if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
                {
                    if (Vector3.Distance(center, enemy.transform.position) <= waveRadius)
                    {
                        targetEnemies.Add(enemy);
                    }
                }
            }
        }

        // 3. 탐색된 대상 적들에게 CombatResolver를 통해 범위 공격 적용
        foreach (var enemy in targetEnemies)
        {
            CombatResolver.DealDamage(null, enemy, waveDamage, isCollision: false);
        }

        // 4. 시각 효과(VFX) 생성
        if (waveVfxPrefab != null)
        {
            Instantiate(waveVfxPrefab, center, Quaternion.identity);
        }

        Debug.Log($"[Effect_CounterWave] 반격 파동 발동 완료! 중심: {center}, 적중: {targetEnemies.Count}마리, 피해: {waveDamage}");

        // 소모형 부적 대응 (영구 부적인 경우 내부에서 무시됨)
        TriggerUse();

        return targetEnemies.Count;
    }
}
