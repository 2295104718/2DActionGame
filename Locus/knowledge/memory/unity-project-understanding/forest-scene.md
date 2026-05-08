---
id: kd_64333276-7466-4f2a-afe9-ea9eef9291fe
type: memory
path: unity-project-understanding/forest-scene.md
title: forest-scene
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1778256201908
updatedAt: 1778256201914
---

# forest-scene

## Summary
Structural lookup notes for `Assets/Scenes/Forest.unity`, including tilemap layer responsibilities and current dressing roots.

<!-- locus:body:start -->
- `Assets/Scenes/Forest.unity` uses a `Grid` root with tilemap layers `Back1`, `Back2`, `Back3`, `Platform`, `Front1`, `Front2`, `Front3`, and `Spike`.
- `Platform` uses `Assets/Tilemap/Tiles/Rule Tiles/Forest2.asset` as the main ground rule tile and carries the walkable collider setup (`TilemapCollider2D` + `CompositeCollider2D`).
- `Spike` is a separate hazard tilemap with `Attack` and trigger composite collider.
- Forest scene dressing currently includes a `Background` root for the sky sprite plus a `BackgroundDecor` root containing tree `SpriteRenderer` objects for layered depth.
- Forest interactive objects are placed as top-level roots: `Chest1`, `Chest2`, `Teleport`, and a `Snail` prefab instance.
<!-- locus:body:end -->
