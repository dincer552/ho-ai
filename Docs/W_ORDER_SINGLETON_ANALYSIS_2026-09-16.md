# Winger Order Singleton Analysis — 2026-09-16

## Scope

This note analyzes the new M. Gobiet W-L singleton screenshots from 2026-09-16. The screenshots use the same player, same winger slot (left wing), and the four winger individual-order choices available in this setup. No production coefficient is changed by this note.

## Observed seven-sector ratings

Using the four screenshots supplied on 2026-09-16:

| Source | Order interpretation | DEF-L | DEF-C | DEF-R | MID | ATT-L | ATT-C | ATT-R |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 1000049155 | Normal | 1.50 | 1.25 | 0.00 | 2.00 | 4.00 | 1.25 | 0.00 |
| 1000049152 | Towards Middle | 1.25 | 1.50 | 0.00 | 2.25 | 1.25 | 1.75 | 0.00 |
| 1000049153 | Offensive | 1.00 | 1.00 | 0.00 | 2.25 | 1.50 | 2.25 | 0.00 |
| 1000049154 | Defensive | 1.50 | 1.75 | 0.00 | 2.25 | 1.25 | 1.25 | 0.00 |

The order labels above are based on the observed order-marker convention already established from the supplied screenshots: plain = Normal, upward marker = Defensive, inward/centralizing winger marker = Towards Middle, and the remaining attacking marker = Offensive. These labels should remain revisable if a later source image or manager confirmation contradicts them.

## Within-set deltas versus Normal W-L

The cleanest comparison is within this four-screenshot set because the player and slot are unchanged.

### Towards Middle − Normal

```text
DEF-L  -0.25
DEF-C  +0.25
DEF-R   0.00
MID    +0.25
ATT-L  -2.75
ATT-C  +0.50
ATT-R   0.00
```

Main observed effect: a large reduction in left-wing attack with a smaller increase in central attack, plus a small central-defence and midfield shift.

### Offensive − Normal

```text
DEF-L  -0.50
DEF-C  -0.25
DEF-R   0.00
MID    +0.25
ATT-L  -2.50
ATT-C  +1.00
ATT-R   0.00
```

Main observed effect: left-wing attack falls while central attack rises more strongly than in Towards Middle; defence is slightly reduced.

### Defensive − Normal

```text
DEF-L   0.00
DEF-C  +0.50
DEF-R   0.00
MID    +0.25
ATT-L  -2.75
ATT-C   0.00
ATT-R   0.00
```

Main observed effect: central defence rises while left-wing attack falls substantially; central attack is unchanged at the displayed level.

## What this tells us

1. **Order changes do not look like simple global multipliers.** The same player/slot changes different sectors in different directions, so order must be represented as a sector redistribution matrix rather than one scalar multiplier.

2. **The strongest empirical order axis is side attack redistribution.** Normal W-L has the highest left attack in this set. Towards Middle and Defensive both suppress that side attack heavily. Offensive also suppresses left-side attack while increasing central attack.

3. **Towards Middle and Offensive are distinguishable.** Both increase central attack, but Offensive raises central attack more in the displayed fixture (+1.00 vs +0.50 relative to Normal) while also lowering defence slightly.

4. **Defensive has a visibly different signature.** The clearest change is +0.50 central defence and no displayed central-attack increase.

5. The recurring +0.25 MID shift across all three non-Normal screenshots is notable but should not yet be converted into a production coefficient. It may include display/rounding/state interaction rather than being a pure order coefficient.

6. Existing research already showed W-L/W-R mirror behavior and singleton/pair additivity under Normal order. The new data extends that evidence from position/side mapping into individual-order behavior. The prior controlled W analysis found raw single-player contributions that add closely in pair screens. fileciteturn568file0L2-L2

## What is still missing from the DB

The current W-L order set is sufficient to begin order analysis. **We do not need 2-player or 3-player data yet.**

The next high-value dataset is the same four winger orders on **W-R with M. Gobiet**:

```text
W-R Normal
W-R Defensive
W-R Offensive
W-R Towards Middle
```

Existing DB already contains Gobiet W-R Normal, so the minimum missing additions are:

```text
W-R Defensive
W-R Offensive
W-R Towards Middle
```

An additional W-R Normal screenshot from the same session is useful as a same-state baseline, but not strictly required.

## Production decision

No production contribution coefficient is changed from this dataset yet.

Reason: the order deltas are strong evidence for a redistribution matrix, but they are still observed displayed ratings. The unresolved raw-to-display scale, experience contribution, and state/form separation remain part of the calibration problem described in the main research document. fileciteturn569file0L2-L2

## Next step

Collect **M. Gobiet W-R Defensive / Offensive / Towards Middle**. Then compare the W-L and W-R delta signatures for mirror symmetry. If the mirrored differences agree, we can promote the winger order behavior from screenshot evidence to a stronger calibrated order matrix without changing the global skill coefficients.
