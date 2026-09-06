# HattrickAI V5 — Technical Manual Source Index

**Document purpose:** This file is the source-of-truth index for the consolidated technical manual and its dated publication snapshots.

## Update / freshness policy

The consolidated PDF is a publication artifact. Its technical content must be traceable to the Markdown documents listed below.

**Base PDF snapshot date:** 2026-09-05  
**Base PDF:** `HattrickAI_V5_Teknik_Manuel_A8_FINAL.pdf`  
**Base PDF page count:** 208

**Stage 9 publication snapshot:** 2026-09-06  
**Stage 9 snapshot:** `HattrickAI_V5_Teknik_Manuel_A9_SNAPSHOT_2026-09-06.pdf`

The Stage 9 snapshot is a dated publication supplement to the 05.09.2026 A8 base manual. It records the Stage 9 Motor Output JSON Database changes without pretending that the older 208-page PDF contains those later changes.

A Markdown file changed after the base snapshot date is not silently treated as represented by the older PDF. Material changes must be captured by a new dated snapshot or by regeneration of the consolidated PDF.

## Source documents

| Document | Scope | Base PDF source | Stage 9 status |
|---|---|---:|---|
| `PROJECT_MEMORY.md` | Project state, decisions and boundaries | 2026-09-05 | Updated 2026-09-06 |
| `ENGINE_MAP.md` | Engine/code map | 2026-09-05 | Unchanged |
| `CHANGE_HISTORY.md` | Technical change history | 2026-09-05 | Updated 2026-09-06 |
| `SYSTEM_ARCHITECTURE.md` | System architecture and runtime flow | 2026-09-05 | Unchanged |
| `DATA_MODEL.md` | Data structures and contracts | 2026-09-05 | Unchanged |
| `MATCH_ENGINE_MATH.md` | Match-engine mathematics/reference | 2026-09-05 | Unchanged |
| `MOTOR_TECHNICAL_MANUAL.md` | M3–M11 technical descriptions | 2026-09-05 | Unchanged |
| `REAL_MATCH_ANALYSIS.md` | Real fixture analysis | 2026-09-05 | Unchanged |
| `WEB_USER_MANUAL.md` | User-facing web manual | 2026-09-05 | Unchanged |
| `WEB_INTERFACE.md` | Web interface technical description | 2026-09-05 | Unchanged |
| `WEB_UI_FILE_MAP.md` | Frontend file/function map | 2026-09-05 | Unchanged |
| `DEVELOPER_API_MANUAL.md` | Backend/API/developer manual | 2026-09-05 | Unchanged |
| `M8_PHASE_D_PDF_CALIBRATION.md` | M8 PDF/calibration-specific notes | 2026-09-05 | Unchanged |
| `MOTOR_OUTPUT_JSON_SCHEMA.md` | Stage 9 JSON snapshot contract | 2026-09-06 | Included in A9 snapshot |

## Stage 9 publication contents

The 06.09.2026 A9 snapshot documents:

1. JSON snapshot schema `hattrickai-v5-motor-database-v1`.
2. `MotorResultArchive` persistence behavior.
3. `MOTOR_DB_PATH`, run JSON and `latest.json` behavior.
4. Motor DB API: latest, list and runId access.
5. Backend analysis-to-archive integration.
6. Motor Panel download behavior using the real latest archive snapshot.
7. C19 archive/schema/latest/list/run lookup regression.
8. C20 deterministic JSON regression; `savedAt` and `runId` are excluded from deterministic payload comparison while remaining validated metadata.
9. The production tactical boundary: Stage 9 does not add a tactic selector and does not relabel `TeamTactic.Normal` as an optimized tactic.

## Revision rule

1. Change the relevant Markdown source first.
2. Record the change in `CHANGE_HISTORY.md` when it is a project-level technical change.
3. Regenerate the consolidated PDF, or create a clearly named dated supplement when the full consolidation is not being regenerated.
4. Update this index with the exact snapshot date and filename.
5. Record the generated publication artifact in README and project change history.

## Important distinction

The individual `.md` files remain the maintainable technical sources. The PDF and dated supplements are frozen publication snapshots. They do not replace the `.md` files.

## Content discipline

Only repository-verified behavior, formulas, configuration values, fixtures and documented boundaries belong in the manual. If a value or behavior is not supported by the repository sources, it must not be presented as a V5 production fact.
