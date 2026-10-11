using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace Emberfall
{
    // Explicit local review only: no overlay, save changes, quality changes or
    // simulation overrides. Normal launches do not create this component.
    internal sealed class FrameReviewProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string tag, unity, device, graphics, quality;
            public int width, height, nativeWidth, nativeHeight, targetFrameRate, minTargetFrameRate, maxTargetFrameRate, frames, enemies, antialiasing, pixelLights;
            public float dpi, layoutScale, layoutWidth, layoutHeight, shadowDistance;
            public Rect safeArea;
            public double elapsedSeconds, fps, meanMs, p50Ms, p95Ms, p99Ms, maxMs;
            public bool renderCountersAvailable, allocationCounterAvailable;
            public double meanBatches, meanDrawCalls, meanSetPass, meanTriangles, meanGcBytes;
        }
        private readonly List<double> intervals = new List<double>(4096);
        private ProfilerRecorder batches, draws, passes, triangles, allocations;
        private string tag;
        private double began, previous, eligibleSince, sumBatches, sumDraws, sumPasses, sumTriangles, sumAllocations;
        private int renderSamples, allocationSamples;
        private int minTarget = int.MaxValue, maxTarget = int.MinValue;
        private bool recording;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnableRequestedReview()
        {
            string requested = Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW");
            if (string.IsNullOrEmpty(requested)) return;
            var name = new System.Text.StringBuilder();
            foreach (char c in requested) if (char.IsLetterOrDigit(c) || c == '-') name.Append(c);
            var probe = new GameObject("Explicit local frame review").AddComponent<FrameReviewProbe>();
            probe.tag = name.Length > 0 ? name.ToString() : "review";
            DontDestroyOnLoad(probe.gameObject);
        }
        private void Start()
        {
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1);
            draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            passes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1);
            triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
            allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        }
        private void LateUpdate()
        {
            var session = GameSession.Instance;
            if (session == null || !session.HasStarted || session.InputBlocked)
            { eligibleSince = 0; previous = 0; return; }
            double now = Time.realtimeSinceStartupAsDouble;
            if (!recording)
            {
                if (eligibleSince == 0) eligibleSince = now;
                if (now - eligibleSince < 3) return;
                recording = true; began = previous = now; return;
            }
            if (previous == 0) { previous = now; return; }
            intervals.Add((now - previous) * 1000); previous = now;
            minTarget = Math.Min(minTarget, Application.targetFrameRate);
            maxTarget = Math.Max(maxTarget, Application.targetFrameRate);
            if (batches.Valid && batches.Count > 0 && draws.Valid && draws.Count > 0 && passes.Valid && passes.Count > 0 && triangles.Valid && triangles.Count > 0)
            { sumBatches += batches.LastValue; sumDraws += draws.LastValue; sumPasses += passes.LastValue; sumTriangles += triangles.LastValue; renderSamples++; }
            if (allocations.Valid && allocations.Count > 0) { sumAllocations += allocations.LastValue; allocationSamples++; }
            if (intervals.Count >= 8192 || now - began >= 20) Save(session, now);
        }
        private void Save(GameSession session, double now)
        {
            double sum = 0; foreach (double value in intervals) sum += value;
            intervals.Sort();
            var layout = MobileControls.Layout;
            var report = new Report
            {
                tag = tag, unity = Application.unityVersion, device = SystemInfo.deviceModel,
                graphics = SystemInfo.graphicsDeviceName, quality = QualitySettings.shadows.ToString(),
                antialiasing = QualitySettings.antiAliasing, pixelLights = QualitySettings.pixelLightCount, shadowDistance = QualitySettings.shadowDistance,
                width = Screen.width, height = Screen.height, nativeWidth = Display.main.systemWidth, nativeHeight = Display.main.systemHeight,
                safeArea = Screen.safeArea, dpi = Screen.dpi,
                layoutScale = layout.Scale, layoutWidth = layout.Width, layoutHeight = layout.Height,
                targetFrameRate = Application.targetFrameRate, frames = intervals.Count, enemies = session.Enemies.Count,
                minTargetFrameRate = minTarget, maxTargetFrameRate = maxTarget,
                elapsedSeconds = now - began, fps = sum > 0 ? intervals.Count * 1000 / sum : 0,
                meanMs = sum / Math.Max(1, intervals.Count), p50Ms = Percentile(.5), p95Ms = Percentile(.95), p99Ms = Percentile(.99), maxMs = Percentile(1),
                renderCountersAvailable = renderSamples > 0, allocationCounterAvailable = allocationSamples > 0,
                meanBatches = sumBatches / Math.Max(1, renderSamples), meanDrawCalls = sumDraws / Math.Max(1, renderSamples),
                meanSetPass = sumPasses / Math.Max(1, renderSamples), meanTriangles = sumTriangles / Math.Max(1, renderSamples),
                meanGcBytes = sumAllocations / Math.Max(1, allocationSamples)
            };
            string directory = Path.Combine(Application.persistentDataPath, "Review");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "frame-review-" + tag + ".json"), JsonUtility.ToJson(report, true));
            Debug.Log("Explicit local frame review saved: " + tag + " | fps=" + report.fps.ToString("F2") + " | p95ms=" + report.p95Ms.ToString("F2"));
            Destroy(gameObject);
        }
        private double Percentile(double value)
        { return intervals.Count == 0 ? 0 : intervals[Math.Min(intervals.Count - 1, (int)Math.Ceiling(value * intervals.Count) - 1)]; }
        private void OnDestroy()
        { batches.Dispose(); draws.Dispose(); passes.Dispose(); triangles.Dispose(); allocations.Dispose(); }
    }
}
