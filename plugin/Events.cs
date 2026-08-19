using System;

namespace MegabonkTracker;

public abstract class TrackerEvent
{
    public string type = "";
    public string ts = DateTimeOffset.Now.ToString("O");
}

public sealed class RunStartedEvent : TrackerEvent
{
    public RunStartedEvent() => type = "run_started";
    public string character = "";
}

public sealed class RunEndedEvent : TrackerEvent
{
    public RunEndedEvent() => type = "run_ended";
    public string outcome = "";
    public int durationSeconds;
}

// Diagnostic-only, for #gap-011: confirm/rule out whether losing window focus (alt-tab, click-away)
// is what causes pause/resume events to be missed or duplicated. Remove once that's resolved.
public sealed class ApplicationFocusChangedEvent : TrackerEvent
{
    public ApplicationFocusChangedEvent() => type = "application_focus_changed";
    public bool hasFocus;
    public bool isPausedAtTimeOfFocusChange;
}

public sealed class UpgradePickedEvent : TrackerEvent
{
    public UpgradePickedEvent() => type = "upgrade_picked";
    public string name = "";
    public int level;
    public int maxLevel;
    public string rarity = "";
    public StatChangeEntry[] statChanges = Array.Empty<StatChangeEntry>();
}

public sealed class StatChangeEntry
{
    public string stat = "";
    public string modifyType = "";
    public float amount;
}

public sealed class EnemyHealthSnapshotEvent : TrackerEvent
{
    public EnemyHealthSnapshotEvent() => type = "enemy_health_snapshot";
    public float totalHp;
    public float avgHp;
    public int enemyCount;
}

// #gap-016: per-tick (cheap, and self-corrects if #gap-010's suspected StartPlaying-on-stage-
// transition bug means a "once per run" capture would miss a re-trigger) snapshot of progression
// ceilings the wiki either got wrong (weapon level cap) or doesn't document at all (enemy cap,
// Final Swarm). See docs/game-limits.md "Quiet mechanics" for how each field was found.
public sealed class ProgressionLimitsSnapshotEvent : TrackerEvent
{
    public ProgressionLimitsSnapshotEvent() => type = "progression_limits_snapshot";
    public int maxWeaponLevelBase;
    public int maxTomeLevelBase;
    public int weaponMaxLevel; // base + any bonus from items like Stainless Steel Pot
    public int tomeMaxLevel;   // base + any bonus from items like Wizard's Hat
    public int numExtraWeaponLevels;
    public int numExtraTomeLevels;
    public int numAvailableWeaponSlots;
    public int numMaxWeaponSlots;
    public int numAvailableTomeSlots;
    public int numMaxTomeSlots;
    public bool canUnlockWeapons;
    public bool canUnlockTomes;
    public bool weaponsMaxed; // WeaponInventory.isMaxed - every held weapon at max level
    public bool tomesMaxed;   // TomeInventory.isMaxed - every held tome at max level
    public int numMaxEnemies;
    public bool hasMaxEnemies;
    public bool isFinalSwarm;
}

public sealed class DamageSnapshotEvent : TrackerEvent
{
    public DamageSnapshotEvent() => type = "damage_snapshot";
    public DamageEntry[] sources = Array.Empty<DamageEntry>();
    public float totalDamage;
}

public sealed class DamageEntry
{
    public string source = "";
    public float damage;
    public int level;
}

public sealed class WeaponStatsSnapshotEvent : TrackerEvent
{
    public WeaponStatsSnapshotEvent() => type = "weapon_stats_snapshot";
    public WeaponStatsEntry[] weapons = Array.Empty<WeaponStatsEntry>();
}

public sealed class WeaponStatsEntry
{
    public string weapon = "";
    public int level;
    public StatValueEntry[] baseStats = Array.Empty<StatValueEntry>();
    public StatValueEntry[] currentStats = Array.Empty<StatValueEntry>();
}

public sealed class StatValueEntry
{
    public string stat = "";
    public float value;
}

public sealed class EffectAppliedEvent : TrackerEvent
{
    public EffectAppliedEvent() => type = "effect_applied";
    public string source = ""; // class name of whatever triggered this (shrine, gravestone, encounter reward, etc.)
    public string effectType = "";
    public string stat = "";
    public string modifyType = "";
    public float amount;
    public bool permanent;
    public float duration;
    public bool isPositiveEffect;
}

public sealed class PlayerStatsSnapshotEvent : TrackerEvent
{
    public PlayerStatsSnapshotEvent() => type = "player_stats_snapshot";
    public StatValueEntry[] baseStats = Array.Empty<StatValueEntry>();
    public StatValueEntry[] currentStats = Array.Empty<StatValueEntry>();
    // The XpIncreaseMultiplier EStat in currentStats is the raw, uncapped sum of every XP-Gain
    // source - it climbs forever and never reflects the game's actual clamp. maxXpMultiplier is
    // the real ceiling (PlayerXp.maxXpMultiplier, confirmed via decompile) the game applies when
    // awarding XP, so dashboard/tracker.py can compute the effective multiplier as
    // min(XpIncreaseMultiplier, maxXpMultiplier) instead of showing a number that's misleading
    // once a build has out-scaled the cap. See #gap-015.
    public float maxXpMultiplier;
}

// Moai and Shady Guy grant items directly (InventoryUtility.GetRandomItemsMoai/GetRandomItemsShadyGuy)
// rather than going through EffectStat.ApplyEffect like every other shrine/gravestone/encounter
// effect - so they don't produce effect_applied events at all under the existing tracking, and
// need their own event type. See docs/game-limits.md "Quiet mechanics" and #gap-016.
public sealed class ItemGrantedEvent : TrackerEvent
{
    public ItemGrantedEvent() => type = "item_granted";
    public string source = ""; // "Moai" or "ShadyGuy"
    public string item = "";
    public string rarity = "";
}

public sealed class RunCountersSnapshotEvent : TrackerEvent
{
    public RunCountersSnapshotEvent() => type = "run_counters_snapshot";
    public int gold;
    public int characterLevel;
    public int banishesUsed;
    public int refreshesUsed;
    public int skipsUsed;
}

public sealed class GamePausedEvent : TrackerEvent
{
    public GamePausedEvent() => type = "game_paused";
}

public sealed class GameResumedEvent : TrackerEvent
{
    public GameResumedEvent() => type = "game_resumed";
}

public sealed class PerformanceSnapshotEvent : TrackerEvent
{
    public PerformanceSnapshotEvent() => type = "performance_snapshot";
    public float avgFps;
    public float minFps;
    public int frameCount;
    public int spikeCount; // frames whose deltaTime exceeded the spike threshold this window
}
