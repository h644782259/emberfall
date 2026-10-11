using System;using System.Linq;using System.Reflection;using Emberfall;using UnityEngine;
namespace Emberfall
{
    public enum ElementalistSpecialization{None,Burn,Shatter}
    public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}
    public static class CombatBalance{public static float RankPower(int rank){throw new NotSupportedException("opening damage outside this fixture");}}
    public static class CombatArea
    {
        public static SkillVisualRecipe Recipe;public static int StatusSkill,Cast;public static float Duration,Interval,Startup,Tick,Final;
        public static void Spawn(PlayerController player,GameSession game,Vector3 at,float size,CombatDamage amount,float disable,float startup,float activeTime,float tickInterval,Color tint,bool followPlayer=false,bool fallingMeteor=false,float pulling=0,CombatDamage finisher=default(CombatDamage),int statusSkill=-1,int statusRank=1,int castId=0,SkillVisualRecipe visual=SkillVisualRecipe.Neutral)
        {Recipe=visual;StatusSkill=statusSkill;Cast=castId;Duration=activeTime;Interval=tickInterval;Startup=startup;Tick=amount.Amount;Final=finisher.Amount;}
    }
    public sealed partial class PlayerController
    {
        public int Hits,LastCast;public float Total;public void HitArea(Vector3 at,float radius,CombatDamage damage,float knockback=0,float stun=0,int castId=0)
        {Hits++;Total+=damage.Amount;LastCast=castId;}
    }
    public static class CombatProjectile
    {
        public static int Count,LastCast;public static float Total;
        public static void Friendly(PlayerController owner,GameSession game,Vector3 at,Vector3 direction,CombatDamage damage,Color color,bool pierce,bool arrow,bool basic,float size,float speed,int castId=0)
        {Count++;Total+=damage.Amount;LastCast=castId;}
    }
    // Only arrow finales execute here. A healing cleanup call indicates fixture misuse.
    public class AdvancedSkillVfx
    { public void Stop(){throw new Exception("unexpected healing anchor in arrow readability fixture");} }
    public partial class ArrowSequenceFixture
    {
        private AdvancedSkillVfx healingAura; // Actual Sequence.OnDisable field, null on these non-healing routes.
        PlayerController owner;GameSession session;FilledSkillVfx.ArrowBatchHandle arrowBatch;int step,steps,rank,skill=9,castId=73;
        HeroClass heroClass=HeroClass.Ranger;Vector3 target;float range=1;Color color=new Color(1,1,1);CombatDamage damage=new CombatDamage(10,true,2);
        public ArrowSequenceFixture(PlayerController hero,int rank){owner=hero;session=GameSession.Instance;this.rank=rank;steps=SkillDamageBudgets.AdvancedSteps(heroClass,skill,rank);arrowBatch=FilledSkillVfx.BeginArrowBatch(hero,target,6,color);}
        public void Run(){while(step<steps){Event();step++;}}
        public void Cancel(){OnDisable();}
        private static Vector3 Circle(float angle,float radius)=>new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
    }
}
public static class CombatReadabilityVisualTests
{
    static int checks;
    static void Check(bool okay,string why){checks++;if(!okay)throw new Exception(why);}
    static PlayerController Reset(bool reduced=false)
    {
        foreach(var obj in GameObject.All.ToArray())UnityEngine.Object.Destroy(obj);GameObject.All.Clear();
        Check(CombatVisualLease.Active==0,"destroy releases every global visual lease");
        WorldTraversal.Reset(ZoneKind.Dungeon);EffectPreferences.ReducedEffects=reduced;Application.isMobilePlatform=true;
        var hero=new GameObject("hero").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=hero,HasStarted=true};return hero;
    }
    static GameObject[] Effects()=>GameObject.All.Where(x=>x.activeInHierarchy&&!x.Destroyed&&x.GetComponent<FilledSkillVfx>()!=null).ToArray();
 static double RequiredArea(GameObject part,bool horizontal)
 {
  var m=part.GetComponent<MeshFilter>().sharedMesh;double sum=0;
  for(int i=0;i<m.triangles.Length;i+=3){Vector3 a=part.transform.TransformPoint(m.vertices[m.triangles[i]]),b=part.transform.TransformPoint(m.vertices[m.triangles[i+1]]),c=part.transform.TransformPoint(m.vertices[m.triangles[i+2]]);var u=b-a;var v=c-a;double x=(double)u.y*v.z-(double)u.z*v.y,y=(double)u.z*v.x-(double)u.x*v.z,z=(double)u.x*v.y-(double)u.y*v.x;sum+=horizontal?Math.Abs(y)*.5:Math.Sqrt(x*x+y*y+z*z)*.5;}return sum;
 }
    public static string Run()
    {
        checks=0;
        foreach(var kind in new[]{FilledVfxKind.Ice,FilledVfxKind.Fire,FilledVfxKind.Summon,FilledVfxKind.Sword,FilledVfxKind.Lightning,FilledVfxKind.Arcane})
        {
            var hero=Reset();WorldTraversal.AddBox(Vector3.zero,new Vector2(.2f,6));
            Vector3 center=CombatSight.GroundPoint(new Vector3(-3,0,0),new Vector3(3,0,0));
            Check(CombatSight.Direct(new Vector3(-3,0,0),center)&&CombatSight.Area(center,new Vector3(-2,0,0)),"near-wall input must have real cast and damage LOS");
            FilledSkillVfx.Impact(hero,center,4,kind,new Color(1,1,1));
            var root=Effects()[0];var main=GameObject.All.First(o=>o.name.EndsWith("Primary "+kind)&&!o.Destroyed);
            Check((root.transform.position-center).sqrMagnitude==0&&main.transform.localPosition.sqrMagnitude==0,"primary actual impact anchor must not relocate to clear ground");
            var required=GameObject.All.Where(o=>o.activeInHierarchy&&(o.name.Contains("Primary ")||o.name.Contains("Landing base")||o.name.Contains("Contact flash"))).ToArray();
            Check(required.Length==3,"all three required shapes must exist before vertex traversal");
            foreach(var part in required)Check(RequiredArea(part,!part.name.Contains("Primary "))>.001,"EMPTY_REQUIRED_SHAPE: actual primary/contact requires positive area");
            foreach(var mesh in GameObject.All.Where(o=>o.activeInHierarchy&&o.GetComponent<MeshFilter>()!=null))foreach(var vertex in mesh.GetComponent<MeshFilter>().sharedMesh.vertices)
                Check(CombatSight.Area(center,mesh.transform.TransformPoint(vertex)),"production primary/contact/decorative vertices stay behind cover");
        }

        for(int rank=1;rank<=3;rank++)
        {
            var hero=Reset();hero.CastRain(rank);
            Check(CombatArea.Recipe==SkillVisualRecipe.ArrowRain&&CombatArea.StatusSkill==-1,"actual non-poison arrow rain must request the independent arrow recipe");
            Check(CombatArea.Cast==91&&CombatArea.Startup==.3f&&CombatArea.Duration==rank+3&&CombatArea.Interval==.4f,"actual arrow field timing and cast identity remain unchanged");
            var field=SkillDamageBudgets.EarlyField(HeroClass.Ranger,rank);
            Check(Math.Abs(CombatArea.Tick*field.Ticks+CombatArea.Final-(rank==1?38:rank==2?54:72))<.001f,"actual field spawn retains rank 1-3 total damage budget");
        }
        foreach(bool reduced in new[]{false,true})
        {
            var hero=Reset(reduced);
            for(int i=0;i<40;i++)FilledSkillVfx.Charge(null,hero,Vector3.zero,3,new Color(1,1,1),8);
            int count=Effects().Length;Check(count==(reduced?12:20),"shared bounded visual cap applies to sustained meshes");
            var first=Effects()[0];FilledSkillVfx.Impact(hero,Vector3.zero,3,FilledVfxKind.Sword,new Color(1,1,1));
            Check(!first.activeInHierarchy&&Effects().Any(x=>x.name=="Filled Sword effect")&&Effects().Length==count,"new main silhouette must evict old sustained mesh");
            var meshes=GameObject.All.Where(x=>x.name.EndsWith("Primary Sword")&&!x.Destroyed).Select(x=>x.GetComponent<MeshFilter>().sharedMesh).ToArray();
            foreach(var obj in Effects())UnityEngine.Object.Destroy(obj);
            Check(meshes.All(m=>m.Destroyed)&&CombatVisualLease.Active==0,"retirement destroys owned clipped meshes and immediately returns leases");
            for(int i=0;i<count;i++)FilledSkillVfx.Impact(hero,Vector3.zero,3,FilledVfxKind.Ice,new Color(1,1,1));
            first=Effects()[0];FilledSkillVfx.ArrowRain(hero,Vector3.zero,4,new Color(1,1,1),true);
            Check(!first.activeInHierarchy&&Effects().Any(x=>x.name=="Filled ArrowRain effect")&&Effects().Length==count,"finale admission must precede old primary silhouettes");
        }
        {
            var hero=Reset(true);var roots=new System.Collections.Generic.List<GameObject>();
            for(int i=0;i<12;i++){var obj=new GameObject("old particles");Check(CombatVisualLease.Attach(obj,CombatVisualPriority.Decoration)!=null,"fill decoration pool");roots.Add(obj);}
            FilledSkillVfx.Impact(hero,Vector3.zero,3,FilledVfxKind.Lightning,new Color(1,1,1));
            Check(!roots[0].activeInHierarchy&&CombatVisualLease.Active==12,"global mesh primary replaces particle decoration");
            var current=Effects()[0];current.SetActive(false);Check(CombatVisualLease.Active==11,"disable returns shared capacity immediately");current.SetActive(false);UnityEngine.Object.Destroy(current);Check(CombatVisualLease.Active==11,"double disable and destroy release only once");
        }
        {var hero=Reset();var cancelled=new ArrowSequenceFixture(hero,1);cancelled.Cancel();Check(Effects().Length==0&&CombatVisualLease.Active==0,"actual sequence cancellation retires its unfinished arrow batch");}
        for(int rank=1;rank<=3;rank++)
        {
            var hero=Reset();CombatProjectile.Count=0;CombatProjectile.Total=0;
            var sequence=new ArrowSequenceFixture(hero,rank);sequence.Run();sequence.Cancel();
            Check(Effects().Length==1,"all ultimate arrow events must share one visual owner");
            Check(hero.Hits==8+rank&&Math.Abs(hero.Total-((7+rank)*13.5f+85))<.001f&&hero.LastCast==73,"actual rank 1-3 arrow event damage and cast budget stay unchanged");
            Check(CombatProjectile.Count==(rank==3?12:0)&&Math.Abs(CombatProjectile.Total-(rank==3?144:0))<.001f,"actual rank 3 radial projectile budget stays unchanged");
            var effect=Effects()[0];GameSession.Instance.InputBlocked=true;Time.deltaTime=.5f;effect.Call("Update");
            Check(effect.activeInHierarchy,"pause preserves shared terminal feedback");GameSession.Instance.InputBlocked=false;Time.deltaTime=.4f;for(int i=0;i<3;i++)effect.Call("Update");
            Check(!effect.activeInHierarchy&&CombatVisualLease.Active==0,"shared terminal feedback has one finite retirement");
        }
        {
            var hero=Reset();var fx=FilledSkillVfx.BeginArrowBatch(hero,Vector3.zero,4,new Color(1,1,1));
            fx.ArrowBeat(Vector3.zero,4,false);var effect=Effects().Single();GameSession.Instance.InputBlocked=true;hero.CombatEpoch++;Time.deltaTime=0;effect.Call("Update");
            Check(!fx.IsValid&&!effect.activeInHierarchy&&CombatVisualLease.Active==0,"old epoch batch retires even while blocked at zero time");
        }
        Reset();return "PASS: "+checks+" production visual priority/batch/anchor/resource checks (managed substitutes, not Unity)";
    }
}
