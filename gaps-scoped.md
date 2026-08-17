<!-- Scoped gaps: actionable without further discovery. Format:
- [ ] <specific, actionable description>  [<area>] — <why it matters>  #gap-id
-->

- [ ] Verify `weapon_stats_snapshot` and `statChanges`-on-pick events actually populate correctly against a live game run (built and unit-tested against old data, but never observed end-to-end from a real play session)  [plugin/dashboard] — the feature was built and deployed but the user stopped for the night before restarting the game to test it; if the live data looks wrong (empty arrays, wrong stat names, etc.) this needs fixing before it's trustworthy  #gap-001
