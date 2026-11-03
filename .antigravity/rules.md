# Antigravity Agent Operational Rules (Unity 6)

## 1. Automated Verification Loop
- **Mandatory Verification**: Every C# modification must be immediately verified by running `python Tools/RunTests.py` via terminal.
- **Fail-Fast**: Do not consider a task complete until all EditMode tests pass with exit code 0.

## 2. Autonomous Self-Refining Policy
- **Log Diagnostics**: If tests fail or compilation errors occur, parse the stack trace from `TestResults/unity-test.log` or `editmode-results.xml`.
- **Iteration Limit**: Attempt up to 3 self-refinement iterations.
- **Escalation**: If failures persist after 3 attempts, halt execution and report the root cause and diff to the user.

## 3. Unity Best Practices & Code Safety
- **Logic Isolation**: Prioritize plain C# (POCO / interfaces) decoupled from `MonoBehaviour` and Scene dependencies to maximize headless testability.
- **Scene/Prefab Guardrail**: Avoid direct manual edits to binary or complex YAML `.unity` and `.prefab` files unless explicitly requested.
- **Meta Integrity**: Check `git status` before committing to avoid untracked or dangling `.meta` files.
