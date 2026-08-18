# Wiki-Derived Build Analysis: Methodology + Bandit/Dexecutioner Worked Example

This documents a repeatable method for deriving a data-grounded weapon/tome build from
`docs/wiki-reference/` alone, plus the full worked example for Bandit that produced it. Use the
**Methodology** section as the checklist for any future character; the **Worked Example** section
is Bandit-specific and will go stale as the game patches — don't treat its conclusions as
transferable, only its process.

## When to use this vs. this tracker's own run data

Wiki-only analysis answers "what does the math/mechanics say should be strongest." This tracker's
own `runs.db` data answers "what actually happened in play." They are not interchangeable:

- If existing runs were played for a **different objective** than the one being optimized for
  (e.g. survival/progression runs used to answer a pure-max-DPS question), exclude them —
  playstyle bias will masquerade as a build signal. This is what happened with the Bandit
  analysis below: ~15 completed runs existed, all played to survive, none played to max DPS.
- Local run data is still useful as a **sanity-check / tiebreaker**, never as the primary signal,
  when two wiki-derived candidates are otherwise close — see Step 5.
- Once enough *objective-matched* runs exist (played specifically to test the wiki-derived build),
  flip the priority: real data should start overriding wiki inference, per the wiki-reference
  README's own stated rule ("where live event data conflicts with a wiki claim, trust the live
  data").

## Methodology

### Step 1 — Anchor on the character's own kit, not a generic "best build"
Pull the character's starting weapon and passive from `characters.md`. Everything downstream is
justified by amplifying *this specific kit*, not by independently picking "good" weapons/tomes
and rationalizing the fit afterward. If the passive has a named mechanic (e.g. an uncapped
per-level scalar), identify what it stacks additively with — that's usually the highest-leverage
early pick.

### Step 2 — Read the starting weapon's own tip text as the primary source of truth
A weapon's tip line in `weapons.md` (e.g. "Size/Quantity/Cooldown Tomes increase execute
opportunities") is the wiki contributor's synthesis of the mechanic, and is more reliable than
inferring synergy from the upgrade-table shape alone. Use it to lock the tome shortlist before
touching any other weapon.

### Step 3 — Verify every stat on the table actually does something before valuing it
A stat appearing on a weapon's upgrade table does not mean it contributes damage. Check whether it
has a required paired stat elsewhere on the same table (e.g. Crit Chance without a paired Crit
Damage line is inert — a "crit" just deals normal damage). Don't assume a stat matters just
because a general rule of thumb says that *category* of stat is usually strong.

### Step 4 — Check whether a mechanic is weapon-specific before generalizing it
If a weapon has a unique named mechanic (an execute chance, a self-scaling snowball effect, an
HP-based multiplier), assume it does **not** transfer to other weapons unless the wiki explicitly
says so — even if another weapon has a superficially similar upgrade-table shape (e.g. also
lacking a crit line). Table-shape similarity is not mechanic similarity.

### Step 5 — Score every remaining weapon slot against the full roster, on fixed criteria, in one pass
This is the step most likely to get skipped by iterating conversationally (pick a candidate,
compare it only to whatever was already under discussion, move on) — and skipping it is exactly
what let a strong candidate (Mines, in the Bandit example) go unnoticed for several turns of
back-and-forth before a full sweep surfaced it. Don't compare new candidates only against
whatever's currently in the build slot; re-score against the entire catalog every time the
criteria change.

Score every weapon in `weapons.md` (all ~29, not just the ones already discussed) on the same
fixed columns, in a table, in one pass:
1. **Damage ceiling at Legendary tier** (or the game's max rarity) — pull directly, don't eyeball.
2. **Targeting reliability**, from the weapon's *description* text, not its upgrade table —
   stationary/orbiting/homing/auto-target-nearest weapons are structurally immune to a real
   failure mode (facing-dependent or manually-aimed weapons lose effective DPS against dense
   swarms because the player physically cannot track every target). This is the single most
   commonly missed axis because it never shows up in the numeric tables at all — only in prose.
3. **Explicit synergy with the already-locked tomes** — does the weapon's own tip text name one
   of the tomes chosen in Step 2, by name? A stated match outweighs an inferred one.
4. **Local run data, as a tiebreaker only** — average/max damage share from `damage_by_source` if
   the weapon has ever been fielded. Absence of data is not evidence against a weapon; presence of
   strong data is a tiebreaker between two theoretically-similar candidates (see worked example:
   Wireless Dagger beat Chunkers on this basis alone, both being otherwise-comparable
   "safe-targeting" picks).

### Step 6 — Re-run the full comparison whenever a constraint changes
If the tome list, weapon-slot count, or objective changes mid-analysis, don't patch the previous
conclusion — re-run Step 5 against the new constraint. A weapon dominated under one tome set may
become the best option under another (e.g. dropping Precision Tome from the build changes which
weapons' crit lines are "wasted upside" vs. "actively wrong pick").

### Step 7 — Name what's still unverified
State plainly which mechanics the wiki simply doesn't document (e.g. whether a "piercing
projectile" weapon auto-targets or not). Don't round an undocumented mechanic to "probably fine."
Flag it as an open question this tracker's own live data could answer once a matching-objective
run exists.

---

## Worked Example: Bandit / Dexecutioner

Snapshot date 2026-08-17, sourced from `docs/wiki-reference/characters.md`, `weapons.md`,
`tomes.md`. Subject to the same staleness caveat as the rest of `docs/wiki-reference/`.

### Final build

- **Weapons:** Dexecutioner (forced/starting), Sniper Rifle, Mines, Wireless Dagger
- **Tomes:** Cooldown, Size, XP, Luck

### Step 1 applied
Bandit's starting weapon is Dexecutioner (`weapons.md:153-159`); passive **Flowstate** is +1%
Attack Speed per level, uncapped (`characters.md:153`) — the highest-leverage early anchor, since
an uncapped free scalar is worth compounding rather than duplicating with a capped alternative.

### Step 2 applied
Dexecutioner's own tip (`weapons.md:158`) names **Size, Quantity, Cooldown** as its
execute-opportunity levers. This became the tome shortlist before any other weapon was considered.

### Step 3 applied — Dexecutioner's Crit Chance stat is dead weight
Dexecutioner's table (`weapons.md:157`) lists Crit Chance (5%→10%) but no Crit Damage line
anywhere, and nothing else in the wiki gives it one. Per the stat system definition
(`stats.md:22`), Crit Damage is the multiplier a crit applies — without it, a "crit" deals normal
damage. Precision Tome and crit-chance itemization are therefore not justified for this weapon,
even though crit-stacking is a real, strong pattern for *other* weapons in this game.

### Step 4 applied — the execute mechanic doesn't generalize
The 2% instant-execute (`weapons.md:154`) is Dexecutioner-specific. An earlier pass of this
analysis incorrectly proposed Wireless Dagger and Slutty Rocket as if they shared or fed the
execute roll, reasoning from "also lacks a crit line" table-shape similarity rather than checking
the actual mechanic scope. They don't share it — only Dexecutioner's own attack
rate/hitbox/projectile-count feed its own roll.

### Tome selection (Steps 2 + user constraint)
Cooldown Tome is doubly justified: a named Dexecutioner lever, *and* the only other weapon in the
full catalog whose tip explicitly names it is Sniper Rifle ("Attack Speed reduces shot delay,"
`weapons.md:112`). User required XP and Luck tomes for late-game progression, capping the
Dexecutioner-lever slots at 2 — Cooldown and Size were kept for having the broadest multi-weapon
textual confirmation; Quantity was cut despite also being a named lever (next pick if a 5th tome
slot ever opens).

### Step 5 applied — full 29-weapon roster comparison
Earlier passes compared each new weapon candidate only against whichever 1-2 weapons were already
under discussion (Sword vs. Dexecutioner, then Katana vs. Sword, then Sniper Rifle vs. Katana),
never against the full field at once. Running the complete fixed-criteria sweep surfaced a strong
candidate that had never come up:

| Weapon | Dmg (Legendary) | Targeting | Tome tip match | Local data (avg/max dmg) |
|---|---|---|---|---|
| Sniper Rifle | 8 | Manual-aim, not facing-dependent | Cooldown ✓ | 21.4k / 2 runs |
| Mines | 6 | **Stationary — zero targeting risk** | Cooldown ✓ + Size ✓ (only weapon besides Dexecutioner with both) | none fielded |
| Wireless Dagger | 4 | Homing, "near-guaranteed hits" | none named | 10k avg / 28.5k max, 10 runs — 2nd-highest avg in dataset |
| Chunkers | 6 | Orbit, zero aim required | Size ✓ | none fielded |
| Katana | 4.4 | Auto-target closest | none (Precision only, unused in this tome set) | 4.3k avg / 3 runs |
| Hero Sword | 4 | Unstated (see Step 7) | Size ✓ | none fielded |
| Sword | 4 | **Facing-dependent, no auto-target** — confirmed failure mode | Size (tip only) | — |

**Mines** was the clearest miss from prior turns: stationary deployment is structurally immune to
the targeting failure mode (not just "probably safe" like orbit/homing — nothing to aim at all),
Damage ceiling of 6 beats every prior pick except Sniper Rifle, and it's the only weapon besides
Dexecutioner itself with an explicit tip-text match on *both* locked tomes.

**Wireless Dagger vs. Chunkers** (the direct question that prompted this full sweep): both are
theoretically safe on targeting (homing vs. orbit), Chunkers has the higher table Damage ceiling,
but Chunkers has never been fielded in any logged run while Wireless Dagger has real, if
survival-biased, damage evidence across 10 runs. Per Step 5's tiebreaker rule, real data breaks a
tie between two theoretically-comparable picks — Wireless Dagger won on that basis.

**Sword** — confirmed, not inferred, targeting failure mode: "slashes... in a wide sweeping arc"
(`weapons.md:19`), facing-dependent with no stated auto-target. Also strictly dominated by
Dexecutioner's own table (identical Damage/Size curves, but Knockback instead of Crit Chance, and
Knockback is stated as pure defensive utility with "no damage/hit-count benefit,"
`weapons.md:22`). Two independent reasons to exclude it, not one.

**Katana vs. Sniper Rifle** — an earlier pass incorrectly grouped both as "facing-dependent" like
Sword before correction. Katana's "targets the closest enemy" (`weapons.md:183`) is auto-target;
Sniper Rifle's "manually-aimed" (`weapons.md:108`) describes initial aim, not enemy-tracking.
Neither shares Sword's failure mode. Sniper Rifle still wins on higher Damage ceiling (8 vs 4.4)
and explicit Cooldown synergy; Katana's crit line goes unfed by this tome set (no Precision Tome).

### Step 7 applied — open gaps
Dexecutioner's and Hero Sword's targeting reliability is genuinely unstated in the wiki (both
described only as "piercing... projectile," `weapons.md:154`/`204`) — this is why Hero Sword lost
its slot once directly compared against Mines/Wireless Dagger on the same criteria: it had no
confirmed safety advantage to offset a lower Damage ceiling and no Cooldown-tome match. Whether
Dexecutioner itself holds up under swarm density is still open and is exactly the kind of question
this tracker's own damage-over-time data (`weapon_stats_history`, the live chart) could settle —
if Dexecutioner's DPS holds flat or grows through a stretch of rising enemy density in a real run,
it isn't meaningfully facing-limited; a visible dip alongside a spike in enemy count would confirm
it is. Not yet tested; a natural follow-up once a longer/deeper (Tier 3+) run is captured.

### Explicitly out of scope
- Items and shrine/gravestone buffs — scoped to weapons and tomes only per user instruction.
- This tracker's own run data as a *primary* signal — see "When to use this vs. this tracker's own
  run data" above.
- Statistical validation against real play — this is a paper analysis from wiki tables, a starting
  hypothesis to test, not a proven-optimal build.
