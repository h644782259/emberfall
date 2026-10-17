using System;
using System.IO;
using UnityEngine;
namespace Emberfall.Editor
{ public static class SmallActorPrimitiveValidation
{
        public static void Run()
        {
            ValidateAt(1280,890);
            ValidateAt(1600,1112);
            ValidateAt(1920,1334);
        }
        private static void ValidateAt(int width,int height)
        {
            var root=new GameObject("Small primitive scene");
            var target=new RenderTexture(width,height,24){antiAliasing=2};
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            var type=typeof(CombatModel).Assembly.GetType("Emberfall.SmallActorPrimitiveReview");
            var apply=type.GetMethod("Apply",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            var dispose=type.GetMethod("Dispose",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            try
            {
                var models=new[]{CombatModel.Hero(root.transform,HeroClass.Vanguard),CombatModel.Hero(root.transform,HeroClass.Ranger),CombatModel.Hero(root.transform,HeroClass.Summoner),CombatModel.Enemy(root.transform,EnemyKind.Goblin,false),CombatModel.Enemy(root.transform,EnemyKind.Guardian,false),CombatModel.Enemy(root.transform,EnemyKind.Wisp,false)};
                for(int i=0;i<models.Length;i++)models[i].transform.position=new Vector3((i-2.5f)*1.4f,0,0);
                foreach(var transform in root.GetComponentsInChildren<Transform>())transform.gameObject.layer=30;
                foreach(var renderer in root.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
                    if(material!=null&&material.shader.name=="Emberfall/Actor Surface")
                    {material.EnableKeyword("EMBER_ACTOR_BAKED_RELIEF");material.SetTexture("_ReliefTex",Resources.Load<Texture2D>("WorldArt/ActorReliefAtlas"));}
                var light=new GameObject("Actor light").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.cullingMask=1<<30;light.intensity=1.3f;
                var camera=new GameObject("Actor comparison camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.transform.position=new Vector3(0,5,22);camera.transform.LookAt(new Vector3(0,1,0));camera.cullingMask=1<<30;camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.15f);
                string directory=Path.Combine(Path.GetTempPath(),"emberfall-small-actor",width.ToString());
                Debug.Log($"SMALL ACTOR RESOLUTION {width}x{height}");
                Directory.CreateDirectory(directory);
                for(int angle=0;angle<3;angle++)
                {
                    light.transform.rotation=Quaternion.Euler(25,150+angle*45,0);Color32[] before=null;
                    for(int mode=0;mode<2;mode++)
                    {
                        apply.Invoke(null,new object[]{camera,mode==1});
                        Debug.Log("SMALL ACTOR FILTERS "+type.GetProperty("ChangedFilters",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null));camera.Render();RenderTexture.active=target;
                        pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();var colors=pixels.GetPixels32();
                        File.WriteAllBytes(Path.Combine(directory,$"actors-{angle}-{mode}.png"),pixels.EncodeToPNG());
                        if(mode==0){before=colors;continue;}
                        long total=0;int changed=0;
                        for(int i=0;i<colors.Length;i++){int delta=Math.Abs(colors[i].r-before[i].r)+Math.Abs(colors[i].g-before[i].g)+Math.Abs(colors[i].b-before[i].b);total+=delta;if(delta>24)changed++;}
                        double mean=total/(colors.Length*765.0),fraction=changed/(double)colors.Length;
                        Debug.Log($"SMALL ACTOR SCENE {angle}: mean={mean}, changed={fraction}");
                        if(mean>.001||fraction>.003)throw new Exception("Small primitive image difference exceeds threshold");
                        ValidateActorRegions(models,camera,before,colors,target.width,target.height,angle);
                    }
                }
            }
            finally {dispose.Invoke(null,null);RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
        }

        private static void ValidateActorRegions(CombatModel[] models,Camera camera,Color32[] before,Color32[] after,int width,int height,int angle)
        {
            for(int actor=0;actor<models.Length;actor++)
            {
                bool found=false;Bounds bounds=default;
                foreach(var renderer in models[actor].GetComponentsInChildren<Renderer>())
                {
                    if(!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
                    if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);
                }
                if(!found)throw new Exception("Actor has no visible renderers");
                Vector2 low=new Vector2(width,height),high=Vector2.zero;
                for(int corner=0;corner<8;corner++)
                {
                    Vector3 point=camera.WorldToScreenPoint(bounds.center+Vector3.Scale(bounds.extents,
                        new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                    if(point.z<=camera.nearClipPlane)throw new Exception("Actor region crosses near plane");
                    low=Vector2.Min(low,point);high=Vector2.Max(high,point);
                }
                int x0=Mathf.Clamp(Mathf.FloorToInt(low.x)-2,0,width-1),x1=Mathf.Clamp(Mathf.CeilToInt(high.x)+2,0,width);
                int y0=Mathf.Clamp(Mathf.FloorToInt(low.y)-2,0,height-1),y1=Mathf.Clamp(Mathf.CeilToInt(high.y)+2,0,height);
                int count=(x1-x0)*(y1-y0),changed=0;long total=0;
                if(count<=0)throw new Exception("Empty actor region");
                for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
                {
                    int i=y*width+x;int delta=Math.Abs(after[i].r-before[i].r)+Math.Abs(after[i].g-before[i].g)+Math.Abs(after[i].b-before[i].b);
                    total+=delta;if(delta>24)changed++;
                }
                double mean=total/(count*765.0),fraction=changed/(double)count;
                Debug.Log($"SMALL ACTOR REGION {angle}/{actor}: pixels={count}, mean={mean}, changed={fraction}");
                if(mean>.015||fraction>.08)throw new Exception("Local actor region difference exceeds threshold");
            }
        }

}}
