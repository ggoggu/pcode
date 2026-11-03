# 04. 독립 서브에이전트 기반 적대적 코드 심사 및 Unity 6 5대 최적화 검증 체계 구현 보고서
**PenguinShot 프로젝트 (Unity 6 & Google Antigravity)**

---

## Executive Summary (요약)

본 문서는 **FC 프로젝트(Unreal Engine 5.8 Dedicated Server)**의 멀티모델 독립 교차 검증 체계(`/interrogate`, `adversarial-auditor`)를 분석하고, 이를 **Unity 6 (6000.3.19f1) 핀볼/슬링샷 로그라이크 디펜스 아키텍처**에 맞추어 완벽하게 이식·특화한 **적대적 코드 심사관(Adversarial Code Auditor)** 및 5대 계층 연쇄 구축 완료 보고서입니다.

단일 생성 AI 모델이 코드를 작성할 때 필연적으로 발생하는 **인지적 맹점(Blind Spot), 오버엔지니어링, 핫패스(`Update`) 내 GC 가비지 생성, 이벤트 구독 누수, `.meta` 파일 누락** 문제를 원천 차단하기 위해, 코드 수정 권한이 원천 배제된 독립된 **Read-Only Auditor 서브에이전트(`adversarial-auditor`)**를 등록하고, **Unity 6 도메인 5대 핵심 차원(간결성, 기존 구조 재사용, 범위 내 적정 확장성, Unity 6 핫패스 GC/메모리 최적화, 라이프사이클 및 메타 안전성)**을 기준으로 엄격하게 코드를 심사하도록 구축했습니다.

---

## 1. FC 프로젝트 vs PenguinShot 프로젝트 서브에이전트 비교

| 구분 | FC 프로젝트 (Unreal Engine 5.8) | PenguinShot 프로젝트 (Unity 6) |
| :--- | :--- | :--- |
| **핵심 도메인 과제** | 데디케이트 서버 복제, RPC 소유권, 대역폭(`COND_*`), FastArray | 핫패스(`Update`) Zero GC Alloc, 컴포넌트 캐싱, 이벤트 누수 방지 |
| **비즈니스 분리 기준** | Server Authority vs Autonomous/Simulated Client | Pure C# POCO Domain Tier vs MonoBehaviour Presentation Tier |
| **테스트 철학** | Headless Automation Test (Engine batch) | Headless-First: Sub-second EditMode Unit Test (<3s) |
| **데이터 SSOT** | `UDataTable`, `UPrimaryDataAsset` | `ScriptableObject`, `DataAsset` |
| **에셋 정합성** | Binary Cooked Asset (.uasset) | `.meta` 파일 동기화, YAML 직렬화 보존 |
| **서브에이전트 역할** | `adversarial-auditor` (Read-Only) | `adversarial-auditor` (Read-Only) |
| **CLI 파이프라인 연계** | `python Scripts/pipeline.py audit` | `python Tools/RunTests.py --mode audit` |

---

## 2. 5대 계층 연쇄 구축 내역

```
PenguinShot/
├── .agents/
│   └── skills/
│       └── interrogate/
│           └── SKILL.md            <-- [Skill 계층] /interrogate 슬래시 커맨드 및 자동 티어링
├── .antigravity/
│   ├── rules/
│   │   ├── 01-unity-standards.md
│   │   ├── 02-test-harness.md
│   │   ├── 03-git-meta-safety.md
│   │   └── 04-adversarial-audit-standards.md <-- [Rule 계층] Unity 6 맞춤 5대 심사 차원
│   └── skills/
│       └── interrogate/
│           └── SKILL.md            <-- [Skill 계층 미러링]
├── AGENTS.md                       <-- [거버넌스 계층] 프로젝트 루트 시스템 및 파이프라인 총괄 지침
├── Tools/
│   └── RunTests.py                 <-- [CLI 파이프라인 계층] --mode {test, audit, static} 확장
└── define_subagent                 <-- [SubAgent 계층] Read-Only 'adversarial-auditor' 세션 등록
```

### ① SubAgent 계층 (`adversarial-auditor`)
- **Read-Only 격리**: `EnableWriteTools: false`로 설정하여 코드 오염 방지 및 순수 비판·감사 전담.
- **역할**: `git diff`와 메인 에이전트의 구현 의도(Stated Intent)를 입력받아 5대 차원 점수 및 판정(`APPROVED (LGTM)` vs `CHANGES REQUESTED (BLOCKING)`) 산출.

### ② Rule 계층 (`04-adversarial-audit-standards.md`)
- Unity 6 5대 핵심 감사 차원 정의:
  1. **Code Conciseness**: 최소 수정 원칙, 불필요한 래퍼/과설계 배제, C# 9.0+ 관용구.
  2. **Architecture & Existing Code Reuse**: 기존 Manager, ScriptableObject, EventChannel 재사용, Data SSOT.
  3. **Scoped Extensibility & YAGNI**: 스펙 맞춤 설계, 불필요한 제네릭 팩토리 배제.
  4. **Unity 6 Hot-Path GC & Memory Optimization**: `Update` 내 LINQ, `new`, string interpolation, Boxing 금지, NonAlloc Physics API, Object Pooling.
  5. **Lifecycle, Meta Safety & Testability**: POCO 도메인 분리, `OnDisable`/`OnDestroy` 이벤트 구독 해제, `.meta` 파일 무결성.

### ③ 거버넌스 계층 (`AGENTS.md`)
- FC 프로젝트의 명확한 구조를 차용하여 루트에 신설:
  - 1. Project Vision & "Headless-First" Architecture (POCO vs MonoBehaviour)
  - 2. Authority & Separation of Concerns (4-Tier 모델)
  - 3. Modular Pipeline & Verification Protocols (Fast Track, Audit, Static, Tests)
  - 4. Modular Domain Rules 연계
  - 5. Minimal Modifications & Structural Preservation

### ④ Skill 계층 (`.agents/skills/interrogate/SKILL.md`)
- `/interrogate` 또는 "검사해줘", "코드 리뷰해줘" 프롬프트 시 자동 구동.
- `git add -N .` 및 C# diff 필터링(`git diff HEAD -- "*.cs"`).
- 온디맨드 자동 티어링: 일반 기본값은 `Model: inherit`, 사용자가 "자동 티어링" 요청 시 diff 규모에 따라 `flash` / `pro` 자동 분기.
- Closed-Loop Remediation: BLOCKING 결함 감지 시 메인 에이전트가 자동 수정 후 테스트 러너 검증.

### ⑤ CLI 파이프라인 계층 (`Tools/RunTests.py`)
- `--mode test` (기본값): 기존 Unity batchmode Headless 테스트 100% 보존.
- `--mode audit`: Git diff 패키징, `.meta` 누락 검사, 핫패스 GC 할당 정적 린터 구동 (SubAgent 전달용 패키지).
- `--mode static`: 프로젝트 전체 C# 스크립트 정적 스캔.
- `--json`: JSON 구조화 출력 지원.

---

## 3. 실전 정적 검증 결과

`python Tools/RunTests.py --mode static` 실행 결과, 195개 C# 파일 중 상태 머신 내 실시간 인스턴스화 결함 7건을 정확하게 포착함을 실증했습니다:
- `EnemyReturnToSplineState.cs`: Update() 내부 `new EnemyPathFollowState` 동적 할당 탐지
- `EnemyStunState.cs`: Update() 내부 `new EnemyReturnToSplineState` 동적 할당 탐지
- 이처럼 서브에이전트 감사 파이프라인이 유니티의 치명적인 GC 문제를 즉각적으로 잡아낼 수 있습니다.
