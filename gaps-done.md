<!-- Resolved gaps, moved here from gaps-scoped.md or gaps-unscoped.md when fixed. Format:
- [x] <short description>  [<area>] — <what the fix was>  #gap-id
-->

## Live verification (2026-08-17)

- [x] `upgrade_picked`, `player_stats_snapshot`, `damage_snapshot` events confirmed working end-to-end against a live game run  [plugin/dashboard] — deployed and tested; found and fixed two real bugs blocking them: (1) `WeaponData.GetBaseStat(EStat)` throwing `KeyNotFoundException` for undefined stats crashed the entire poll tick every frame, (2) `SelectUpgrade`'s postfix threw `IndexOutOfRangeException` iterating the Il2Cpp `upgradeOffer` list via `foreach`, silently dropping every pick after the first. Fixed with per-stat try/catch in `ReadStats` and indexed access instead of `foreach`. See commit 8721535.  #gap-001
- [x] Live in-game stutter caused by polling all 57 `EStat` values against every weapon + player every second  [plugin] — root cause: the exception-catching added for gap-001 turned an already-expensive 57-stat walk into repeated caught exceptions per tick (undefined stats throw by design in the game's own code). Fix: stopped polling weapon/player stats live entirely — they're now captured once at `OnDied`, since the build-analysis use case only needs end-of-run values. Damage and run counters stay on the live 1s poll (cheap reads, not the bottleneck). See commit 2c7c328.  #gap-006
