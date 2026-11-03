# 🐧 펭귄 능력 명세서 (Penguin Abilities)

> 프로젝트 내 모든 펭귄의 능력 동작 메커니즘, 스크립트 구조 및 파라미터를 통합 관리하는 명세서입니다.

---

## 📌 펭귄 능력 통합 요약표

| ID / 티어 | 계열 | 펭귄 이름 | 능력명 | 타입 | 주요 스크립트 | 바로가기 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **P-02 / T1** | 화력/딜러 | 헤비볼 펭귄 | 과부하 적 관통 | 패시브 | `HeavyballAbility.cs` | [이동](#1-헤비볼-펭귄-heavyballability) |
| **P-03 / T2** | 화력/딜러 | 작살 펭귄 | 처치 시 최단 거리 적 자동 발사 | 패시브 | `HarpoonAbility.cs` | [이동](#2-작살-펭귄-harpoonability) |
| **P-04 / T3** | 범위/버스트 | 폭풍 펭귄 | 과부하 충돌 데미지 범위 분배 | 패시브 | 개발 예정 | [이동](#3-폭풍-펭귄-stormability) |
| **P-04 (분리) / T미정** | 범위/버스트 | 미명명 | 범위 지속 데미지 & 과부하 강화 | 패시브 | 개발 예정 | [이동](#4-미명명-p-04-분리) |
| **P-05 / T3** | 범위/버스트 | 도화선 펭귄 | 과부하 첫 충돌 시 범위 폭발 | 패시브 | `FuseAbility.cs` | [이동](#5-도화선-펭귄-fuseability) |
| **P-08 / T4** | 범위/버스트 | 러시안 룰렛 펭귄 | 러시안 룰렛 (충돌 누적 확률 발동 & 필드 이탈) | 패시브 | `RussianRouletteAbility.cs` | [이동](#6-러시안-룰렛-펭귄-russianrouletteability) |
| **P-18 / T미정** | 범위/버스트 | 방화범 펭귄 | 화염 마크 & 과부하 연쇄 발화 | 패시브 | `PyroAbility.cs`, `BurningMark.cs` | [이동](#7-방화범-펭귄-pyroability) |
| **P-07 / T2** | 디버프/표식 | 취약 펭귄 | 과부하 충돌 적 취약 표식 부여 | 패시브 | `VulnerabilityAbility.cs`, `VulnerabilityMark.cs` | [이동](#8-취약-펭귄-vulnerabilityability) |
| **P-09 / T2** | 디버프/표식 | 고립 펭귄 | 과부하 진입 시 고립 적 표식 & 증폭 | 패시브 | `IsolatedAbility.cs` | [이동](#9-고립-펭귄-isolatedability) |
| **P-17 / T4** | 디버프/표식 | 정산 펭귄 | 과부하 누적 데미지 추적 & 종료 시 추가 정산 | 패시브 | `PaydayAbility.cs`, `PaydayMark.cs` | [이동](#10-정산-펭귄-paydayability) |
| **P-14 / T3** | 성장/특수 | 쉐빙 펭귄 | 고체력 적 대상 데미지 증폭 | 패시브 | `ShavingAbility.cs` | [이동](#11-쉐빙-펭귄-shavingability) |
| **P-16 / T3** | 성장/특수 | 눈싸움 펭귄 | 충돌 축적 & 과부하 눈덩이 연쇄 발사 | 패시브 | 개발 예정 | [이동](#12-눈싸움-펭귄-snowballability) |
| **P-24 / T미정** | 성장/특수 | 어미 펭귄 | 과부하 진입 시 새끼 펭귄 소환 발사 | 패시브 | 개발 예정 | [이동](#13-어미-펭귄-motherability) |

---

## 1. 헤비볼 펭귄 (HeavyballAbility)

### 1.1 개요
- **ID / 티어:** P-02 / T1
- **계열:** 화력 / 딜러
- **능력명:** 과부하 적 관통 (Ghost Pass-through)
- **타입:** 패시브 (과부하 상태 연동)
- **관련 클래스:** `HeavyballAbility.cs`, `HeavyballAbilitySO.cs`

### 1.2 동작 메커니즘
1. **과부하 진입 (`OnOverloadStarted`):**
   - 중복 타격 방지용 `HashSet` 초기화
   - 펭귄 레이어를 `GhostPenguin`으로 전환하여 적 충돌체(Solid Collider) 관통 허용
2. **적 관통 충돌 (`OnEnemyTriggerEnter`):**
   - 과부하 상태에서 적 트리거 충돌 시 1회에 한해 콤보(`AddCombo()`) 추가
   - `CombatResolver`를 통해 데미지 산출 및 전달
3. **과부하 종료 (`OnOverloadEnded`):**
   - 타격 목록 초기화
   - `Owner.RestoreDefaultLayer()`를 호출하여 기본 레이어로 안전 복구

### 1.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **Project Settings** | `GhostPenguin` Layer | `Project Settings > Tags and Layers`에 등록 필수 |
| **SO Asset** | `HeavyballAbilitySO` | `PenguinPinball/Abilities/Heavyball` 경로로 생성 |

### 1.4 주의사항 및 Edge Case
- **레이어 누락:** `GhostPenguin` 레이어가 없으면 레이어 전환 실패 에러 로그가 출력됩니다.
- **복구 유예:** 과부하 종료 시 적 내부 갇힘 방지를 위해 `RestoreDefaultLayer()`의 안전 탈출 로직이 동작합니다.

---

## 2. 작살 펭귄 (HarpoonAbility)

### 2.1 개요
- **ID / 티어:** P-03 / T2
- **계열:** 화력 / 딜러
- **능력명:** 처치 시 최단 거리 적 자동 발사 (Chain Launch)
- **타입:** 패시브 (처치 이벤트 감지 및 과부하 연동)
- **관련 클래스:** `HarpoonAbility.cs`, `HarpoonAbilitySO.cs`

### 2.2 동작 메커니즘
1. **이벤트 구독 (`Initialize` / `Dispose`):**
   - `GameEvents.EnemyKilled` 글로벌 정적 이벤트 구독 및 해제
2. **처치 조건 검사 (`OnKill`):**
   - 처치 주체가 본인(`source == Owner`)이고 **과부하 상태(`IsOverloaded`)**인지 확인
3. **타겟 탐색 및 발사 (`FindNearest` / `LaunchTowards`):**
   - 처치 위치 기준 가장 가까운 살아있는 `Enemy` 탐색
   - 대상 방향으로 현재 속도 크기(`linearVelocity.magnitude`)를 유지한 채 자동 발사

### 2.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **Event System** | `GameEvents.EnemyKilled` | 적 처치 이벤트 연동 필수 |
| **SO Asset** | `HarpoonAbilitySO` | `PenguinPinball/Abilities/Harpoon` 경로로 생성 |

### 2.4 주의사항 및 Edge Case
- **메모리 누수:** `Dispose()` 호출 시 이벤트 구독 해제가 필수입니다.
- **과부하 조건:** 과부하 상태가 아닐 때의 처치는 자동 발사가 발동하지 않습니다.

---

## 3. 폭풍 펭귄 (StormAbility)

### 3.1 개요
- **ID / 티어:** P-04 / T3
- **계열:** 범위 / 버스트
- **능력명:** 과부하 충돌 데미지 범위 분배
- **타입:** 패시브 (과부하 상태 연동)
- **관련 클래스:** 개발 예정

### 3.2 동작 메커니즘 (기획 스펙)
1. **과부하 충돌:** 과부하 상태에서 충돌 발생 시 데미지를 범위 내 적들에게 균등하게 분배합니다.
2. **범위 판정:** 범위 내에 존재하는 각 적에게 개별 충돌 판정이 적용됩니다.

### 3.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **상태** | 개발 예정 | 코드 구현 후 세부 스펙 반영 예정 |

---

## 4. 미명명 (P-04 분리)

### 4.1 개요
- **ID / 티어:** P-04 분리 / T미정
- **계열:** 범위 / 버스트
- **능력명:** 범위 지속 데미지 & 과부하 강화
- **타입:** 패시브
- **관련 클래스:** 개발 예정

### 4.2 동작 메커니즘 (기획 스펙)
1. **기본 효과:** 범위 내 적에게 공격력 기반 지속 데미지를 부여합니다.
2. **과부하 강화:** 과부하 진입 시 범위가 확장되고 데미지가 증가합니다.
3. **콤보 제약:** 과부하 상태에서의 지속 데미지는 별도의 데미지 소스로 취급되어 콤보 카운트에 반영되지 않습니다.

### 4.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **상태** | 개발 예정 (채택 상태) | 코드 구현 후 세부 스펙 반영 예정 |

---

## 5. 도화선 펭귄 (FuseAbility)

### 5.1 개요
- **ID / 티어:** P-05 / T3
- **계열:** 범위 / 버스트
- **능력명:** 과부하 첫 충돌 시 범위 폭발
- **타입:** 패시브 (과부하 상태 연동)
- **관련 클래스:** `FuseAbility.cs`, `FuseAbilitySO.cs`

### 5.2 동작 메커니즘
1. **과부하 진입 (`OnOverloadStarted`):**
   - 폭발 가동 상태(`armed = true`)로 전환
2. **첫 충돌 및 범위 폭발 (`OnEnemyCollision`):**
   - 가동 상태에서 적과 첫 충돌 시 발동 후 비활성화(`armed = false`)
   - 충돌 시점의 콤보 수치에 비례하여 데미지 산정 (`기본 데미지 * (1 + 콤보수 * comboDamageBonus)`)
   - 펭귄 위치 기준 지정된 반지름(`explosionRadius`) 내 살아있는 모든 적에게 계산된 폭발 데미지 전달 (`Physics.OverlapSphereNonAlloc`)
3. **콤보 초기화 및 종료:**
   - 폭발 적용 후 펭귄의 콤보를 초기화 (`ResetCombo()`)
   - 과부하 종료 (`OnOverloadEnded`) 시 가동 상태 해제 (`armed = false`)

### 5.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **SO Asset** | `FuseAbilitySO` | `PenguinPinball/Abilities/Fuse` 경로로 생성 |
| **Inspector Parameter** | `explosionRadius` (`float`) | 폭발 범위 반지름 (기본값: `3f`, 최소: `0.1f`) |
| **Inspector Parameter** | `comboDamageBonus` (`float`) | 콤보당 추가되는 데미지 비율 (기본값: `0.1f`, 최소: `0f`) |

### 5.4 주의사항 및 Edge Case
- **1회성 발동:** 과부하 진입 후 단 한 번의 첫 충돌 시에만 폭발이 발동하고 콤보가 리셋됩니다.
- **최대 타겟 제한:** 내부 충돌 탐색 버퍼 크기가 32개로 설정되어 있어, 한 번의 폭발로 최대 32개의 콜라이더까지만 감지됩니다.

---

## 6. 러시안 룰렛 펭귄 (RussianRouletteAbility)

### 6.1 개요
- **ID / 티어:** P-08 / T4
- **계열:** 범위 / 버스트
- **능력명:** 러시안 룰렛 (충돌 누적 확률 발동 & 필드 이탈)
- **타입:** 패시브
- **관련 클래스:** `RussianRouletteAbility.cs`, `RussianRouletteAbilitySO.cs`

### 6.2 동작 메커니즘
1. **충돌 카운트 누적 (`OnEnemyCollision`):**
   - 적 충돌 시마다 내부 충돌 횟수(`_hitCount`)가 1씩 증가합니다.
2. **단계별 확률 검사:**
   - 충돌 횟수에 따라 지정된 확률(`triggerProbabilities`)로 발동 여부를 검사합니다 (1회차 1/6 ~ 6회차 100%).
3. **고정 데미지 적용 및 필드 이탈:**
   - 확률 조건 충족 시 데미지를 고정 데미지(`fixedDamage`)로 변경하고, 이탈 플래그(`shouldDrain = true`)를 활성화하여 필드에서 퇴장시킨 뒤 카운트를 초기화합니다.

### 6.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **SO Asset** | `RussianRouletteAbilitySO` | `PenguinPinball/Abilities/RussianRoulette` 경로로 생성 |
| **Inspector Parameter** | `maxHitCount` (`int`) | 최대 충돌 횟수 (기본값: `6`, 최소: `1`) |
| **Inspector Parameter** | `fixedDamage` (`float`) | 발동 시 적용할 고정 데미지 (기본값: `600f`) |
| **Inspector Parameter** | `triggerProbabilities` (`float[]`) | 충돌 횟수별 발동 확률 배열 (크기는 `maxHitCount`와 일치 권장) |

### 6.4 주의사항 및 Edge Case
- **필드 이탈 처리:** 발동 시 `shouldDrain = true`가 전달되어 펭귄 컨트롤러 측에서 드레인 및 필드 이탈 처리가 진행됩니다.
- **확률 배열 불일치:** `triggerProbabilities` 배열 길이가 `maxHitCount`보다 작을 경우 `Mathf.Clamp`에 의해 마지막 인덱스의 확률이 반복 적용될 수 있습니다.

---

## 7. 방화범 펭귄 (PyroAbility)

### 7.1 개요
- **ID / 티어:** P-18 / T미정
- **계열:** 범위 / 버스트
- **능력명:** 화염 마크 & 과부하 연쇄 발화 (Burn & Flame AOE)
- **타입:** 패시브
- **관련 클래스:** `PyroAbility.cs`, `PyroAbilitySO.cs`, `BurningMark.cs`

### 7.2 동작 메커니즘
1. **화염 마크 부여 (`OnEnemyCollision`):**
   - 적 충돌 시 기본 데미지의 비율(`burnDamageRatio`)에 해당하는 지속 데미지 디버프(`BurningMark`)를 적에게 부여합니다.
   - 지정된 지속시간(`burnDuration`) 동안 `tickInterval` 주기마다 나뉜 데미지가 들어갑니다.
2. **지속시간 갱신:**
   - 이미 불타는 적에게 재충돌할 경우 불태우기 지속시간이 다시 초기값(`burnDuration`)으로 리셋됩니다.
3. **과부하 범위 연쇄 발화:**
   - **과부하 상태**에서 이미 불타는 적에게 재충돌할 경우, 해당 위치 기준 범위(`aoeRadius`) 내의 모든 적에게 화염 디버프를 전이시켜 동시에 불태웁니다.

### 7.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **SO Asset** | `PyroAbilitySO` | `PenguinPinball/Abilities/Pyro` 경로로 생성 |
| **Project Settings** | `Enemy` Layer | `Physics.OverlapSphere` 탐색을 위해 `Enemy` 레이어 설정 필수 |
| **Inspector Parameter** | `burnDuration` (`float`) | 화염 지속시간 (기본값: `3.0f`) |
| **Inspector Parameter** | `burnDamageRatio` (`float`) | 기본 데미지 대비 총 지속 데미지 비율 (기본값: `0.5f`, 0~1) |
| **Inspector Parameter** | `tickInterval` (`float`) | 데미지 주기 (기본값: `0.5f`) |
| **Inspector Parameter** | `aoeRadius` (`float`) | 과부하 연쇄 발화 범위 반지름 (기본값: `2.5f`) |

### 7.4 주의사항 및 Edge Case
- **컴포넌트 동적 추가:** `BurningMark`는 적 런타임에 `AddComponent` 방식으로 추가되며, 적 사망 시 `Update`에서 자동 파기 처리됩니다.
- **연쇄 발화 조건:** 과부하 상태이더라도 **이미 불타는 상태의 적**과 재충돌해야 범위 발화(AOE)가 유발됩니다. 첫 충돌 시에는 발동하지 않습니다.

---

## 8. 취약 펭귄 (VulnerabilityAbility)

### 8.1 개요
- **ID / 티어:** P-07 / T2
- **계열:** 디버프 / 표식
- **능력명:** 과부하 취약 표식 (Vulnerability Mark)
- **타입:** 패시브
- **관련 클래스:** `VulnerabilityAbility.cs`, `VulnerabilityAbilitySO.cs`, `VulnerabilityMark.cs`

### 8.2 동작 메커니즘
1. **표식 부여 (`OnEnemyCollision`):**
   - 과부하 상태에서 충돌한 적에게 `VulnerabilityMark`를 부여합니다. (이미 부착된 적과 충돌 시 표식을 재부여/갱신)
2. **데미지 증폭 및 소모 (`ConsumeAndModifyDamage`):**
   - 취약 표식을 보유한 적이 충돌로 데미지를 입을 때 추가 데미지(증폭)를 받으며, 1회 적용 후 표식이 소모됩니다.
3. **표식 유지 및 정리 (`TrimExcessMarks`):**
   - 과부하 동안 표식이 계속 유지되며, 설정된 최대 유지 개수(`MaxMarkCount`)를 초과하면 가장 오래된 표식부터 순차적으로 해제됩니다. 과부하 종료 시 제한 개수가 조정됩니다.

### 8.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **SO Asset** | `VulnerabilityAbilitySO` | `PenguinPinball/Abilities/Vulnerability` 경로로 생성 |
| **Inspector Parameter** | `normalMaxMarkCount` (`int`) | 일반 상태 시 최대 유지 표식 개수 (기본값: `1`) |
| **Inspector Parameter** | `overloadMaxMarkCount` (`int`) | 과부하 상태 시 최대 유지 표식 개수 (기본값: `3`) |

### 8.4 주의사항 및 Edge Case
- **상태 전환 제어:** 과부하 진입/종료 이벤트(`OnOverloadStarted`, `OnOverloadEnded`) 발생 시 초과된 표식을 즉시 정리(`TrimExcessMarks`)합니다.

---

## 9. 고립 펭귄 (IsolatedAbility)

### 9.1 개요
- **ID / 티어:** P-09 / T2
- **계열:** 디버프 / 표식
- **능력명:** 고립 표식 부여 & 데미지 증폭 (Isolation)
- **타입:** 패시브
- **관련 클래스:** `IsolatedAbility.cs`, `IsolatedAbilitySO.cs`

### 9.2 동작 메커니즘 (기획 스펙 기준)
1. **고립 조건 검사 및 표식 부여:**
   - 과부하 진입 시점에 각 적을 기준으로 일정 반경(`isolationCheckRadius`) 내에 다른 적이 없는 조건 만족 시 고립 표식을 자동으로 부여합니다.
2. **조건 변화 감지:**
   - 주변에 다른 적이 접근하여 고립 조건을 만족하지 못하게 되면 고립 표식이 자동으로 해제됩니다.
3. **증폭 데미지 적용 및 표식 소멸:**
   - 고립 표식을 보유한 적과 충돌 시 200% 데미지(`damageMultiplier`)를 입히고 적의 고립 표식을 소멸시킵니다.

### 9.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **SO Asset** | `IsolatedAbilitySO` | `PenguinPinball/Abilities/Isolated` 경로로 생성 |
| **Inspector Parameter** | `isolationCheckRadius` (`float`) | 고립 여부를 탐색할 반경 (기본값: `5f`) |
| **Inspector Parameter** | `damageMultiplier` (`float`) | 고립 적 충돌 시 데미지 배율 (기본값: `2f`, 200%) |

### 9.4 주의사항 및 코드 수정 필요 사항
- **구현 방식 수정 필요:** 현재 작성된 코드(`OnEnemyCollision` 시점 즉시 주변 적 검사 및 데미지 곱셈)를 **과부하 진입 시 자동 표식 부여 및 조건 불만족 시 자동 해제, 충돌 후 표식 소멸** 구조로 개편이 필요합니다.

---

## 10. 정산 펭귄 (PaydayAbility)

### 10.1 개요
- **ID / 티어:** P-17 / T4
- **계열:** 디버프 / 표식
- **능력명:** 추적 표식 & 과부하 종료 데미지 정산 (Payday)
- **타입:** 패시브
- **관련 클래스:** `PaydayAbility.cs`, `PaydayAbilitySO.cs`, `PaydayMark.cs`

### 10.2 동작 메커니즘
1. **추적 표식 부여 및 데미지 누적 (`OnEnemyCollision`):**
   - 과부하 중 충돌한 적에게 `PaydayMark`를 부여하고, 과부하 동안 해당 적에게 입힌 데미지를 표식에 누적 기록합니다.
2. **과부하 종료 시 추가 정산 (`OnOverloadEnded`):**
   - 과부하가 종료되는 시점에 표식이 부착된 모든 대상의 누적 데미지를 합산하고, 설정된 정산 비율(`damageMultiplier`, 예: 40%)에 해당하는 추가 폭발 데미지를 입힙니다 (`PopMark`).
3. **콤보 미반영:**
   - 정산 폭발 데미지는 별도 데미지 소스(`isCollision: false`)로 전달되어 콤보 카운트에 추가되지 않습니다.

### 10.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **SO Asset** | `PaydayAbilitySO` | `PenguinPinball/Abilities/Payday` 경로로 생성 |
| **Inspector Parameter** | `damageMultiplier` (`float`) | 과부하 종료 시 정산할 데미지 비율 (기본값: `0.4f`, 40%, 범위: 0~1) |

### 10.4 주의사항 및 Edge Case
- **사망 적 예외 처리:** 과부하 종료 정산 시 이미 사망한 적은 정산 대상에서 제외되며 `PaydayMark` 컴포넌트가 파괴 해제됩니다.

---

## 11. 쉐빙 펭귄 (ShavingAbility)

### 11.1 개요
- **ID / 티어:** P-14 / T3
- **계열:** 성장 / 특수
- **능력명:** 고체력 적 선제 타격 증폭 (Shaving)
- **타입:** 패시브
- **관련 클래스:** `ShavingAbility.cs`, `ShavingAbilitySO.cs`

### 11.2 동작 메커니즘
1. **적 체력 비율 검사 (`OnEnemyCollision`):**
   - 충돌한 적의 현재 체력 비율(`CurrentHealth / MaxHealth`)이 기준치(`healthThresholdRatio`, 기본 80%) 이상인지 확인합니다.
2. **조건부 데미지 증폭:**
   - 조건 만족 시 일반 상태에서는 140%(`normalDamageMultiplier`), 과부하 상태에서는 180%(`overloadedDamageMultiplier`)의 데미지 배율을 적용합니다.

### 11.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **SO Asset** | `ShavingAbilitySO` | `PenguinPinball/Abilities/Shaving` 경로로 생성 |
| **Inspector Parameter** | `healthThresholdRatio` (`float`) | 증폭 조건 체력 비율 (기본값: `0.8f`, 80%, 범위: 0~1) |
| **Inspector Parameter** | `normalDamageMultiplier` (`float`) | 일반 상태 데미지 배율 (기본값: `1.4f`, 140%) |
| **Inspector Parameter** | `overloadedDamageMultiplier` (`float`) | 과부하 상태 데미지 배율 (기본값: `1.8f`, 180%) |

### 11.4 주의사항 및 Edge Case
- **단일 충돌 산정:** 충돌 직전 시점의 체력 비율을 기준으로 판정하므로, 첫 타격 시 체력이 80% 이상이라면 해당 타격 전체에 증폭 데미지가 적용됩니다.

---

## 12. 눈싸움 펭귄 (SnowballAbility)

### 12.1 개요
- **ID / 티어:** P-16 / T3
- **계열:** 성장 / 특수
- **능력명:** 충돌 축적 & 과부하 눈덩이 발사
- **타입:** 패시브 (충전 & 과부하 연동)
- **관련 클래스:** 개발 예정

### 12.2 동작 메커니즘 (기획 스펙 기준)
1. **충전 축적:**
   - 필드의 오브젝트 또는 적과 충돌할 때마다 충전 수치를 1씩 축적합니다.
2. **과부하 눈덩이 연쇄 발사:**
   - 과부하 진입 시 축적된 충전 수치만큼 가장 가까운 적을 향해 눈덩이를 연쇄 발사하고 충전량을 소모합니다.
3. **콤보 미반영:**
   - 눈덩이 데미지는 별도 데미지 소스로 취급되어 콤보 카운트를 올리지 않습니다.

### 12.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **상태** | 개발 예정 (채택 상태) | 코드 구현 후 세부 스펙 반영 예정 |

---

## 13. 어미 펭귄 (MotherAbility)

### 13.1 개요
- **ID / 티어:** P-24 / T미정
- **계열:** 성장 / 특수
- **능력명:** 과부하 새끼 펭귄 생성 발사
- **타입:** 패시브 (과부하 진입 연동)
- **관련 클래스:** 개발 예정

### 13.2 동작 메커니즘 (기획 스펙 기준)
1. **새끼 펭귄 생성 및 발사:**
   - 과부하 진입 시점에 새끼 펭귄 3마리를 생성하여 필드로 발사합니다.
2. **능력치 계승:**
   - 생성된 새끼 펭귄은 어미 펭귄 공격력의 30% 수치를 지닙니다.

### 13.3 설정 및 요구사항
| 분류 | 항목명 | 설명 |
| :--- | :--- | :--- |
| **상태** | 개발 예정 (채택 상태) | 코드 구현 후 세부 스펙 반영 예정 |