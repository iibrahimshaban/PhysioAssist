# FOLLOW-UP: Decide handling for legacy 3-section report format

**Status:** Open — needs product/clinical decision (NOT a code fix yet)
**Area:** Initial Report module — `Client/src/app/Features/initial-report/initial-report.component.ts` (`parseReportText`)
**Raised:** During merge of `origin/main` into `Nourhan/Feat/intake-module` (initial-report resolved to the 2-section architecture, decision "A3").
**Severity:** Latent edge case — affects **zero** existing records as of this merge (all 19 stored reports have empty `ReportText`).

## Background

The initial-report screen moved from a **3-section** format:

```
=== Examination ===
...
=== Diagnosis ===
...
=== Treatment Plan ===
...
```

to the current **2-section** format:

```
=== Examination ===
...
=== Treatment Plan ===
...
```

`parseReportText` now splits `reportText` **only** on the `=== Treatment Plan ===`
marker. Everything before it becomes the Examination field; everything after
becomes the Treatment Plan field.

## The problem

If a report saved in the **old 3-section format** is ever loaded, the
`=== Diagnosis ===` marker **and its clinical content are absorbed verbatim into
the Examination field**, then baked permanently into the 2-section format on the
next save.

### Verified behavior (synthetic trace against the actual merged code)

**Input:**
```
=== Examination ===
Patient shows limited shoulder ROM. Tenderness on palpation of the right trapezius.
Posture assessment reveals forward head carriage.
=== Diagnosis ===
Rotator cuff tendinopathy, right shoulder. Rule out impingement syndrome.
=== Treatment Plan ===
1. Manual therapy 2x/week.
...
```

**Resulting Examination field:**
```
Patient shows limited shoulder ROM. Tenderness on palpation of the right trapezius.
Posture assessment reveals forward head carriage.
=== Diagnosis ===
Rotator cuff tendinopathy, right shoulder. Rule out impingement syndrome.
```

(Treatment Plan parses correctly.)

## Why this is deferred, not fixed now

Simply string-stripping the Diagnosis block would **silently destroy clinical
data**. The correct behavior is a product/clinical decision, not a technical one.

## Decision needed

When an old-format (3-section) report is loaded, what should happen to the
**Diagnosis** content? Options to weigh:

1. **Discard** the Diagnosis section entirely (lossy — likely unacceptable for
   clinical records).
2. **Prepend/append** it to the Treatment Plan (or Examination) with a clear
   label, e.g. `Legacy Diagnosis: ...`.
3. **Store it in a separate archived-notes field** (requires a DB/DTO change) so
   nothing is lost and it stays distinguishable.
4. **Block editing / show a migration prompt** for old-format reports until a
   human resolves them.

## Trigger conditions to watch

This path is currently unreachable in production data, but could become reachable
via:
- Restoring an older database backup containing 3-section reports.
- Another branch/feature that still writes 3-section `reportText`.
- Data import from an external/legacy system.

## References
- Code: `Client/src/app/Features/initial-report/initial-report.component.ts` — see the `KNOWN LIMITATION` comment above `parseReportText`.
- This file is referenced from that comment as `FOLLOWUP-legacy-3section-report-format.md`.
