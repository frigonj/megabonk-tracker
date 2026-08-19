using System.Collections.Generic;
using System.Linq;
using Assets.Scripts._Data;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.UI.InGame.Rewards;
using Inventory__Items__Pickups.Xp_and_Levels;
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
            var character = __instance.GetPlayerInventory()?.characterData?.eCharacter.ToString() ?? "";
            EventSink.Emit(new RunStartedEvent { character = character });
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
    // Prefix, not Postfix: the upgradeOffer *parameter* threw IndexOutOfRangeException (both via
    // foreach and via indexed get_Item) when read in a Postfix, and live testing showed it reads
    // as empty when read in a Prefix - suggesting the parameter itself isn't the reliable copy of
    // the offer. UpgradeButton has its own `upgradeOffer` field (confirmed in decompiled source),
    // which is what the button's own click handler almost certainly populates and forwards from -
    // read that instead of the method parameter.
    private static void Prefix(IUpgradable upgradable, List<StatModifier> upgradeOffer, UpgradeButton btn, ERarity rarity)
    {
        try
        {
            if (upgradable == null) return;

            // btn.upgradeOffer is Il2CppSystem.Collections.Generic.List<StatModifier> - a different
            // type than the upgradeOffer parameter (System.Collections.Generic.List<StatModifier> as
            // Harmony binds it here) despite the same name, so they can't be unified with `??`.
            // ReadStatModifiers takes count+indexer as delegates so both list types share one loop.
            List<StatChangeEntry> statChanges;
            var btnOffer = btn?.upgradeOffer;
            if (btnOffer != null)
            {
                statChanges = ReadStatModifiers(() => btnOffer.Count, i => btnOffer[i]);
            }
            else if (upgradeOffer != null)
            {
                statChanges = ReadStatModifiers(() => upgradeOffer.Count, i => upgradeOffer[i]);
            }
            else
            {
                statChanges = new List<StatChangeEntry>();
            }

            EventSink.Emit(new UpgradePickedEvent
            {
                name = upgradable.GetName(),
                level = upgradable.GetLevel(),
                maxLevel = upgradable.GetMaxLevel(),
                rarity = rarity.ToString(),
                statChanges = statChanges.ToArray()
            });
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogError($"Patch_UpgradePicker_SelectUpgrade failed: {ex}");
        }
    }

    private static List<StatChangeEntry> ReadStatModifiers(System.Func<int> getCount, System.Func<int, StatModifier> getItem)
    {
        var statChanges = new List<StatChangeEntry>();
        int count = getCount();
        for (int i = 0; i < count; i++)
        {
            StatModifier mod;
            try
            {
                mod = getItem(i);
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
        return statChanges;
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

// Moai and Shady Guy grant items directly rather than routing through EffectStat.ApplyEffect (see
// ItemGrantedEvent) - same stash-before/read-in-postfix pattern as EffectSource above, but tracked
// separately since UpgradePicker.SelectItem is a single shared method for both sources (and
// possibly others, e.g. chests) with no back-reference to which one triggered it.
public static class ItemSource
{
    public static string Current = "unknown";
}

[HarmonyPatch(typeof(InteractableShrineMoai), "Interact")]
public static class Patch_ShrineMoai_Interact
{
    private static void Prefix() => ItemSource.Current = "Moai";
}

[HarmonyPatch(typeof(InteractableShadyGuy), "Interact")]
public static class Patch_ShadyGuy_Interact
{
    private static void Prefix() => ItemSource.Current = "ShadyGuy";
}

[HarmonyPatch(typeof(UpgradePicker), "SelectItem")]
public static class Patch_UpgradePicker_SelectItem
{
    private static void Postfix(ItemData itemData)
    {
        try
        {
            if (itemData == null) return;
            EventSink.Emit(new ItemGrantedEvent
            {
                source = ItemSource.Current,
                item = itemData.eItem.ToString(),
                rarity = itemData.rarity.ToString()
            });
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogError($"Patch_UpgradePicker_SelectItem failed: {ex}");
        }
        finally
        {
            ItemSource.Current = "unknown";
        }
    }
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

// Diagnostic for #gap-011 - user reported the pause/resume mismatch seems to happen around
// losing window focus (alt-tab, clicking away), not the in-game pause menu itself. Logs every
// focus change plus IsPaused at that moment so the two can be correlated directly from the event
// stream instead of inferred from timestamps. Remove once #gap-011 is resolved.
[HarmonyPatch(typeof(AlwaysUi), "OnApplicationFocus")]
public static class Patch_AlwaysUi_OnApplicationFocus
{
    private static void Postfix(bool hasFocus)
    {
        try
        {
            EventSink.Emit(new ApplicationFocusChangedEvent
            {
                hasFocus = hasFocus,
                isPausedAtTimeOfFocusChange = DamagePoller.IsPaused
            });
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogError($"Patch_AlwaysUi_OnApplicationFocus failed: {ex}");
        }
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

    // Frame-time sampling to correlate reported stutter against actual FPS data, independent of
    // the stat-lookup exception path above - a stutter with no exceptions logged means the cause
    // is elsewhere (GC pressure from JSON/file writes, WebSocket broadcast cost, etc). A spike is
    // any single frame's deltaTime implying under 30fps; sampled unconditionally so it also covers
    // menus/level-up screens, not just active runs.
    private const float SpikeFpsThreshold = 30f;
    private float _perfWindowTimer;
    private float _perfMinFps = float.MaxValue;
    private float _perfFpsSum;
    private int _perfFrameCount;
    private int _perfSpikeCount;

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            float fps = 1f / dt;
            _perfFpsSum += fps;
            _perfFrameCount++;
            if (fps < _perfMinFps) _perfMinFps = fps;
            if (fps < SpikeFpsThreshold) _perfSpikeCount++;
        }
        _perfWindowTimer += dt;
        if (_perfWindowTimer >= PollIntervalSeconds)
        {
            if (RunActive && _perfFrameCount > 0)
            {
                EventSink.Emit(new PerformanceSnapshotEvent
                {
                    avgFps = _perfFpsSum / _perfFrameCount,
                    minFps = _perfMinFps,
                    frameCount = _perfFrameCount,
                    spikeCount = _perfSpikeCount
                });
            }
            _perfWindowTimer = 0f;
            _perfFpsSum = 0f;
            _perfMinFps = float.MaxValue;
            _perfFrameCount = 0;
            _perfSpikeCount = 0;
        }

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
            EmitEnemyHealthSnapshot();
            EmitProgressionLimitsSnapshot();
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogError($"DamagePoller tick failed: {ex}");
        }
    }

    public static void EmitProgressionLimitsSnapshot()
    {
        var weaponInv = ActiveGameManager?.GetPlayerInventory()?.weaponInventory;
        var tomeInv = ActiveGameManager?.GetPlayerInventory()?.tomeInventory;
        var enemyMgr = EnemyManager.Instance;
        if (weaponInv == null || tomeInv == null || enemyMgr == null) return;

        EventSink.Emit(new ProgressionLimitsSnapshotEvent
        {
            maxWeaponLevelBase = InventoryUtility.MAX_WEAPON_LEVEL_BASE,
            maxTomeLevelBase = InventoryUtility.MAX_TOME_LEVEL_BASE,
            weaponMaxLevel = InventoryUtility.GetWeaponMaxLevel(),
            tomeMaxLevel = InventoryUtility.GetTomeMaxLevel(),
            numExtraWeaponLevels = InventoryUtility.GetNumExtraWeaponLevels(),
            numExtraTomeLevels = InventoryUtility.GetNumExtraTomeLevels(),
            numAvailableWeaponSlots = InventoryUtility.GetNumAvailableWeaponSlots(),
            numMaxWeaponSlots = InventoryUtility.GetNumMaxWeaponSlots(),
            numAvailableTomeSlots = InventoryUtility.GetNumAvailableTomeSlots(),
            numMaxTomeSlots = InventoryUtility.GetNumMaxTomeSlots(),
            canUnlockWeapons = InventoryUtility.CanUnlockWeapons(),
            canUnlockTomes = InventoryUtility.CanUnlockTomes(),
            weaponsMaxed = weaponInv.isMaxed,
            tomesMaxed = tomeInv.isMaxed,
            numMaxEnemies = enemyMgr.GetNumMaxEnemies(),
            hasMaxEnemies = enemyMgr.HasMaxEnemies(),
            isFinalSwarm = enemyMgr.IsFinalSwarm()
        });
    }

    // For comparing DPS against what's currently standing between the player and survival - a
    // build whose DPS isn't keeping pace with total enemy HP present is losing ground even before
    // it dies. hp is clamped to 0 in the sum: dead-but-not-yet-despawned enemies can transiently
    // read negative (their Despawn coroutine runs a frame or more after the killing blow), which
    // would otherwise understate the total.
    public static void EmitEnemyHealthSnapshot()
    {
        var enemies = EnemyManager.Instance?.enemies;
        if (enemies == null) return;

        float totalHp = 0f;
        int count = 0;
        foreach (var kv in enemies)
        {
            var enemy = kv.Value;
            if (enemy == null) continue;
            totalHp += System.Math.Max(enemy.hp, 0f);
            count++;
        }

        EventSink.Emit(new EnemyHealthSnapshotEvent
        {
            totalHp = totalHp,
            avgHp = count > 0 ? totalHp / count : 0f,
            enemyCount = count
        });
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
            currentStats = currentStats,
            maxXpMultiplier = PlayerXp.maxXpMultiplier
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
