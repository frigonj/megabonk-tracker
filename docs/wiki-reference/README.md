# Megabonk Community Wiki Reference

This directory is a point-in-time snapshot of the Megabonk community wiki, pulled in as reference material to help fill the gap left by having no official game-mechanics documentation. The plugin reverse-engineers class names via IL2CPP decompilation and infers stat/item/tome behavior from raw event data; these files give a human-readable cross-reference to sanity-check that inference against community knowledge.

**Source:** https://megabonk.wiki/wiki/Main_Page (MediaWiki, community-maintained)
**Snapshot date:** 2026-08-17

## Important caveat

This is **not official game documentation**. It is fan-maintained and may:
- lag behind the current game version
- contain errors, guesses, or "TBD" placeholders where the community hasn't filled in details yet
- describe mechanics that have since been rebalanced (cross-check against `patch-notes.md` for known version history)

Treat it as a hint/reference source, not ground truth. Where this tracker's own live event data conflicts with a wiki claim, trust the live data.

## What was pulled

Every top-level category from the wiki's main page **except Builds** (see below), including every individual page linked from each category's index. Boss/character/item/weapon/tome pages that appeared only as redlinks (no page created yet by the community) are noted as such in the relevant file rather than silently omitted.

| File | Category | Contents |
|---|---|---|
| `characters.md` | Characters | All 21 characters — starting weapon, passive, unlock requirement, base stats |
| `stats.md` | Stats | All player/weapon/misc stats, plus Conditions (status effects), Interactables, Weapon Slots, Tome Slots |
| `weapons.md` | Weapons | All 29 weapons (7 starter + 22 unlockable) — description, 5-tier rarity upgrade tables, unlock requirement, synergies |
| `tomes.md` | Tomes | All 23 tomes (13 default + 10 unlockable) — effect, per-level scaling by rarity, unlock requirement |
| `items.md` | Items | All 85 individual item pages linked from the index — effect, rarity, unlock requirement, synergies |
| `shop.md` | Shop | All permanent Shop upgrades — cost progression, unlock thresholds |
| `world.md` | World | Overview of the 3 maps (Forest, Desert, Graveyard) |
| `maps.md` | Maps | Full per-map detail — tiers, biome description, mini-bosses, bosses, unlock chains |
| `quests.md` | Quests | Every character/weapon/tome/item/skin unlock quest as a lookup table |
| `challenges.md` | Challenges | Every challenge across Forest/Desert tiers — effect and Silver reward |
| `bosses.md` | Bosses | Boss/mini-boss roster per map, plus full Bark Vader fight mechanics (the only boss with a detailed wiki page) |
| `patch-notes.md` | Patch Notes | Full version history from V1.0.4 through V1.0.64 (+ hotfixes), useful for correlating event data against the game version that produced it |

## What was deliberately excluded

**Builds** — the wiki's community-contributed build guides (e.g. "Amog Builds", "Athena Builds/AFK Build", etc. — roughly 35 pages) were **intentionally skipped in full**, per explicit project scope. This tracker's purpose is empirical build analysis from live data; community-authored build guides are player opinion/strategy content, not factual game-mechanics reference, and are out of scope.

## Pages skipped for other reasons

A handful of boss names (Lil Bark, Chadbark, Chunkham The Terrible, Stone Golem, Sand Golem, Scorpionussy, Anubis, Anubruh, Juge Anubis, Calcium's Dad, Ghostham the Dead, Jeff, Mike, Ted, Gary) appear only as **redlinks** on the wiki's Bosses index — no dedicated page exists yet for them. These are recorded by name only in `bosses.md` rather than fabricated. Bark Vader is the sole boss with full documented mechanics.

No talk pages, user pages, or non-content special pages were encountered as candidates during the pull (`Special:AllPages` was used to enumerate the full page list up front, and only game-content pages were fetched).

## How this was gathered

1. Fetched the Main Page and `Special:AllPages` to build a complete inventory of every wiki page (262 pages total).
2. For each non-Builds category, fetched the index page to confirm the individual pages it links to, cross-checked against the AllPages inventory.
3. Fetched every individual page (character, weapon, tome, item, map, boss, patch version) and extracted full factual content — not summaries.
4. Organized extracted content into one markdown file per category, with a header noting the source URL and snapshot date on every file.
