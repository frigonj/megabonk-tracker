# Megabonk Wiki Reference — Patch Notes

Source: https://megabonk.wiki/wiki/Patch_Notes + individual version pages (community wiki, MediaWiki)
Snapshot date: 2026-08-17

This is a point-in-time snapshot of community-maintained wiki content. It is **not official game documentation** and may drift from actual current game behavior/balance as patches are released. Use as a reference/hint source only — verify against live event data where possible.

This file is especially useful for the tracker project: it documents when specific characters/items/mechanics were introduced or rebalanced, which matters for interpreting historical event data against the correct game version.

## Version History Overview

| Version | Date | Title |
|---|---|---|
| V1.0.64 | January 2026 | Hats & Recovery Update (+ hotfixes v1.0.65, v1.0.69) |
| V1.0.49 | December 25, 2025 | Christmas Update |
| V1.0.41 | December 14, 2025 | Spooky Update |
| V1.0.17 | October 20, 2025 | Fog of War & Visuals Update |
| V1.0.12 | October 8, 2025 | Leaderboard Fix & Final Swarm Update |
| V1.0.7 | September 29, 2025 | Hotfixes |
| V1.0.4 | September 25, 2025 | Content and Balance Changes |
| V1.0.0 | September 18, 2025 | Official Release |

---

## V1.0.64 — Hats & Recovery Update (January 2026)

**New content:** 21 new hats + achievements; new Megachad skin.

**Lost Save Recovery:** New Settings → Other button; uses Steam Achievements to restore lost progress (cannot fully restore all saves or achievements not tracked by Steam).

**UI:** Banished items now visible in pause menu/chests/shrines; stats display while holding Tab over the map; new Unlocks tab window showing upgradable stats per weapon and character-unique stats; character rotation in Character Select via mouse/controller.

**Balance:**
- Pots/Vases grant scaling Luck based on stage.
- Robinette passive: damage-per-1k-gold increased 2.5% → 3.5% after 1M gold banked.
- Feathers: +10% forward jump speed, +1 jump.
- Bone: knockback 1.25 → 1.5, Knockback upgrade removed, Crit Chance/Crit Damage upgrades added instead.
- Shotgun: knockback 2.5 → 3, Knockback upgrade removed, Crit Damage upgrade added.
- Vlad: +5% flat Lifesteal (baseline buff).
- Robinette: +5% flat Gold Gain (baseline buff).
- Spaceman: +5% flat XP Gain (baseline buff).
- Za Warudo: capped at 25 max per run.

**Other:** Item proc order now by enum name instead of acquisition order; XP shards have size variation for value distinction; level-up screens no longer inherit movement inputs; Scythe volume lowered; new Effects settings for "Enemy Attack Indicator Color" and "XP & Gold increase text."

**Bug fixes:** Steam Rich Presence errors, leaderboard item limits, Dicehead passive triggering, Chunkers projectile speed, Joe's Dagger Execute damage, Roberto's ghost chest spawning, auto-leveling tome/weapon limits, miniboss spawning, poison damage caps, projectile opacity, Spicy Meatball/Bush damage multipliers, UI overlaps, hat shield outlines, money pickup loops, boss arena invisible floors, sword swing damage, Rollerblades post-teleport functionality.

### Hotfix v1.0.65 (January 26, 2026)
Fixed: Joe's Dagger "Crazy Damage" bug, Shotgun Crit Damage upgrade showing 0%, Top Hat unlock restrictions, rocket transparency setting, Kevin quest rarity, Tab+Desert loading black screen, translation errors.

### Hotfix v1.0.69 (February 4, 2026)
Fixed: level 9999 exploit via weapon/tome banishing combinations, Auto Leveling tome stat grants, Leeching Crystal/Megachad Flex interaction, HP Regen 1-per-tick cap, Sheriff's Hat achievement unlocking. Cheesy Hat now available on Forest and Desert. Lowered Shady hat requirements. Translation fixes.

---

## V1.0.49 — Christmas Update (December 25, 2025)

**New content:** Santa hat (cosmetic); Wizard's Hat (item).

**Buffs:**
- Dicehead: passive reward multiplier 50% → 75%, minimum reward 4% → 6%.
- Beefy Ring: now scales with Overheal.
- Power Gloves: increased base damage and radius, decreased minimum cooldown.
- Brass Knuckles: base radius 7 → 8.
- Golden Sneakers: gold-per-meter doubled, 0.05 → 0.1.
- Bob's Light: one-shot chance vs Red Ghosts 15% → 33%.
- Roberto: chest interval extended 60s → 120s max, scaling 5s/chest → 2.5s/chest (i.e. takes longer to ramp but scales further).
- Unstable Transfusion: Bloodmark application chance 27% → 35%.
- Giant Fork: Megacrit damage now +0.15 per fork stack.
- Clover: Luck bonus 7.5% → 10%.
- Luck Tome: Luck bonus 7% → 8%.

**Nerfs:**
- Fox: passive Luck 2% → 1.5% per level.
- Pot (Stainless Steel): now only affects weapons; diminishing returns per stack.
- Credit Card Green: each card now increases chest prices by 10%.
- Kevin: rarity Rare → Epic.

**Balance/other:**
- Wizard's Hat = tome-focused Pot alternative.
- Removed Silver gain from Charge Shrines, Chaos Tome, Dicehead passive.
- Removed Jump Height from Chaos Tome and Dicehead passive.
- Golden Shield: more gold from damage; reduced interaction with Kevin/Leeching Crystal.
- Bush's "Bullseye" passive damage now displays separately from Sniper Rifle damage in stat breakdowns.

**Bug fixes:** Kevin and Leeching Crystal now correctly proc Megachad's Flex passive; fixed a reset exploit at crypt exit; fixed Cursed Grabbies functionality; fixed Microwave interaction with maxed items; fixed Steam Rich Presence level display; fixed Ninja passive end-screen display; fixed Big Bob darkness attack visual bug; fixed custom music reset issue.

**Note (as of v1.0.19 per Chaos Tome page):** Legendary-tier Chaos Tome upgrades reportedly grant double the intended bonus — flagged by the wiki as a known bug, status as of this snapshot unclear (not explicitly listed as fixed in later patch notes reviewed).

---

## V1.0.41 — Spooky Update (December 14, 2025)

**New content:**
- Map: Graveyard.
- Character: Roberto.
- Legendary items: Pot (Stainless Steel), Snek.
- Epic item: Bob's Light.
- Rare item: Pumpkin.
- Common item: Old Mask.
- Feature: Console (accessible via F10).

**Character buffs:**
- Dicehead: passive minimum reward 0.02 → 0.04; +1% Poison Damage per level (new).
- Chunkers: starts with 1 additional projectile.
- Amog: passive radius scales 4 → 12 over 220 levels; +1% Poison Damage per level (new).
- Calcium: momentum loss on hit reduced 50% → 25%, decreasing further at higher levels.

**Character nerfs:**
- Noelle: passive 1% → 0.75%; damage-per-frozen-enemy 2% → 1.5%.
- Cursed Grabbies: proc-per-tick limit added, capped at 250.

**Item/weapon changes:**
- Poison Flask: starting projectile speed 12 → 16.
- Dragon's Breath: 200% faster rotation, automatic enemy targeting, damage/duration buffed.

**Gameplay balancing:**
- Final Swarm Silver multiplier reduced 16x → 8x.
- New CC-cap system: after 20 minutes, ghost speed increases and stun/freeze immunity threshold gradually lowers.
- Attempted fix for the "caveman" exploit (hiding in caves to avoid engagement).

**New settings:** Advanced Settings toggle (reduces UI clutter for new players); Shrine Counter (shows availability/usage); HUD visibility toggle; chest/portal animation skip options; Silver Pot enable/disable; Item Feed display toggle.

**Bug fixes:** 8 issues resolved including Giant Fork Megacrit malfunction, unauthorized item purchases, animation glitches, and various exploit patches.

---

## V1.0.17 — Fog of War & Visuals Update (October 20, 2025)

**Character/item buffs:**
- Fox: passive Luck 1% → 2% per level.
- Birdo: passive now grants 1% Airborne Damage per level.
- Bush: crit damage 0.5% → 1% per level; Bullseye marks appear more frequently; explosion scales with Size.
- Megachad: passive now grants 2.5% damage per Flex activation.
- Noelle: gains Size instead of Duration per level; damage multiplier 0.02x per frozen enemy.
- Dicehead: passive rewards halved at level 50 instead of level 25 (i.e. weaker falloff, effectively a buff).
- Calcium: gains 0.75% damage per level plus 0.5% per 1% Speed Multiplier.
- Ogre: passive damage 1% → 1.5% per level.
- Scarf: damage 33% → 50%.
- Brass Knuckles: changed from Additive to Flat scaling; now increases Size per stack.
- Spaceman: no longer takes fall damage.
- Cursed Doll: can now curse up to 5 enemies per tick (previously 1).

**Nerfs:**
- Robinette: passive damage gain reduced after 200k gold, for late-game balance.

**System changes:** DX12 support added as an alternative to DX11; particle/effects opacity slider added to Visuals settings; Silver Tome now toggleable; Charge Shrines distinguished by a different minimap color; money particles move faster during Final Swarm.

**Bug fixes:** Shady Guy disappearing, HP display, Campfire removal bug, chest purchase text, Shattered Wisdom (likely "Shattered Knowledge") damage, paused-game item damage ticking, intro music, Epic item banishing, Chaos Tome luck double-application, various menu bugs.

**Note:** Per Stats page (characters.md cross-reference), characters also gain +1 Max HP per level "since v1.0.17" — a baseline change not itemized explicitly in this patch's own changelog capture but referenced elsewhere on the wiki.

---

## V1.0.12 — Leaderboard Fix & Final Swarm Update (October 8, 2025)

**Buffs:**
- Chaos Tome: reward bonus +50%.
- Black Hole: default size +20%.
- Bush (Marksman passive): cooldown −0.4s per 10 levels, capping at 1s at level 100.
- Dexecutioner, Axe, Sword: improved projectile behavior at high Duration values.
- General projectile weapons: improved reliability, reduced projectile loss.

**Nerfs:**
- XP Tome: XP gain 9% → 7%.
- Black Hole: proc coefficient 0.9 → 0.7 (aligned with similar weapons).
- Joe's Dagger: max damage growth capped at 200/minute × number of daggers owned.
- "Rolly Bones" enemies: movement speed decreased for combat pacing.

**Stability:** Fixed save corruption after crashes/forced exits; fixed leaderboard exploits/manipulation; corrected Black Hole hitbox scaling; Spaceman passive now correctly grants XP per level; fixed Spiky Shield stacking issue; improved projectile duration logic to prevent premature despawn.

**Features:** Added Megachad's Theme soundtrack; optimized FPS with Dexecutioner/Black Hole; Final Swarm redesigned with ghost enemies and reduced phase duration; leaderboard backend secured (temporarily removed "All Leaderboards" pending character-specific implementation); permanent ban list established for cheaters.

**Planned (as of this patch, per wiki):** adjustable particle/projectile opacity, expanded maps, enhanced tooltips, quest tracking UI, multiplayer support, additional content. (Note: several of these — opacity sliders, additional maps — did ship in later patches per this changelog.)

---

## V1.0.7 — Hotfixes (September 29, 2025)

- "Kevin no longer makes you invincible (rip kevin abusers)" — closed an exploit where Kevin's self-damage was being used for invulnerability.
- Leaderboard reset; developers noted ongoing work to stabilize leaderboards and address cheating.
- Fixed Dice description/upgrade text.
- Optimized late-game performance for: Bananarang, Chunkers, Axe, Flamewalker, Blood Magic, Poison Flask, Black Hole, Frostwalker, Katana.
- Shield recharge no longer interrupted by Kevin or Leeching Crystal self-damage.
- Bush's passive buffed; fixed a bug where it granted Crit Chance instead of the intended Crit Damage.
- Added volume slider for XP/gold sound effects; reduced Energy Core sound volume.
- Fixed Anubis laser rendering with lava/water; fixed Space Noodle stunlocking the boss if the boss "steals" the noodle; improved menu escape-key behavior during skin previews; attempted fix for Speed Boi time-slowdown infinite loop; fixed players taking damage while using Aegis with an active Shield Powerup; fixed Rank XP not being awarded when leaving the final stage via teleporter; several additional minor fixes.

---

## V1.0.4 — Content and Balance Changes (September 25, 2025)

- Fixed pink texture issue affecting some players.

**Bark Vader (Final Boss) fight changes:** returns one weapon per phase (starter weapon only required for phase 1); pylon zones enlarged; reduced boss healing; orb damage reduced; blue-orb freeze duration 3s → 1s; orb cooldown increased (less frequent); weapon range increased during the fight. (See bosses.md for the full current-state fight breakdown.)

**Progression:** Weapon/Tome Slot unlock thresholds adjusted — 3rd slot at 25/35 quests, 4th slot at 45/55 quests (matches current quests.md table).

**Character balance:**
- All characters ~10% faster baseline movement; slower characters buffed more significantly.
- Athena: +2 Thorns/level; +150% attack speed when shieldless.
- Ogre: increased jump height; Axe now spawns with 2 projectiles.
- Dicehead: late-game passive nerfed.
- Calcium: passive deals more damage scaling with speed.
- Bush: more marks spawned; gains Crit Damage per level.
- Chunkers: increased base damage and upgrade values.

**Item/weapon fixes:** Dice crit bug fixed (previously only affected the Dice weapon itself, not all weapons); Clover slightly buffed; Shotgun range increased; Microwave cost lowered; exploding spider enemies deal less damage/have less HP; Refresh/Skip/Banish cheaper early game; Cactus projectiles can no longer pierce Aegis, can now be evaded, fewer projectiles spawned per trigger.

**Bug fixes:** fixed death-during-teleport invincibility exploit; music no longer spams the same theme (25% chance variation), selectable via boombox; music no longer randomly stops; enemies no longer spawn on top of trees/castles; fixed camera clipping into walls; tornadoes no longer "abduct" players; various UI fixes; falling out of bounds now triggers a teleport.

**New features:** projectile aim setting (balanced targeting vs. random aim); chest prices shown in HUD; Silver Pots added (grant Silver currency); minimap lock setting; Sandstorms shortened/less obstructive; bosses can no longer be knocked off-map; quest claiming accelerated; Crit Damage multiplier now displays correctly; various typo fixes; Video Settings zoom-out option; numbered-key upgrade selection can be disabled; Shop unlocks earlier in progression.

**Leaderboards:** temporary issues due to cheaters, proper fix planned; all-time and weekly leaderboards planned.

**New content:** Quin's Mask item.
