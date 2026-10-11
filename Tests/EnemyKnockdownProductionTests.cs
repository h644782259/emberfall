using System;
using System.Linq;
using Emberfall;
using UnityEngine;
namespace Emberfall
{
    public enum EnemyKind{Goblin,Guardian,Slime,Wisp}
    public sealed partial class EnemyController:MonoBehaviour{private ThreatAdmissionPolicy threatAdmission;private int threatMember;public bool IsDead,IsBoss;public EnemyKind Kind;public EnemyStatusEffects StatusEffects;}
    public partial class EnemyStatusEffects:MonoBehaviour
    {
        private EnemyController enemy;private Transform model;private CombatModel knockdownModel;private bool wasDown;
        private float downTime;public float MoveMultiplier=>1;public bool IsAirborne;public float AirborneHeight;
        public float Remaining=>downTime;
        public void Setup(EnemyController owner){enemy=owner;}
        public void Timer(float remaining){downTime=remaining;}
        public void Sample(){LateUpdate();}
    }
    public sealed partial class CombatModel:MonoBehaviour
    {
        private bool isHero,articulatedEnemy,dying,slime,floating,quadruped,treantCompanion,pilotCharging;
        private EnemyController enemyOwner;private Quaternion bodyRestRotation=Quaternion.identity;
        private Transform body,decoration,tailRig,wolfJaw,spine,pelvis,headRig,leftArm,rightArm,leftElbow,rightElbow,leftLeg,rightLeg,leftKnee,rightKnee;
        private Transform[] paws=new Transform[4];private Vector3[] pawOrigins=new Vector3[4];private Vector3 companionBodyScale;
        private float smoothedSpeed,gaitPhase,phase,recoilStarted=-10f,recoilStrength;private Vector3 recoilDirection;
        private LocomotionPoseState locomotion=new LocomotionPoseState();private EnemyPosePhase enemyActionPhase;private float enemyActionProgress;
        private LargeRigBoundary largeBossRig;
        private sealed class LargeRigBoundary{public void Animate(float speed,float attack){throw new Exception("unexpected large rig");}}
        private void AnimateHero(float speed,float attack,bool hurt,float delta){throw new Exception("hero outside test scope");}
        private void ApplyCompanionPose(){}
        public void Setup(EnemyController owner,bool supported=true)
        {
            enemyOwner=owner;articulatedEnemy=supported;body=new GameObject("body").transform;body.SetParent(transform,false);
            var joints=new Transform[11];for(int i=0;i<joints.Length;i++){joints[i]=new GameObject("joint"+i).transform;joints[i].SetParent(transform,false);joints[i].localPosition=new Vector3((i%2-.5f)*.4f,.7f+i*.08f,0);}
            spine=joints[0];pelvis=joints[1];headRig=joints[2];leftArm=joints[3];rightArm=joints[4];leftElbow=joints[5];rightElbow=joints[6];leftLeg=joints[7];rightLeg=joints[8];leftKnee=joints[9];rightKnee=joints[10];
        }
        public float DownWeight=>knockdownPose.Weight;
        public Quaternion[] Snapshot()=>new[]{transform.localRotation,spine.localRotation,pelvis.localRotation,headRig.localRotation,leftArm.localRotation,rightArm.localRotation,leftElbow.localRotation,rightElbow.localRotation,leftLeg.localRotation,rightLeg.localRotation,leftKnee.localRotation,rightKnee.localRotation,body.localRotation};
        public void Death(){dying=true;}
        public void Attack(EnemyPosePhase value,float progress){enemyActionPhase=value;enemyActionProgress=progress;}
    }
}
public static class EnemyKnockdownProductionTests
{
    static int checks;
    static void C(bool value,string label){checks++;if(!value)throw new Exception(label);}
    internal static bool Same(Quaternion[] a,Quaternion[] b)=>a.Zip(b,(x,y)=>(x.Rotate(Vector3.up)-y.Rotate(Vector3.up)).sqrMagnitude<.00000001f&&(x.Rotate(Vector3.forward)-y.Rotate(Vector3.forward)).sqrMagnitude<.00000001f).All(v=>v);
    internal static (CombatModel model,EnemyController enemy,EnemyStatusEffects status) Create(bool heavy=false,bool supported=true,bool boss=false)
    {
        GameSession.Instance=new GameSession{HasStarted=true};Time.deltaTime=.016f;Time.time=10;
        var host=new GameObject("logical root");host.transform.position=new Vector3(5,0,8);var enemy=host.AddComponent<EnemyController>();enemy.Kind=heavy?EnemyKind.Guardian:EnemyKind.Goblin;enemy.IsBoss=boss;
        var status=host.AddComponent<EnemyStatusEffects>();status.Setup(enemy);enemy.StatusEffects=status;
        var visual=new GameObject("visual");visual.transform.SetParent(host.transform,false);visual.transform.localScale=Vector3.one*(heavy?1.15f:.8f);var model=visual.AddComponent<CombatModel>();model.Setup(enemy,supported);return(model,enemy,status);
    }
    internal static void Frame((CombatModel model,EnemyController enemy,EnemyStatusEffects status) scene,float remaining,float delta=.016f,bool animate=true)
    {
        Time.frameCount++;Time.deltaTime=delta;Time.time+=Math.Max(0,delta);scene.status.Timer(remaining);
        if(animate)scene.model.Animate(0,0,false);scene.status.Sample();C(scene.status.Remaining==remaining,"visual never modifies granted control timer");
        C(scene.enemy.transform.position.x==5&&scene.enemy.transform.position.z==8&&scene.enemy.transform.localRotation.Equals(Quaternion.identity),"visual never moves or rotates logical root");
    }
    public static string Run()
    {
        foreach(bool heavy in new[]{false,true})foreach(float dt in new[]{1f/120,1f/60,1f/30,.1f})
        {
            var s=Create(heavy);Frame(s,0,dt);var upright=s.model.Snapshot();Frame(s,1,dt);
            C(s.model.DownWeight>0&&s.model.DownWeight<1,"fall has intermediate pose");C(!Same(upright,s.model.Snapshot()),"actual joint pose moves during fall");
            float last=s.model.DownWeight;for(float age=dt;age<.4f;age+=dt){Frame(s,1-age,dt);C(s.model.DownWeight>=last-.0001f,"fall advances continuously into hold");last=s.model.DownWeight;}
            C(s.model.DownWeight>.99f,"full down hold reached");
            Frame(s,.55f,dt);var held=s.model.Snapshot();Frame(s,.50f,dt);C(Same(held,s.model.Snapshot()),"stable hold has no stance drift");
            for(float remaining=.5f;remaining>0;remaining-=dt)Frame(s,Math.Max(0,remaining),dt);
            Frame(s,0,dt);C(s.model.DownWeight<.001f,"get-up completes at real status expiry");C(Same(upright,s.model.Snapshot()),"idle pose restored without one-frame snap after recovery");
            Console.WriteLine("TIMELINE heavy="+heavy+" dt="+dt+" fall/hold/get-up PASS");
        }
        foreach(bool heavy in new[]{false,true})
        {
            var s=Create(heavy);for(int i=0;i<20;i++)Frame(s,.7f);
            for(int j=0;j<3;j++)Frame(s,.10f);float before=s.model.DownWeight;Frame(s,.6f);C(s.model.DownWeight>=before&&s.model.DownWeight-before<.13f,"re-control reverses smoothly from current pose");
            var previous=s.model.Snapshot();GameSession.Instance.InputBlocked=true;
            for(int i=0;i<20;i++){Frame(s,.6f,.1f,false);C(Same(previous,s.model.Snapshot()),"paused pose stays exact");}
            GameSession.Instance.InputBlocked=false;Frame(s,.5f);C(s.model.DownWeight>before,"resume advances retained fall");
            s.status.IsAirborne=true;s.status.AirborneHeight=1.25f;
            for(int i=0;i<20;i++)Frame(s,.5f);
            C(s.model.DownWeight==0,"airborne supersedes ground pose");C(Math.Abs(s.model.transform.localPosition.y-1.25f)<.00001f,"airborne authoritative height remains exact");
            s.status.IsAirborne=false;s.status.AirborneHeight=0;Frame(s,.5f);C(s.model.DownWeight>0&&s.model.DownWeight<.2f,"landing resumes down pose without snap");
            var death=s.model.Snapshot();var pos=s.model.transform.localPosition;s.model.Death();
            for(int i=0;i<20;i++){Frame(s,.5f,.1f,false);C(Same(death,s.model.Snapshot())&&(s.model.transform.localPosition-pos).sqrMagnitude<.000001f,"death owns final pose");}
        }
        {
            var s=Create();s.model.Attack(EnemyPosePhase.Windup,.8f);Frame(s,0);var attacking=s.model.Snapshot();
            s.model.Recoil(Vector3.forward,1);Time.frameCount++;s.model.Animate(0,.8f,true);var recoilPose=s.model.Snapshot();s.status.Sample();
            C(s.model.DownWeight==0&&Same(attacking.Take(12).ToArray(),recoilPose.Take(12).ToArray())&&Same(recoilPose,s.model.Snapshot()),"ordinary hurt never fakes interruption or replaces attack joints");
            C(s.model.transform.localPosition.z>0,"ordinary recoil remains additive");
        }
        foreach(bool boss in new[]{false,true})
        {
            var s=Create(false,!boss?false:true,boss);Frame(s,.7f);C(s.model.DownWeight==0,"unsupported/boss retains original path");
            C(boss?Quaternion.Angle(s.model.transform.localRotation,Quaternion.identity)<.06f:Quaternion.Angle(s.model.transform.localRotation,Quaternion.Euler(0,0,72))<.06f,"legacy fallback rotation preserved");
        }
        {
            var s=Create();Frame(s,.6f);var pose=s.model.Snapshot();float weight=s.model.DownWeight;s.status.Sample();C(s.model.DownWeight==weight&&Same(pose,s.model.Snapshot()),"duplicate late sample advances once and never accumulates");
            foreach(float invalid in new[]{0f,-1f,float.NaN,float.PositiveInfinity}){Frame(s,.6f,invalid,false);C(s.model.DownWeight==weight&&Same(pose,s.model.Snapshot()),"invalid delta freezes visual pose");}
        }
        return "PASS: "+checks+" actual Animate/knockdown/status-LateUpdate assertions; managed transforms only, no Unity execution";
    }
}
