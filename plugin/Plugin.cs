using System.Collections.Generic;
using System.Linq;
using Assets.Scripts._Data;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
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
        EventSink.ResetForNewRun();
        DamagePoller.RunActive = true;
        DamagePoller.ActiveGameManager = __instance;
        DamagePoller.KnownWeaponBaseStats.Clear();
        EventSink.Emit(new RunStartedEvent());
    }
}

[HarmonyPatch(typeof(GameManager), "OnDied")]
public static class Patch_GameManager_OnDied
{
    private static void Postfix(GameManager __instance)
    {
        DamagePoller.RunActive = false;
        DamagePoller.EmitSnapshot();
        EventSink.Emit(new RunEndedEvent { outcome = "died" });
    }
}

[HarmonyPatch(typeof(UpgradePicker), "SelectUpgrade")]
public static class Patch_UpgradePicker_SelectUpgrade
{
    private static void Postfix(IUpgradable upgradable, List<StatModifier> upgradeOffer, UpgradeButton btn, ERarity rarity)
    {
        if (upgradable == null) return;

        var statChanges = new List<StatChangeEntry>();
        if (upgradeOffer != null)
        {
            foreach (var mod in upgradeOffer)
            {
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
}

public class DamagePoller : MonoBehaviour
{
    public static bool RunActive;
    public static GameManager ActiveGameManager;

    // Base stats are fixed per weapon type and only need to be captured once, the first time
    // that weapon is seen this run - avoids re-walking all EStat values every tick for no reason.
    public static readonly Dictionary<EWeapon, StatValueEntry[]> KnownWeaponBaseStats = new();

    private static readonly EStat[] AllStats = (EStat[])System.Enum.GetValues(typeof(EStat));

    private const float PollIntervalSeconds = 1f;
    private float _timer;

    private void Update()
    {
        if (!RunActive) return;
        _timer += Time.deltaTime;
        if (_timer < PollIntervalSeconds) return;
        _timer = 0f;
        EmitSnapshot();
        EmitWeaponStatsSnapshot();
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

            if (!KnownWeaponBaseStats.TryGetValue(eWeapon, out var baseStats))
            {
                baseStats = ReadStats(stat => weapon.weaponData?.GetBaseStat(stat) ?? 0f);
                KnownWeaponBaseStats[eWeapon] = baseStats;
            }

            var currentStats = ReadStats(weapon.GetValue);

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

    private static StatValueEntry[] ReadStats(System.Func<EStat, float> getValue)
    {
        var result = new List<StatValueEntry>();
        foreach (var stat in AllStats)
        {
            float value = getValue(stat);
            if (value == 0f) continue; // skip stats this weapon doesn't use
            result.Add(new StatValueEntry { stat = stat.ToString(), value = value });
        }
        return result.ToArray();
    }
}
