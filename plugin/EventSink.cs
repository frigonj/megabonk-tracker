using System;
using System.IO;

namespace MegabonkTracker;

public static class EventSink
{
    private static readonly string LiveEventsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MegabonkTracker", "live_events.ndjson");

    private static readonly object WriteLock = new();

    public static void ResetForNewRun()
    {
        lock (WriteLock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LiveEventsPath)!);
            File.WriteAllText(LiveEventsPath, string.Empty);
        }
    }

    public static void Emit(TrackerEvent evt)
    {
        try
        {
            string line = SimpleJson.Serialize(evt);
            lock (WriteLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LiveEventsPath)!);
                File.AppendAllText(LiveEventsPath, line + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"EventSink.Emit failed: {ex}");
        }
    }
}
