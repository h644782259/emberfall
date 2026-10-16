using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace Emberfall
{
    // Explicit local review only. The render-isolation tag temporarily hides
    // rendering categories and restores them; ordinary reviews are read-only.
    // Normal launches do not create this component or change rendering.
    internal sealed partial class FrameReviewProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string tag, unity, device, graphics, quality, savedUtc;
            public int width, height, nativeWidth, nativeHeight, targetFrameRate, minTargetFrameRate, maxTargetFrameRate, frames, enemies, antialiasing, pixelLights;
            public float dpi, layoutScale, layoutWidth, layoutHeight, shadowDistance;
            public Rect safeArea;
            public double elapsedSeconds, fps, meanMs, p50Ms, p95Ms, p99Ms, maxMs;
            public bool renderCountersAvailable, allocationCounterAvailable, runtimeInstancingDisabled, bakedActorReliefDisabled;
            public bool frameTimingEnabled;
            public int gpuTimingSamples, cpuTimingSamples, enabledDecorationTriangles;
            public double meanGpuMs, p95GpuMs, meanMainThreadMs, meanRenderThreadMs, meanMainThreadPresentWaitMs;
            public float decorationDistance;
            public double meanBatches, meanDrawCalls, meanSetPass, meanTriangles, meanGcBytes;
            public int activeRenderers, visibleRenderers, visibleMaterials, visibleMeshMaterialGroups, staticBatchedRenderers;
            public Vector3 cameraPosition, playerPosition;
            public Quaternion cameraRotation;
            public string renderIsolation;
            public string graphicsPreset;
            public int thermalState,minThermalState,maxThermalState;
        }
        private readonly FrameTiming[] timingBuffer = new FrameTiming[1];
        private readonly List<double> gpuTimes = new List<double>(8192);
        private ulong lastTimingTimestamp;
        private int cpuTimingSamples;
        private double mainThreadMs, renderThreadMs, presentWaitMs;
        private double reviewSeconds = 20;
        private readonly List<double> intervals = new List<double>(4096);
        private ProfilerRecorder batches, draws, passes, triangles, allocations;
        private string tag;
        private double began, previous, eligibleSince, sumBatches, sumDraws, sumPasses, sumTriangles, sumAllocations;
        private int renderSamples, allocationSamples;
        private int minTarget = int.MaxValue, maxTarget = int.MinValue;
        private bool recording;
        private bool renderComparison;
        private bool terrainComparison;
        private bool opaqueComparison;
        private int minThermal=99,maxThermal=-1;
        private bool autoContinue;
        private int savedSleepTimeout;
        private bool heldAwake;
        private int comparisonWindow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnableRequestedReview()
        {
            string requested = Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW");
            if (string.IsNullOrEmpty(requested)) return;
            var name = new System.Text.StringBuilder();
            foreach (char c in requested) if (char.IsLetterOrDigit(c) || c == '-') name.Append(c);
            var probe = new GameObject("Explicit local frame review").AddComponent<FrameReviewProbe>();
            probe.tag = name.Length > 0 ? name.ToString() : "review";
            // Opt-in device profiling uses the real title-screen action once;
            // never creates a role, selects quality, or bypasses scene loading.
            probe.autoContinue = Environment.GetEnvironmentVariable("EMBERFALL_PERF_AUTO_CONTINUE") == "1";
            if (probe.tag.StartsWith("performance-long-", StringComparison.Ordinal)) probe.reviewSeconds = 120;
            if (probe.tag == "performance-render-ab")
            {
                probe.renderComparison = true;
                probe.reviewSeconds = 30;
            }
            if(probe.tag == "performance-terrain-ab")
            {
                probe.terrainComparison=true;probe.reviewSeconds=45;
                Shader.EnableKeyword("EMBER_TERRAIN_LEGACY");
            }
            if(probe.tag=="performance-opaque-ab")
            {probe.opaqueComparison=true;probe.reviewSeconds=45;Shader.EnableKeyword("EMBER_WORLD_LEGACY_CLIP");}
            DontDestroyOnLoad(probe.gameObject);
        }
        private void Start()
        {
            savedSleepTimeout = Screen.sleepTimeout;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            heldAwake = true;
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1);
            draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            passes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1);
            triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
            allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        }
        private void LateUpdate()
        {
            var session = GameSession.Instance;
            if (autoContinue && session != null && session.Progression != null && !session.HasStarted && !session.SceneLoading)
            {
                var ui = session.GetComponent<GameUI>();
                if (ui != null) { autoContinue = false; ui.ContinueRecentAdventure(); }
                return;
            }
            if (session == null || !session.HasStarted || session.InputBlocked)
            { eligibleSince = 0; previous = 0; return; }
            double now = Time.realtimeSinceStartupAsDouble;
            if (!recording)
            {
                if (eligibleSince == 0) { eligibleSince = now; if(renderComparison) PrepareRenderIsolationWindow(); }
                EnforceRenderIsolation();
                if (now - eligibleSince < (renderComparison||terrainComparison||opaqueComparison ? (comparisonWindow == 0 ? 30 : 5) : 3)) return;
                recording = true; began = previous = now; return;
            }
            if (previous == 0) { previous = now; return; }
            EnforceRenderIsolation();
            CaptureWorkTimings();
            intervals.Add((now - previous) * 1000); previous = now;
            minThermal=Math.Min(minThermal,session.PerformanceThermalState);maxThermal=Math.Max(maxThermal,session.PerformanceThermalState);
            minTarget = Math.Min(minTarget, Application.targetFrameRate);
            maxTarget = Math.Max(maxTarget, Application.targetFrameRate);
            if (batches.Valid && batches.Count > 0 && draws.Valid && draws.Count > 0 && passes.Valid && passes.Count > 0 && triangles.Valid && triangles.Count > 0)
            { sumBatches += batches.LastValue; sumDraws += draws.LastValue; sumPasses += passes.LastValue; sumTriangles += triangles.LastValue; renderSamples++; }
            if (allocations.Valid && allocations.Count > 0) { sumAllocations += allocations.LastValue; allocationSamples++; }
            if (intervals.Count >= 8192 || now - began >= reviewSeconds) Save(session, now);
        }
        private void CaptureWorkTimings()
        {
            if(!FrameTimingManager.IsFeatureEnabled()) return;
            FrameTimingManager.CaptureFrameTimings();
            if(FrameTimingManager.GetLatestTimings(1,timingBuffer)==0) return;
            var timing=timingBuffer[0];
            if(timing.frameStartTimestamp==0 || timing.frameStartTimestamp==lastTimingTimestamp) return;
            lastTimingTimestamp=timing.frameStartTimestamp;
            if(timing.gpuFrameTime>0 && !double.IsNaN(timing.gpuFrameTime) && !double.IsInfinity(timing.gpuFrameTime)) gpuTimes.Add(timing.gpuFrameTime);
            if(timing.cpuMainThreadFrameTime>0)
            {
                cpuTimingSamples++; mainThreadMs+=timing.cpuMainThreadFrameTime;
                renderThreadMs+=timing.cpuRenderThreadFrameTime;presentWaitMs+=timing.cpuMainThreadPresentWaitTime;
            }
        }
        private void Save(GameSession session, double now)
        {
            double sum = 0; foreach (double value in intervals) sum += value;
            intervals.Sort();
            gpuTimes.Sort();double gpuSum=0;foreach(double time in gpuTimes)gpuSum+=time;
            var layout = MobileControls.Layout;
            var report = new Report
            {
                tag = renderComparison||terrainComparison||opaqueComparison ? tag + "-" + comparisonWindow : tag, unity = Application.unityVersion, device = SystemInfo.deviceModel,
                savedUtc = DateTime.UtcNow.ToString("o"),
                graphicsPreset=GraphicsQualityRuntime.Current.Mode.ToString(),thermalState=session.PerformanceThermalState,minThermalState=minThermal,maxThermalState=maxThermal,
                bakedActorReliefDisabled = Environment.GetEnvironmentVariable("EMBERFALL_DISABLE_BAKED_ACTOR_RELIEF") == "1",
                runtimeInstancingDisabled = Environment.GetEnvironmentVariable("EMBERFALL_DISABLE_RUNTIME_INSTANCING") == "1",
                cameraPosition = Camera.main == null ? Vector3.zero : Camera.main.transform.position,
                cameraRotation = Camera.main == null ? Quaternion.identity : Camera.main.transform.rotation,
                playerPosition = session.Player == null ? Vector3.zero : session.Player.transform.position,
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
                frameTimingEnabled=FrameTimingManager.IsFeatureEnabled(),gpuTimingSamples=gpuTimes.Count,cpuTimingSamples=cpuTimingSamples,
                meanGpuMs=gpuSum/Math.Max(1,gpuTimes.Count),p95GpuMs=gpuTimes.Count==0?0:gpuTimes[Math.Min(gpuTimes.Count-1,(int)Math.Ceiling(gpuTimes.Count*.95)-1)],
                meanMainThreadMs=mainThreadMs/Math.Max(1,cpuTimingSamples),meanRenderThreadMs=renderThreadMs/Math.Max(1,cpuTimingSamples),meanMainThreadPresentWaitMs=presentWaitMs/Math.Max(1,cpuTimingSamples),
                decorationDistance=GraphicsQualityRuntime.DetailDistance(GraphicsQualityRuntime.Current),
                meanBatches = sumBatches / Math.Max(1, renderSamples), meanDrawCalls = sumDraws / Math.Max(1, renderSamples),
                meanSetPass = sumPasses / Math.Max(1, renderSamples), meanTriangles = sumTriangles / Math.Max(1, renderSamples),
                meanGcBytes = sumAllocations / Math.Max(1, allocationSamples)
            };
            report.renderIsolation = renderComparison ? RenderIsolationMode : terrainComparison ? (comparisonWindow%2==0?"legacy-terrain":"analytic-terrain") : opaqueComparison ? (comparisonWindow%2==0?"legacy-clip":"opaque-fast") : "none";
            foreach(var world in FindObjectsByType<PreparedWorldDetailBudget>()) report.enabledDecorationTriangles+=world.VisibleTriangleCount;
            var materials = new HashSet<Material>();
            var meshGroups = new HashSet<(Mesh, Material)>();
            foreach (var renderer in FindObjectsByType<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                report.activeRenderers++;
                if(renderer.isPartOfStaticBatch)report.staticBatchedRenderers++;
                if (!renderer.isVisible) continue;
                report.visibleRenderers++;
                var filter = renderer.GetComponent<MeshFilter>();
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    materials.Add(material);
                    if (filter != null && filter.sharedMesh != null)
                        meshGroups.Add((filter.sharedMesh, material));
                }
            }
            report.visibleMaterials = materials.Count; report.visibleMeshMaterialGroups = meshGroups.Count;
            string directory = Path.Combine(Application.persistentDataPath, "Review");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "frame-review-" + report.tag + ".json"), JsonUtility.ToJson(report, true));
            Debug.Log("Explicit local frame review saved: " + tag + " | fps=" + report.fps.ToString("F2") + " | p95ms=" + report.p95Ms.ToString("F2"));
            if ((renderComparison||terrainComparison||opaqueComparison) && ++comparisonWindow < (renderComparison?5:4))
            {
                RestoreRenderIsolation();
                if(terrainComparison){if(comparisonWindow%2==0)Shader.EnableKeyword("EMBER_TERRAIN_LEGACY");else Shader.DisableKeyword("EMBER_TERRAIN_LEGACY");}
                if(opaqueComparison){if(comparisonWindow%2==0)Shader.EnableKeyword("EMBER_WORLD_LEGACY_CLIP");else Shader.DisableKeyword("EMBER_WORLD_LEGACY_CLIP");}
                gpuTimes.Clear();cpuTimingSamples=0;mainThreadMs=renderThreadMs=presentWaitMs=0;lastTimingTimestamp=0;
                minThermal=99;maxThermal=-1;
                intervals.Clear(); recording = false; eligibleSince = previous = 0;
                sumBatches = sumDraws = sumPasses = sumTriangles = sumAllocations = 0;
                renderSamples = allocationSamples = 0;
                minTarget = int.MaxValue; maxTarget = int.MinValue;
            }
            else Destroy(gameObject);
        }
        private double Percentile(double value)
        { return intervals.Count == 0 ? 0 : intervals[Math.Min(intervals.Count - 1, (int)Math.Ceiling(value * intervals.Count) - 1)]; }
        private void OnDestroy()
        { if(heldAwake)Screen.sleepTimeout=savedSleepTimeout; batches.Dispose(); draws.Dispose(); passes.Dispose(); triangles.Dispose(); allocations.Dispose(); RestoreRenderIsolation(); if(terrainComparison)Shader.DisableKeyword("EMBER_TERRAIN_LEGACY"); if(opaqueComparison)Shader.DisableKeyword("EMBER_WORLD_LEGACY_CLIP"); }
    }
}
