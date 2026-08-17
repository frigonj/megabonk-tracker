using System.Globalization;
using System.Text;

namespace MegabonkTracker;

// Minimal hand-written JSON writer for this plugin's fixed set of event DTOs.
// Avoids depending on Newtonsoft.Json, which BepInEx does not deploy alongside plugins.
public static class SimpleJson
{
    public static string Serialize(TrackerEvent evt) => evt switch
    {
        RunStartedEvent e => Obj(
            Field("type", e.type), Field("ts", e.ts),
            Field("character", e.character)),

        RunEndedEvent e => Obj(
            Field("type", e.type), Field("ts", e.ts),
            Field("outcome", e.outcome), Field("durationSeconds", e.durationSeconds)),

        UpgradePickedEvent e => Obj(
            Field("type", e.type), Field("ts", e.ts),
            Field("name", e.name), Field("level", e.level), Field("rarity", e.rarity),
            FieldRaw("statChanges", SerializeArray(e.statChanges, SerializeStatChange))),

        DamageSnapshotEvent e => Obj(
            Field("type", e.type), Field("ts", e.ts),
            Field("totalDamage", e.totalDamage), FieldRaw("sources", SerializeArray(e.sources, SerializeDamageEntry))),

        WeaponStatsSnapshotEvent e => Obj(
            Field("type", e.type), Field("ts", e.ts),
            FieldRaw("weapons", SerializeArray(e.weapons, SerializeWeaponStats))),

        EffectAppliedEvent e => Obj(
            Field("type", e.type), Field("ts", e.ts),
            Field("source", e.source), Field("effectType", e.effectType),
            Field("stat", e.stat), Field("modifyType", e.modifyType), Field("amount", e.amount),
            Field("permanent", e.permanent), Field("duration", e.duration), Field("isPositiveEffect", e.isPositiveEffect)),

        PlayerStatsSnapshotEvent e => Obj(
            Field("type", e.type), Field("ts", e.ts),
            FieldRaw("baseStats", SerializeArray(e.baseStats, SerializeStatValue)),
            FieldRaw("currentStats", SerializeArray(e.currentStats, SerializeStatValue))),

        RunCountersSnapshotEvent e => Obj(
            Field("type", e.type), Field("ts", e.ts),
            Field("gold", e.gold), Field("characterLevel", e.characterLevel),
            Field("banishesUsed", e.banishesUsed), Field("refreshesUsed", e.refreshesUsed), Field("skipsUsed", e.skipsUsed)),

        GamePausedEvent e => Obj(Field("type", e.type), Field("ts", e.ts)),

        GameResumedEvent e => Obj(Field("type", e.type), Field("ts", e.ts)),

        _ => "{}"
    };

    private static string SerializeArray<T>(T[] items, System.Func<T, string> serializeItem)
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < items.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(serializeItem(items[i]));
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static string SerializeDamageEntry(DamageEntry s) =>
        Obj(Field("source", s.source), Field("damage", s.damage), Field("level", s.level));

    private static string SerializeStatChange(StatChangeEntry s) =>
        Obj(Field("stat", s.stat), Field("modifyType", s.modifyType), Field("amount", s.amount));

    private static string SerializeStatValue(StatValueEntry s) =>
        Obj(Field("stat", s.stat), Field("value", s.value));

    private static string SerializeWeaponStats(WeaponStatsEntry w) =>
        Obj(
            Field("weapon", w.weapon), Field("level", w.level),
            FieldRaw("baseStats", SerializeArray(w.baseStats, SerializeStatValue)),
            FieldRaw("currentStats", SerializeArray(w.currentStats, SerializeStatValue)));

    private static string Obj(params string[] fields) => "{" + string.Join(",", fields) + "}";

    private static string Field(string key, string value) => $"\"{key}\":{Quote(value)}";
    private static string Field(string key, int value) => $"\"{key}\":{value.ToString(CultureInfo.InvariantCulture)}";
    private static string Field(string key, float value) => $"\"{key}\":{value.ToString(CultureInfo.InvariantCulture)}";
    private static string Field(string key, bool value) => $"\"{key}\":{(value ? "true" : "false")}";
    private static string FieldRaw(string key, string rawJson) => $"\"{key}\":{rawJson}";

    private static string Quote(string s)
    {
        if (s == null) return "null";
        var sb = new StringBuilder("\"");
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
