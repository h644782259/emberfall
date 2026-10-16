using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class ActorReliefBake
    {
        public static void BuildDevice() { Run(); Validate(); IOSBuild.Export(); }

        public static void Validate()
        {
            string directory = Path.Combine(Path.GetTempPath(), "emberfall-actor-relief");
            Directory.CreateDirectory(directory);
            var root = new GameObject("Actor relief comparison");
            var target = new RenderTexture(768, 512, 24);
            var pixels = new Texture2D(768, 512, TextureFormat.RGB24, false);
            var materials = new Material[3];
            var previous = RenderTexture.active;
            try
            {
                for (int i=0; i<3; i++)
                {
                    materials[i] = new Material(Resources.Load<Shader>("WorldArt/ActorSurface"));
                    materials[i].mainTexture = Resources.Load<Texture2D>("WorldArt/ActorMaterialAtlas");
                    materials[i].SetTexture("_ReliefTex", Resources.Load<Texture2D>("WorldArt/ActorReliefAtlas"));
                    materials[i].SetVector("_Region", i==0 ? new Vector4(.48f,.48f,.01f,.51f) : i==1 ? new Vector4(.48f,.48f,.51f,.01f) : new Vector4(.48f,.48f,.51f,.51f));
                    materials[i].SetFloat("_PatternScale", i==0 ? 2.2f : i==1 ? 1 : 1.5f);
                    materials[i].SetFloat("_Relief", i==0 ? 1.5f : i==1 ? .45f : 1.1f);
                    materials[i].SetFloat("_Metallic", i==2 ? .58f : 0);
                    materials[i].color = i==0 ? new Color(.18f,.45f,.7f) : i==1 ? new Color(.8f,.65f,.5f) : new Color(.6f,.65f,.7f);
                    var part=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    part.transform.SetParent(root.transform,false); part.layer=30;
                    part.transform.position=new Vector3((i-1)*1.3f,0,0);
                    part.GetComponent<Renderer>().sharedMaterial=materials[i];
                }
                var light=new GameObject("Relief light").AddComponent<Light>();
                light.transform.SetParent(root.transform); light.type=LightType.Directional;
                light.transform.rotation=Quaternion.Euler(30,-25,0);light.intensity=1.3f;light.cullingMask=1<<30;
                var camera=new GameObject("Relief camera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform);camera.transform.position=new Vector3(0,0,-2.6f);
                camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.1f,.12f,.15f);camera.targetTexture=target;
                Color32[] before=null;
                for(int variant=0;variant<2;variant++)
                {
                    foreach(var material in materials)
                        if(variant==0)material.DisableKeyword("EMBER_ACTOR_BAKED_RELIEF");else material.EnableKeyword("EMBER_ACTOR_BAKED_RELIEF");
                    camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,768,512),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(directory,variant==0?"before.png":"after.png"),pixels.EncodeToPNG());
                    var colors=pixels.GetPixels32();
                    if(variant==0)before=colors;
                    else
                    {
                        long total=0;int changed=0,max=0;
                        for(int i=0;i<colors.Length;i++)
                        {
                            int delta=Math.Max(Math.Abs(colors[i].r-before[i].r),Math.Max(Math.Abs(colors[i].g-before[i].g),Math.Abs(colors[i].b-before[i].b)));
                            total+=delta;max=Math.Max(max,delta);if(delta>3)changed++;
                        }
                        File.WriteAllText(Path.Combine(directory,"result.txt"),"mean max-channel error="+(double)total/colors.Length+" /255; pixels >3/255="+changed+"/"+colors.Length+"; max="+max+"\n");
                    }
                }
                foreach(var message in ShaderUtil.GetShaderMessages(materials[0].shader))
                    if(message.severity.ToString()=="Error")throw new Exception(message.message);
                Debug.Log("Actor relief comparison: "+directory);
            }
            finally
            {
                RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);
                foreach(var material in materials)if(material!=null)UnityEngine.Object.DestroyImmediate(material);
                target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            }
        }

        public static void Run()
        {
            const string path = "Assets/Resources/WorldArt/ActorReliefAtlas.png";
            var source = Resources.Load<Texture2D>("WorldArt/ActorMaterialAtlas");
            var shader = Shader.Find("Hidden/Emberfall/Actor Relief Bake");
            if (source == null || shader == null) throw new InvalidOperationException("Missing actor relief bake source.");
            var material = new Material(shader);
            var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, target, material);
                RenderTexture.active = target;
                result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                result.Apply();
                File.WriteAllBytes(path, result.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(result);
                UnityEngine.Object.DestroyImmediate(material);
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = source.width;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            Debug.Log("Baked actor relief atlas " + source.width + "x" + source.height);
        }
    }
}
