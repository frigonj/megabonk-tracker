using System.Collections.Generic;
using System.Linq;
using Assets.Scripts._Data;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.UI.InGame.Rewards;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace MegabonkTracker;

[BepInPlugin(GUID, MODNAME, VERSION)]
[BepInProcess("Megabonk.exe")]
public class Plugin : BasePlugin
{
    public const string
        MODNAME = "MegabonkTracker",
        AUTHOR = "frigonj",
        GUID = AUTHOR + "_" + MODNAME,
        VERSION = "1.0.0";

    internal static new ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"Loading {MODNAME} v{VERSION}");

        AddComponent<DamagePoller>();

        var harmony = new Harmony(GUID);
        harmony.PatchAll();

        Log.LogInfo($"{MODNAME} loaded.");
    }
}

[HarmonyPatch(typeof(GameManager), "StartPlaying")]
public static class Patch_GameManager_StartPlaying
{
    private static void Postfix(GameManager __instance)
    {
        try
        {
            EventSink.ResetForNewRun();
            DamagePoller.RunActive = true;
            DamagePoller.IsPaused = false;
            DamagePoller.ActiveGameManager = __instance;
            DamagePoller.KnownWeaponBaseStats.Clear();
            DamagePoller.KnownPlayerBaseStats = null;
            EventSink.Emit(new RunStartedEvent());
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogError($"Patch_GameManager_StartPlaying failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(GameManager), "OnDied")]
public static class Patch_GameManager_OnDied
{
    private static void Postfix(GameManager __instance)
    {
        DamagePoller.RunActive = false;
        try
        {
            DamagePoller.EmitSnapshot();
            DamagePoller.EmitWeaponStatsSnapshot();
            DamagePoller.EmitPlayerStatsSnapshot();
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogError($"DamagePoller end-of-run snapshot failed during OnDied: {ex}");
        }
        // RunEndedEvent must fire regardless of whether the final snapshots succeeded, so the
        // dashboard doesn't leave the run stuck showing "in progress" forever.
        EventSink.Emit(new RunEndedEvent { outcome = "died" });
    }
}

[HarmonyPatch(typeof(UpgradePicker), "SelectUpgrade")]
public static class Patch_UpgradePicker_SelectUpgrade
{
    // Prefix, not Postfix: upgradeOffer's IndexOutOfRangeException (both via foreach and via
    // indexed get_Item) pointed at the native-backed list being mutated out from under us. The
    // most likely cause is SelectUpgrade's own body consuming/clearing the offer list as part of
    // applying the pick - reading it before the original method runs, while it's still in its
    // pre-mutation state, avoids the race instead of trying to read it faster/more defensively
    // after the fact.
    private static void Prefix(IUpgradable upgradable, List<StatModifier> upgradeOffer, UpgradeButton btn, ERarity rarity)
    {
        try
        {
            if (upgradable == null) return;

            var statChanges = new List<StatChangeEntry>();
            if (upgradeOffer != null)
            {
                int count = upgradeOffer.Count;
                for (int i = 0; i < count; i++)
                {
                    StatModifier mod;
                    try
                    {
                        mod = upgradeOffer[i];
                    }
                    catch (System.Exception)
                    {
                        break; // list shrank under us - stop rather than keep indexing past its new end
                    }
                    if (mod == null) continue;
                    statChanges.Add(new StatChangeEntry
                    {
                        stat = mod.stat.ToString(),
                        modifyType = mod.modifyType.ToString(),
                        amount = mod.modification
                    });
                }
            }

            EventSink.Emit(new UpgradePickedEvent
            {
                name = upgradable.GetName(),
                level = upgradable.GetLevel(),
                rarity = rarity.ToString(),
                statChanges = statChanges.ToArray()
            });
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogError($"Patch_UpgradePicker_SelectUpgrade failed: {ex}");
        }
    }
}

// EffectStat.ApplyEffect is the shared mechanism behind shrine buffs, gravestone effects, and
// encounter-reward stat changes - EffectStat itself has no back-reference to whatever triggered it,
// so the source-specific patches below stash a name in EffectSource just before the effect queue
// processes, and the ApplyEffect postfix reads it. Best-effort: a source class not patched here
// will still be captured with source="unknown" rather than silently dropped.
public static class EffectSource
{
    public static string Current = "unknown";
}

[HarmonyPatch(typeof(InteractableShrineGreed), "Interact")]
public static class Patch_ShrineGreed_Interact
{
    private static void Prefix() => EffectSource.Current = "ShrineGreed";
}

[HarmonyPatch(typeof(InteractableGravestone), "Interact")]
public static class Patch_Gravestone_Interact
{
    private static void Prefix() => EffectSource.Current = "Gravestone";
}

[HarmonyPatch(typeof(EncounterOffer), "ApplyEffects")]
public static class Patch_EncounterOffer_ApplyEffects
{
    private static void Prefix() => EffectSource.Current = "EncounterOffer";
}

[HarmonyPatch(typeof(EffectStat), "ApplyEffect")]
public static class Patch_EffectStat_ApplyEffect
{
    private static void Postfix(EffectStat __instance)
    {
        try
        {
            if (__instance == null) return;
            var mod = __instance.statModifier;

            EventSink.Emit(new EffectAppliedEvent
            {
                source = EffectSource.Current,
                effectType = __instance.effectType.ToString(),
                stat = mod != null ? mod.stat.ToString() : "",
                modifyType = mod != null ? mod.modifyType.ToString() : "",
                amount = mod != null ? mod.modification : __instance.value,
                permanent = __instance.permanent,
                duration = __instance.duration,
                isPositiveEffect = __instance.isPositiveEffect
            });
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogError($"Patch_EffectStat_ApplyEffect failed: {ex}");
        }
        finally
        {
            EffectSource.Current = "unknown";
        }
    }
}

[HarmonyPatch(typeof(PauseUi), "Pause")]
public static class Patch_PauseUi_Pause
{
    private static void Postfix()
    {
        if (DamagePoller.IsPaused) return;
        DamagePoller.IsPaused = true;
        EventSink.Emit(new GamePausedEvent());
    }
}

[HarmonyPatch(typeof(PauseUi), "Resume")]
public static class Patch_PauseUi_Resume
{
    private static void Postfix()
    {
        if (!DamagePoller.IsPaused) return;
        DamagePoller.IsPaused = false;
        EventSink.Emit(new GameResumedEvent());
    }
}

public class DamagePoller : MonoBehaviour
{
    public static bool RunActive;
    public static bool IsPaused;
    public static GameManager ActiveGameManager;

    // Base stats are fixed per weapon type and only need to be captured once, the first time
    // that weapon is seen this run - avoids re-walking all EStat values every tick for no reason.
    public static readonly Dictionary<EWeapon, StatValueEntry[]> KnownWeaponBaseStats = new();
    public static StatValueEntry[] KnownPlayerBaseStats;

    // GetBaseStat/GetValue/GetStat throw for any EStat the underlying dictionary doesn't define -
    // that's how the game itself is written, not something this plugin can avoid at the call site.
    // Walking all 57 EStat values every tick meant eating that exception on ~50 of them per weapon
    // per second, which is expensive even caught. Once the first pass on a given weapon/player
    // discovers which keys actually exist (didn't throw), every later read only tries those -
    // collapsing the exception count from "every stat, every tick" to "every stat, once".
    public static readonly Dictionary<EWeapon, EStat[]> KnownWeaponValidStats = new();
    public static EStat[] KnownPlayerValidStats;

    public static int ExceptionsThisSecond;
    private static float _exceptionLogTimer;

    private static readonly EStat[] AllStats = (EStat[])System.Enum.GetValues(typeof(EStat));

    private const float PollIntervalSeconds = 1f;
    private float _timer;

    private void Update()
    {
        if (!RunActive || IsPaused) return;
        _timer += Time.deltaTime;
        _exceptionLogTimer += Time.deltaTime;

        if (_exceptionLogTimer >= 1f)
        {
            if (ExceptionsThisSecond > 0)
                Plugin.Log?.LogInfo($"DamagePoller: {ExceptionsThisSecond} stat-lookup exceptions in the last second");
            ExceptionsThisSecond = 0;
            _exceptionLogTimer = 0f;
        }

        if (_timer < PollIntervalSeconds) return;
        _timer = 0f;

        // An uncaught exception here kills every poll for the rest of the run (Unity swallows it,
        // Update() never runs again on the affected object) - log and keep going rather than lose
        // the whole event stream to one bad snapshot.
        try
        {
            EmitSnapshot();
            EmitRunCountersSnapshot();
            EmitWeaponStatsSnapshot();
            EmitPlayerStatsSnapshot();
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogError($"DamagePoller tick failed: {ex}");
        }
    }

    public static void EmitSnapshot()
    {
        var damageSources = RunStats.damageSources;
        if (damageSources == null) return;

        var entries = new List<DamageEntry>();
        float total = 0f;
        foreach (var kv in damageSources)
        {
            var src = kv.Value;
            if (src == null) continue;
            entries.Add(new DamageEntry { source = kv.Key, damage = src.damage, level = src.GetLevel() });
            total += src.damage;
        }

        EventSink.Emit(new DamageSnapshotEvent { sources = entries.ToArray(), totalDamage = total });
    }

    public static void EmitWeaponStatsSnapshot()
    {
        var weapons = ActiveGameManager?.GetPlayerInventory()?.weaponInventory?.weapons;
        if (weapons == null) return;

        var weaponEntries = new List<WeaponStatsEntry>();
        foreach (var kv in weapons)
        {
            var eWeapon = kv.Key;
            var weapon = kv.Value;
            if (weapon == null) continue;

            StatValueEntry[] baseStats;
            if (KnownWeaponBaseStats.TryGetValue(eWeapon, out var cachedBase))
            {
                baseStats = cachedBase;
            }
            else
            {
                var (entries, _) = DiscoverValidStats(stat => weapon.weaponData?.GetBaseStat(stat) ?? 0f);
                baseStats = entries;
                KnownWeaponBaseStats[eWeapon] = baseStats;
            }

            StatValueEntry[] currentStats;
            if (KnownWeaponValidStats.TryGetValue(eWeapon, out var validCurrentStats))
            {
                currentStats = ReadKnownStats(validCurrentStats, weapon.GetValue);
            }
            else
            {
                var (entries, valid) = DiscoverValidStats(weapon.GetValue);
                currentStats = entries;
                KnownWeaponValidStats[eWeapon] = valid;
            }

            weaponEntries.Add(new WeaponStatsEntry
            {
                weapon = eWeapon.ToString(),
                level = weapon.level,
                baseStats = baseStats,
                currentStats = currentStats
            });
        }

        EventSink.Emit(new WeaponStatsSnapshotEvent { weapons = weaponEntries.ToArray() });
    }

    public static void EmitPlayerStatsSnapshot()
    {
        var playerStats = ActiveGameManager?.GetPlayerInventory()?.playerStats;
        if (playerStats == null) return;

        if (KnownPlayerBaseStats == null)
        {
            var (entries, _) = DiscoverValidStats(PlayerStatsNew.GetBaseValue);
            KnownPlayerBaseStats = entries;
        }

        StatValueEntry[] currentStats;
        if (KnownPlayerValidStats != null)
        {
            currentStats = ReadKnownStats(KnownPlayerValidStats, playerStats.GetStat);
        }
        else
        {
            var (entries, valid) = DiscoverValidStats(playerStats.GetStat);
            currentStats = entries;
            KnownPlayerValidStats = valid;
        }

        EventSink.Emit(new PlayerStatsSnapshotEvent
        {
            baseStats = KnownPlayerBaseStats,
            currentStats = currentStats
        });
    }

    public static void EmitRunCountersSnapshot()
    {
        var inventory = ActiveGameManager?.GetPlayerInventory();
        if (inventory == null) return;

        EventSink.Emit(new RunCountersSnapshotEvent
        {
            gold = inventory.goldInt,
            characterLevel = inventory.GetCharacterLevel(),
            banishesUsed = inventory.banishesUsed,
            refreshesUsed = inventory.refreshesUsed,
            skipsUsed = inventory.skipsUsed
        });
    }

    // Walks every EStat once, catching the expected per-key exception, and returns both the
    // rendered entries and the subset of keys that didn't throw - callers cache that key set
    // and use ReadKnownStats from then on to avoid re-triggering the exception every tick.
    private static (StatValueEntry[] entries, EStat[] validStats) DiscoverValidStats(System.Func<EStat, float> getValue)
    {
        var result = new List<StatValueEntry>();
        var valid = new List<EStat>();
        foreach (var stat in AllStats)
        {
            float value;
            try
            {
                value = getValue(stat);
            }
            catch (System.Exception)
            {
                ExceptionsThisSecond++;
                continue;
            }
            valid.Add(stat);
            if (value == 0f) continue; // skip stats this weapon doesn't use, but it's still a "valid" key
            result.Add(new StatValueEntry { stat = stat.ToString(), value = value });
        }
        return (result.ToArray(), valid.ToArray());
    }

    // Fast path once valid keys are known for this weapon/player - no try/catch needed since every
    // key here already succeeded once.
    private static StatValueEntry[] ReadKnownStats(EStat[] validStats, System.Func<EStat, float> getValue)
    {
        var result = new List<StatValueEntry>(validStats.Length);
        foreach (var stat in validStats)
        {
            float value = getValue(stat);
            if (value == 0f) continue;
            result.Add(new StatValueEntry { stat = stat.ToString(), value = value });
        }
        return result.ToArray();
    }
}
