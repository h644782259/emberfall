using UnityEngine;

namespace Emberfall
{
    public sealed partial class CombatModel
    {
        public void ApplyHeroImpactPose(float height,float velocity,float recovery)
        {
            if(!isHero||spine==null)return;
            float lift=Mathf.Clamp01(height/.25f),fall=Mathf.Clamp01(-velocity/4f),weight=height>0?1f:Mathf.Clamp01(recovery/.18f);
            spine.localRotation*=Quaternion.Euler((Mathf.Lerp(12,-18,lift)+fall*14)*weight,0,0);
            leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(-35-lift*20,0,-18-lift*18),weight);
            rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(-28-lift*24,0,18+lift*18),weight);
            leftLeg.localRotation=Quaternion.Slerp(leftLeg.localRotation,Quaternion.Euler(-12-lift*16,0,-5),weight);
            rightLeg.localRotation=Quaternion.Slerp(rightLeg.localRotation,Quaternion.Euler(-10-lift*8,0,5),weight);
            leftKnee.localRotation=Quaternion.Slerp(leftKnee.localRotation,Quaternion.Euler(24+lift*20+fall*12,0,0),weight);
            rightKnee.localRotation=Quaternion.Slerp(rightKnee.localRotation,Quaternion.Euler(22+lift*16+fall*10,0,0),weight);
        }
        private float bossParalysisPose;
        private int bossParalysisFrame=-1;
        private void ApplyBossParalysisPose(float delta)
        {
            if(enemyOwner==null||!enemyOwner.IsBoss||enemyOwner.IsDead)return;
            if(delta>0&&bossParalysisFrame!=Time.frameCount)
            {
                bossParalysisFrame=Time.frameCount;
                bossParalysisPose=Mathf.MoveTowards(bossParalysisPose,enemyOwner.IsBossParalyzed?1f:0f,
                    delta/(enemyOwner.IsBossParalyzed?.5f:.65f));
            }
            float weight=Mathf.SmoothStep(0,1,bossParalysisPose);
            if(weight<=0)return;
            if(largeBossRig!=null){largeBossRig.ApplyParalysisPose(weight);return;}
            if(articulatedEnemy)
            {
                // Knees yield first; the chest then folds over the supporting leg.
                float fold=Mathf.SmoothStep(0,1,Mathf.Clamp01((bossParalysisPose-.15f)/.85f));
                transform.localPosition+=Vector3.down*(.38f*weight);
                spine.localRotation=Quaternion.Slerp(spine.localRotation,Quaternion.Euler(42,0,8),fold);
                headRig.localRotation=Quaternion.Slerp(headRig.localRotation,Quaternion.Euler(24,-8,0),fold);
                leftLeg.localRotation=Quaternion.Slerp(leftLeg.localRotation,Quaternion.Euler(-58,0,-10),weight);
                rightLeg.localRotation=Quaternion.Slerp(rightLeg.localRotation,Quaternion.Euler(-38,0,12),weight);
                leftKnee.localRotation=Quaternion.Slerp(leftKnee.localRotation,Quaternion.Euler(108,0,0),weight);
                rightKnee.localRotation=Quaternion.Slerp(rightKnee.localRotation,Quaternion.Euler(78,0,0),weight);
                leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(-38,0,-20),fold);
                rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(18,0,18),fold);
                leftElbow.localRotation=Quaternion.Slerp(leftElbow.localRotation,Quaternion.Euler(-55,0,0),fold);
                rightElbow.localRotation=Quaternion.Slerp(rightElbow.localRotation,Quaternion.Euler(-12,0,0),fold);
                if(leftFoot!=null)leftFoot.localRotation=Quaternion.Slerp(leftFoot.localRotation,Quaternion.Euler(-40,0,0),weight);
                if(rightFoot!=null)rightFoot.localRotation=Quaternion.Slerp(rightFoot.localRotation,Quaternion.Euler(-32,0,0),weight);
            }
            else if(slime&&body!=null)
                body.localScale=Vector3.Lerp(body.localScale,new Vector3(1.55f,.48f,1.4f),weight);
            else if(body!=null)
            {
                transform.localPosition+=Vector3.down*(.48f*weight);
                body.localRotation=Quaternion.Slerp(body.localRotation,bodyRestRotation*Quaternion.Euler(32,0,18),weight);
            }
        }
        private float controlPoseWeight;
        private int controlPoseFrame=-1;
        private void ApplyEnemyControlPose(float delta)
        {
            if(!articulatedEnemy||isHero||enemyOwner==null||enemyOwner.IsDead||enemyOwner.IsBoss)return;
            var status=enemyOwner.StatusEffects;
            if(status!=null&&status.IsAirborne)
            {
                float lift=Mathf.SmoothStep(0,1,Mathf.Clamp01(status.AirborneHeight/.35f));
                float fall=Mathf.Clamp01(-status.AirborneVelocity/5f);
                spine.localRotation=Quaternion.Slerp(spine.localRotation,Quaternion.Euler(Mathf.Lerp(-18,16,fall),0,0),lift);
                leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(-55,0,-35),lift);
                rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(-48,0,32),lift);
                leftLeg.localRotation=Quaternion.Slerp(leftLeg.localRotation,Quaternion.Euler(-22,0,-8),lift);
                rightLeg.localRotation=Quaternion.Slerp(rightLeg.localRotation,Quaternion.Euler(-12,0,8),lift);
                leftKnee.localRotation=Quaternion.Slerp(leftKnee.localRotation,Quaternion.Euler(36+fall*16,0,0),lift);
                rightKnee.localRotation=Quaternion.Slerp(rightKnee.localRotation,Quaternion.Euler(24+fall*20,0,0),lift);
                return;
            }
            bool supported=enemyOwner.IsStunned && (status==null||!status.KnockedDown&&!status.IsAirborne);
            if(delta>0&&!float.IsNaN(delta)&&!float.IsInfinity(delta)&&controlPoseFrame!=Time.frameCount)
            {
                controlPoseFrame=Time.frameCount;
                controlPoseWeight=Mathf.MoveTowards(controlPoseWeight,supported?1:0,delta/(supported?.09f:.16f));
            }
            float weight=controlPoseWeight;
            if(weight<=0)return;
            // A granted stun visibly stops the gait and transfers weight into a
            // bent supporting stance. Frozen limbs hold this same rigid brace;
            // the status timer alone decides when navigation may resume.
            bool frozen=status!=null&&status.IsFrozen;
            spine.localRotation=Quaternion.Slerp(spine.localRotation,Quaternion.Euler(frozen?5:14,0,0),weight);
            headRig.localRotation=Quaternion.Slerp(headRig.localRotation,Quaternion.Euler(frozen?-2:-7,0,0),weight);
            leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(-26,0,-15),weight);
            rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(-30,0,15),weight);
            leftElbow.localRotation=Quaternion.Slerp(leftElbow.localRotation,Quaternion.Euler(-42,0,0),weight);
            rightElbow.localRotation=Quaternion.Slerp(rightElbow.localRotation,Quaternion.Euler(-46,0,0),weight);
            leftLeg.localRotation=Quaternion.Slerp(leftLeg.localRotation,Quaternion.Euler(-12,0,-3),weight);
            rightLeg.localRotation=Quaternion.Slerp(rightLeg.localRotation,Quaternion.Euler(-12,0,3),weight);
            leftKnee.localRotation=Quaternion.Slerp(leftKnee.localRotation,Quaternion.Euler(24,0,0),weight);
            rightKnee.localRotation=Quaternion.Slerp(rightKnee.localRotation,Quaternion.Euler(24,0,0),weight);
            if(leftFoot!=null)leftFoot.localRotation=Quaternion.Slerp(leftFoot.localRotation,Quaternion.Euler(-12,0,0),weight);
            if(rightFoot!=null)rightFoot.localRotation=Quaternion.Slerp(rightFoot.localRotation,Quaternion.Euler(-12,0,0),weight);
        }
    }
}
