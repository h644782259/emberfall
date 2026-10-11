# Structured battle recap

The recap UI now renders an immutable, character-slot-scoped settlement snapshot rather than the old paragraph summary.

## Display

- A result header with dungeon tier/wave and actual challenge mode
- Up to four priority action-count cards, with large numerals and existing icon-atlas symbols
- On defeat, the recorded last attack source and post-mitigation damage; this is labelled “last hit,” not an invented causal diagnosis
- Recorded gold lost, settlement-time fragments, and capped exchange progress; pending-at-settlement chest/first-clear availability appears as compact reward labels
- Equipment mechanisms, blessings, and remaining positive action counts as wrapping chips
- At most one short, contextual retry suggestion
- Empty or unrecorded metric/reward sections are omitted; no fabricated damage total, DPS, elapsed time, kill total, or numeric clear-reward amount

The main action is outside the scrollable body. Compact mobile layouts retain a 44-touch-unit button and independently scrolling cards. Larger layouts use more metric columns. The number text respects the existing size preference and shrinks within a card only when necessary to prevent clipping.

## State ownership

`BuildRunSummary` moved from GameSession.Expedition to GameSession.Feedback and now also captures `LastRunRecap`. This stores copies of action counts, mechanism/blessing names, and actual settlement state. It is not saved or used for rewards. Slot switching hides another character's prior recap.

`RecordRecapGoldLoss` refreshes the defeat snapshot after the real gold deduction. A subsequent incoming-damage callback can update the final hit without losing that gold-loss amount. Expedition action counters and last-hit data now reset on both dungeon and wilderness zone entry, avoiding previous-dungeon actions leaking into a wilderness death recap.

`DrawStructuredRunRecap(bool death)` returns only the primary-button click. The caller remains responsible for returning to adventure or respawning. No button grants a reward or changes a snapshot.

## Verification

Pure tests cover real/nonpositive/missing metrics, snapshot isolation, unique generic chips, capped exchange progress, non-fabricated empty sections, one-tip behaviour, finite input handling, responsive card/chip bounds, and fixed-footer separation across compact phone, portrait, tablet, and desktop canvases.

Unity rendering and device screenshots require separate visual validation. API compilation must not be reported as a rendered visual pass.

## Optional arena settlement

Snapshots also store the actual arena name and terminal failure reason. Timed-out or blocked-spawn runs show that reason instead of implying an earlier recorded hit caused defeat. Positive gold/experience/material reward deltas appear only when the runtime has recorded a successful grant; zero XP at the level cap and capped-out resources are omitted. Ordinary-dungeon chest/first-clear labels are not presented as new arena rewards. The runtime must record credited deltas, not copy an unclamped reward proposal.
