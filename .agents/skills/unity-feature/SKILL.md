---
name: unity-feature
description: Implement or design Unity gameplay features, systems, synergies, items, combat behavior, game flow, data, or UI integration in this repository. Use for 신규 기능, 기능 구현, 시스템 추가, 시너지 구현, gameplay feature work. Do not use for pure compiler-error diagnosis or branch conflict resolution.
---

# Unity Feature Workflow

## Objective

Implement the requested feature with a minimal, modular, behavior-preserving change that follows the repository's Unity and architecture rules.

## Workflow

### 1. Freeze behavior

Extract:

- goal,
- observable result,
- non-goals,
- trigger/input,
- authoritative runtime state,
- reset/stack semantics,
- presentation requirements,
- designer-tunable values.

If a correctness-critical gameplay decision is absent and cannot be derived from supplied specs or existing patterns, return a numbered question list before implementation.

### 2. Map the repository surface

Read `ARCHITECTURE.md` and inspect:

- target domain folder,
- nearest controller/orchestrator,
- runtime state/result/context types,
- event publishers/subscribers,
- relevant SO definitions,
- relevant Presenter/View/Director types,
- all callers of any public API to be modified.

Do not start by writing a new manager.

### 3. Choose ownership

Write a one-paragraph ownership decision:

```text
<Gameplay runtime/service> owns <domain state/policy>.
<Controller> coordinates <ordering>.
<Event/result> publishes <display-relevant fact>.
<Presenter> maps it to <View>.
```

Check the reverse dependency before adding a cross-domain reference.

### 4. Plan when required

For multi-system work, create `docs/exec-plans/active/<task>.md` from the repository template.

Milestones should usually separate:

1. domain/data contract,
2. runtime behavior,
3. presentation/UI,
4. integration/reset,
5. validation.

Do not proceed past a failed milestone validation.

### 5. Implement narrowly

Rules:

- preserve public/serialized compatibility,
- prefer focused collaborators over growing large controllers,
- prefer state over flag clusters,
- use events/results/contexts for boundaries,
- use `Reset()` for inspector auto-wiring,
- add SO Tooltips,
- keep Views domain-agnostic,
- reuse buffers in hot paths,
- use DOTween in presentation components.

### 6. Validate each milestone

Run:

```bash
python scripts/harness/verify.py
```

When the full Unity project/editor is available:

```bash
python scripts/harness/run_unity_compile_check.py
```

Define and execute a play-mode smoke path when the environment supports it. Otherwise write the exact smoke path and report it as not run.

### 7. Self-review

Use `docs/engineering/CODE_REVIEW.md`.

Specifically inspect:

- event subscription pairs,
- reset/teardown,
- stale tween/coroutine callbacks,
- serialized changes,
- hot-path allocations,
- accidental cross-domain cycles,
- unrelated diff expansion.

### 8. Report

Final report sections:

```text
Implemented
- ...

Architecture
- owner/boundary summary

Validation
- harness
- Unity compile
- play-mode
- diff review

Setup / scene application
- only concrete inspector or hierarchy steps required

Known limitations
- only unvalidated or intentionally deferred facts
```
