// Actual production components and resource decoder, executed in a managed scene fixture.
// No damage APIs exist in this fixture; runtime geometry is measured from decoded binaries.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Emberfall;
using UnityEngine;
public static class BlenderSkillVfxProductionTests
{
    static int checks;
    static string root;
    static readonly string[] names={"Crescent","CrescentCore","Shard","Fault"};
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static void Invoke(Type type,string method,object target=null){type.GetMethod(method,BindingFlags.NonPublic|(target==null?BindingFlags.Static:BindingFlags.Instance)).Invoke(target,null);}
    static PlayerController Fresh(bool mobile=false,bool reduced=false)
    {
        foreach(var old in GameObject.All.ToArray())UnityEngine.Object.Destroy(old);
        GameObject.All.Clear();Invoke(typeof(BlenderSkillVfx),"Reset");Invoke(typeof(CombatVisualLease),"Reset");
        Resources.Values.Clear();
        foreach(var name in names)Resources.Values["BlenderVfx/"+name]=new TextAsset{bytes=File.ReadAllBytes(Path.Combine(root,"Assets/Resources/BlenderVfx/"+name+".bytes"))};
        Resources.Values["FilledSpell"]=new Shader();Application.isMobilePlatform=mobile;EffectPreferences.ReducedEffects=reduced;EffectPreferences.EffectsScale=1;
        CombatSight.Wall=float.PositiveInfinity;Time.deltaTime=.01f;
        var hero=new GameObject("Hero").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=hero,HasStarted=true};return hero;
    }
    static GameObject Effect()=>GameObject.All.Single(o=>!o.Destroyed&&o.GetComponent<BlenderSkillVfx>()!=null);
    static GameObject[] Parts(GameObject effect)=>GameObject.All.Where(o=>!o.Destroyed&&o.transform.parent==effect.transform&&o.GetComponent<MeshFilter>()!=null).ToArray();
    static float Age(GameObject effect)=>(float)typeof(BlenderSkillVfx).GetField("age",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(effect.GetComponent<BlenderSkillVfx>());
    static void Sample(GameObject effect,float age){typeof(BlenderSkillVfx).GetField("age",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(effect.GetComponent<BlenderSkillVfx>(),age);Invoke(typeof(BlenderSkillVfx),"Sample",effect.GetComponent<BlenderSkillVfx>());}
    static void Reject(string label,Action setup)
    {
        var hero=Fresh();setup();Check(!BlenderSkillVfx.TryPlay(hero,1,false),label+" returns fallback");
        Check(!GameObject.All.Any(o=>o.GetComponent<BlenderSkillVfx>()!=null)&&CombatVisualLease.Active==0,label+" allocates no component/lease");
    }
    public static string Run(string repository)
    {
        root=repository;
        foreach(var name in names)
        {
            var bytes=File.ReadAllBytes(Path.Combine(root,"Assets/Resources/BlenderVfx/"+name+".bytes"));var mesh=AuthoredActorMeshes.Decode(bytes,name);
            Check(mesh!=null&&mesh.vertices.Length>=3&&mesh.triangles.Length%3==0,"real binary decodes "+name);
            Console.WriteLine("RESOURCE "+name+" vertices="+mesh.vertices.Length+" triangles="+mesh.triangles.Length/3+" bytes="+bytes.Length);
            Check(AuthoredActorMeshes.Decode(bytes.Take(bytes.Length-1).ToArray(),name)==null,"truncation rejected");
            Check(AuthoredActorMeshes.Decode(bytes.Concat(new byte[]{0}).ToArray(),name)==null,"trailing data rejected");
            foreach(var mutation in new Action<byte[]>[]{b=>b[0]=0,b=>Array.Copy(BitConverter.GetBytes(4097),0,b,4,4),b=>Array.Copy(BitConverter.GetBytes(float.NaN),0,b,12,4),b=>Array.Clear(b,24,12),b=>Array.Copy(BitConverter.GetBytes(-1),0,b,b.Length-4,4)})
            {
                var corrupt=(byte[])bytes.Clone();mutation(corrupt);
                Check(AuthoredActorMeshes.Decode(corrupt,name)==null,"strict malformed resource rejected "+name);
            }
            Reject("missing "+name,()=>Resources.Values.Remove("BlenderVfx/"+name));
            Reject("corrupt "+name,()=>Resources.Values["BlenderVfx/"+name]=new TextAsset{bytes=new byte[20]});
        }
        Reject("missing shader",()=>Resources.Values.Remove("FilledSpell"));
        Reject("unsupported shader",()=>((Shader)Resources.Values["FilledSpell"]).isSupported=false);
        Reject("covered footprint",()=>CombatSight.Wall=.1f);
        foreach(var invalid in new[]{0f,-1f,float.NaN,float.PositiveInfinity,float.NegativeInfinity}){var hero=Fresh();Check(!BlenderSkillVfx.TryPlay(hero,invalid,false),"invalid range fallback");}
        Check(!BlenderSkillVfx.TryPlay(null,1,false),"null hero fallback");{var hero=Fresh();hero.IsDead=true;Check(!BlenderSkillVfx.TryPlay(hero,1,false),"dead hero fallback");}
        foreach(bool shock in new[]{false,true})foreach(int tier in new[]{0,1,2})foreach(float range in new[]{.35f,1f,2.25f})
        {
            var hero=Fresh(tier==1,tier==2);hero.transform.position=new Vector3(8,0,-3);hero.transform.rotation=Quaternion.Euler(0,37,0);
            Check(BlenderSkillVfx.TryPlay(hero,range,shock),"valid effect handled");var effect=Effect();var parts=Parts(effect);
            Check(parts.Length==(tier==0?12:tier==1?8:5),"quality caps 12/8/5");
            Check(parts[0].transform.localScale.x!=1&&parts[0].GetComponent<MeshRenderer>().Opacity>0,"age-zero geometry initialized");
            Check(parts.Take(shock?1:5).All(o=>o.activeSelf),"age-zero primary/contact visible");
            Check(parts.Count(o=>o.name=="Hot contact core")==2,"two hot cores retained on every tier");
            Check(CombatVisualLease.Active==1,"one actual lease acquired");
            float footprint=CombatSight.LastFootprint,maxRadius=0;
            // Check every decoded animated vertex against the actual radius passed to the coverage gate.
            for(int step=0;step<=115;step++)
            {
                Sample(effect,step*.00999f);
                foreach(var part in parts.Where(p=>p.activeSelf))foreach(var vertex in part.GetComponent<MeshFilter>().sharedMesh.vertices)
                {
                    var point=part.transform.TransformPoint(vertex)-effect.transform.position;float radius=(float)Math.Sqrt(point.x*point.x+point.z*point.z);maxRadius=Math.Max(maxRadius,radius);
                    Check(radius<=footprint+.0002f,"animated vertex contained shock="+shock+" tier="+tier+" age="+Age(effect)+" radius="+radius+" footprint="+footprint);
                }
            }
            Console.WriteLine("GEOMETRY shock="+shock+" tier="+tier+" range="+range+" maxRadius="+maxRadius+" certifiedRadius="+footprint);
            hero.transform.position=Vector3.zero;UnityEngine.Object.Destroy(effect);CombatSight.Wall=footprint-.001f;
            Check(!BlenderSkillVfx.TryPlay(hero,range,shock),"insufficient certified footprint fallback");
        }
        foreach(string reason in new[]{"death","epoch","session","null-session","player","not-started","finished","expired"})
        {
            var hero=Fresh();BlenderSkillVfx.TryPlay(hero,1,false);var effect=Effect();
            if(reason=="death")hero.IsDead=true;else if(reason=="epoch")hero.CombatEpoch++;else if(reason=="session")GameSession.Instance=new GameSession();else if(reason=="null-session")GameSession.Instance=null;else if(reason=="player")GameSession.Instance.Player=new GameObject().AddComponent<PlayerController>();else if(reason=="not-started")GameSession.Instance.HasStarted=false;else if(reason=="finished")GameSession.Instance.ModeFinished=true;else Time.deltaTime=1.16f;
            effect.Call("Update");Check(effect.Destroyed&&!effect.activeSelf&&CombatVisualLease.Active==0,reason+" retires and releases actual lease");Check(Parts(effect).Length==0,reason+" destroys children");
        }
        {
            var hero=Fresh();BlenderSkillVfx.TryPlay(hero,1,true);var effect=Effect();
            GameSession.Instance.InputBlocked=true;Time.deltaTime=.4f;effect.Call("Update");Check(Age(effect)==0,"pause holds age");
            GameSession.Instance.InputBlocked=false;Time.deltaTime=0;effect.Call("Update");Check(Age(effect)==0,"zero delta holds age");
            Time.deltaTime=-1;effect.Call("Update");Check(Age(effect)==0,"negative delta holds age");
            Time.deltaTime=.2f;effect.Call("Update");Check(Math.Abs(Age(effect)-.2f)<.00001f,"resume advances age");
            GameSession.Instance.InputBlocked=true;hero.CombatEpoch++;effect.Call("Update");Check(effect.Destroyed&&CombatVisualLease.Active==0,"epoch cleanup even while paused");
        }
        foreach(int tier in new[]{0,1,2})
        {
            var hero=Fresh(tier==1,tier==2);int capacity=tier==0?32:tier==1?20:12;
            for(int i=0;i<capacity;i++)Check(CombatVisualLease.Attach(new GameObject("reserved contact"),CombatVisualPriority.RealContact)!=null,"fill real pool");
            Check(BlenderSkillVfx.TryPlay(hero,1,false),"budget rejection consumes presentation request");
            Check(!GameObject.All.Any(o=>!o.Destroyed&&o.GetComponent<BlenderSkillVfx>()!=null)&&CombatVisualLease.Active==capacity,"budget rejection allocates no VFX children or extra lease");
        }
        {var hero=Fresh();BlenderSkillVfx.TryPlay(hero,1,false);var meshes=Parts(Effect()).Select(p=>p.GetComponent<MeshFilter>().sharedMesh).Distinct().ToArray();Invoke(typeof(BlenderSkillVfx),"Reset");Check(meshes.All(m=>m.Destroyed),"subsystem reset destroys cached meshes");}
        return "PASS: "+checks+" production authored VFX/decoder/lease assertions; managed only, no Unity/GPU validation";
    }
}
