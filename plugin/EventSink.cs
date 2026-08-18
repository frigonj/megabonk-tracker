using System;
using System.IO;
using System.Text;

namespace MegabonkTracker;

public static class EventSink
{
    private static readonly string LiveEventsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MegabonkTracker", "live_events.ndjson");

    private static readonly object WriteLock = new();

    // A FileStream is opened once and kept open for the plugin's lifetime instead of reopening on
    // every Emit() - the DamagePoller's 1s tick can write several events back to back (damage,
    // run counters, weapon stats, player stats, performance), and each open+seek+write+close was
    // real, measurable per-tick cost on the Unity main thread. Kept open across runs; ResetForNewRun
    // truncates it in place rather than closing/reopening, since the dashboard's tail loop detects
    // a new run by watching for the file size to shrink.
    private static FileStream _stream;

    private static FileStream GetStream()
    {
        if (_stream == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LiveEventsPath)!);
            _stream = new FileStream(LiveEventsPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        }
        return _stream;
    }

    public static void ResetForNewRun()
    {
        lock (WriteLock)
        {
            var stream = GetStream();
            stream.SetLength(0);
            stream.Seek(0, SeekOrigin.Begin);
        }
    }

    public static void Emit(TrackerEvent evt)
    {
        try
        {
            string line = SimpleJson.Serialize(evt) + "\n";
            byte[] bytes = Encoding.UTF8.GetBytes(line);
            lock (WriteLock)
            {
                var stream = GetStream();
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"EventSink.Emit failed: {ex}");
        }
    }
}
