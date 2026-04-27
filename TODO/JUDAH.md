# Judah TODOs

| ID | Task | Status | Blocked By | Unblocks | Notes |
|---|---|---|---|---|---|
| J1 | One enemy working (base enemy + attack) | Done | P2, B1 | J2, T05 | Base enemy scene, AI chase/patrol, attack damage, and death handling exist in the repo. |
| J2 | Enemy variations (3-5) + tuning | Done | J1, B3 | J3, T13 | Build from base enemy scene inheritance. |
| J3 | Boss prototype | Not Started | J2, B3 | T13 | Define phase hooks early to avoid rework. |
| J4 | Structure Design | Not Started | Unblocked | Level Building | Build structures such as walls, floors, ceilings, etc. for level building. |

## External blockers for Judah
- Baseline enemy variation and weapon behavior blockers are cleared; boss prototyping can now start.

## Recent Completed Work
- Added enemy sprite art to `Enemy.tscn` using the existing enemy assets in `assets/enemies`.
- Set up idle/front-facing and walking animations for the base enemy.
- Updated enemy visual logic so it uses the front-facing sprite while idle and flips the walk animation based on movement direction.
