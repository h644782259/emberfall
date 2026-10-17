using System;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Emberfall.Editor
{
    public static class WorldProjectionValidation
    {
        public static void RunAndExport() { Run(); IOSBuild.Export(); }
        public static void Run()
        {
            var type=typeof(GameSession).Assembly.GetType("Emberfall.WorldProjectionReview");
            var apply=type.GetMethod("Apply",BindingFlags.NonPublic|BindingFlags.Static);
            var restore=type.GetMethod("Restore",BindingFlags.NonPublic|BindingFlags.Static);
            var world=WorldBuilder.Build(ZoneKind.Wilderness);
            var camera=new GameObject("Lighting validation camera").AddComponent<Camera>();
            camera.transform.position=new Vector3(0,5.7541227f,-20.71851f);camera.transform.rotation=new Quaternion(.21643959f,0,0,.976296f);
            camera.fieldOfView=55;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;
            var target=new RenderTexture(1280,889,24);target.antiAliasing=2;camera.targetTexture=target;
            var pixels=new Texture2D(1280,889,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            string directory=Path.Combine(Path.GetTempPath(),"emberfall-world-projection");Directory.CreateDirectory(directory);
            var images=new Color32[2][];
            int previousPixelLights = QualitySettings.pixelLightCount;
            var previousShadows = QualitySettings.shadows;
            float previousShadowDistance = QualitySettings.shadowDistance;
            try
            {
                QualitySettings.pixelLightCount=1;QualitySettings.shadows=ShadowQuality.HardOnly;QualitySettings.shadowDistance=24;
                for(int mode=0;mode<2;mode++)
                {
                    int count=(int)apply.Invoke(null,new object[]{mode==1});
                    if(mode==1&&count==0)throw new Exception("No eligible world materials");
                    camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,889),0,0);pixels.Apply();
                    images[mode]=pixels.GetPixels32();File.WriteAllBytes(Path.Combine(directory,mode==0?"standard.png":"pruned.png"),pixels.EncodeToPNG());
                    Debug.Log("WORLD PROJECTION MATERIALS "+count);
                }
                long difference=0;int changed=0;
                for(int i=0;i<images[0].Length;i++)
                {var a=images[0][i];var b=images[1][i];int delta=Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);difference+=delta;if(delta>24)changed++;}
                string result="Normalized mean image difference="+(difference/(images[0].Length*3.0*255))+"; changed pixels="+changed+"/"+images[0].Length;
                File.WriteAllText(Path.Combine(directory,"result.txt"),result);Debug.Log(result);
                if(difference/(images[0].Length*3.0*255)>.005 || changed>images[0].Length*.02)throw new Exception("Projection image difference exceeded threshold");
                restore.Invoke(null,null);
                foreach(var material in Resources.FindObjectsOfTypeAll<Material>())
                    if(material.shader!=null&&material.shader.name=="Emberfall/Weathered World"&&material.GetFloat("_ProjectionFloor")!=-1)throw new Exception("Projection review did not restore");
            }
            finally
            {
                QualitySettings.pixelLightCount = previousPixelLights;
                QualitySettings.shadows = previousShadows;
                QualitySettings.shadowDistance = previousShadowDistance;
                restore.Invoke(null,null);RenderTexture.active=previous;
                UnityEngine.Object.DestroyImmediate(world);UnityEngine.Object.DestroyImmediate(camera.gameObject);
                target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
