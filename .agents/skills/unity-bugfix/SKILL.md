---
name: unity-bugfix
description: Diagnose and fix Unity C# compiler errors, runtime exceptions, incorrect behavior, regressions, DOTween callback/type issues, lifecycle bugs, and event bugs in this repository. Use for 에러 원인, 버그 수정, regression, CSxxxx, NullReferenceException, incorrect behavior. Do not use for merge-conflict resolution unless the bug is already isolated after the merge.
---

# Unity Bug-Fix Workflow

## Objective

Identify the exact broken invariant and apply the smallest patch that restores it without damaging neighboring behavior.

## 1. Start from evidence

Collect the strongest available evidence:

- complete compiler error and line,
- stack trace,
- Unity console message,
- reproduction steps,
- before/after behavior,
- relevant diff or merge context.

Do not redesign the system before locating the failing path.

## 2. State the invariant

Write:

```text
Expected invariant: <what must always be true>.
Observed break: <where/how it becomes false>.
```

Examples:

- a delegate passed to DOTween must match the installed API's callback type/signature,
- a presenter must subscribe exactly once per enabled lifetime,
- battle reset must clear all state from the previous battle,
- a serialized reference must not be assumed populated by `Reset()` at runtime.

## 3. Trace the failing path

Inspect:

- exact file/line,
- enclosing method and lifecycle,
- target API/type signature,
- nearby working usage of the same API,
- callers,
- event subscribers,
- reset/disable path.

Search the repository for the same pattern before inventing a fix.

## 4. Rank root causes

Prefer a causal explanation over symptoms.

For each plausible cause, ask whether it explains all evidence. Reject causes that do not explain the exact error/path.

## 5. Patch narrowly

A safe bug patch:

- changes the fewest contracts necessary,
- does not rename serialized data,
- does not refactor unrelated methods,
- preserves event timing unless event timing is the bug,
- preserves tween semantics unless tween semantics are the bug,
- preserves hot-path buffer reuse.

If the bug comes from an invalid state model, a focused enum/state correction may be safer than adding another boolean.

## 6. Check neighboring regressions

Identify at least two neighboring paths that could regress and inspect them.

Examples:

- success/no-target paths,
- first trigger/repeated trigger,
- battle start/battle end,
- enable/disable/re-enable,
- null optional presenter/reference,
- normal speed/changed battle speed.

## 7. Validate

Run:

```bash
python scripts/harness/verify.py
```

Then run Unity compile when available:

```bash
python scripts/harness/run_unity_compile_check.py
```

For compiler errors, do not claim resolution without a compiler run when the editor environment is available.

## 8. Report

Use:

```text
Root cause
- exact type/API/state mismatch

Fix
- exact code-path correction

Why this is safe
- preserved neighboring behavior/contracts

Validation
- commands/tests actually run

Remaining limitation
- only what could not be executed or observed
```
