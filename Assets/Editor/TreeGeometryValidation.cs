using System;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Emberfall.Editor
{
    public static class TreeGeometryValidation
    {
        public static void RunAndExport(){Run();IOSBuild.Export();}
        public static void Run()
        {
            const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
            var factory=typeof(WorldBuilder).GetMethod("CreateNaturalTreeMeshes",flags);
            for(int variant=0;variant<3;variant++)
            {
                var original=(Mesh[])factory.Invoke(null,new object[]{variant,false});
                var compact=(Mesh[])factory.Invoke(null,new object[]{variant,true});
                try
                {
                    var a=original[0].vertices;var b=compact[0].vertices;
                    for(int i=0;i<2*(7*12+2);i++)if(a[i]!=b[i])throw new Exception("Main trunk changed");
                    for(int k=0;k<2;k++)
                    {
                        if(compact[k].triangles.Length>=original[k].triangles.Length)throw new Exception("No geometry reduction");
                        var v=compact[k].vertices;var triangles=compact[k].triangles;
                        var normals=compact[k].normals;foreach(var index in triangles)if(!float.IsFinite(normals[index].sqrMagnitude)||normals[index].sqrMagnitude<.5f)throw new Exception("Invalid normals");
                        for(int i=0;i<triangles.Length;i+=3)
                            if(Vector3.Cross(v[triangles[i+1]]-v[triangles[i]],v[triangles[i+2]]-v[triangles[i]]).sqrMagnitude<1e-16f)throw new Exception("Degenerate tree face");
                        Debug.Log($"TREE GEOMETRY {variant}/{k}: {original[k].triangles.Length/3} -> {compact[k].triangles.Length/3}");
                    }
                }
                finally{foreach(var m in original)UnityEngine.Object.DestroyImmediate(m);foreach(var m in compact)UnityEngine.Object.DestroyImmediate(m);}
            }
            var apply=typeof(WorldBuilder).GetMethod("ApplyTreeGeometryReview",flags);
            var dispose=typeof(WorldBuilder).GetMethod("DisposeTreeGeometryReview",flags);
            var world=WorldBuilder.Build(ZoneKind.Wilderness);
            var camera=new GameObject("Tree geometry validation camera").AddComponent<Camera>();
            camera.fieldOfView=55;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;
            var target=new RenderTexture(1920,1334,24){antiAliasing=2};camera.targetTexture=target;
            var pixels=new Texture2D(1920,1334,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            string directory=Path.Combine(Path.GetTempPath(),"emberfall-tree-geometry");Directory.CreateDirectory(directory);
            int lights=QualitySettings.pixelLightCount;var shadows=QualitySettings.shadows;float distance=QualitySettings.shadowDistance;
            try
            {
                QualitySettings.pixelLightCount=1;QualitySettings.shadows=ShadowQuality.HardOnly;QualitySettings.shadowDistance=24;
                foreach(int view in new[]{0,1,2})
                {
                    camera.transform.position=view==0?new Vector3(0,5.7541227f,-20.71851f):view==1?new Vector3(-8,5.7541227f,-12):new Vector3(9,5.7541227f,5);
                    camera.transform.rotation=new Quaternion(.21643959f,0,0,.976296f);
                    var images=new Color32[2][];
                    for(int mode=0;mode<2;mode++)
                    {
                        apply.Invoke(null,new object[]{mode==1});camera.Render();RenderTexture.active=target;
                        pixels.ReadPixels(new Rect(0,0,1920,1334),0,0);pixels.Apply();images[mode]=pixels.GetPixels32();
                        File.WriteAllBytes(Path.Combine(directory,$"view-{view}-{mode}.png"),pixels.EncodeToPNG());
                    }
                    long difference=0;int changed=0;
                    for(int i=0;i<images[0].Length;i++)
                    {var a=images[0][i];var b=images[1][i];int delta=Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);difference+=delta;if(delta>24)changed++;}
                    double mean=difference/(images[0].Length*3.0*255),fraction=changed/(double)images[0].Length;
                    Debug.Log($"TREE IMAGE {view}: mean={mean} changedFraction={fraction}");
                    if(mean>.005||fraction>.02)throw new Exception("Tree image difference exceeded threshold");
                }
            }
            finally
            {
                QualitySettings.pixelLightCount=lights;QualitySettings.shadows=shadows;QualitySettings.shadowDistance=distance;
                dispose.Invoke(null,null);RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(world);UnityEngine.Object.DestroyImmediate(camera.gameObject);
                target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
