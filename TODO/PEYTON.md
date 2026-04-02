# Peyton TODOs

| ID | Task | Status | Blocked By | Unblocks | Notes |
|---|---|---|---|---|---|
| P1 | Build project structure + starter scripts | In Progress | None | P2, P3, B1, J1 | Keep script namespaces and scene paths consistent. |
| P2 | Player movement + collision baseline support | In Progress | P1 | B1, J1, P3 | Leave extension points for Ben's polish work. |
| P3 | HUD basics (health + water) | In Progress | P1, P2 | P4, P5 | Wire with signals/autoload, avoid hard coupling. |
| P4 | Main menu + start/quit flow | In Progress | P3, T05 | T14 | Start button should load the first level scene. |
| P5 | Water currency UI integration | Not Started | P3, B3 | T14 | Depend on weapon/enemy reward loop from Ben/Judah. |

## External blockers for Peyton
- `T05` Test level playable (shared)
- `B3` Weapon/resource reward behavior finalized (Ben)
