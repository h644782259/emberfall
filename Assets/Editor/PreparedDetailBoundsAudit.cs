using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall.Editor
{
    public static class PreparedDetailBoundsAudit
    {
        private const int Width = 960, Height = 667;
        public static void Run() => RunAudit(false);
        public static void RunShadowed() => RunAudit(true);
        private static void RunAudit(bool shadowed)
        {
            PreparedDetailBoundsValidation.Run();
            string oldReview = Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW");
            string oldBounds = Environment.GetEnvironmentVariable("EMBERFALL_REVIEW_TIGHT_DETAIL_BOUNDS");
            Environment.SetEnvironmentVariable("EMBERFALL_PERF_REVIEW", "detail-bounds-native-audit");
            Environment.SetEnvironmentVariable("EMBERFALL_REVIEW_TIGHT_DETAIL_BOUNDS", "1");
            GameObject world = null;
            var camera = new GameObject("Isolated bounds audit camera").AddComponent<Camera>(); camera.enabled = false;
            camera.fieldOfView = 48; camera.aspect = 960f / 667; camera.nearClipPlane = .1f; camera.farClipPlane = 150;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.065f,.1f,.16f);
            var target = new RenderTexture(Width, Height, 24); camera.targetTexture = target;
            var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var previousTarget = RenderTexture.active; var previousShadows = QualitySettings.shadows;
            float previousShadowDistance = QualitySettings.shadowDistance;
            var rows = new List<string>();
            // Deterministic shader animation for image comparison, including shadow passes.
            var fixedTime = new CommandBuffer { name = "Bounds audit fixed shader time" };
            fixedTime.SetGlobalVector("_Time",new Vector4(.5f,10f,20f,30f));
            camera.AddCommandBuffer(CameraEvent.BeforeDepthTexture,fixedTime);
            camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,fixedTime);
            camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha,fixedTime);
            try
            {
                QualitySettings.shadows = shadowed ? ShadowQuality.All : ShadowQuality.Disable;
                QualitySettings.shadowDistance = 34;
                world = WorldBuilder.Build(ZoneKind.Wilderness);
                foreach(var light in world.GetComponentsInChildren<Light>())
                    light.AddCommandBuffer(LightEvent.BeforeShadowMap,fixedTime);
                PreparedWorldDetailBudget.Prepare(world, GraphicsQualityPolicy.Resolve(GraphicsQualityMode.Balanced, new GraphicsDeviceCapabilities(true,4096,0,6,50)));
                var chunks = new List<MeshRenderer>(); var compact = new List<Bounds>(); var legacy = new List<Bounds>();
                foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!renderer.name.StartsWith("Prepared decoration ", StringComparison.Ordinal)) continue;
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    var old = mesh.bounds; var scale = renderer.transform.lossyScale;
                    old.Expand(new Vector3(3 / Mathf.Max(.01f,Mathf.Abs(scale.x)),8 / Mathf.Max(.01f,Mathf.Abs(scale.y)),3 / Mathf.Max(.01f,Mathf.Abs(scale.z))));
                    chunks.Add(renderer); compact.Add(renderer.localBounds); legacy.Add(old);
                }
                if (chunks.Count == 0) throw new Exception("No prepared decorative chunks in audit");
                var positions = new[] {new Vector3(0,5.7542f,-20.71866f),new Vector3(0,19,-24),new Vector3(-18,8,-10),new Vector3(18,8,-10),new Vector3(0,8,20),new Vector3(-20,8,0),new Vector3(20,8,0),new Vector3(-14,12,14),new Vector3(14,12,14)};
                for (int view = 0; view < positions.Length; view++)
                {
                    camera.transform.position = positions[view];
                    if(view==0)camera.transform.rotation=Quaternion.Euler(25,0,0); else camera.transform.LookAt(Vector3.up);
                    var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                    long oldTriangles = 0, newTriangles = 0;
                    for (int i=0;i<chunks.Count;i++)
                    {
                        var renderer=chunks[i]; long triangles=renderer.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3;
                        renderer.localBounds=legacy[i]; if(GeometryUtility.TestPlanesAABB(planes,renderer.bounds))oldTriangles+=triangles;
                    }
                    // Warm the render state before comparing bounds; first capture can differ.
                    CaptureSettled(camera,pixels);
                    var control=Capture(camera,pixels);
                    var original=Capture(camera,pixels);
                    Save(pixels,view,"original");
                    for(int i=0;i<chunks.Count;i++)
                    {
                        var renderer=chunks[i];renderer.localBounds=compact[i];
                        if(GeometryUtility.TestPlanesAABB(planes,renderer.bounds))newTriangles+=renderer.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3;
                    }
                    // Bounds updates can leave one transient render; compare settled state on both sides.
                    CaptureSettled(camera,pixels);
                    var changed=Capture(camera,pixels);
                    var candidateRepeat=Capture(camera,pixels);
                    for(int i=0;i<chunks.Count;i++)chunks[i].localBounds=legacy[i];
                    CaptureSettled(camera,pixels);
                    var restored=Capture(camera,pixels);
                    pixels.SetPixels32(changed);pixels.Apply();
                    Save(pixels,view,"candidate");
                    LogDifferences(view,"candidate",original,changed);
                    LogDifferences(view,"candidate-repeat",changed,candidateRepeat);
                    LogDifferences(view,"restored",original,restored);
                    long controlDifference=0; int occupied=0;
                    for(int i=0;i<original.Length;i++)
                    {
                        controlDifference+=Math.Abs(original[i].r-control[i].r)+Math.Abs(original[i].g-control[i].g)+Math.Abs(original[i].b-control[i].b);
                        if(!original[i].Equals(original[0]))occupied++;
                    }
                    if(occupied<original.Length/20)throw new Exception("Audit image is blank or insufficiently populated");
                    long difference=0;
                    for(int i=0;i<original.Length;i++)difference+=Math.Abs(original[i].r-changed[i].r)+Math.Abs(original[i].g-changed[i].g)+Math.Abs(original[i].b-changed[i].b);
                    double mean=difference/(original.Length*3.0*255);
                    string row=$"DETAIL BOUNDS AUDIT shadows={shadowed} view={view} chunks={chunks.Count} frustumTriangles={oldTriangles}->{newTriangles} pixelDifference={mean} controlDifference={controlDifference/(original.Length*3.0*255)} populatedPixels={occupied}";
                    rows.Add(row);Debug.Log(row);
                    AssertEquivalent(original,control,"baseline",view);
                    AssertEquivalent(original,changed,"candidate",view);
                    AssertEquivalent(original,candidateRepeat,"candidate repeat",view);
                    AssertEquivalent(original,restored,"restored",view);
                    File.WriteAllLines(Path.Combine(Path.GetTempPath(),"emberfall-detail-bounds-audit.txt"),rows);
                }
                File.WriteAllLines(Path.Combine(Path.GetTempPath(),"emberfall-detail-bounds-audit.txt"),rows);
            }
            finally
            {
                Environment.SetEnvironmentVariable("EMBERFALL_PERF_REVIEW",oldReview);
                Environment.SetEnvironmentVariable("EMBERFALL_REVIEW_TIGHT_DETAIL_BOUNDS",oldBounds);
                QualitySettings.shadows=previousShadows;QualitySettings.shadowDistance=previousShadowDistance;RenderTexture.active=previousTarget;
                if(world!=null)UnityEngine.Object.DestroyImmediate(world);
                UnityEngine.Object.DestroyImmediate(camera.gameObject);fixedTime.Release();target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
        private static void AssertEquivalent(Color32[] a,Color32[] b,string label,int view)
        {
            // Permit at most eight one-code-value pixel fluctuations in a 640k-pixel
            // image. This is an explicit visual tolerance, not bit-identical rendering.
            // Geometry containment is independently checked before this image audit.
            int count=0;
            for(int i=0;i<a.Length;i++)
            {
                int r=Math.Abs(a[i].r-b[i].r),g=Math.Abs(a[i].g-b[i].g),blue=Math.Abs(a[i].b-b[i].b);
                if(r+g+blue==0)continue;
                if(++count>8 || Math.Max(r,Math.Max(g,blue))>1)
                    throw new Exception($"Bounds visual tolerance exceeded: view={view} variant={label} pixel={i%Width},{i/Width}");
            }
        }
        private static void LogDifferences(int view,string label,Color32[] a,Color32[] b)
        {
            int count=0,max=0;long total=0;var samples=new System.Text.StringBuilder();
            for(int i=0;i<a.Length;i++)
            {
                int r=Math.Abs(a[i].r-b[i].r),g=Math.Abs(a[i].g-b[i].g),blue=Math.Abs(a[i].b-b[i].b);
                if(r+g+blue==0)continue;
                count++;total+=r+g+blue;max=Math.Max(max,Math.Max(r,Math.Max(g,blue)));
                if(count<=12)samples.Append($" ({i%Width},{i/Width}):{a[i]}->{b[i]}");
            }
            Debug.Log($"BOUNDS PIXELS view={view} variant={label} changed={count} maxChannelDelta={max} total={total} samples={samples}");
        }
        private static void Save(Texture2D pixels,int view,string variant)
        {File.WriteAllBytes(Path.Combine(Path.GetTempPath(),$"emberfall-bounds-{view}-{variant}.png"),pixels.EncodeToPNG());}
        private static void CaptureSettled(Camera camera,Texture2D pixels)
        {
            var previous=Capture(camera,pixels);int stable=0;
            for(int attempt=0;attempt<8;attempt++)
            {
                var current=Capture(camera,pixels);bool equal=true;
                for(int i=0;i<current.Length;i++)if(!current[i].Equals(previous[i])){equal=false;break;}
                stable=equal?stable+1:0;
                if(stable>=2)return;
                previous=current;
            }
            throw new Exception("Audit renderer did not settle within eight captures");
        }
        private static Color32[] Capture(Camera camera,Texture2D pixels)
        {camera.Render();RenderTexture.active=camera.targetTexture;pixels.ReadPixels(new Rect(0,0,Width,Height),0,0);pixels.Apply();return pixels.GetPixels32();}
    }
}
