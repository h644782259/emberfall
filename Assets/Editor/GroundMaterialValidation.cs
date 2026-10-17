using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace Emberfall.Editor
{
    public static class GroundMaterialValidation
    {
        public static void PrepareTexture()
        {
            const string directory="Assets/Resources/WorldArt/Review";
            const string path=directory+"/GroundMeadow512.png";
            if(!AssetDatabase.IsValidFolder(directory))AssetDatabase.CreateFolder("Assets/Resources/WorldArt","Review");
            if(!File.Exists(path)&&!AssetDatabase.CopyAsset("Assets/Resources/WorldArt/GroundMeadow.png",path))throw new Exception("Cannot prepare review texture");
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var settings=importer.GetPlatformTextureSettings("iPhone");
            settings.overridden=true;settings.maxTextureSize=512;settings.format=TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(settings);importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture.width!=512||texture.height!=512)throw new Exception("Review texture not512");
            Debug.Log("GROUND REVIEW TEXTURE "+texture.width+"x"+texture.height+" "+texture.format);
        }
        public static void RunAndExport(){Run();IOSBuild.Export();}
        public static void Run()
        {
            PrepareTexture();
            var type=typeof(WorldBuilder).Assembly.GetType("Emberfall.GroundMaterialReview");
            const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
            var apply=type.GetMethod("Apply",flags);var restore=type.GetMethod("Restore",flags);
            ValidatePlatformSelection(apply,restore);
            var world=WorldBuilder.Build(ZoneKind.Wilderness);
            var camera=new GameObject("Tree geometry validation camera").AddComponent<Camera>();
            camera.fieldOfView=55;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;
            var target=new RenderTexture(1920,1334,24){antiAliasing=2};camera.targetTexture=target;
            var pixels=new Texture2D(1920,1334,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            string directory=Path.Combine(Path.GetTempPath(),"emberfall-ground-review");Directory.CreateDirectory(directory);
            int lights=QualitySettings.pixelLightCount;var shadows=QualitySettings.shadows;float distance=QualitySettings.shadowDistance;
            try
            {
                QualitySettings.pixelLightCount=1;QualitySettings.shadows=ShadowQuality.HardOnly;QualitySettings.shadowDistance=24;
                foreach(int view in new[]{0,1,2})
                {
                    camera.transform.position=view==0?new Vector3(0,5.7541227f,-20.71851f):view==1?new Vector3(-8,5.7541227f,-12):new Vector3(9,5.7541227f,5);
                    camera.transform.rotation=new Quaternion(.21643959f,0,0,.976296f);
                    var images=new Color32[3][];
                    for(int mode=0;mode<3;mode++)
                    {
                        apply.Invoke(null,new object[]{mode});camera.Render();RenderTexture.active=target;
                        pixels.ReadPixels(new Rect(0,0,1920,1334),0,0);pixels.Apply();images[mode]=pixels.GetPixels32();
                        File.WriteAllBytes(Path.Combine(directory,$"view-{view}-{mode}.png"),pixels.EncodeToPNG());
                    }
                    for(int candidate=1;candidate<3;candidate++)
                    {long difference=0;int changed=0;
                    for(int i=0;i<images[0].Length;i++)
                    {var a=images[0][i];var b=images[candidate][i];int delta=Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);difference+=delta;if(delta>24)changed++;}
                    double mean=difference/(images[0].Length*3.0*255),fraction=changed/(double)images[0].Length;
                    Debug.Log($"GROUND IMAGE {view}/{candidate}: mean={mean} changedFraction={fraction}");
                    if(mean>.015||fraction>.08)throw new Exception("Ground image difference exceeded threshold");
                    }
                }
            }
            finally
            {
                QualitySettings.pixelLightCount=lights;QualitySettings.shadows=shadows;QualitySettings.shadowDistance=distance;
                restore.Invoke(null,null);RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(world);UnityEngine.Object.DestroyImmediate(camera.gameObject);
                target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
        private static void ValidatePlatformSelection(MethodInfo reviewApply,MethodInfo reviewRestore)
        {
            const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
            var profile=typeof(GraphicsQualityRuntime).GetField("current",flags);
            var configure=typeof(WorldBuilder).GetMethod("ApplyMeadowTexture",flags);
            object previous=profile.GetValue(null);
            var material=new Material(Resources.Load<Shader>("WorldArt/WeatheredWorld"));
            try
            {
                foreach(var display in new[]{GraphicsDisplayClass.Phone,GraphicsDisplayClass.Tablet,GraphicsDisplayClass.Unknown})
                foreach(var mode in new[]{GraphicsQualityMode.Smooth,GraphicsQualityMode.Balanced,GraphicsQualityMode.High})
                {
                    profile.SetValue(null,GraphicsQualityPolicy.Resolve(mode,new GraphicsDeviceCapabilities(true,4096,0,6,50,false,display)));
                    configure.Invoke(null,new object[]{material});
                    if(!material.IsKeywordEnabled("EMBER_MEADOW_SIMPLE"))throw new Exception("Mobile meadow variant missing");
                    reviewApply.Invoke(null,new object[]{0});
                    if(material.IsKeywordEnabled("EMBER_MEADOW_SIMPLE"))throw new Exception("Review baseline did not select original meadow");
                    reviewRestore.Invoke(null,null);
                    if(!material.IsKeywordEnabled("EMBER_MEADOW_SIMPLE"))throw new Exception("Review failed to restore mobile meadow default");
                }
                profile.SetValue(null,GraphicsQualityPolicy.Resolve(GraphicsQualityMode.High,new GraphicsDeviceCapabilities(false,16000,4096,8,50)));
                configure.Invoke(null,new object[]{material});
                if(material.IsKeywordEnabled("EMBER_MEADOW_SIMPLE"))throw new Exception("Desktop meadow unexpectedly simplified");
                Debug.Log("GROUND PLATFORM PASS: phone/tablet/unknown, all mobile tiers; desktop preserved; baseline override and restoration");
            }
            finally {reviewRestore.Invoke(null,null);profile.SetValue(null,previous);UnityEngine.Object.DestroyImmediate(material);}
        }
    }
}
