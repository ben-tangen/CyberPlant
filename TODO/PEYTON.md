# Peyton TODOs

| ID | Task | Status | Blocked By | Unblocks | Notes |
|---|---|---|---|---|---|
| P1 | Build project structure + starter scripts | Done | None | P2, P3, B1, J1 | Script namespaces, scene paths, and autoload structure are in place. |
| P2 | Player movement + collision baseline support | Done | P1 | B1, J1, P3 | Walk, jump, gravity, and baseline collision are implemented. |
| P3 | HUD basics (health + water) | Done | P1, P2 | P4, P5 | HUD is wired to player health and GameManager water updates. |
| P4 | Main menu + start/quit flow | Done | P3, T05 | T14 | Main menu loads Level01 and supports quit flow. |
| P5 | Water currency UI integration | Done | P3, B3 | T14 | Water pickups/enemy rewards feed GameManager and update HUD correctly. |

## External blockers for Peyton
- Major UI blockers are cleared; remaining work depends on shared content, balancing, and any future weapon/resource expansion.
