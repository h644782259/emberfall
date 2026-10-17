using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall.Editor
{
    // Native image acceptance for an opt-in shader candidate, not a performance benchmark.
    public static class WorldSingleGrainAudit
    {
        private const int Width=1280, Height=889;
        private const string Keyword="EMBER_WORLD_SINGLE_GRAIN_REVIEW";
        public static void Run()
        {
            bool oldKeyword=Shader.IsKeywordEnabled(Keyword);
            var previous=RenderTexture.active;
            var oldShadows=QualitySettings.shadows;
            var camera=new GameObject("Single grain audit camera").AddComponent<Camera>();
            camera.enabled=false;camera.fieldOfView=48;camera.aspect=(float)Width/Height;
            camera.nearClipPlane=.1f;camera.farClipPlane=150;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.1f,.16f);
            var target=new RenderTexture(Width,Height,24);camera.targetTexture=target;
            var pixels=new Texture2D(Width,Height,TextureFormat.RGB24,false);
            var fixedTime=new CommandBuffer{name="Fixed shader animation"};
            fixedTime.SetGlobalVector("_Time",new Vector4(.5f,10f,20f,30f));
            camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,fixedTime);
            camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha,fixedTime);
            string output=Path.Combine(Path.GetTempPath(),"emberfall-single-grain-audit");Directory.CreateDirectory(output);
            GameObject world=null;
            try
            {
                QualitySettings.shadows=ShadowQuality.Disable;
                world=WorldBuilder.Build(ZoneKind.Wilderness);
                // Match the established mobile meadow; this experiment targets other world surfaces.
                var meadow=Resources.Load<Texture2D>("WorldArt/GroundMeadow");
                foreach(var material in Resources.FindObjectsOfTypeAll<Material>())
                    if(material.shader!=null && material.shader.name=="Emberfall/Weathered World")
                    {
                        material.SetFloat("_ProjectionFloor",.003f);
                        if(meadow!=null && material.mainTexture==meadow)material.EnableKeyword("EMBER_MEADOW_SIMPLE");
                    }
                var positions=new[]{new Vector3(0,5.7542f,-20.71866f),new Vector3(-18,8,-10),new Vector3(0,8,20)};
                for(int view=0;view<positions.Length;view++)
                {
                    camera.transform.position=positions[view];
                    if(view==0)camera.transform.rotation=Quaternion.Euler(25,0,0);else camera.transform.LookAt(Vector3.up);
                    Shader.DisableKeyword(Keyword);CaptureSettled(camera,pixels);var original=Capture(camera,pixels);
                    File.WriteAllBytes(Path.Combine(output,$"{view}-baseline.png"),pixels.EncodeToPNG());
                    Shader.EnableKeyword(Keyword);CaptureSettled(camera,pixels);var candidate=Capture(camera,pixels);
                    File.WriteAllBytes(Path.Combine(output,$"{view}-candidate.png"),pixels.EncodeToPNG());
                    Shader.DisableKeyword(Keyword);CaptureSettled(camera,pixels);var restored=Capture(camera,pixels);
                    long delta=0,restoreDelta=0;int changed=0,populated=0;
                    for(int i=0;i<original.Length;i++)
                    {
                        var a=original[i];var b=candidate[i];var c=restored[i];
                        int d=Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);
                        delta+=d;if(d>24)changed++;
                        restoreDelta+=Math.Abs(a.r-c.r)+Math.Abs(a.g-c.g)+Math.Abs(a.b-c.b);
                        if(!a.Equals(original[0]))populated++;
                    }
                    double mean=delta/(original.Length*3.0*255);
                    string row=$"SINGLE GRAIN AUDIT view={view} meanDifference={mean} changedPixels={changed} restoredDelta={restoreDelta} populated={populated}";
                    Debug.Log(row);File.AppendAllText(Path.Combine(output,"result.txt"),row+"\n");
                    if(populated<original.Length/20 || restoreDelta>24)throw new Exception("Invalid or unstable image comparison");
                    if(mean>.01 || changed>original.Length*.08)throw new Exception("Single grain visual difference exceeds candidate budget");
                }
            }
            finally
            {
                if(oldKeyword)Shader.EnableKeyword(Keyword);else Shader.DisableKeyword(Keyword);
                QualitySettings.shadows=oldShadows;RenderTexture.active=previous;
                if(world!=null)UnityEngine.Object.DestroyImmediate(world);
                UnityEngine.Object.DestroyImmediate(camera.gameObject);fixedTime.Release();target.Release();
                UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
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
