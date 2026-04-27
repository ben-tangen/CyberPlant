# Team Tasks (Dependency View)

## Core Foundation
| ID | Task | Owner | Status | Blocked By | Unblocks |
|---|---|---|---|---|---|
| T01 | Project structure + starter scenes/scripts | Peyton | Done | None | T02, T03, T04, T05, T06 |
| T02 | Player movement + collision | Peyton | Done | T01 | T03, T04, T07 |
| T03 | Combat basics (hit detection + damage) | Ben | Done | T01, T02 | T04, T08, T12 |
| T04 | One enemy working (base enemy + attack) | Judah | Done | T02, T03 | T05, T09, T12 |
| T05 | One test level playable | Shared | Done | T02, T04 | T06, T10 |
| T06 | UI basics (health + water HUD) | Peyton | Done | T01, T02, T05 | T10, T11 |

## Gameplay Expansion
| ID | Task | Owner | Status | Blocked By | Unblocks |
|---|---|---|---|---|---|
| T07 | Movement/combat feel polish | Ben | Not Started | T02, T03 | T13 |
| T08 | Weapon system (switching + behaviors) | Ben | Not Started | T03 | T11, T12, T13 |
| T09 | Enemy variations (3-5 types) | Judah | Done | T04 | T12, T13 |
| T10 | Main menu + level flow wiring | Peyton | Done | T05, T06 | T14 |
| T11 | Water currency loop (pickup -> manager -> UI) | Peyton | Done | T06, T08 | T14 |
| T12 | Boss prototype | Judah | Not Started | T03, T04, T08, T09 | T13 |

## Production
| ID | Task | Owner | Status | Blocked By | Unblocks |
|---|---|---|---|---|---|
| T13 | Balance + game-feel pass | Shared | Not Started | T07, T08, T09, T12 | T14 |
| T14 | Final QA + bug fixing + submission prep | Shared | Not Started | T10, T11, T13 | None |

## Blocking Highlights
- Ben is now mainly blocked by `T08` follow-through and later polish work rather than baseline systems.
- Judah has the base enemy architecture available, so the next major work is `T09` enemy variations and `T12` boss prototyping.
- Peyton's baseline UI and menu flow are in place, so the remaining shared work is content, balancing, and final integration.
