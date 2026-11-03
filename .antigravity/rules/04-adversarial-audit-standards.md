# 04. Adversarial Code Audit & Optimization Standards (Unity 6 / C#)

This document establishes the official review rubric used by the **`adversarial-auditor`** subagent and code review workflows (`/interrogate`) in the **PenguinShot** project.

All proposed C# code changes, refactorings, and architectural modifications are rigorously evaluated against the following **5 Core Dimensions**:

---

## 1. Code Conciseness (코드 간결성 & 최소 수정 원칙)

- **Minimal Surface Area (최소 수정 원칙)**:
  - Strive for the smallest clean functional diff required to fulfill the feature or fix the bug.
  - Do not touch, reformat, or alter unrelated files or functions.
- **No Premature Over-Engineering (과설계 배제)**:
  - Reject single-use wrapper classes, empty helper layers, premature generic factories, or pass-through abstractions.
  - Prioritize direct, readable code over speculative architectural complexity.
- **Idiomatic Modern C# (C# 9.0+ / .NET Standard 2.1)**:
  - Favor clean pattern matching (`switch` expressions, type patterns), null-coalescing (`??`, `??=`), expression-bodied members, and modern collections.
  - Avoid archaic verbose patterns unless strictly required for engine serialization.

---

## 2. Reuse of Existing Architecture & Code (기존 구조 및 코드 재사용)

- **Leverage Existing Assets & Managers**:
  - Before writing a new manager, subsystem, or utility, verify whether an existing system in `Assets/Game/`, `Assets/Manager/`, `Assets/Data/`, or `Assets/Effect/` already handles it (e.g. `InGameManager`, `WaveManager`, `BenchManager`, `StatModifierRegistry`, `EffectBase`).
- **No Parallel Workflows (중복 구조 배제)**:
  - Prohibit duplicate event dispatching pipelines, redundant stat calculations, or secondary state containers when an established pattern already exists.
- **Data Asset Single Source of Truth (SSOT)**:
  - All gameplay configurations, stats, ability parameters, and wave definitions must strictly originate from **Engine Assets** (`ScriptableObject`, `DataAsset`).
  - Hardcoded magic numbers and direct filesystem I/O (`File.ReadAllText`) at runtime are forbidden.

---

## 3. Scoped Extensibility & YAGNI (범위 내 적정 확장성)

- **Fit-to-Scope Design (스펙 맞춤 설계)**:
  - Extensibility must strictly match the active feature requirement.
  - New interfaces or virtual methods are warranted only if multiple implementations or automated test mocks are explicitly required.
- **YAGNI (You Aren't Gonna Need It) Enforcement**:
  - Reject speculative abstractions designed for hypothetical future game mechanics.
- **Clean Architectural Boundaries**:
  - Decouple mathematical calculations and state transition logic from visual/presentation logic.

---

## 4. Unity 6 & Hot-Path GC/Memory Optimization (유니티 6 핫패스 GC 및 메모리 최적화)

- **Zero Per-Frame Heap Allocation**:
  - In hot paths (`Update()`, `FixedUpdate()`, `LateUpdate()`, collision/trigger callbacks, tight loops):
    - **NO LINQ**: Prohibit `.Where()`, `.Select()`, `.ToList()`, `.Any()`, `foreach` over interfaces that cause enumerator heap allocation.
    - **NO Per-Frame `new`**: Reuse collections via `.Clear()`, avoid instantiating objects or lambda closures inside update loops.
    - **NO String Formatting**: Prohibit string concatenation or interpolation (`$"Score: {score}"`) per frame. Use cached text buffers or dirty-flag updates.
    - **NO Boxing**: Prevent value-type boxing when passing enums, structs, or primitives to generic objects or messaging channels.
- **Component & Transform Caching**:
  - Prohibit `GetComponent<T>()`, `FindObjectOfType`, `GameObject.Find` inside per-frame updates. Cache all references in `Awake()` or `OnEnable()`.
- **Physics NonAlloc Queries**:
  - Mandatory use of NonAlloc physics methods (`Physics2D.OverlapCircleNonAlloc`, `Physics2D.RaycastNonAlloc`, `ContactFilter2D`) with preallocated static/member array buffers.
- **Object Pooling**:
  - Entities that spawn and despawn frequently (coins, bullets, projectiles, floating combat text, visual effects) MUST be managed via an Object Pool rather than continuous `Instantiate()` and `Destroy()`.
- **Modern Unity 6 Async & Coroutines**:
  - Utilize Unity 6 `Awaitable` where applicable. If using Coroutines, cache `YieldInstruction` instances (e.g., `WaitForSeconds`) to eliminate garbage generation.

---

## 5. Lifecycle, Meta Safety & Testability (엔진 라이프사이클, 메타 무결성 및 테스트성)

- **Domain Logic Isolation & Headless Testability**:
  - Business logic, math, ability triggers, and data transformations should be implemented as pure C# POCO classes or decoupled interfaces.
  - This guarantees fast, headless EditMode unit test execution (<3s) via `python Tools/RunTests.py --platform EditMode`.
- **Event Subscription Hygiene (메모리 누수 방지)**:
  - Every subscription to a C# `event`, delegate, or UnityEvent MUST be explicitly unsubscribed in `OnDisable()` or `OnDestroy()`.
  - Never leave dangling listeners that retain references to destroyed objects.
- **Git & Meta File Integrity**:
  - Whenever a new `.cs`, `.asmdef`, or asset file is created or renamed, the corresponding `.meta` file must be created and tracked.
  - Never manually modify complex YAML `.unity` scenes or `.prefab` files without explicit instruction.
- **Assembly Definition Boundaries**:
  - Ensure code resides in the proper assembly definition (`PenguinShot.Runtime`, `PenguinShot.Editor`, `Tests.EditMode`, `Tests.PlayMode`) to maintain fast compilation times.

---

## 6. Review Verdict Standards

The auditor classifies all findings and delivers an authoritative verdict:

| Verdict | Condition | Action Required |
| :--- | :--- | :--- |
| **`APPROVED (LGTM)`** | 0 BLOCKING issues, code adheres to all 5 dimensions (Audit score >= 8.5/10) | Proceed with commit or testing |
| **`CHANGES REQUESTED (BLOCKING)`** | >= 1 BLOCKING issue detected (GC in Update, memory leak, missing .meta, arch violation) | Main agent must autonomously remediate all BLOCKING items before commit |
