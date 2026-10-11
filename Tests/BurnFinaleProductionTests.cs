using System;using System.Collections.Generic;using System.Reflection;using UnityEngine;using Emberfall;
namespace UnityEngine
{
    public class MonoBehaviour{public Emberfall.EnemyController OwnerEnemy;public Transform transform=new Transform();public T GetComponent<T>()where T:class=>OwnerEnemy as T;public T GetComponentInChildren<T>()where T:class=>null;}
    public class Transform{public Vector3 position;public Quaternion localRotation;}
    public struct Quaternion{public static Quaternion identity=>new Quaternion();public static Quaternion Euler(float a,float b,float c)=>new Quaternion();}
    public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new Vector3();public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
    public struct Color{public Color(float r,float g,float b){}}
    public static class Time{public static int frameCount;public static float deltaTime,time;}
    public static class Mathf{public const float PI=(float)Math.PI;public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Clamp(float a,float b,float c)=>Math.Min(c,Math.Max(a,b));public static float Clamp01(float a)=>Clamp(a,0,1);public static float Sin(float a)=>(float)Math.Sin(a);}
}
namespace Emberfall
{
    internal static class FilledSkillVfx{internal static int Confirmed;internal static void ConfirmFinale(PlayerController hero,int castId){Confirmed++;}}
    public enum HeroClass{Arcanist}public enum ElementalistSpecialization{None,Burn,Shatter}
    public class CombatModel:MonoBehaviour{internal bool TryAnimateKnockdown(float remaining,bool airborne,float dt)=>false;}
    public class GameSession{public static GameSession Instance;public bool HasStarted=true,CombatEnded,InputBlocked;public PlayerController Player;public List<EnemyController> Enemies=new List<EnemyController>();public void RecordClassTutorial(HeroClass hero){} }
    public class EnemyController:MonoBehaviour
    {
        public enum ThreatTier{Normal,Elite}public ThreatTier Tier;public bool IsDead,IsBoss,IsStunned;public float HitFootprintBonus,ControlStunRemaining;public EnemyStatusEffects StatusEffects;
        public float Health=10000;public bool IgnoreDamage;public Action OnDamage;public readonly List<DamageEvent> Hits=new List<DamageEvent>();
        public class DamageEvent{public float Amount;public bool Impact,Critical;}
        public void TakeDamage(float value,Vector3 offset,float knockback=0,float stun=0,bool impact=true,bool critical=false,int practiceCastId=0){if(IsDead||IgnoreDamage)return;Hits.Add(new DamageEvent{Amount=value,Impact=impact,Critical=critical});Health-=Math.Max(1,value*.5f);if(Health<=0)IsDead=true;OnDamage?.Invoke();}
        public void Provoke(){}public float ApplyControl(float duration)=>duration;
    }
    public sealed partial class PlayerController:MonoBehaviour
    {
        public int CombatEpoch=1,ProcHits,Boons;public bool DodgeBurn;public bool IsDead;public ElementalistSpecialization Specialization=ElementalistSpecialization.Burn;private GameSession session=>GameSession.Instance;
        private bool ValidAimTarget(EnemyController e)=>e!=null&&!e.IsDead;
        internal void RegisterSkillHit(int cast){ProcHits++;}private void ApplySpellDodgeBoon(EnemyController enemy){Boons++;if(DodgeBurn)enemy.StatusEffects.Burn(this,2,200);}
    }
    public static class ElementalCombatVfx{public enum Element{Fire,Poison}public static int Clears;public static void OnEnemy(EnemyController e,Element element,float duration){}public static void ClearFire(EnemyController e){Clears++;}public static void ClearPoison(EnemyController e){}}
    public static class CombatFx{public static int CashContacts;public static void BurnContact(PlayerController owner,Vector3 point,bool finale=false){CashContacts++;}public static Vector3 Flat(Vector3 v){v.y=0;return v;}public static void Ring(Vector3 p,float r,Color c,float duration,float width){} }
    public static class CombatSight{public static bool Area(Vector3 a,Vector3 b)=>true;}
    public static class DestructibleProp{public static Action OnStrike;public static void StrikeArea(PlayerController p,Vector3 at,float r,CombatDamage d,int cast){OnStrike?.Invoke();} }
    public static class PlayerUpgradeRules{public const float PoisonDetonationTicks=3;}
}
public static class BurnFinaleProductionTests
{
    static int checks;
    static void Check(bool okay,string why){checks++;if(!okay)throw new Exception(why);}
    static void Tick(EnemyStatusEffects status,float delta){Time.frameCount++;Time.deltaTime=delta;status.GetType().GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(status,null);}
    static void Frame(float delta){Time.frameCount++;Time.deltaTime=delta;}
    static EnemyController Create(out PlayerController player)
    {
        Time.frameCount=0;Time.deltaTime=0;Time.time=0;CombatFx.CashContacts=0;DestructibleProp.OnStrike=null;ElementalCombatVfx.Clears=0;player=new PlayerController();GameSession.Instance=new GameSession{Player=player};
        var enemy=new EnemyController();enemy.StatusEffects=new EnemyStatusEffects{OwnerEnemy=enemy};enemy.StatusEffects.GetType().GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(enemy.StatusEffects,null);GameSession.Instance.Enemies.Add(enemy);return enemy;
    }
    public static string Run()
    {
        checks=0;PlayerController owner;
        var enemy=Create(out owner);var status=enemy.StatusEffects;
        Check(status.ResolveBurnFinale(owner,1,30)==0&&status.IsBurning&&enemy.Hits.Count==0,"without existing own burn finale leaves a fresh three-second schedule");
        for(int i=0;i<6;i++)Tick(status,.5f);
        Check(enemy.Hits.Count==6&&!status.IsBurning&&enemy.Hits.TrueForAll(h=>h.Amount==5&&!h.Critical&&!h.Impact),"fresh burn executes exactly six ordinary noncritical DOT events");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,6);Frame(.5f);
        int count=status.ResolveBurnFinale(owner,2,60);
        Check(enemy.Hits.Count==2&&enemy.Hits[0].Amount==1&&enemy.Hits[1].Amount==60,"due original-strength tick must settle before strongest refresh and future cash");
        Check(count==6&&!status.IsBurning&&ElementalCombatVfx.Clears==1,"future endpoint-inclusive six ticks claimed and remaining fire retired");
        Tick(status,.5f);Check(enemy.Hits.Count==2,"claimed future burn cannot tick again");
        status.Burn(owner,3,90);Check(status.ResolveBurnFinale(owner,2,120)==0&&enemy.Hits.Count==2,"same cast target cannot cash again after a new burn");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,90);Frame(.1f);
        Check(status.ResolveBurnFinale(owner,3,3)==6&&Math.Abs(enemy.Hits[0].Amount-90)<.001f,"weaker refresh never discards strongest own DPS");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,6,60);Frame(.1f);
        Check(status.ResolveBurnFinale(owner,4,3)==6&&status.IsBurning,"only future three-second window is consumed when old burn is longer");
        for(int i=0;i<60;i++)Tick(status,.1f);
        Check(enemy.Hits.Count==7&&Math.Abs(enemy.Hits[0].Amount-30)<.001f,"future events after the three-second cash window remain exactly once");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);Frame(3);
        Check(status.ResolveBurnFinale(owner,5,60)==0&&enemy.Hits.Count==6&&status.IsBurning,"all endpoint-due events settle before deciding expired burn is not existing");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);enemy.Health=1;Frame(.5f);
        Check(status.ResolveBurnFinale(owner,6,60)==0&&enemy.IsDead&&enemy.Hits.Count==1,"due lethal tick must prevent merge and cash on dead target");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);var foreign=new PlayerController();status.Burn(foreign,3,3000);Frame(.2f);
        Check(status.ResolveBurnFinale(owner,7,30)==0&&enemy.Hits.Count==0,"foreign owner strength and pending events are never cashed");
        for(int i=0;i<6;i++)Tick(status,.5f);Check(enemy.Hits.Count==6&&enemy.Hits.TrueForAll(h=>h.Amount==5),"single-owner replacement resets strength and phase instead of inheriting foreign burn");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,3000);owner.CombatEpoch++;Frame(.2f);
        Check(status.ResolveBurnFinale(owner,8,30)==0&&enemy.Hits.Count==0,"old epoch burn does not count as existing own burn");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);GameSession.Instance.InputBlocked=true;Frame(1);
        Check(status.ResolveBurnFinale(owner,9,60)==0&&enemy.Hits.Count==0,"blocked finale never advances or claims burn events");
        GameSession.Instance.InputBlocked=false;Frame(.1f);Check(status.ResolveBurnFinale(owner,9,60)==6&&enemy.Hits.Count==1,"blocked attempt leaves cast receipt available after resume");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);Frame(.5f);enemy.OnDamage=()=>status.ResolveBurnFinale(owner,10,300);
        Check(status.ResolveBurnFinale(owner,10,60)==6&&enemy.Hits.Count==2,"same-cast reentry cannot repeat merge or cash");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);Frame(.5f);enemy.OnDamage=()=>owner.CombatEpoch++;
        Check(status.ResolveBurnFinale(owner,11,60)==0&&enemy.Hits.Count==1,"world epoch change during due callback cancels remaining settlement");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);Frame(.1f);
        owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,true,2),12,true);
        Check(enemy.Hits.Count==2&&enemy.Hits[1].Amount==30&&!enemy.Hits[1].Critical&&!enemy.Hits[1].Impact&&enemy.Hits[0].Amount==65&&enemy.Hits[0].Critical,"actual player final branch preserves direct crit damage and adds only noncritical DOT cash");
        Check(owner.BurnCashFeedback(out int cashed,out float expires)&&cashed==1&&expires==2&&CombatFx.CashContacts==1,"actual accepted cash publishes one target and one bounded contact");
        Check(owner.ProcHits==1&&owner.Boons==1,"cash does not re-enter skill-hit/resource/boon hooks");
        owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,true,2),12,true);Check(owner.BurnCashFeedback(out cashed,out expires)&&cashed==1&&CombatFx.CashContacts==1,"same target same cast never doubles feedback");
        Time.time=2;Check(!owner.BurnCashFeedback(out cashed,out expires),"cash presentation expires on gameplay clock");
        enemy=Create(out owner);status=enemy.StatusEffects;
        owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),13,false);
        Check(enemy.Hits.Count==1&&enemy.Hits[0].Amount==65&&status.IsBurning,"nonfinal production branch only applies normal burn and direct damage");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);Tick(status,.5f);Time.deltaTime=.5f;
        status.ResolveBurnFinale(owner,14,60);Check(enemy.Hits.Count==2&&enemy.Hits[1].Amount==60,"status Update followed by finale in same frame cannot advance burn clock twice");
        enemy=Create(out owner);status=enemy.StatusEffects;owner.DodgeBurn=true;
        owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),15,true);
        Check(enemy.Hits.Count==1&&enemy.Hits[0].Amount==65&&status.IsBurning,"same-hit dodge boon burn is not mistaken for preexisting burn");
        for(int i=0;i<6;i++)Tick(status,.5f);
        Check(enemy.Hits.Count==7&&!status.IsBurning&&enemy.Hits[1].Amount==50,"without old burn same-hit boon retains exactly the merged six future ticks");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);owner.DodgeBurn=true;enemy.Health=1;Frame(.5f);
        owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),20,true);
        Check(enemy.Hits.Count==1&&enemy.IsDead&&owner.Boons==0&&owner.ProcHits==0,"actual player due lethal stops boon direct and future cash");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);Frame(.5f);enemy.OnDamage=()=>owner.CombatEpoch++;
        owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),21,true);
        Check(enemy.Hits.Count==1&&owner.Boons==0&&owner.ProcHits==0,"actual player due epoch change stops the retired impact");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);owner.DodgeBurn=true;Frame(.1f);
        owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),19,true);
        Check(enemy.Hits.Count==2&&enemy.Hits[1].Amount==300&&!status.IsBurning,"existing burn cash includes same-hit boon strength and clears its future events");
        for(int i=0;i<8;i++)Tick(status,.5f);Check(enemy.Hits.Count==2,"stopping after boon-assisted cash produces no extra burn ticks");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);Frame(.1f);var settlement=status.PrepareBurnFinale(owner,16,60);
        Check(settlement.Apply()&&!settlement.Apply()&&enemy.Hits.Count==1,"prepared cash receipt delivers exactly once");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);Frame(.1f);settlement=status.PrepareBurnFinale(owner,17,60);owner.CombatEpoch++;settlement.Apply();Check(enemy.Hits.Count==0,"prepared cash cannot cross world epoch after direct callback");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);Frame(.1f);enemy.Health=1;int confirmedBefore=FilledSkillVfx.Confirmed;owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),18,true);
        Check(FilledSkillVfx.Confirmed==confirmedBefore+1,"actual direct lethal finale confirms visual tail without cash");
        Check(enemy.Hits.Count==1&&enemy.Hits[0].Amount==65&&enemy.IsDead&&CombatFx.CashContacts==0&&!owner.BurnCashFeedback(out _,out _),"direct hit keeps priority and dead target cannot receive cash or contact");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);Frame(.1f);settlement=status.PrepareBurnFinale(owner,101,60);enemy.IsDead=true;Check(!settlement.Apply()&&enemy.Hits.Count==0,"direct-lethal target never reports accepted burn cash");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);enemy.IgnoreDamage=true;Frame(.1f);Check(status.ResolveBurnFinale(owner,102,60)==0&&enemy.Hits.Count==0,"claimed ticks without actual HP loss never report cash success");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);
        var second=new EnemyController();second.StatusEffects=new EnemyStatusEffects{OwnerEnemy=second};second.StatusEffects.GetType().GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(second.StatusEffects,null);GameSession.Instance.Enemies.Add(second);second.StatusEffects.Burn(owner,3,30);
        Frame(.1f);owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),103,true);
        Check(owner.BurnCashFeedback(out cashed,out expires)&&cashed==2&&CombatFx.CashContacts==2,"actual final area aggregates two accepted targets into one cast receipt");
        GameSession.Instance.InputBlocked=true;Check(!owner.BurnCashFeedback(out cashed,out expires),"paused feedback is hidden");GameSession.Instance.InputBlocked=false;
        owner.CombatEpoch++;Check(!owner.BurnCashFeedback(out cashed,out expires),"feedback cannot survive a room epoch");
        enemy=Create(out owner);status=enemy.StatusEffects;status.Burn(owner,3,30);bool broken=false,observed=false;
        DestructibleProp.OnStrike=()=>CombatImpactBatch.Resolve(()=>observed=broken);enemy.OnDamage=()=>broken=true;
        owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),104,true);
        Check(observed,"actual player area defers prop arbitration until enemy resolution completes");
        enemy=Create(out owner);bool drained=false;DestructibleProp.OnStrike=()=>{CombatImpactBatch.Resolve(()=>drained=true);owner.CombatEpoch++;};
        owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),105,true);Check(drained,"actual player early epoch return still closes impact batch");
        enemy=Create(out owner);DestructibleProp.OnStrike=()=>{CombatImpactBatch.Resolve(()=>drained=false);throw new InvalidOperationException();};
        try{owner.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),106,true);}catch(InvalidOperationException){}
        Check(!drained,"actual player exceptional exit still drains batch");
        var gate=new BurnFinaleReceipts();Check(gate.TryEnter(10)&&gate.TryEnter(8)&&!gate.TryEnter(10),"recent out-of-order finale completions retain independent receipts");
        for(int i=11;i<80;i++)Check(gate.TryEnter(i),"new finale receipt accepted within bounded storage");
        Check(!gate.TryEnter(8)&&!gate.TryEnter(10),"evicted old cast receipts never become payable again");gate.Clear();Check(gate.TryEnter(8),"new owner epoch receives a clean receipt scope");
        var schedule=new ScheduledTickWindow(6,.5f,.5f);schedule.Elapse(.5f,true);
        Check(schedule.ClaimFutureTicks(3)==0&&schedule.HasDueTicks,"future claim refuses to consume overdue events");schedule.TryTakeDueTick(true);
        Check(schedule.ClaimFutureTicks(1.2f)==2&&!schedule.Complete,"future claim counts discrete events instead of continuous remaining-time damage");
        schedule.Elapse(5.5f,true);int remaining=0;while(schedule.TryTakeDueTick(true))remaining++;
        Check(remaining==9&&schedule.Complete,"only claimed future events are removed from the schedule");
        return "PASS: "+checks+" actual burn finale/owner/epoch/due/cash/player-path checks (managed recipients, not Unity)";
    }
}
