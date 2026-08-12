---
name: unity-safe-merge
description: Resolve Git merge conflicts or semantically merge Unity C# files from dev and a feature branch while preserving behavior from both sides. Use for 충돌 해결, dev 병합, merge conflict, ours/theirs comparison, safe semantic merge. Do not pick one side wholesale unless analysis proves the other side has no required behavior.
---

# Unity Safe Merge Workflow

## Objective

Produce a semantic merge that preserves all non-conflicting behavior from both branches and does not introduce duplicate state, events, or lifecycle work.

## 1. Gather three versions

Prefer:

```text
BASE   = common ancestor
OURS   = target/current branch
THEIRS = incoming branch
```

When BASE is unavailable, state that limitation and reconstruct intent from the two sides plus call sites.

## 2. Inventory branch-specific behavior

Before editing, create a behavior table:

| Area | Ours | Theirs | Merge requirement |
|---|---|---|---|
| fields/state | ... | ... | preserve/combine |
| events | ... | ... | preserve/combine |
| lifecycle | ... | ... | deduplicate/order |
| public API | ... | ... | preserve callers |
| runtime logic | ... | ... | semantic merge |
| presentation | ... | ... | preserve ownership |

Do not compare only lines. Compare behavior.

## 3. Trace branch-added APIs

Search all call sites for methods, properties, events, and types added by either side.

Any API with live callers is part of the merge contract unless the task explicitly removes it.

## 4. Merge by responsibility

For each conflicted region:

1. Identify the responsibility.
2. Preserve each side's independent behavior.
3. Reconcile ordering where both modify the same lifecycle or state transition.
4. Deduplicate repeated subscriptions, resets, buffers, and callbacks.
5. Avoid creating two sources of truth for one state.

Never use “newer timestamp wins” as a merge policy.

## 5. Unity-specific checks

Inspect:

- serialized fields retained from both sides,
- `Reset()` wiring for new references,
- `OnEnable`/`OnDisable` subscription pairs,
- coroutines and tween cancellation,
- battle/run reset state,
- SO field compatibility,
- public members referenced by scenes/editor scripts/other code.

## 6. Compile and review

Run:

```bash
python scripts/harness/verify.py
```

Then, when available:

```bash
python scripts/harness/run_unity_compile_check.py
```

Search for all conflict markers:

```bash
rg "^(<<<<<<<|=======|>>>>>>>)" .
```

Review the merged diff and the callers of every conflicted public API.

## 7. Report preservation explicitly

Use:

```text
Preserved from ours
- ...

Preserved from theirs
- ...

Semantic reconciliation
- ordering/state/event decision

Validation
- ...

Unverified Unity asset risk
- scene/prefab/SO wiring not inspected, if applicable
```
