---
name: interrogate
description: >-
  Trigger an adversarial code review and architecture audit using an independent read-only subagent.
  Use this skill whenever the user asks for code review, adversarial review, code audit,
  inspection before committing, checking Unity GC/memory optimizations, or verifying conciseness and architecture reuse.
---

# /interrogate - Adversarial Code & Architecture Audit (Unity 6 / C#)

This skill invokes an independent, read-only subagent (`adversarial-auditor`) to ruthlessly critique recent C# code changes against the **PenguinShot** project's 5 Core Engineering Dimensions before committing.

## When to Run

- After implementing a new gameplay feature, ability, effect, or manager.
- Before committing or merging code (`python Tools/RunTests.py`).
- Whenever the user prompts:
  - `"검사해줘"` / `"/interrogate"`
  - `"코드 리뷰해줘"` / `"적대적 코드 심사해줘"`
  - `"최적화 검사해줘"` / `"GC 할당 검사해줘"`
  - `"다른 모델(서브에이전트)에 확인받아줘"`

---

## The 5 Core Audit Dimensions

1. **Code Conciseness (코드 간결성 & 최소 수정 원칙)**
   - Minimal surface area changes; no superfluous boilerplate or premature over-engineering. Clean modern C# 9.0+.
2. **Reuse of Existing Architecture & Code (기존 구조 및 코드 재사용)**
   - Strict utilization of existing Managers, ScriptableObjects, and Effect/Stat systems in `Assets/`.
3. **Scoped Extensibility (범위 내 적정 확장성)**
   - Clean, modular design sized for the specified feature requirements without speculative YAGNI violations.
4. **Unity 6 Hot-Path GC & Memory Optimization (유니티 핫패스 GC 및 메모리 최적화)**
   - Zero per-frame heap allocations (No LINQ, No `new`, No string formatting, No boxing in `Update`), NonAlloc physics, Object Pooling.
5. **Lifecycle, Meta Safety & Testability (라이프사이클, 메타 무결성 및 헤드리스 테스트성)**
   - Event unsubscribe hygiene in `OnDisable`/`OnDestroy`, `.meta` integrity, pure C# POCO domain logic for sub-second EditMode tests.

---

## Execution Workflow

### Step 1: Collect Context & State Intent (Filtering & Staging)
1. Run `git add -N .` to ensure newly created untracked C# files and `.meta` files are recognized by git diff.
2. Extract the text-only C# diff:
   ```powershell
   git diff HEAD -- "*.cs"
   ```
   *(Or run `python Tools/RunTests.py --mode audit` to get an aggregated diff, static GC check, and .meta status).*
3. State the explicit feature intent in 1-2 paragraphs (what the code is supposed to do and why).

### Step 2: Invoke the Adversarial Auditor Subagent
- **기본 모드 (Default)**: 항상 `Model: inherit` (일반적인 `/interrogate` 또는 "코드 검사해줘" 요청 시 기본 모델을 그대로 상속).
- **자동 티어링 모드 (On-Demand Auto-Tiering)**:
  사용자가 **"자동 티어링 적용해줘"**, **"규모에 맞춰 알아서 모델 골라 검사해줘"** 와 같이 자동 티어링을 명시적으로 요청한 경우에만 diff를 분석하여 최적 모델을 자동 선택:
  - 경량 로컬 로직 (<150 diff lines, 단일 컴포넌트 경미한 수정): `Model: flash` 자동 할당
  - 핵심 아키텍처/매니저/복합 로직 (>150 diff lines, Manager, State Machine, Domain POCO): `Model: pro` 자동 할당
  - 사용자의 자동 티어링 요청이 없는 기본 검사에서는 임의로 분기하지 않고 항상 `Model: inherit` 유지.

Invoke the defined `adversarial-auditor` subagent using `invoke_subagent`:
- **TypeName**: `adversarial-auditor`
- **Role**: `Adversarial Unity Code & Architecture Auditor`
- **Model**: `inherit` (사용자의 자동 티어링 명시 요청 시에만 diff에 따라 `pro` 또는 `flash`)
- **Prompt**:
  ```text
  Please perform a rigorous adversarial audit on the following Unity 6 / C# changes.

  ## Stated Intent
  <Brief summary of intent>

  ## Code Changes (Git Diff / Targeted C# Files)
  <Git diff or file contents>

  Audit the code strictly against the 5 dimensions:
  1. Code Conciseness & Minimal Surface Area
  2. Architecture & Existing Code Reuse
  3. Scoped Extensibility & YAGNI
  4. Unity 6 Hot-Path GC & Memory Optimization
  5. Lifecycle, Meta Safety & Testability

  Provide the structured Markdown report with your Verdict (APPROVED (LGTM) or CHANGES REQUESTED (BLOCKING)).
  ```

### Step 3: Triage Findings & Closed-Loop Remediation
- If Verdict is **`CHANGES REQUESTED (BLOCKING)`**:
  - The main agent must **autonomously resolve all `BLOCKING` issues immediately** (e.g., removing per-frame allocations in `Update`, adding missing event unsubscriptions, caching components, creating missing `.meta` files).
  - Review `WARNING` and `SUGGESTION` items for practical value and apply where clean.
  - Re-verify using `python Tools/RunTests.py --mode audit` or `python Tools/RunTests.py --platform EditMode`.
  - Present the audit findings alongside the applied fix comparison to the user.
- If Verdict is **`APPROVED (LGTM)`**:
  - Present the audit summary to the user.
  - Proceed with commit or testing.
