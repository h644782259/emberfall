using UnityEngine;
namespace Emberfall
{
    public enum PlayerHitReaction { Light, Push, Charge, Launch }
    public sealed partial class PlayerController
    {
        internal void ReviveAtCurrentPosition()
        {
            EndGhost();ClearPlayerImpact();CancelCombatPose();hurtFlashUntil=0;hurtTimer=0;nextImpactControl=Time.time+3;
            transform.position=WorldTraversal.NearestWalkable(transform.position,.45f);
            RefreshStats(true);invulnerability=3;model.transform.localRotation=Quaternion.identity;
            model.ResetLocomotion();model.SetBlenderPilotOwnerAlive(true);
        }
        private Vector3 impactVelocity;
        private float impactHeight,impactVerticalVelocity,impactRecovery,impactGroundY,nextImpactControl,hurtFlashUntil;
        private int impactEpoch;
        internal bool ImpactControlled => impactEpoch==CombatEpoch&&(impactRecovery>0||impactHeight>0||impactVerticalVelocity>0);
        public float HurtFlash => Mathf.Clamp01((hurtFlashUntil-Time.time)/.3f);
        private void ClearPlayerImpact()
        {impactHeight=impactVerticalVelocity=impactRecovery=0;impactVelocity=Vector3.zero;}
        private void ReceiveHitReaction(Vector3 origin,PlayerHitReaction reaction,float damage)
        {
            Vector3 direction=CombatFx.Flat(transform.position-origin);if(direction.sqrMagnitude<.001f)direction=-transform.forward;direction.Normalize();
            hurtFlashUntil=Time.time+.3f;
            GameAudio.PlayPlayerHurt(reaction!=PlayerHitReaction.Light);
            if(model!=null)model.Recoil(direction,Mathf.Clamp(damage/Mathf.Max(1,MaxHealth)*8f,.85f,1.7f));
            HitFeedback.Spawn(transform.position+Vector3.up*1.1f,direction,reaction==PlayerHitReaction.Light?.85f:1.55f);
            if(reaction==PlayerHitReaction.Light||Time.time<nextImpactControl||ImpactControlled||Health<=0)return;
            if(charge!=null)charge.Cancel();if(targeting!=null)targeting.Cancel();CancelCombatPose();
            if(FrostStrideActive){burnStrideTime=0;guardTime=0;}
            impactEpoch=CombatEpoch;impactGroundY=WorldTraversal.SurfaceHeight(transform.position,.45f);
            impactHeight=Mathf.Max(0,transform.position.y-impactGroundY);jumping=rangerVault=false;platformFallSpeed=0;
            bool launch=reaction==PlayerHitReaction.Launch;
            impactVerticalVelocity=launch?Mathf.Sqrt(2*EnemyImpactMotion.Gravity*1.05f):0;
            impactVelocity=direction*(reaction==PlayerHitReaction.Charge?6.5f:launch?2.3f:3.4f);
            impactRecovery=launch?.18f:reaction==PlayerHitReaction.Charge?.26f:.20f;
            nextImpactControl=Time.time+(launch?1.6f:1f);
        }
        private void AdvancePlayerImpact(float dt)
        {
            Vector3 ground=transform.position;ground.y=impactGroundY;
            Vector3 step=impactVelocity*(1-Mathf.Exp(-5*dt))/5;
            ground=session.MovePlayerWithBodies(ground,step,dt,ref platformFallSpeed,.45f);impactGroundY=ground.y;
            impactVelocity*=Mathf.Exp(-5*dt);
            bool airborne=impactHeight>0||impactVerticalVelocity>0;
            if(airborne)
            {
                impactHeight+=impactVerticalVelocity*dt-EnemyImpactMotion.Gravity*dt*dt*.5f;
                impactVerticalVelocity-=EnemyImpactMotion.Gravity*dt;
                if(impactHeight<=0)
                {
                    impactHeight=impactVerticalVelocity=0;
                    PhysicalSkillVfx.Impact(ground,Vector3.up,new Color(.65f,.55f,.4f),.7f);
                    GameAudio.PlayPlayerHurt(true);
                }
            }
            else impactRecovery=Mathf.Max(0,impactRecovery-dt);
            transform.position=session.ClipPlayerAirMotion(transform.position,ground+Vector3.up*impactHeight);
            model.SetLocomotion(Vector3.zero,dt,stats.MoveSpeed,false);
            model.Animate(0,0,true);model.ApplyHeroImpactPose(impactHeight,impactVerticalVelocity,impactRecovery);
        }
    }
}
