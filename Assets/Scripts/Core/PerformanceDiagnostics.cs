using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

namespace Emberfall
{
    // Bounded ten-second summaries, written off the main thread. No gameplay/save changes.
    internal sealed class PerformanceDiagnostics : MonoBehaviour
    {
        private static bool collecting;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCollection() { collecting=false; }
        internal enum Section { Interface, InterfaceUpdate, Player, Enemies, Models, Camera, Session, Text, Hotbar, HudQueries, Wander, Movement, Animation, TextSetup, TextDraw, EnemySpawn, EnemyMesh, CombatEffects, SurfaceBuild, FlameRide, FlameTrailBuild, ActorSlide }
        private static readonly double[] sectionTicks = new double[Enum.GetValues(typeof(Section)).Length], sectionMaxTicks = new double[sectionTicks.Length];
        internal readonly struct Scope : IDisposable
        {
            private readonly int section;
            private readonly long began;
            internal Scope(Section value) { section = (int)value; began = System.Diagnostics.Stopwatch.GetTimestamp(); }
            public void Dispose()
            {
                if (began == 0) return;
                long ticks = System.Diagnostics.Stopwatch.GetTimestamp() - began;
                sectionTicks[section] += ticks;
                sectionMaxTicks[section] = Math.Max(sectionMaxTicks[section], ticks);
            }
        }
        internal static Scope Measure(Section section) { return collecting ? new Scope(section) : default; }
        private static void ResetSections() { Array.Clear(sectionTicks, 0, sectionTicks.Length); Array.Clear(sectionMaxTicks, 0, sectionMaxTicks.Length); }

        [Serializable] private sealed class Sample
        {
            public string localTime, utcTime, scene, zone, panel, worstFramePanel, worstFrameUtc, device, graphics, version;
            public bool loading, paused, inputBlocked, renderCountersAvailable, allocationCounterAvailable;
            public string[] cpuSections;
            public double[] cpuMsPerFrame, cpuMaxCallMs;
            public int gcCollections;
            public int frames, over33Ms, over50Ms, over100Ms, enemies, width, height, targetFps, vSync;
            public double fps, meanMs, p95Ms, p99Ms, maxMs, meanDrawCalls, meanBatches, meanGcBytes, managedMemoryMB;
        }

        private readonly double[] timings = new double[4096];
        private int initialCollections;
        private int count, over33, over50, over100, renderCount, allocationCount;
        private double previous, elapsed, maximum, drawsTotal, batchesTotal, allocationsTotal;
        private string worstPanel, worstUtc, path;
        private GameSession session;
        private GameUI ui;
        private ProfilerRecorder draws, batches, allocations;
        private Task writer;
        private bool failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnableDiagnostics()
        {
            // Release gameplay must not continuously profile and write diagnostic files.
            // Explicit frame reviews retain the same instrumentation as prior comparisons.
            collecting = Debug.isDebugBuild || Environment.GetEnvironmentVariable("EMBERFALL_PERF_DIAGNOSTICS") == "1" ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW"));
            if (!collecting) return;
            var host = new GameObject("Local performance diagnostics");
            DontDestroyOnLoad(host);
            host.AddComponent<PerformanceDiagnostics>();
        }

        private void Start()
        {
            ResetSections(); initialCollections = GC.CollectionCount(0);
            session = GameSession.Instance;
            if (session != null) ui = session.GetComponent<GameUI>();
            path = Path.Combine(Application.persistentDataPath, "Performance", "performance.jsonl");
#if EMBERFALL_VISUAL_VALIDATION
            path = Path.Combine(VisualValidationPlayer.SaveDirectory, "Performance", "performance.jsonl");
#endif
            draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1);
            allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            Debug.Log("Local performance summaries: " + path);
        }

        private void LateUpdate()
        {
            if (failed) return;
            if (writer != null && writer.IsFaulted)
            {
                Debug.LogWarning("Performance logging stopped: " + writer.Exception.GetBaseException().Message);
                failed = true; return;
            }
            if (session == null) { session = GameSession.Instance; if (session != null) ui = session.GetComponent<GameUI>(); }
            if (session == null || session.BackgroundPaused)
            {
                previous = 0;
                count = over33 = over50 = over100 = renderCount = allocationCount = 0;
                elapsed = maximum = drawsTotal = batchesTotal = allocationsTotal = 0;
                worstPanel = null; ResetSections(); initialCollections = GC.CollectionCount(0);
                return;
            }
            double now = Time.realtimeSinceStartupAsDouble;
            if (previous == 0) { previous = now; return; }
            double ms = (now - previous) * 1000; previous = now;
            if (count == timings.Length) { Flush(); if (count == timings.Length) return; }
            timings[count++] = ms; elapsed += ms;
            if (ms > 33.34) over33++;
            if (ms > 50) over50++;
            if (ms > 100) over100++;
            if (ms > maximum) { maximum = ms; worstPanel = ui == null ? "Unknown" : ui.PerformancePanel; worstUtc = DateTime.UtcNow.ToString("o"); }
            if (draws.Valid && draws.Count > 0 && batches.Valid && batches.Count > 0)
            { drawsTotal += draws.LastValue; batchesTotal += batches.LastValue; renderCount++; }
            if (allocations.Valid && allocations.Count > 0) { allocationsTotal += allocations.LastValue; allocationCount++; }
            if (elapsed >= 10000) Flush();
        }

        private void Flush()
        {
            if (count == 0 || failed || session == null || (writer != null && !writer.IsCompleted)) return;
            Array.Sort(timings, 0, count);
            var means = new double[sectionTicks.Length]; var peaks = new double[sectionTicks.Length];
            for (int i = 0; i < means.Length; i++)
            { means[i] = sectionTicks[i] * 1000 / System.Diagnostics.Stopwatch.Frequency / count; peaks[i] = sectionMaxTicks[i] * 1000 / System.Diagnostics.Stopwatch.Frequency; }
            var sample = new Sample
            {
                cpuSections = Enum.GetNames(typeof(Section)), cpuMsPerFrame = means, cpuMaxCallMs = peaks, gcCollections = GC.CollectionCount(0) - initialCollections,
                localTime = DateTimeOffset.Now.ToString("o"), utcTime = DateTime.UtcNow.ToString("o"),
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, zone = session.ZoneName,
                panel = ui == null ? "Unknown" : ui.PerformancePanel, worstFramePanel = worstPanel, worstFrameUtc = worstUtc,
                device = SystemInfo.deviceModel, graphics = SystemInfo.graphicsDeviceName, version = Application.version,
                loading = session.SceneLoading, paused = session.Paused, inputBlocked = session.InputBlocked,
                frames = count, over33Ms = over33, over50Ms = over50, over100Ms = over100,
                enemies = session.Enemies.Count, width = Screen.width, height = Screen.height,
                targetFps = Application.targetFrameRate, vSync = QualitySettings.vSyncCount,
                fps = count * 1000 / elapsed, meanMs = elapsed / count, maxMs = maximum,
                p95Ms = timings[Math.Max(0, (int)Math.Ceiling(count * .95) - 1)],
                p99Ms = timings[Math.Max(0, (int)Math.Ceiling(count * .99) - 1)],
                renderCountersAvailable = renderCount > 0, allocationCounterAvailable = allocationCount > 0,
                meanDrawCalls = drawsTotal / Math.Max(1, renderCount), meanBatches = batchesTotal / Math.Max(1, renderCount),
                meanGcBytes = allocationsTotal / Math.Max(1, allocationCount), managedMemoryMB = GC.GetTotalMemory(false) / 1048576.0
            };
            string line = JsonUtility.ToJson(sample) + Environment.NewLine;
            string destination = path;
            writer = Task.Run(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                if (File.Exists(destination) && new FileInfo(destination).Length >= 2 * 1024 * 1024)
                {
                    string old = destination + ".previous";
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(destination, old);
                }
                File.AppendAllText(destination, line);
            });
            count = over33 = over50 = over100 = renderCount = allocationCount = 0;
            elapsed = maximum = drawsTotal = batchesTotal = allocationsTotal = 0;
            worstPanel = null; ResetSections(); initialCollections = GC.CollectionCount(0);
        }

        private void OnApplicationQuit()
        {
            try { if (writer != null) writer.Wait(1000); Flush(); if (writer != null) writer.Wait(1000); }
            catch (Exception error) { Debug.LogWarning("Performance log could not finish: " + error.Message); }
        }
        private void OnDestroy() { draws.Dispose(); batches.Dispose(); allocations.Dispose(); }
    }
}
