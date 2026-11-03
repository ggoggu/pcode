# AGENTS.md - Lead Systems & Gameplay Architect Directives (Unity 6)

Welcome to the **PenguinShot** project repository. This document defines the architectural vision, tier separation, coding standards, and autonomous loop engineering protocols governing all development in **Unity 6 (6000.3.19f1)**.

---

## 1. Project Vision & "Headless-First" Architecture

PenguinShot is a pinball/slingshot-driven roguelike wave defense game built on **Unity 6 (6000.3.19f1)** with **C# (.NET Standard 2.1 / C# 9.0+)**.

### Architectural Tenet: Decoupled Domain Logic (Headless First)
- All core gameplay rules, stat modifications, bench slot management, ability calculations, and wave logic are implemented as **Pure C# POCO (Plain Old C# Objects) and interfaces**, completely decoupled from `UnityEngine.Object` and `MonoBehaviour`.
- Presentation components (`MonoBehaviour`, Transforms, Physics2D, Particle Systems, uGUI) act strictly as **reactive observers and view controllers**.
- This separation guarantees **100% headless testability**: critical gameplay rules can be verified via headless EditMode unit tests in less than 3 seconds without loading scenes, initializing rendering loops, or requiring GUI windows.

```
+-----------------------------------------------------------------------------------+
|                           DOMAIN LOGIC TIER (Pure C# / POCO)                     |
|  - Domain Rules, Ability Calculators, Stat Modifier Pipeline, Wave Logic          |
|  - Headless-First: Sub-second EditMode Unit Tests (<3s) without Scene or GUI      |
|  - Zero per-frame GC allocations (No LINQ, No 'new', Object Pooling)              |
+----------------------------------------+------------------------------------------+
                                         |
                       Events / State    |  Player Intent / Commands
                         Notifications   |  (Decoupled Interfaces)
                                         v
+-----------------------------------------------------------------------------------+
|                         PRESENTATION TIER (Unity MonoBehaviour)                   |
|  - View Controllers, Slingshot Mechanics, Pinball Physics2D Rigidbody             |
|  - Visual Effects (Particle Systems, Animations), Audio Feedback                  |
|  - Reactive UI (uGUI / TextMesh Pro), Bench Slot View Drag & Drop                 |
+----------------------------------------+------------------------------------------+
                                         |
                       Data Injection    |  Asset References
                    (ScriptableObjects)  |  (Single Source of Truth)
                                         v
+-----------------------------------------------------------------------------------+
|                           DATA & ASSET TIER (Engine SSOT)                         |
|  - ScriptableObjects (PenguinDefinition, AbilitySO, WaveDefinition, EffectConfig) |
|  - Prefabs, Materials, Sprite Atlases, Audio Clips                                |
|  - Strict prohibition of runtime loose file I/O (File.ReadAllText/CSV)            |
+-----------------------------------------------------------------------------------+
```

---

## 2. Authority & Separation of Concerns

All code and assets must adhere strictly to the following four-tier separation:

### 1. Pure C# Domain Tier (Business Logic & State)
- **Path**: `Assets/Game/Scripts/` (Core logic), `Assets/Effect/Scripts/`
- **Classes**: Pure C# classes, POCOs, state machines, modifier registries (`StatModifierRegistry`, `EffectBase`).
- **Responsibilities**:
  - Ability trigger conditions, stat buff stacking, damage calculations, coin economy.
  - Bench inventory placement rules, synthesis/merging algorithms.
- **Rule**: Must not inherit from `MonoBehaviour` unless scene lifecycle integration is strictly mandatory. Must have zero per-frame heap allocations.

### 2. Presentation & View Tier (Unity Scene & Components)
- **Path**: `Assets/Game/`, `Assets/UI/`, `Assets/Effect/`
- **Classes**: `MonoBehaviour` subclasses, `PenguinController`, `SlingshotView`, UI View controllers.
- **Responsibilities**:
  - Handling user input and physics drag-and-drop.
  - Spawning pooled visual/audio effects.
  - Rendering UI widgets, health bars, and coin animations.
- **Strict Rule**: Never mutate core domain state directly through bypassing interfaces. Event listeners subscribed in `OnEnable()` MUST be unsubscribed in `OnDisable()`/`OnDestroy()`.

### 3. Data Tier (Single Source of Truth - SSOT)
- **Classes**: `ScriptableObject` derivatives (`PenguinDefinitionSO`, `AbilitySO`).
- **Responsibilities**:
  - Baseline gameplay stats, projectile speeds, shop item costs, wave tables.
- **Strict Rule**: Gameplay balance data must originate from ScriptableObjects. Direct runtime loose file I/O (`File.ReadAllText` on raw CSV/JSON) is prohibited.

### 4. Test Harness Tier (Headless Verification)
- **Path**: `Assets/Tests/EditMode/`, `Assets/Tests/PlayMode/`
- **Responsibilities**:
  - Fast regression protection for math, state machines, inventory, and abilities.
  - EditMode tests must run cleanly via `python Tools/RunTests.py --platform EditMode`.

---

## 3. Modular Pipeline & Verification Protocols

To maintain high development velocity and optimize AI token consumption, the project utilizes a **Tiered On-Demand Test & Audit Runner** (`Tools/RunTests.py`).

### Autonomous Execution Modes:

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│                              PIPELINE EXECUTION MODES                                │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ 1. [Default] Fast Mode        : Direct C# coding & Q&A (Zero CLI overhead)           │
│ 2. Audit Mode (`--mode audit`): Git diff staging, .meta check & GC scan (~0.05s)     │
│ 3. Static Mode (`--mode static): Full project-wide C# static analysis (~0.2s)        │
│ 4. SubAgent Audit (`/interrogate`): Read-only adversarial review across 5 dimensions │
│ 5. Headless Tests (`--platform EditMode`): Fast Unity batchmode unit tests           │
│ 6. Full Release (`--platform PlayMode`): Complete integration & frame test harness   │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

### Protocol Guidelines:

1. **Default Mode (Fast Track)**:
   - For regular code edits, refactorings, or explanations, do **not** run test batchmode automatically. Provide clean C# code and explanations directly.

2. **On-Demand Verification (Single Tool Call)**:
   - When verification or review is requested:
     - **Diff & GC Audit**: `python Tools/RunTests.py --mode audit` (or append `--json`)
     - **Full Static Scan**: `python Tools/RunTests.py --mode static`
     - **Headless Unit Tests**: `python Tools/RunTests.py --platform EditMode`
     - **Adversarial Subagent Review**: Trigger `/interrogate` (invoking `adversarial-auditor`)

3. **Compact Reporting & Closed-Loop Remediation**:
   - If tests fail or the auditor returns `CHANGES REQUESTED (BLOCKING)`, the main agent autonomously resolves all blocking items before asking the user to commit.

---

## 4. Modular Domain Rules (.antigravity/rules/)

Feature-specific standards are modularized in `.antigravity/rules/` and applied on-demand:

- **Unity 6 Engineering Standards**: Refer to `.antigravity/rules/01-unity-standards.md` (Modern APIs, GC allocation control, serialization safety).
- **Test Harness Protocol**: Refer to `.antigravity/rules/02-test-harness.md` (Fast edit vs strict test loop, self-refining policy).
- **Git & Meta File Safety**: Refer to `.antigravity/rules/03-git-meta-safety.md` (Mandatory `.meta` tracking, ignore guard, conventional commits).
- **Adversarial Subagent Audit (`/interrogate`)**: Refer to `.agents/skills/interrogate/SKILL.md` and `.antigravity/rules/04-adversarial-audit-standards.md` (Read-only subagent review across 5 core dimensions).

---

## 5. Minimal Modifications & Structural Preservation

All autonomous agents and developers must strictly adhere to the principle of least disruption:

1. **Minimal Surface Area Changes (최소한의 수정)**:
   - Apply only the strictly necessary modifications required to solve the task.
   - Do not refactor, rewrite, or alter unrelated files or functions.
   - Strive for the smallest clean functional diff.

2. **Maximize Reuse of Existing Architecture & Code (기존 구조 및 코드 재사용)**:
   - Thoroughly investigate existing Managers (`InGameManager`, `WaveManager`, `BenchManager`), ScriptableObjects, and Effect classes before introducing new abstractions.
   - Maintain consistency with existing project naming conventions and folder structures.

3. **Ground Truth Over Guessing (에셋 추측 금지 & 메타 무결성)**:
   - Never guess component bindings or inspector references; inspect existing prefab/SO structures when needed.
   - Every newly created `.cs` file MUST have a corresponding `.meta` file tracked.
