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

public sealed class UpgradePickedEvent : TrackerEvent
{
    public UpgradePickedEvent() => type = "upgrade_picked";
    public string name = "";
    public int level;
    public string rarity = "";
    public StatChangeEntry[] statChanges = Array.Empty<StatChangeEntry>();
}

public sealed class StatChangeEntry
{
    public string stat = "";
    public string modifyType = "";
    public float amount;
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
