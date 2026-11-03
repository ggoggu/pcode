# Git Integrity, Meta Safety & Checkpoint Policy

## 1. Safety & Meta Synchronization (Mandatory for all modes)
- **Meta Synchronization**: Whenever a new `.cs`, `.asmdef`, or asset file is created or renamed, ensure corresponding `.meta` files are tracked and valid.
- **Clean Workspace**: Run `git status` before finishing tasks to ensure no untracked cache artifacts, orphaned `.meta` files, or unintended `ProjectSettings` modifications exist.
- **Ignore Guard**: Never stage or commit `Library/`, `Temp/`, `Logs/`, `obj/`, or user settings (`.vs/`, `.idea/`).

## 2. Conditional Auto-Commit (Opt-In: `[Commit]` / `[Verify & Commit]`)
- **Trigger**: Run git commit commands ONLY when the user explicitly includes `[Commit]` or `[Verify & Commit]` in the prompt.
- **Commit Workflow**:
  1. Complete code changes and ensure tests pass with exit code 0 (if verification is enabled).
  2. Stage relevant files explicitly (`git add Assets/ Tests/ Tools/`).
  3. Commit using Conventional Commits format (e.g., `feat(inventory): add item split logic with unit tests`).
  4. Report the resulting commit hash and message.

## 3. Failure & Rollback Protocol
- **Abort on Failure**: If tests or builds fail after self-refinement attempts, do NOT commit.
- **Rollback Option**: When instructed or under auto-rollback flags, revert uncommitted working tree changes using `git restore .` and `git clean -fd` for untracked broken files.
