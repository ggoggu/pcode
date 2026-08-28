// 프로그래머 A(펭귄 담당)와 합의할 인터페이스
// PenguinController가 이 인터페이스를 구현해야 함
public interface IPenguinEffectable
{
    // ── 축소 효과 (축소형 적) ──
    // 축소 발동: 일시적으로 크기(= 충돌 판정 범위)를 축소
    void ApplyShrink(float duration, float scaleMultiplier);
    // 축소 해제: 원래 크기로 복귀 (축소 중 외부 충돌 시 즉시 호출됨)
    void EndShrink();

    // ── 콤보 파괴 효과 (콤보 파괴형 적) ──
    // 콤보 즉시 삭제 + 과부하 상태 해제
    void ApplyComboReset();

    // ── 정지 효과 (콤보 파괴형 적) ──
    // 0.5초간 중력 미적용 + 속도 0으로 수렴
    void ApplyFreeze(float duration);
    // 정지 해제: 중력 복귀 + 속도 감소 중단 (정지 중 외부 충돌 시 즉시 호출됨)
    void EndFreeze();
}
