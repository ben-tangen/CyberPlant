# Team Tasks (Dependency View)

## Core Foundation
| ID | Task | Owner | Status | Blocked By | Unblocks |
|---|---|---|---|---|---|
| T01 | Project structure + starter scenes/scripts | Peyton | In Progress | None | T02, T03, T04, T05, T06 |
| T02 | Player movement + collision | Peyton | In Progress | T01 | T03, T04, T07 |
| T03 | Combat basics (hit detection + damage) | Ben | Not Started | T01, T02 | T04, T08, T12 |
| T04 | One enemy working (base enemy + attack) | Judah | Not Started | T02, T03 | T05, T09, T12 |
| T05 | One test level playable | Shared | Not Started | T02, T04 | T06, T10 |
| T06 | UI basics (health + water HUD) | Peyton | In Progress | T01, T02, T05 | T10, T11 |

## Gameplay Expansion
| ID | Task | Owner | Status | Blocked By | Unblocks |
|---|---|---|---|---|---|
| T07 | Movement/combat feel polish | Ben | Not Started | T02, T03 | T13 |
| T08 | Weapon system (switching + behaviors) | Ben | Not Started | T03 | T11, T12, T13 |
| T09 | Enemy variations (3-5 types) | Judah | Not Started | T04 | T12, T13 |
| T10 | Main menu + level flow wiring | Peyton | In Progress | T05, T06 | T14 |
| T11 | Water currency loop (pickup -> manager -> UI) | Peyton | Not Started | T06, T08 | T14 |
| T12 | Boss prototype | Judah | Not Started | T03, T04, T08, T09 | T13 |

## Production
| ID | Task | Owner | Status | Blocked By | Unblocks |
|---|---|---|---|---|---|
| T13 | Balance + game-feel pass | Shared | Not Started | T07, T08, T09, T12 | T14 |
| T14 | Final QA + bug fixing + submission prep | Shared | Not Started | T10, T11, T13 | None |

## Blocking Highlights
- Ben is mainly blocked by `T02` (player movement) and `T03` (combat baseline).
- Judah is mainly blocked by `T03` (damage system) and `T04` (first enemy architecture).
- Peyton's UI/menu work is blocked by `T05` once base HUD is in place, because final menu flow depends on a playable test level.
