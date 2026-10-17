using System;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Emberfall.Editor
{
    public static class PreparedTerrainValidation
    {
        public static void Run()
        {
            var type=typeof(GameSession).Assembly.GetType("Emberfall.PreparedTerrainVertexData");
            var field=type.GetMethod("HeightGradient",BindingFlags.Static|BindingFlags.NonPublic);
            int checks=0;float maxHeight=0,maxSlope=0;
            bool enabled=WorldTerrain.Enabled,dungeon=WorldTerrain.Dungeon;float radius=WorldTerrain.Radius;
            string oldRequest=Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW");
            int lights=QualitySettings.pixelLightCount;var shadows=QualitySettings.shadows;float shadowDistance=QualitySettings.shadowDistance;
            float oldGlobal=Shader.GetGlobalFloat("_EmberPreparedTerrain");
            try
            {
                foreach(var zone in new[]{ZoneKind.Wilderness,ZoneKind.Dungeon})foreach(float extent in new[]{18f,42f,72f})
                {
                    WorldTerrain.Configure(zone,0,extent);
                    for(int i=-90;i<=90;i++)for(int j=-90;j<=90;j++)
                    {
                        Vector3 p=new Vector3(i*.513f,0,j*.497f),v=(Vector3)field.Invoke(null,new object[]{p});
                        float h=Mathf.Abs(v.x-WorldTerrain.Height(p));
                        float dx=(WorldTerrain.Height(p+Vector3.right*.02f)-WorldTerrain.Height(p-Vector3.right*.02f))/.04f;
                        float dz=(WorldTerrain.Height(p+Vector3.forward*.02f)-WorldTerrain.Height(p-Vector3.forward*.02f))/.04f;
                        float error=Mathf.Max(Mathf.Abs(dx-v.y),Mathf.Abs(dz-v.z));maxHeight=Mathf.Max(h,maxHeight);maxSlope=Mathf.Max(error,maxSlope);checks++;
                        if(h>.00001f||error>.001f)throw new Exception($"Terrain parity failed {zone}/{extent}/{p}: {h}/{error}");
                    }
                }
                Debug.Log($"PREPARED TERRAIN FIELD PASS {checks} points heightError={maxHeight} slopeError={maxSlope}");
                Environment.SetEnvironmentVariable("EMBERFALL_PERF_REVIEW","performance-terrain-precompute-ab");
                QualitySettings.pixelLightCount=1;QualitySettings.shadows=ShadowQuality.HardOnly;QualitySettings.shadowDistance=24;
                foreach(var zone in new[]{ZoneKind.Wilderness,ZoneKind.Dungeon})Render(zone);
            }
            finally
            {
                Environment.SetEnvironmentVariable("EMBERFALL_PERF_REVIEW",oldRequest);Shader.SetGlobalFloat("_EmberPreparedTerrain",oldGlobal);
                QualitySettings.pixelLightCount=lights;QualitySettings.shadows=shadows;QualitySettings.shadowDistance=shadowDistance;
                WorldTerrain.Configure(dungeon?ZoneKind.Dungeon:ZoneKind.Wilderness,enabled?0:1,radius);
            }
        }
        static void Render(ZoneKind zone)
        {
            var world=WorldBuilder.Build(zone);var camera=new GameObject("Terrain parity camera").AddComponent<Camera>();
            camera.transform.position=new Vector3(0,5.7541227f,-20.71851f);camera.transform.rotation=new Quaternion(.21643959f,0,0,.976296f);
            camera.fieldOfView=55;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;
            var target=new RenderTexture(1920,1334,24){antiAliasing=2};camera.targetTexture=target;
            var pixels=new Texture2D(1920,1334,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            string directory=Path.Combine(Path.GetTempPath(),"emberfall-prepared-terrain");Directory.CreateDirectory(directory);
            try
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();
                PreparedWorldDetailBudget.Prepare(world,GraphicsQualityPolicy.Resolve(GraphicsQualityMode.Balanced,new GraphicsDeviceCapabilities(true,4096,2048,6,50)));
                timer.Stop();long vertices=0;
                foreach(var filter in world.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null&&filter.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord2))vertices+=filter.sharedMesh.vertexCount;
                if(vertices==0)throw new Exception("No prepared terrain vertices");
                Debug.Log($"PREPARED TERRAIN STREAM {zone} vertices={vertices} bytes={vertices*16} allPreparationMs={timer.Elapsed.TotalMilliseconds}");
                var images=new Color32[2][];
                for(int mode=0;mode<2;mode++)
                {
                    Shader.SetGlobalFloat("_EmberPreparedTerrain",mode);camera.Render();RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,1920,1334),0,0);pixels.Apply();images[mode]=pixels.GetPixels32();
                    File.WriteAllBytes(Path.Combine(directory,$"{zone}-{mode}.png"),pixels.EncodeToPNG());
                }
                long difference=0;int changed=0;
                for(int i=0;i<images[0].Length;i++){var a=images[0][i];var b=images[1][i];int delta=Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);difference+=delta;if(delta>24)changed++;}
                double mean=difference/(images[0].Length*3.0*255),fraction=changed/(double)images[0].Length;
                Debug.Log($"PREPARED TERRAIN IMAGE {zone} mean={mean} changedFraction={fraction}");
                if(mean>.0001||fraction>.0005)throw new Exception("Terrain precompute image mismatch");
            }
            finally
            {
                Shader.SetGlobalFloat("_EmberPreparedTerrain",0);RenderTexture.active=previous;
                UnityEngine.Object.DestroyImmediate(world);UnityEngine.Object.DestroyImmediate(camera.gameObject);
                target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
