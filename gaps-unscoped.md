<!-- Unscoped gaps: not yet actionable — needs a direction decision, root cause investigation, or reproduction. Format:
- [ ] <description as currently understood>  [<area>] — <why it matters, what's missing to make it actionable>  #gap-id
-->

- [ ] `RunStartedEvent.character` is always empty — no field found on `GameManager` exposing the selected character at run start  [plugin] — would let per-character filtering in `/history`; needs someone to find the right class/field (possibly on a player-selection UI class not yet decompiled) before this is scoped  #gap-002
- [ ] Combo analysis (`db.py: combo_stats`) is not statistically reliable yet — only a handful of completed runs exist, so every combo's sample_size is 1-4  [dashboard] — not a code bug, but the min_sample threshold (currently 2) may need raising once more run data exists; needs the user to decide what sample size they'd trust, and to actually play more runs  #gap-003
