# Conditional Test Harness & Verification Loop

## 1. Execution Modes

### Mode A: Fast Direct Edit (Default - Token Saver)
- **Trigger**: Standard requests without explicit verification tags.
- **Behavior**: Write/modify C# code directly and finish immediately. **Do NOT run `Tools/RunTests.py` or Unity batchmode.**
- **Output**: Present the code diff or implementation summary concisely.

### Mode B: Strict Verification Loop (Opt-In)
- **Trigger**: The user prompt explicitly includes tags such as `[Verify]`, `[Test]`, `[TDD]`, or explicitly requests test verification.
- **Behavior**:
  1. Modify or create target C# scripts and corresponding tests in `Assets/Tests/EditMode/`.
  2. Execute `python Tools/RunTests.py --platform EditMode` via terminal.
  3. Parse results from `TestResults/EditMode-results.xml` and `TestResults/unity-test.log`.

## 2. Autonomous Self-Refining Policy (Mode B Only)
- **Failure Diagnostics**: If tests fail or compilation errors occur, extract the script file, line number, and stack trace.
- **Iteration Limit**: Attempt up to 3 self-refinement iterations to fix errors and re-run tests.
- **Escalation**: If failures persist after 3 attempts, halt execution and report the root cause, remaining errors, and diff to the user.

## 3. Scope Boundary
- Pure C# domain logic, math, state machines, and data structures should target EditMode test coverage when Mode B is invoked.
- Presentation logic (`MonoBehaviour` rendering/UI) remains exempt from headless unit test requirements unless PlayMode is explicitly specified.
