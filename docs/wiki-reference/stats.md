# Megabonk Wiki Reference — Stats

Source: https://megabonk.wiki/wiki/Stats (+ https://megabonk.wiki/wiki/Conditions, https://megabonk.wiki/wiki/Interactables, https://megabonk.wiki/wiki/Weapon_Slots, https://megabonk.wiki/wiki/Tome_Slots) — community wiki, MediaWiki
Snapshot date: 2026-08-17

This is a point-in-time snapshot of community-maintained wiki content. It is **not official game documentation** and may drift from actual current game behavior/balance as patches are released. Use as a reference/hint source only — verify against live event data where possible.

## Player Stats

- **Max HP** — the maximum amount of HP the player has. Increased via items (Oats, Demonic Blood) or the HP Tome. Characters gain 1 Max HP per level (since v1.0.17).
- **HP Regen** — health passively regenerated per minute.
- **Overheal** — allows extra health equal to a % of normal HP. Sources: Chonkplate, Gas Mask (only two known sources per wiki).
- **Shield** — absorbs damage before it reaches HP. Recharges after 5 seconds without damage taken. A single damage instance cannot penetrate a shield to reach HP directly.
- **Armor** — percentage damage reduction. Increases are additive with diminishing returns; cannot reach 100% (other pages note a practical cap around 94%).
- **Evasion** — percentage chance to completely avoid damage. Increases are additive with diminishing returns; cannot reach 100% (cap around ~94%, per Evasion Tome page). Ninja's execute mechanic is built around this stat.
- **Lifesteal** — percentage chance to steal 1 HP from an enemy per attack. Over 100%, grants a chance to steal 2 HP per attack.
- **Thorns** — reflects damage back to enemies that hit you. Core to defensive/retaliation builds.

## Weapon Stats

- **Damage** — overall weapon damage as an "x.x" multiplier (e.g. 2x = 200% base damage; 15x = 1500%).
- **Crit Chance** — chance for an attack to critically hit. Over 100% enables "Overcrit" — applies the Crit Damage multiplier multiple times.
- **Crit Damage** — extra damage on a critical hit, as an "x.x" multiplier. Example: 10x Damage × 5x Crit Damage = 50x on a critical hit.
- **Attack Speed** — speed of weapon attacks as a percentage of 100% base. Reduces delay between attacks/multi-projectile cycles. For Aura, increases the damage tick rate.
- **Projectile Count** — number of projectiles fired per attack cycle. Affects rocks, shields, fire, etc. depending on the weapon.
- **Projectile Bounces** — number of times a projectile bounces between enemies. Only available as a level-up upgrade on specific weapons: Revolver, Bone, Wireless Dagger, Lightning Staff.

## Miscellaneous Stats

- **Size** — size of projectiles/Aura as an "x.x" multiplier. Larger sizes hit more enemies.
- **Projectile Speed** — travel speed of projectiles as an "x.x" multiplier; also affects rotation speed on certain weapons.
- **Duration** — how long a weapon effect persists before disappearing, as an "x.x" multiplier. Applies to: Axe, Flamewalker, Frostwalker, Space Noodle, Chunkers, Tornado, Black Hole, Dragon's Breath, and elemental status effects.
- **Damage to Elites** — damage multiplier vs Elite enemies ("x.x"). Stage bosses count as Elites.
- **Knockback** — how far enemies are pushed back on hit, as an "x.x" multiplier.
- **Movement Speed** — character move speed, as an "x.x" multiplier.
- **Extra Jumps** — number of jumps performable before landing. Example: 3 extra jumps = initial jump + 3 mid-air jumps.
- **Jump Height** — how high the character jumps (affects initial and extra jumps).
- **Luck** — affects rarity of items from all sources, as a % increase. Does NOT affect item proc chance or crit chance.
- **Difficulty** — affects quantity of enemies plus their HP/Damage/Speed. Does NOT increase XP drops but does increase spawn rates.
- **Pickup** — pickup range, a vague flat number per the wiki. Does NOT affect powerup drop pickup range.
- **XP Gain** — XP gained per XP shard, as an "x.x" multiplier. Capped at 10x.
- **Gold Gain** — gold value per kill/jar, as an "x.x" multiplier.
- **Silver Gain** — multiplier applied to Silver Gain for the run, as an "x.x" multiplier.
- **Elite Spawn Increase** — increases the amount of Elites that spawn, as an "x.x" multiplier.
- **Powerup Multiplier** — increases both magnitude and duration of powerup drops, as an "x.x" multiplier.
- **Powerup Drop Chance** — chance an enemy drops a random powerup on death, as an "x.x" multiplier. Also increases chest drop chances.

---

## Conditions (Status Effects)

### Positive (Power-Ups)
- **Speed Up** — dramatically improves movement speed for a base of 10 seconds.
- **Rage** — dramatically increases attack speed for a base of 10 seconds.
- **Shield (Power-Up)** — blocks all damage and knockback, but negative status effects can still take hold.
- **Stonks** — each defeated enemy awards one gold coin directly to the player.

### Negative
- **Poison** — 1 damage/sec for ~5 seconds. Stacking poison applications increases DPS and resets duration; all active poison instances on a target expire simultaneously once they lapse.
- **Frost** — slows enemies; duration varies by source. Distinct from Freeze; can be applied without Ice Cube items.
- **Freeze** — severe (but not total) deceleration. Per the wiki: "does not actually completely stop enemies" (or the player, when hit by Bark Vader's blue orb).
- **Red Condition (unnamed)** — blocks all regeneration/healing effects for 5 seconds. Applied by red projectiles in the Bark Vader fight (see bosses.md).

---

## Interactables

- **Charge Shrine** — teal-square stage object; grants a stat bonus when the player stands nearby for several seconds. A colored bubble marks the charging area; charging progress resets quickly on leaving the area; a white ring fills to show progress. Bubble color does not affect rarity of the reward.
  - **Gold Charge Shrines** rarely spawn and exclusively grant Legendary-tier bonuses.
  - **Item interactions:** Wrench reduces charge time by 4% and increases bonus by 7.5% (rounded up) per copy. Beacon adds +2 shrine spawns per copy owned to future stages, and grants completed shrines a healing ring (radius scales with Size).
  - **Rewards table:** 27 possible stat categories (Max HP, Damage, Crit Chance, Movement Speed, etc.) across 5 rarity tiers (Common/Uncommon/Rare/Epic/Legendary), with values increasing by tier. Full per-stat numeric table was not enumerated by the wiki's summary — only the category count and tier structure were confirmed.
  - Located specifically on the Desert map (per World page); Desert-specific mechanic of charging a shrine during a Sandstorm counts toward the Tornado weapon unlock quest.
- **Teleporter/Portal** — appears after defeating a stage boss, allowing progression to the next stage/tier if requirements are met.
- **Sandstorm** (Desert-specific timed event) — decreases visibility to minimum briefly; at least one guaranteed per Desert run.
- Per the wiki's own Interactables page, documentation here is sparse — most interactable-object detail (shrines, teleporters, sandstorms) is described secondhand via the World/map pages rather than a dedicated catalog.

## Weapon Slots (Shop Upgrade)
- **Effect:** "Allows a player to have more weapons during a run."
- Purchased via the Shop; see shop.md for cost/unlock progression.
- Wiki page itself is sparse — sections for "Overview," "How to unlock," "Tips," "Notes" exist but were largely unfilled as of last wiki edit (Oct 6, 2025).

## Tome Slots (Shop Upgrade)
- **Effect:** "Unlocks more slots for tomes in a run."
- Purchased via the Shop; see shop.md for cost/unlock progression.
- Wiki page itself is sparse (last edited Oct 5, 2025); detailed content not filled in.
