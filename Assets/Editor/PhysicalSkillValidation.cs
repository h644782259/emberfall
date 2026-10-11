using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Emberfall.Editor
{
    public static class PhysicalSkillValidation
    {
        private static readonly List<string> checks=new List<string>();
        private static readonly Type Motion=typeof(PlayerController).Assembly.GetType("Emberfall.PhysicalSkillMotion",true);
        private static object Invoke(string name,params object[] args)=>Motion.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
        private static Mesh BodyMesh(string name)=>(Mesh)typeof(PlayerController).Assembly.GetType("Emberfall.HeroSkillBodyAssets",true).GetProperty(name,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        private static bool BodyContact(Vector3 a,Vector3 b,Vector3 feet,float height,float r,float pr,out float fraction){object[] args={a,b,feet,height,r,pr,0f};bool result=(bool)Invoke("BodyContact",args);fraction=(float)args[6];return result;}
        private static Vector3 BallisticPosition(Vector3 a,Vector3 b,float t,float end)=>(Vector3)Invoke("BallisticPosition",a,b,t,end);
        private static Vector3 BallisticVelocity(Vector3 a,Vector3 b,float t,float end)=>(Vector3)Invoke("BallisticVelocity",a,b,t,end);
        private static void Advance(ref Vector3 p,ref Vector3 v,float r,float t){object[] args={p,v,r,t,.3f,.65f};Invoke("Advance",args);p=(Vector3)args[0];v=(Vector3)args[1];}
        private static void AdvanceSupported(ref Vector3 p,ref Vector3 v,ref int revision,float r,float t){object[] args={p,v,revision,r,t,.3f,.65f};Invoke("AdvanceSupported",args);p=(Vector3)args[0];v=(Vector3)args[1];revision=(int)args[2];}
        private static Vector3 ResolveContact(Vector3 velocity,Vector3 normal){object[] args={velocity,normal,.3f,.65f,.016f};Invoke("ResolveContact",args);return (Vector3)args[0];}
        private static bool ClearFlight(Vector3 a,Vector3 b,float r)=>(bool)Invoke("ClearFlight",a,b,r,null);
        private static bool BoxContact(Vector3 a,Vector3 b,Vector3 c,Vector3 size,float r,out float fraction){object[] args={a,b,c,size,r,0f};bool result=(bool)Invoke("BoxContact",args);fraction=(float)args[5];return result;}
        private static bool SweepScenery(Vector3 a,Vector3 b,float r,out RaycastHit hit){object[] args={a,b,r,default(RaycastHit)};bool result=(bool)Invoke("SweepScenery",args);hit=(RaycastHit)args[3];return result;}

        private static void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);checks.Add("PASS "+label);}
        public static void Run()
        {
            try
            {
                foreach(string key in new[]{"HeroSkillBody","PhysicalContactSurface","SpellSolidSurface","SpellEnergySurface","FilledSpell","LightningDischarge","BlackHoleLens","BlackHoleLensMobile","GroundSkillSurface"})
                {var shader=Resources.Load<Shader>(key);Check(shader!=null&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),key+" native graphics shader");}
                foreach(string key in new[]{"SkillMatter/NaturalMatterAtlas","SkillMatter/NaturalFlame"})Check(Resources.Load<Texture2D>(key)!=null,key+" available in runtime resources");
                foreach(var mesh in new[]{BodyMesh("Spirit"),BodyMesh("Armor"),BodyMesh("GroundBreak")})
                Check(mesh!=null&&mesh.vertexCount>0&&mesh.bounds.size.x>.1f&&mesh.bounds.size.y>.05f&&mesh.bounds.size.z>.1f,mesh.name+" has 3D volume");
                float fraction;
                Check(BodyContact(new Vector3(0,1,0),new Vector3(10,1,0),new Vector3(5,0,0),2,.5f,.1f,out fraction)&&Mathf.Abs(fraction-.44f)<.002f,"surface entry precedes body center");
                Check(!BodyContact(new Vector3(0,5,0),new Vector3(10,5,0),new Vector3(5,0,0),2,.5f,.1f,out fraction),"flight over body does not deal damage");
                Check(!BodyContact(new Vector3(0,1,0),new Vector3(10,1,0),new Vector3(5,4,0),2,.5f,.1f,out fraction),"height-separated body is not hit");
                Vector3 from=new Vector3(0,9,0),to=new Vector3(4,.5f,2);float duration=.7f;
                Check((BallisticPosition(from,to,duration,duration)-to).sqrMagnitude<.00001f,"meteor arrives exactly on damage beat");
                Vector3 v0=BallisticVelocity(from,to,.2f,duration),v1=BallisticVelocity(from,to,.3f,duration);
                Check(Mathf.Abs((v1.y-v0.y)/.1f+9.81f)<.001f,"flight has gravity acceleration");
                Vector3 rebound=ResolveContact(new Vector3(4,-10,2),Vector3.up);
                Check((rebound-new Vector3(2.6f,3,1.3f)).sqrMagnitude<.00001f,"normal restitution and tangential friction keep distinct physical impulses");
                bool previous=WorldTerrain.ReliefEnabled;WorldTerrain.ReliefEnabled=false;WorldTraversal.Reset(ZoneKind.Dungeon);
                try
                {
                    Vector3 position=new Vector3(0,4,0),velocity=new Vector3(2,0,0);Advance(ref position,ref velocity,.1f,.2f);
                    Check(Mathf.Abs(position.y-(4-9.81f*.02f))<.002f&&Mathf.Abs(position.x-.4f)<.002f,"debris integrates real gravity and inertia");
                    for(int i=0;i<200;i++)Advance(ref position,ref velocity,.1f,.02f);
                    Check(position.y>=.099f&&position.y<=.12f&&velocity.sqrMagnitude<.03f,"debris bounces then settles without ground penetration");
                    WorldTraversal.AddPlatform(new Vector3(3,0,0),new Vector2(2,2),1);
                    var contactRoot=new GameObject("Native height-aware impact test");
                    var contactSource=new Mesh{name="Contact support sample",vertices=new[]{new Vector3(0,.02f,0),new Vector3(.2f,.02f,.2f),new Vector3(.3f,.02f,0)},uv=new[]{Vector2.zero,Vector2.one,Vector2.right},triangles=new[]{0,1,2}};
                    try
                    {
                        Type anchor=typeof(PlayerController).Assembly.GetType("Emberfall.AnchoredImpactMesh",true);
                        foreach(var support in new[]{new Vector3(3,1,0),new Vector3(5,0,0)})
                        {
                            contactRoot.transform.position=support;Mesh contactMesh=null;
                            try
                            {
                                contactMesh=(Mesh)anchor.GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{contactSource,contactRoot.transform,Vector3.zero,Vector3.one,Quaternion.identity});
                                Check(contactMesh.vertexCount>=3,"anchored impact retains visible geometry on "+(support.y>.5f?"a raised support":"lower terrain"));
                                foreach(var vertex in contactMesh.vertices){Vector3 world=contactRoot.transform.TransformPoint(vertex);float surface=WorldTraversal.SurfaceHeight(world,.015f);Check(Mathf.Abs(world.y-surface-.02f)<.0001f,"native contact vertex rests on its actual support without sinking or floating");}
                            }
                            finally{if(contactMesh!=null)UnityEngine.Object.DestroyImmediate(contactMesh);}
                        }
                    }
                    finally{UnityEngine.Object.DestroyImmediate(contactSource);UnityEngine.Object.DestroyImmediate(contactRoot);}
                    Check(ClearFlight(new Vector3(1,2,0),new Vector3(5,2,0),.1f),"flight clears platform when above its top");
                    Check(!ClearFlight(new Vector3(1,.5f,0),new Vector3(5,.5f,0),.1f),"flight collides with platform side");
                    var prop=WorldTraversal.AddDynamicCircle(new Vector3(-6,0,0),.6f);
                    typeof(WorldTraversal).GetMethod("SetPhysicalBounds",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{prop,new Vector3(-6,.15f,0),new Vector3(1.2f,.3f,1.2f)});
                    Check(ClearFlight(new Vector3(-8,1,0),new Vector3(-4,1,0),.1f),"projectile clears finite low prop while its navigation obstacle remains");
                    Check(!WorldTraversal.IsWalkable(new Vector3(-6,0,0),.1f),"finite prop still blocks feet navigation");
                    Check(BoxContact(new Vector3(-8,.15f,0),new Vector3(-4,.15f,0),new Vector3(-6,.15f,0),new Vector3(1.2f,.3f,1.2f),.1f,out fraction)&&Mathf.Abs(fraction-.325f)<.002f,"low wide prop contact matches its actual bounds");
                    position=new Vector3(-6,2,0);velocity=Vector3.zero;for(int i=0;i<150;i++)Advance(ref position,ref velocity,.1f,.02f);
                    Check(position.y>=.399f&&position.y<.42f&&velocity.sqrMagnitude<.03f,"debris settles on finite prop top without becoming a player platform");
                    int supportRevision=-1;position=new Vector3(-6,.4f,0);velocity=Vector3.zero;AdvanceSupported(ref position,ref velocity,ref supportRevision,.1f,.016f);
                    Check(supportRevision==WorldTraversal.Revision,"native debris sleeps only on its actual registered support");
                    WorldTraversal.RemoveDynamicObstacle(prop);
                    AdvanceSupported(ref position,ref velocity,ref supportRevision,.1f,.016f);
                    Check(position.y<.4f&&velocity.y<0&&supportRevision==-1,"native support removal wakes settled matter before movement");
                    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary native sweep test";wall.transform.position=new Vector3(-3,2,0);wall.transform.localScale=new Vector3(.15f,3,3);Physics.SyncTransforms();
                    try{RaycastHit contact;Check(SweepScenery(new Vector3(-5,2,0),new Vector3(-1,2,0),.1f,out contact)&&contact.distance<2,"native PhysX swept volume hits thin wall");}
                    finally{UnityEngine.Object.DestroyImmediate(wall);}
                }
                finally{WorldTerrain.ReliefEnabled=previous;WorldTraversal.Reset(ZoneKind.Wilderness);}
                string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../ArtSource/Review/PhysicalSkills-20261011"));Directory.CreateDirectory(directory);File.WriteAllLines(Path.Combine(directory,"native-physics-checks.txt"),checks);Debug.Log("Physical skill native checks passed: "+checks.Count);
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception error){Debug.LogException(error);if(Application.isBatchMode)EditorApplication.Exit(1);throw;}
        }
    }
}
