using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace Emberfall.Editor
{
    public static class WorldOpaqueValidation
    {
        public static void Run()
        {
            var shader=Resources.Load<Shader>("WorldArt/WeatheredWorld");
            if(shader==null||!shader.isSupported)throw new Exception("World shader unavailable");
            var material=new Material(shader);
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.layer=31;
            var renderer=quad.GetComponent<Renderer>();renderer.sharedMaterial=material;
            material.SetColor("_EmissionColor",Color.white);material.SetFloat("_TerrainFollow",0);
            var host=new GameObject("Opaque validation camera");var camera=host.AddComponent<Camera>();
            camera.enabled=false;camera.transform.position=new Vector3(0,0,-2);camera.orthographic=true;
            camera.orthographicSize=.6f;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var target=new RenderTexture(128,128,24);camera.targetTexture=target;
            var texture=new Texture2D(128,128,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            Func<Color32[]> capture=()=>{camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,128,128),0,0);texture.Apply();return texture.GetPixels32();};
            Action<Color32[],Color32[]> equal=(a,b)=>{for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)>1||Math.Abs(a[i].g-b[i].g)>1||Math.Abs(a[i].b-b[i].b)>1)throw new Exception("Opaque/fade shader changed pixel "+i);};
            try
            {
                Shader.EnableKeyword("EMBER_WORLD_LEGACY_CLIP");var opaque=capture();
                Shader.DisableKeyword("EMBER_WORLD_LEGACY_CLIP");equal(opaque,capture());
                var occlusion=quad.AddComponent<CameraOcclusionSurface>();
                typeof(CameraOcclusionSurface).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(occlusion,null);
                typeof(CameraOcclusionSurface).GetMethod("SetFade",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(occlusion,new object[]{true,1f});
                if(!renderer.sharedMaterial.IsKeywordEnabled("EMBER_WORLD_FADE"))throw new Exception("Occluder did not opt into fade shader");
                var faded=capture();Shader.EnableKeyword("EMBER_WORLD_LEGACY_CLIP");equal(faded,capture());
                int solid=0,remaining=0;for(int i=0;i<opaque.Length;i++){if(opaque[i].r>100)solid++;if(faded[i].r>100)remaining++;}
                if(solid<1000||remaining>=solid*.9f||remaining<=0)throw new Exception("Fade coverage was lost");
                CameraOcclusionSurface.RestoreAll();if(renderer.sharedMaterial!=material)throw new Exception("Opaque material was not restored");
                Shader.DisableKeyword("EMBER_WORLD_LEGACY_CLIP");equal(opaque,capture());
                Debug.Log("PASS: world opaque/fade pixel equivalence, real occluder fade and restoration; coverage "+remaining+"/"+solid);
            }
            finally
            {
                Shader.DisableKeyword("EMBER_WORLD_LEGACY_CLIP");RenderTexture.active=previous;
                UnityEngine.Object.DestroyImmediate(quad);UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
