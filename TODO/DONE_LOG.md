# Completed Tasks Log

Use this file to record completed work and what it unblocked.

## Entry Format
| Date | Task ID | Task | Owner | What Was Completed | Unblocked Tasks | Notes |
|---|---|---|---|---|---|---|
| YYYY-MM-DD | T## / P# / B# / J# | Short task name | Name | 1-2 sentence summary of what shipped | Comma-separated IDs | Optional links/PR/commit |

## Completed Entries
| Date | Task ID | Task | Owner | What Was Completed | Unblocked Tasks | Notes |
|---|---|---|---|---|---|---|
| 2026-04-02 | T01 | Project structure + starter scenes/scripts | Peyton | Created baseline scripts and TODO dependency docs. | T02, T03, T04, T05, T06 | Initial foundation pass |
| 2026-04-02 | T02 / P2 / P3 | Player movement + HUD baseline | Peyton | Implemented player movement (walk, jump, gravity), created Level01 test scene with ground collision, and connected HUD to GameManager and Player via signals for health and water display. Fixed C# build issues and established working scene structure. | T03, T04, T05, T06 | Established playable baseline and unblocked combat and enemy development |
| 2026-04-08 | P4 / T10 | Main menu + scene flow | Peyton | Implemented MainMenu scene with pixel-art background, overlay, title, and styled buttons. Wired Start and Quit buttons to load Level01 and exit the game. Established UI layout using Control nodes and containers for scalability. | T14 | Provides full game entry point and completes menu flow baseline |