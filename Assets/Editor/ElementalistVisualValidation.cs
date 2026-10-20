using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace Emberfall.Editor
{
    // Runs production VFX without a save, combat scene, simulator, or gameplay changes.
    public static class ElementalistVisualValidation
    {
        [MenuItem("Emberfall/Validation/Elementalist visuals")]
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("EMBER_VFX_OUTPUT")??"/tmp/emberfall-elementalist-visuals";
            Directory.CreateDirectory(output);
            var type=typeof(PlayerController).Assembly.GetType("Emberfall.ElementalistSpellVisual",true);
            var kind=type.GetNestedType("Kind",BindingFlags.NonPublic);
            var constructor=type.GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(Transform),kind,typeof(float)},null);
            var draw=type.GetMethod("Draw",BindingFlags.Instance|BindingFlags.NonPublic);
            var dispose=type.GetMethod("Dispose",BindingFlags.Instance|BindingFlags.NonPublic);
            var root=new GameObject("Elementalist visual validation");
            var camera=root.AddComponent<Camera>();camera.transform.position=new Vector3(15,12,-22);camera.transform.LookAt(new Vector3(0,5,0));
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.065f);camera.fieldOfView=45;camera.depthTextureMode=DepthTextureMode.Depth;
            camera.nearClipPlane=.1f;camera.farClipPlane=80;
            var rt=new RenderTexture(768,768,24);camera.targetTexture=rt;
            var texture=new Texture2D(768,768,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.3f,0);floor.transform.localScale=new Vector3(25,.5f,25);
            var floorMat=new Material(Shader.Find("Standard"));floorMat.color=new Color(.16f,.19f,.23f);floor.GetComponent<Renderer>().sharedMaterial=floorMat;
            var light=new GameObject("Validation key").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-35,0);light.intensity=1.2f;
            try
            {
                foreach(string name in new[]{"Pressure","Lift","Laser","Meteor","Frost"})
                {
                    var parent=new GameObject(name);object visual=constructor.Invoke(new object[]{parent.transform,Enum.Parse(kind,name),name=="Meteor"?.9f:3f});
                    Component frost=null;
                    if(name=="Frost")
                    {
                        dispose.Invoke(visual,null);visual=null;
                        var frostType=typeof(PlayerController).Assembly.GetType("Emberfall.FrostLaneVisual",true);
                        frostType.GetMethod("Spawn",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{parent.transform,null,Vector3.forward,5.2f,9f,2f});
                        frost=parent.GetComponentInChildren(frostType);
                        frostType.GetMethod("Draw",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(frost,new object[]{.85f});
                    }
                    if(name=="Meteor")
                    {
                        var assets=typeof(PlayerController).Assembly.GetType("Emberfall.HeroSkillBodyAssets",true);
                        var rock=new GameObject("Production meteor rock");rock.transform.SetParent(parent.transform,false);
                        rock.AddComponent<MeshFilter>().sharedMesh=(Mesh)assets.GetProperty("Rock",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                        var material=new Material(Resources.Load<Shader>("HeroSkillBody"));material.SetColor("_Tint",new Color(.32f,.12f,.055f));
                        var library=typeof(PlayerController).Assembly.GetType("Emberfall.SkillBodyMaterialLibrary",true);library.GetMethod("Bind",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{material});material.SetFloat("_Heat",.8f);
                        rock.AddComponent<MeshRenderer>().sharedMaterial=material;
                    }
                    try
                    {
                        if(visual!=null)draw.Invoke(visual,new object[]{.85f,1f});
                        if(name=="Pressure")
                        {
                            var core=parent.GetComponentInChildren<MeshRenderer>();
                            foreach(var renderer in parent.GetComponentsInChildren<MeshRenderer>())
                            {
                                if(renderer.name=="Fractured rotating gravity planet"&&renderer.bounds.min.y<2.47f)throw new Exception("Gravity core blocks grounded actors");
                                if(renderer.name=="Fractured ground relief")throw new Exception("Pressure retains ground rocks");
                            }
                        }
                        if(name=="Laser")foreach(var renderer in parent.GetComponentsInChildren<MeshRenderer>())if(renderer.name.Contains("basalt")||renderer.name.Contains("ground relief"))throw new Exception("Laser retains rocks");
                        foreach(bool ortho in new[]{false,true})
                        {
                            camera.orthographic=ortho;camera.orthographicSize=10;
                            camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,768,768),0,0);texture.Apply();
                            File.WriteAllBytes(Path.Combine(output,name+(ortho?"-ortho":"")+".png"),texture.EncodeToPNG());
                        }
                    }
                    finally{if(visual!=null)dispose.Invoke(visual,null);UnityEngine.Object.DestroyImmediate(parent);}
                }
                foreach(string key in new[]{"ElementalistVolume","ElementalistMatter","HeroSkillBody","LaserFootprint"})
                {
                    var shader=Resources.Load<Shader>(key);
                    if(shader==null||!shader.isSupported||ShaderUtil.ShaderHasError(shader))throw new Exception("Shader failed: "+key);
                    foreach(var message in ShaderUtil.GetShaderMessages(shader))if(message.severity.ToString()=="Error")throw new Exception(key+": "+message.message);
                }
                File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: production visual construction, perspective and orthographic render, native shader checks. Not a gameplay or mobile device test.");
            }
            finally
            {
                RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(light.gameObject);
                UnityEngine.Object.DestroyImmediate(floorMat);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
