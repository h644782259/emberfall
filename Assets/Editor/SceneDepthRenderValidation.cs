using System;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Emberfall.Editor
{
    // Render-level regression: onPreCull must produce valid depth on the first
    // consumer frame, rather than merely setting a flag after Unity chose passes.
    public static class SceneDepthRenderValidation
    {
        public static void Run()
        {
            const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
            var type=typeof(GameSession).Assembly.GetType("Emberfall.MobileSceneDepth");
            var reset=type.GetMethod("Reset",flags);var register=type.GetMethod("Register",flags);
            var before=type.GetMethod("BeforeCamera",flags);var after=type.GetMethod("AfterCamera",flags);
            reset.Invoke(null,null);
            Camera.CameraCallback pre=c=>before.Invoke(null,new object[]{c}),post=c=>after.Invoke(null,new object[]{c});
            var camera=new GameObject("First consumer depth camera").AddComponent<Camera>();camera.enabled=false;
            camera.transform.position=new Vector3(0,1.4f,-4);camera.transform.LookAt(new Vector3(0,.3f,0));
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.08f,.1f);camera.nearClipPlane=.1f;camera.farClipPlane=20;
            var target=new RenderTexture(640,480,24);camera.targetTexture=target;
            var pixels=new Texture2D(640,480,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=new Vector3(.4f,.25f,.15f);obstacle.transform.localScale=new Vector3(.8f,1.5f,.8f);
            var opaque=new Material(Shader.Find("Standard")){color=new Color(.3f,.32f,.35f)};obstacle.GetComponent<Renderer>().sharedMaterial=opaque;
            var effect=GameObject.CreatePrimitive(PrimitiveType.Sphere);effect.transform.localScale=Vector3.one*1.7f;
            var renderer=effect.GetComponent<Renderer>();register.Invoke(null,new object[]{renderer});
            var noise=new Texture3D(4,4,4,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
            var values=new Color[64];for(int i=0;i<64;i++){float n=.35f+(i%7)*.09f;values[i]=new Color(n,n,n,1);}noise.SetPixels(values);noise.Apply();
            string directory=Path.Combine(Path.GetTempPath(),"emberfall-scene-depth-render");Directory.CreateDirectory(directory);
            Material material=null;Mesh groundMesh=null;
            var oldFog=RenderSettings.fog;var oldAmbient=RenderSettings.ambientLight;var oldTime=Shader.GetGlobalVector("_Time");
            try
            {
                RenderSettings.fog=false;RenderSettings.ambientLight=Color.white;Shader.SetGlobalVector("_Time",Vector4.zero);
                foreach(string path in new[]{"FilledSpell","SpellEnergySurface","SwordWave","SkillFlowVolume","GroundFlameTrail"})
                {
                    if(path=="GroundFlameTrail")
                    {
                        camera.transform.position=new Vector3(0,3.4f,-4);camera.transform.LookAt(new Vector3(0,.2f,0));
                        groundMesh=new Mesh{name="Depth fixture flame ribbon"};var v=new Vector3[81];var uv=new Vector2[81];var support=new Vector3[81];var indices=new int[8*8*6];int n=0;
                        for(int row=0;row<9;row++)for(int col=0;col<9;col++){int k=row*9+col;float x=col/4f-1;v[k]=new Vector3(x*1.05f,0,row/4f-1);uv[k]=new Vector2(x,0);support[k]=new Vector3(1,1,0);}
                        for(int row=0;row<8;row++)for(int col=0;col<8;col++){int a=row*9+col,b=a+1,c=a+9,d=c+1;indices[n++]=a;indices[n++]=c;indices[n++]=b;indices[n++]=b;indices[n++]=c;indices[n++]=d;}
                        groundMesh.vertices=v;groundMesh.uv=uv;groundMesh.SetUVs(1,new System.Collections.Generic.List<Vector3>(support));groundMesh.triangles=indices;groundMesh.RecalculateBounds();
                        var bounds=groundMesh.bounds;bounds.Expand(Vector3.up*3.4f);groundMesh.bounds=bounds;
                        effect.GetComponent<MeshFilter>().sharedMesh=groundMesh;effect.transform.localScale=Vector3.one;
                    }
                    material=new Material(Resources.Load<Shader>(path));renderer.sharedMaterial=material;
                    material.SetFloat("_Opacity",1);material.SetFloat("_Age",.4f);material.SetFloat("_Clock",.4f);material.SetFloat("_Steps",8);material.SetTexture("_FlowNoise",noise);
                    if(path=="SpellEnergySurface")material.SetFloat("_Heat",0);
                    camera.depthTextureMode=DepthTextureMode.Depth;
                    renderer.enabled=false;var empty=Capture(camera,target,pixels);renderer.enabled=true;
                    var always=Capture(camera,target,pixels);
                    // Poison the global texture after releasing camera-owned depth.
                    // If onPreCull is too late, this renders differently from reference.
                    camera.depthTextureMode=DepthTextureMode.None;Shader.SetGlobalTexture("_CameraDepthTexture",Texture2D.blackTexture);
                    var missing=Capture(camera,target,pixels);
                    Shader.SetGlobalTexture("_CameraDepthTexture",Texture2D.blackTexture);
                    Camera.onPreCull+=pre;Camera.onPostRender+=post;
                    var demand=Capture(camera,target,pixels);
                    Camera.onPreCull-=pre;Camera.onPostRender-=post;
                    if(camera.depthTextureMode!=DepthTextureMode.None)throw new Exception("Depth flags leaked after render");
                    double mean=Difference(always,demand),visible=Difference(always,empty),negative=Difference(always,missing);
                    File.WriteAllBytes(Path.Combine(directory,path+"-demand.png"),pixels.EncodeToPNG());
                    Debug.Log($"DEPTH RENDER {path}: demandDifference={mean} visibleDifference={visible} missingDepthDifference={negative}");
                    if(visible<.00001)throw new Exception("Ineffective fixture: skill was invisible "+path);
                    if(negative<.000001)throw new Exception("Ineffective fixture: depth did not affect image "+path);
                    if(mean>.0001)throw new Exception("First-consumer image differs from always-depth "+path);
                    UnityEngine.Object.DestroyImmediate(material);material=null;
                }
            }
            finally
            {
                Camera.onPreCull-=pre;Camera.onPostRender-=post;reset.Invoke(null,null);
                RenderTexture.active=previous;RenderSettings.fog=oldFog;RenderSettings.ambientLight=oldAmbient;Shader.SetGlobalVector("_Time",oldTime);
                if(material!=null)UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(effect);UnityEngine.Object.DestroyImmediate(obstacle);UnityEngine.Object.DestroyImmediate(camera.gameObject);
                if(groundMesh!=null)UnityEngine.Object.DestroyImmediate(groundMesh);
                UnityEngine.Object.DestroyImmediate(opaque);UnityEngine.Object.DestroyImmediate(noise);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
        static Color32[] Capture(Camera camera,RenderTexture target,Texture2D pixels)
        {camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,480),0,0);pixels.Apply();return pixels.GetPixels32();}
        static double Difference(Color32[] a,Color32[] b)
        {long sum=0;for(int i=0;i<a.Length;i++)sum+=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);return sum/(a.Length*3.0*255);}
    }
}
