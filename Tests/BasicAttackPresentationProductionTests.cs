using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Emberfall;

namespace Emberfall
{
    public enum HeroClass { Vanguard, Arcanist, Ranger, Summoner }
    public enum VisualSurface { Metal, Crystal }
    public enum EquipmentMechanic { ReturningBlade }
    public enum WeaponVisualAnchor { BowArrowRest, StaffCore }
    public struct CombatDamage { public float Amount; public static implicit operator CombatDamage(float value)=>new CombatDamage{Amount=value}; }
    public class EnemyController:MonoBehaviour { public bool IsBoss,IsDead;public float HitFootprintBonus; }
    public class SummonedCompanion:MonoBehaviour { public bool EmpoweredAttackActive; }
    public class ProjectileVolleyBudget<T> { }
    public static class Trace { public static readonly List<string> Events=new List<string>(); }
    public class CombatModel:MonoBehaviour
    {
        public bool HasSocket=true,SwordActionActive; public int WeaponSwingSide=1;public Vector3 Socket;public WeaponVisualAnchor LastAnchor;
        public void PlayAction(int action,bool basic,float duration){SwordActionActive=true;Trace.Events.Add("pose");Socket=transform.position+new Vector3(.6f,1.85f,.72f);}
        public bool TryGetWeaponVisualAnchor(WeaponVisualAnchor anchor,out Vector3 point){LastAnchor=anchor;point=Socket;Trace.Events.Add("socket");return HasSocket;}
    }
    public sealed partial class PlayerController:MonoBehaviour
    {
        public bool IsDead,TraversalStartedThisFrame,SkillReleaseInputBlocked,ReturningCounterReady,ReturningCounterVariant,lastMeleeDamagedEnemy;public int CombatEpoch=4,mobilityRank;
        public HeroClass HeroClass;public CombatModel model;public GameSession session;public EnemyController AimTarget,DesktopTarget;public Vector3 aimPoint;
        public SkillBasicRecoveryClock skillBasicRecovery;public Charge charge;public Bonus ActiveRunBonuses;
        public float attackCooldown,attackAnimation,mobilityTime,counterTime,perfectDodgeCounterTime;public int MeleeCount;
        public int NewCastId()=>17;public CastFirstHitReceipt RetainCastReceipt(int id)=>null;
        public Vector3 EnemyBodyPoint(EnemyController enemy)=>enemy.transform.position+Vector3.up*1.4f;
        private bool MobilePinnedActionAllowed(int index,bool commit)=>true;
        private EnemyController ResolveDesktopBasicTarget()=>DesktopTarget;
        private EnemyController MagicConeTarget()=>null;
        private static bool ValidAimTarget(EnemyController enemy)=>enemy!=null&&!enemy.IsDead&&enemy.gameObject.activeInHierarchy;
        private void FaceAim(){Trace.Events.Add("face");}
        private CombatDamage Damage(float power)=>power;
        private bool HasMechanic(EquipmentMechanic mechanic)=>false;
        private bool Melee(float radius,float angle,CombatDamage damage,float knock,float stun,bool basic=false,bool counterThrust=false){MeleeCount++;return false;}
        public void Attack()=>BasicAttack();
    }
    public sealed class GameSession
    {
        public static GameSession Instance;public PlayerController Player;public bool HasStarted=true,ModeFinished,InputBlocked;
        public void RecordClassTutorial(HeroClass hero){}public void RecordCombatAction(string action){}public void SpawnMechanismText(Vector3 at,string text,Color color){}
    }
    public class Charge { public bool IsCharging,ConsumedThisFrame; }
    public class Bonus { public float AttackSpeedMultiplier=1; }
    public static class MobileControls { public static bool Active=true; }
    public static class GameAudio { public static void PlayBasicAttack(HeroClass hero){Trace.Events.Add("audio:"+hero);} }
    public static class GameBalance { public static Color ClassColor(HeroClass hero)=>new Color(.4f,.65f,1); }
    public static class ReturningCounterRules { public static string Predict(Vector3 from,Vector3 target,bool boss,float bonus,out Vector3 at){at=from;return "disabled";} }
    public static class PlayerUpgradeRules { public static float CounterAfterAttack(float before,float after,bool hit)=>after; }
    public static class WorldTerrain { public static float Height(Vector3 at)=>0; }
    public static class SkillBodyMaterialLibrary { public enum Matter{Rock,Steel,Ice,Vine,Fur,Mud,Wood,Leather}public static void Apply(Material m,Matter matter,float heat=0){}public static void Bind(Material m){} }
    public static class ProjectileVisualBridge { public static void Bind(Transform body,Transform root,CombatModel model,bool arrow,TrailRenderer trail){} }
    public static class CombatReviewEvents { public static bool Enabled;public static void Emit(string s,int id,object target=null,int skill=0,string detail=null){} }
    public static class CombatReviewObjectId { public static int Get(object value)=>1; }
    public class DestructibleProp
    {
        public static int Hits;public static bool FindProjectileHit(PlayerController p,Vector3 a,Vector3 b,float radius,out DestructibleProp prop,out float fraction){prop=new DestructibleProp();fraction=.5f;return true;}
        public void Impact(PlayerController player,int cast,CombatDamage damage){Hits++;}
    }
    public static class ProceduralVisuals
    {
        static Mesh From(VisualMeshData data,string name)=>new Mesh{name=name,vertices=data.Vertices,normals=data.Normals,uv=data.Uv,triangles=data.Triangles};
        public static Mesh WeatheredRock=From(VisualMeshRecipes.Rock(),"WeatheredRock");
        public static GameObject Create(string name,PrimitiveType type,Material material){var obj=new GameObject(name);obj.AddComponent<MeshFilter>().sharedMesh=From(VisualMeshRecipes.RoundBody(type==PrimitiveType.Capsule,16,12),type.ToString());obj.AddComponent<MeshRenderer>().sharedMaterial=material;return obj;}
        public static void ApplySurface(Material material,VisualSurface surface){}
    }
    public static partial class CombatFx
    {
        public static Vector3 Flat(Vector3 value){value.y=0;return value;}public static Material NewGlow()=>new Material(new Shader());
        public static GameObject Ring(params object[] values)=>new GameObject("Contact debris");
    }
    public static class AdvancedSkillVfx { public static void Beam(params object[] values){} }
    public class GroundSkillSurface:MonoBehaviour { public void Retire(){} }
    // Mesh drawing/animation is a boundary. The real rental, Crescent allocation,
    // complete clock, scene invalidation and pool-return methods are extracted.
    internal sealed class FilledSkillVfx:MonoBehaviour
    {
        private class Piece { public Transform Transform; }
        private readonly Piece[] pieces=new Piece[14];private int count,epoch;private PlayerController owner;private GameSession session;
        private bool pooled,disposing,registered,sculpted,confirmedFinale,contactTrack;private float age,life,size,terminalAge,trackRemaining;private int finaleCast;private Color tint;private FilledVfxKind kind;private ulong rentGeneration;private Func<float> contactRemaining;private Func<bool> actionActive;private CombatVisualLease lease;
        private const int PoolCapacity=8;private static int active;private static readonly Stack<FilledSkillVfx> idle=new Stack<FilledSkillVfx>();private static readonly HashSet<FilledSkillVfx> instances=new HashSet<FilledSkillVfx>();private static readonly List<FilledSkillVfx> finales=new List<FilledSkillVfx>();
        private static Mesh sword,arcaneShard;
        private static void EnsureAssets(){sword=AuthoredSpellBases.Load("Sword");arcaneShard=ProceduralVisuals.WeatheredRock;}
        private void EnsurePropertyBlock(){}private void RegisterFinale(int cast){finaleCast=cast;}
        private void ClearPieces(){foreach(var piece in pieces)if(piece!=null)piece.Transform.gameObject.SetActive(false);Array.Clear(pieces);count=0;}
        private void Add(Mesh mesh,Vector3 at,Vector3 dimensions,Quaternion rotation,float delay,int motion,float phase,float footprint=0,string label="Repeated ornament",bool anchored=false){var obj=new GameObject(label);obj.transform.SetParent(transform,false);obj.transform.localPosition=at;obj.transform.localScale=dimensions;obj.transform.localRotation=rotation;obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>();pieces[count++]=new Piece{Transform=obj.transform};}
        private void Animate(Piece piece){}
        internal static void ResetForTest(){idle.Clear();instances.Clear();finales.Clear();active=0;}
        FILLED_METHODS
    }
}

internal static class BasicAttackPresentationProductionTests
{
    static int checks;static readonly BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static object Field(object obj,string name)=>obj.GetType().GetField(name,I).GetValue(obj);
    static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;static bool Near(Vector3 a,Vector3 b)=>(a-b).magnitude<.0001f;
    static CombatProjectile Latest()=>GameObject.All.LastOrDefault(o=>o.GetComponent<CombatProjectile>()!=null&&!o.Destroyed)?.GetComponent<CombatProjectile>();
    static float Flight(CombatProjectile projectile,float distance)=>(float)typeof(CombatProjectile).GetMethod("FlightHeight",I).Invoke(projectile,new object[]{distance});
    static PlayerController Reset(HeroClass hero,float elevation=0,bool reduced=false,bool mobile=true)
    {
        foreach(var obj in GameObject.All.ToArray())UnityEngine.Object.Destroy(obj);GameObject.All.Clear();
        FilledSkillVfx.ResetForTest();
        typeof(CombatVisualLease).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        EffectPreferences.ReducedEffects=reduced;Application.isMobilePlatform=mobile;MobileControls.Active=true;CombatSight.Wall=float.PositiveInfinity;DestructibleProp.Hits=0;Trace.Events.Clear();
        var player=new GameObject("Hero").AddComponent<PlayerController>();player.HeroClass=hero;player.transform.position=new Vector3(elevation>0?60:0,elevation,0);player.model=player.gameObject.AddComponent<CombatModel>();player.aimPoint=new Vector3(player.transform.position.x,0,10);
        player.session=GameSession.Instance=new GameSession{Player=player};return player;
    }
    static void CheckShot(PlayerController player,CombatProjectile shot,bool selected)
    {
        bool arrow=player.HeroClass==HeroClass.Ranger;var body=(Transform)Field(shot,"visualBody");var mesh=body.gameObject.GetComponent<MeshFilter>().sharedMesh;
        Check(Near(shot.transform.position,player.model.Socket),"release begins at the posed weapon socket");
        Check(Trace.Events.IndexOf("pose")<Trace.Events.IndexOf("socket")&&Trace.Events.IndexOf("pose")<Trace.Events.IndexOf("audio:"+player.HeroClass),"pose commits before socket sampling and class audio");
        Check(player.model.LastAnchor==(arrow?WeaponVisualAnchor.BowArrowRest:WeaponVisualAnchor.StaffCore),"class samples its own release socket");
        Check((float)Field(shot,"radius")==.22f&&(float)Field(shot,"speed")== (arrow?20:16)&&(float)Field(shot,"lifetime")==1.15f,"readability preserves original collision radius speed and lifetime");
        Check(Near((float)Field(shot,"launchHeight"),player.model.Socket.y),"physical launch height equals the drawn weapon socket");
        Check(Near((float)Field(shot,"impactHeight"),selected?player.EnemyBodyPoint(player.AimTarget).y:player.model.Socket.y),selected?"selected shot preserves the target body height":"empty shot keeps the elevated weapon height");
        Check(Near(Flight(shot,0),player.model.Socket.y),"flight starts at the physical socket height");
        float distance=(float)Field(shot,"aimedDistance");Check(Near(Flight(shot,distance),(float)Field(shot,"impactHeight")),"flight reaches its actual intended height");
        Check(ReferenceEquals(Field(shot,"basicAimTarget"),player.AimTarget)&&ReferenceEquals(Field(shot,"homingTarget"),arrow?null:player.AimTarget),"class tracking retains only the selected target");
        Check(mesh.name.EndsWith(arrow?"ArrowBody":player.HeroClass==HeroClass.Summoner?"ContractBolt":"CasterBolt"),"basic attack loads the matching real authored resource");
        var points=mesh.vertices.Select(v=>Vector3.Scale(v,Vector3.Scale(body.localScale,shot.transform.localScale))).ToArray();
        float width=points.Max(v=>v.x)-points.Min(v=>v.x),height=points.Max(v=>v.y)-points.Min(v=>v.y),depth=points.Max(v=>v.z)-points.Min(v=>v.z);
        if(arrow)Check(width>=.17f&&height>=.9f&&depth>=.04f&&width<.24f,"arrow has a readable shaft head fletching and material thickness");
        else Check(width>=.4f&&height>=.35f&&depth>=.4f&&width<=.49f&&height<=.49f&&depth<=.49f,"mage solid volume remains readable and close to collision diameter");
        Check(((Material)Field(shot,"bodyMaterial")).Values["_CoreLight"]==(arrow?0:.32f),"caster lighting exposes material without overbright arrow metal");
        Check(!body.gameObject.GetComponent<TrailRenderer>().emitting,"basic body remains solid without a geometric ribbon");
    }
    static void Main(string[] args)
    {
        Resources.Root=args[0];
        foreach(HeroClass hero in new[]{HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner})foreach(bool low in new[]{false,true})foreach(bool mobile in new[]{false,true})foreach(float altitude in new[]{0f,6f})foreach(bool targeted in new[]{false,true})
        {
            var player=Reset(hero,altitude,low,mobile);
            if(targeted){player.AimTarget=new GameObject("Enemy").AddComponent<EnemyController>();player.AimTarget.transform.position=new Vector3(player.transform.position.x+2,altitude+1,8);}
            player.Attack();Check(Latest()!=null,"actual ranged entry creates a physical basic attack");CheckShot(player,Latest(),targeted);
        }
        foreach(HeroClass hero in new[]{HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner})
        {
            var player=Reset(hero,6);player.model.HasSocket=false;player.Attack();Check(Near(Latest().transform.position,player.transform.position+Vector3.up*1.15f+player.transform.forward*.45f),"missing authored socket retains a physical raised fallback");
            player=Reset(hero);CombatSight.Wall=.25f;player.Attack();Check(Latest()==null&&DestructibleProp.Hits==1,"blocked physical socket cannot launch through cover");
        }
        foreach(bool reduced in new[]{false,true})foreach(bool mobile in new[]{false,true})foreach(CombatVisualPriority priority in new[]{CombatVisualPriority.RealContact,CombatVisualPriority.Finale,CombatVisualPriority.Decoration})
        {
            var player=Reset(HeroClass.Vanguard,0,reduced,mobile);int cap=reduced?12:mobile?20:32;
            for(int i=0;i<cap;i++)CombatVisualLease.Attach(new GameObject("Optional ornament"),priority);
            Check(CombatVisualLease.Active==cap,"optional visuals fill the actual tier budget");player.Attack();
            var body=GameObject.All.LastOrDefault(o=>o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null);
            Check(body!=null&&GameObject.All.Any(o=>o.activeInHierarchy&&o.name=="Forged swinging blade"&&o.GetComponent<MeshFilter>().sharedMesh.name.EndsWith("Sword")),"basic sword body survives a saturated optional budget");
            Check(player.MeleeCount==1&&Near((float)Field(body.GetComponent<FilledSkillVfx>(),"life"),.34f)&&CombatVisualLease.Active==cap,"protected sword retains actual melee and bounded lifespan without expanding optional pool");
            Time.deltaTime=.339f;body.Call("Update");Check(body.activeInHierarchy,"basic sword remains visible through ordinary contact duration");
            Time.deltaTime=.002f;body.Call("Update");Check(!body.activeInHierarchy,"basic sword body retires after its finite lifetime");
            CombatFx.Slash(player.transform.position,player.transform.forward,2.3f,new Color(1,1,1));Check(!GameObject.All.Any(o=>o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null),"decorative slash remains budgeted under saturation");
        }
        foreach(string exit in new[]{"death","epoch","session"})
        {
            var player=Reset(HeroClass.Vanguard);player.Attack();var body=GameObject.All.Last(o=>o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null);
            if(exit=="death")player.IsDead=true;else if(exit=="epoch")player.CombatEpoch++;else GameSession.Instance=new GameSession{Player=player};Time.deltaTime=.01f;body.Call("Update");Check(!body.activeInHierarchy,"protected basic body follows actual combat lifetime: "+exit);
        }
        Console.WriteLine("PASS "+checks+" production basic release/socket/raised-empty-flight/real-mesh-bounds/cover/finite-sword-budget checks; managed, not native GPU acceptance.");
    }
}
