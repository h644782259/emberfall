using UnityEngine;

namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private float controlPoseWeight;
        private int controlPoseFrame=-1;
        private void ApplyEnemyControlPose(float delta)
        {
            if(!articulatedEnemy||isHero||enemyOwner==null||enemyOwner.IsDead||enemyOwner.IsBoss)return;
            var status=enemyOwner.StatusEffects;
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
