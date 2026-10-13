using UnityEngine;
namespace Emberfall
{
    public sealed partial class PlayerController
    {
        private float blinkFollowupUntil,blinkFollowupAge;
        private int blinkFollowupEpoch,blinkFollowupFrame;
        private bool blinkFollowupPending;
        private Vector3 blinkFollowupAim,blinkFollowupForward;
        public const int BlinkFollowupRequiredLevel=5;
        public bool BlinkFollowupLearned => session!=null&&session.Progression.Profile.level>=BlinkFollowupRequiredLevel;
        public float BlinkFollowupDamageMultiplier => HeroClass==HeroClass.Vanguard?2.2f:HeroClass==HeroClass.Ranger?2f:HeroClass==HeroClass.Arcanist?1.8f:1.6f;
        public float BlinkFollowupRemaining => !BlinkFollowupLearned||IsDead||CombatEpoch!=blinkFollowupEpoch?0:Mathf.Max(0,Mathf.Min(blinkFollowupUntil-Time.time,dodgeCooldown));
        public string BlinkFollowupName => HeroClass==HeroClass.Vanguard?"裂空斩":HeroClass==HeroClass.Arcanist?"霜雷环":HeroClass==HeroClass.Ranger?"穿影箭":"缚灵印";
        private void ClearBlinkFollowup(){blinkFollowupUntil=0;blinkFollowupPending=false;}
        internal bool TryBlinkFollowup()
        {
            if(BlinkFollowupRemaining<=0||session.InputBlocked||ImpactControlled||jumping||TraversalStartedThisFrame||SkillReleaseInputBlocked||skillBasicRecovery.Blocked||
                charge!=null&&charge.IsCharging||targeting!=null&&targeting.IsTargeting)return false;
            StopSkillChannel();ResolveBasicAttackFacing();FaceAim();
            blinkFollowupAim=ValidAimTarget(AimTarget)?AimTarget.transform.position:aimPoint;
            blinkFollowupForward=transform.forward;
            blinkFollowupUntil=0;blinkFollowupAge=0;blinkFollowupPending=true;blinkFollowupFrame=Time.frameCount;
            AnimateSkillPreparation(0,0);return true;
        }
        private void AdvanceBlinkFollowup(float dt)
        {
            if(!blinkFollowupPending)return;
            if(IsDead||CombatEpoch!=blinkFollowupEpoch||session.CombatEffectsEnded){ClearBlinkFollowup();return;}
            if(session.InputBlocked||dt<=0||Time.frameCount==blinkFollowupFrame)return;
            blinkFollowupAge+=dt;AnimateSkillPreparation(0,Mathf.Clamp01(blinkFollowupAge/.14f));
            if(blinkFollowupAge<.14f)return;
            blinkFollowupPending=false;skillReleaseInputFrame=Time.frameCount;
            transform.rotation=Quaternion.LookRotation(blinkFollowupForward);
            model.PlayAction(0,false);skillBasicRecovery.Begin(HeroClass,0,false);attackAnimation=1;
            Color tint=GameBalance.ClassColor(HeroClass);int cast=NewCastId();
            if(HeroClass==HeroClass.Vanguard)
            {
                FilledSkillVfx.Crescent(this,transform.position,transform.forward,3f,tint,priority:CombatVisualPriority.RealContact,castId:cast);
                Melee(3f,130,Damage(BlinkFollowupDamageMultiplier),1.1f,.2f,castId:cast);
            }
            else if(HeroClass==HeroClass.Ranger)
            {
                Vector3 start=transform.position,end=CombatSight.GroundPoint(start,start+transform.forward*7f);
                FilledSkillVfx.Thrust(this,start+Vector3.up,end+Vector3.up,tint,.3f,.45f,CombatVisualPriority.RealContact);
                Vector3 line=end-start;float length=line.sqrMagnitude;
                for(int i=session.Enemies.Count-1;i>=0;i--)
                {
                    var enemy=session.Enemies[i];if(!ValidAimTarget(enemy))continue;
                    Vector3 delta=CombatFx.Flat(enemy.transform.position-start);
                    float along=length>.01f?Mathf.Clamp01(Vector3.Dot(delta,line)/length):0;
                    if((delta-line*along).magnitude>.45f+enemy.NavigationRadius||!CombatSight.Direct(start,enemy.transform.position))continue;
                    var hit=Damage(BlinkFollowupDamageMultiplier);enemy.TakeDamage(hit.Amount,transform.forward,.35f,.1f,critical:hit.IsCritical,practiceCastId:cast);
                }
            }
            else
            {
                bool frost=HeroClass==HeroClass.Arcanist;float radius=frost?3f:2.6f;
                Vector3 at=frost?transform.position:CombatSight.GroundPoint(transform.position,transform.position+Vector3.ClampMagnitude(CombatFx.Flat(blinkFollowupAim-transform.position),6));
                FilledSkillVfx.Impact(this,at,radius,frost?FilledVfxKind.Lightning:FilledVfxKind.Summon,tint,CombatVisualPriority.RealContact,cast);
                HitArea(at,radius,Damage(BlinkFollowupDamageMultiplier),frost?.45f:0,frost?.2f:.55f,cast);
            }
            GameAudio.PlaySkill(HeroClass,0);session.RecordCombatAction("闪现衔接");
            session.SpawnMechanismText(transform.position+Vector3.up*2.5f,BlinkFollowupName,tint);
        }
    }
}
