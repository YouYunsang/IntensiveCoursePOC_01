---
name: unity-presentation
description: Design or implement Unity presentation, DOTween animation, VFX feedback, HUD gauges, stack visualization, highlights, popups, and presenter/view integration. Use for 연출, DOTween, 시각화, VFX, 게이지, 하이라이트, popup, animation. Keep gameplay truth outside the View and visual tween state.
---

# Unity Presentation Workflow

## Objective

Create readable, designer-tunable presentation without coupling visual objects directly to raw gameplay data or making tween state authoritative gameplay state.

## 1. Define the visual contract

Extract:

- trigger event,
- value/state to visualize,
- stacking/retrigger behavior,
- blocking vs non-blocking behavior,
- interruption behavior,
- reset/disable behavior,
- designer-tunable parameters,
- required material/prefab/view references.

Ask numbered questions when these choices materially change implementation and are not defined by the spec or existing pattern.

## 2. Choose the boundary

Preferred:

```text
Gameplay runtime
  -> event/snapshot/display fact
  -> Presenter/Director
  -> View / visual objects / DOTween
```

The View receives display-ready values. It does not find or calculate gameplay state.

## 3. Separate gameplay and visual state

Examples:

- Gameplay owns `stackCount`; presentation owns how stack objects animate into place.
- Gameplay owns `percent`; presenter formats/clamps it for display; view renders the label/gauge.
- Gameplay owns “explosion triggered”; visual director owns flash, scale, particle, and camera feedback.

A tween completion callback must not silently become the only place a gameplay effect is applied unless an explicit presentation gate models that dependency.

## 4. DOTween ownership

For each Tween/Sequence define:

- owner field,
- retrigger policy: restart / ignore / overlap / queue,
- kill point,
- stale callback protection,
- whether `SetLink` is appropriate.

Do not create unbounded overlap from a frequently published combat event.

## 5. Inspector usability

Expose designer-tuned presentation values with `[SerializeField]` and clear Tooltips. Use a presentation settings SO when multiple presenters share or centrally tune a coherent effect profile.

Typical tunables:

- duration,
- delay,
- ease,
- offset/distance,
- scale/punch,
- vibrato/elasticity,
- count/spacing,
- threshold,
- text format.

Do not put gameplay balance values into a visual settings asset.

## 6. Performance

For stack, coin, popup, or repeated combat feedback:

- reuse lists/buffers,
- avoid per-frame hierarchy scans,
- avoid repeated material instantiation without a clear ownership strategy,
- consider pooling when objects are frequently spawned/destroyed,
- avoid string formatting in `Update`; update text on value change.

## 7. Validate

Check at minimum:

- first trigger,
- rapid repeated trigger,
- max/empty/zero value,
- disable during tween,
- re-enable and retrigger,
- battle/phase reset,
- missing optional presentation reference,
- no duplicate event subscription.

Run repository checks and Unity compile when available.

## 8. Report scene setup

Provide exact setup steps:

```text
1. Add <Presenter> to <GameObject>.
2. Assign <reference>.
3. Assign <SettingsSO/material>.
4. Configure <important tunables>.
5. Enter Play Mode and trigger <event>.
6. Expected visual result: <concrete behavior>.
```

Do not claim the scene setup is verified when scene/prefab assets were not available.
