using System;
using System.Collections.Generic;
using UnityEngine;
using Emberfall;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private readonly Dictionary<int,Material> palette=new Dictionary<int,Material>();
        private void SetBlenderPilotVisible(bool visible){if(visible)throw new Exception("hero-only boundary");}
        internal bool TryBeginLargeBossShutdown()=>false; // Tested actors are ordinary humanoids.
        public void SetLocomotion(Vector3 d,float delta,float speed,bool walking){locomotion.Advance(d.x,d.z,delta,speed,walking,false,0);}
        public void SetEnemyAttackPose(EnemyPosePhase state,float progress){enemyActionPhase=state;enemyActionProgress=Mathf.Clamp01(progress);}
    }
    public sealed partial class EnemyController
    {
        private CombatModel model;private Transform healthRoot;
        private Vector3 walkingDisplacement;private float speed=3,attackAnimation,windup,totalWindup=1;private bool preparing,activeChargePose;
        private GameSession session;
        private ControlBoundary controlPolicy=new ControlBoundary();private sealed class ControlBoundary{internal void Advance(float delta){}}
        private BossBoundary largeBoss;private sealed class BossBoundary{internal BossStateBoundary State=new BossStateBoundary();internal bool Tick(float delta)=>false;}
        private sealed class BossStateBoundary{internal LargeBossPhase Phase;internal bool Interruptible;}
        private WarningBoundary telegraph;private sealed class WarningBoundary{internal void SetInterruptible(bool value){}}
        private float stunTime=1,hurtTime,attackCooldown,chargeTime,flinchUntil;private Vector3 knockVelocity,attackOrigin;
        private bool aggro;private SummonedCompanion companionTarget;private bool CanBeSkillInterrupted=>true;private float NavigationRadius=>.5f;
        public enum ThreatTier{Normal,Elite}public ThreatTier Tier;
        private Material healthFillMaterial=new Material(new Shader());private Color ThreatColor()=>new Color(1,1,1);private void CreateWarning(){}private void ClampPosition(){}
        internal void Bind(CombatModel value){model=value;session=GameSession.Instance;session.Player=new GameObject("player").AddComponent<PlayerController>();}
        // Execute the actual Update prefix through its stunned animation/return branch.
        internal void UpdateAnimation(){Update();}
        internal void Kill(){IsDead=true;BeginDeath();}
    }
    public enum LargeBossPhase{Finished}
    public sealed class SummonedCompanion:MonoBehaviour{public bool IsAlive;public static SummonedCompanion ThreatTarget(EnemyController enemy,Vector3 at)=>null;}
    public static class WorldTraversal{public static Vector3 Move(Vector3 from,Vector3 delta,float radius)=>from+delta;}
    internal partial class EnemyDeathDissolve:MonoBehaviour
    {
        private CombatModel model;private Transform visual;private bool slime,boss;
        internal Quaternion startRotation;internal Vector3 startPosition,startScale;
    }
}
public static class EnemyKnockdownDeathTests
{
    static int checks;
    static void C(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static bool Near(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<.0000001f;
    public static string Run()
    {
        foreach(bool heavy in new[]{false,true})foreach(string phase in new[]{"held","recoil-held","recontrol","airborne-blend","airborne-clear","airborne-paused","airborne-clear-paused","recovered"})foreach(string order in new[]{"before-update","after-update-before-late","after-late"})
        {
            var scene=EnemyKnockdownProductionTests.Create(heavy);scene.enemy.Bind(scene.model);
            for(int i=0;i<24;i++)EnemyKnockdownProductionTests.Frame(scene,.8f);
            if(phase=="recontrol"){for(int i=0;i<3;i++)EnemyKnockdownProductionTests.Frame(scene,.10f);EnemyKnockdownProductionTests.Frame(scene,.8f);}
            if(phase.StartsWith("airborne")){scene.status.IsAirborne=true;scene.status.AirborneHeight=1.25f;for(int i=0;i<(phase.Contains("clear")?24:2);i++)EnemyKnockdownProductionTests.Frame(scene,.8f);}
            if(phase=="recovered")for(int i=0;i<24;i++)EnemyKnockdownProductionTests.Frame(scene,0);
            if(phase=="recoil-held"){scene.model.Recoil(Vector3.forward,1);EnemyKnockdownProductionTests.Frame(scene,.8f);}
            var displayed=scene.model.Snapshot();var position=scene.model.transform.localPosition;
            if(phase.EndsWith("paused"))GameSession.Instance.InputBlocked=true;
            Time.frameCount++;Time.time+=Time.deltaTime;
            if(order!="before-update")scene.enemy.UpdateAnimation();
            if(order=="after-late"){scene.status.Sample();displayed=scene.model.Snapshot();position=scene.model.transform.localPosition;}
            scene.enemy.Kill();var death=scene.enemy.GetComponent<EnemyDeathDissolve>();
            C(death!=null,"actual controller kill creates death owner");
            C(EnemyKnockdownProductionTests.Same(new[]{displayed[0]},new[]{death.startRotation})&&Near(position,death.startPosition),"death capture keeps last complete displayed pose "+phase+" "+order);
            C(EnemyKnockdownProductionTests.Same(displayed,scene.model.Snapshot()),"death restores displayed joint pose before handing off");
            float remaining=scene.status.Remaining;
            // A death-owned transform must survive the old status LateUpdate and later controller frames.
            scene.model.transform.localRotation=Quaternion.Euler(16,31,74);scene.model.transform.localPosition=new Vector3(.4f,.7f,.9f);
            var owned=scene.model.Snapshot();var ownedPosition=scene.model.transform.localPosition;
            scene.status.Sample();Time.frameCount++;scene.enemy.UpdateAnimation();scene.status.Sample();scene.model.BeginDeath();
            C(EnemyKnockdownProductionTests.Same(owned,scene.model.Snapshot())&&Near(ownedPosition,scene.model.transform.localPosition),"death remains sole pose owner after late status and duplicate initialization");
            C(scene.status.Remaining==remaining,"death pose handoff never changes granted control");
        }
        return "PASS: "+checks+" actual stunned controller Update/AnimateModel/BeginDeath -> dissolve capture -> status LateUpdate assertions across 48 death order/phase/rig cases; unstunned AI/damage/particles outside fixture";
    }
}
