using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        // Presentation follows the granted status timer; it never extends control or cancels attacks.
        // Restore the previous overlay before either a fresh base animation or a paused resample.
        private readonly EnemyKnockdownPose knockdownPose = new EnemyKnockdownPose();
        private Transform[] knockdownJoints;
        private Quaternion[] knockdownBaseRotations, knockdownDisplayedRotations;
        private Vector3 knockdownDisplayedPosition;
        private Quaternion knockdownDisplayedRotation;
        private bool knockdownDisplayed;
        private Vector3 knockdownBasePosition;
        private Quaternion knockdownBaseRotation;
        private bool knockdownApplied;
        private int knockdownFrame = -1;
        private void RestoreKnockdownBase()
        {
            if (!knockdownApplied || dying) return;
            transform.localPosition = knockdownBasePosition;
            transform.localRotation = knockdownBaseRotation;
            for (int i=0;i<knockdownJoints.Length;i++)
                if (knockdownJoints[i]!=null) knockdownJoints[i].localRotation=knockdownBaseRotations[i];
            knockdownApplied=false;
        }
        internal bool TryAnimateKnockdown(float downRemaining,bool airborne,float delta)
        {
            if (!articulatedEnemy || isHero || enemyOwner==null || enemyOwner.IsBoss) return false;
            // EnemyDeathDissolve captures the final displayed pose and owns it thereafter.
            if (dying || enemyOwner.IsDead) return true;
            RestoreKnockdownBase();
            bool heavy=enemyOwner.Kind==EnemyKind.Guardian;
            if(knockdownFrame!=Time.frameCount)
            {
                knockdownFrame=Time.frameCount;
                knockdownPose.Advance(downRemaining,airborne,delta,heavy);
            }
            float weight=knockdownPose.Weight;
            if(weight<=0){knockdownDisplayed=false;return true;} // Ordinary hurt/recoil retains the complete current attack pose.
            if(knockdownJoints==null)
            {
                knockdownJoints=new[]{spine,pelvis,headRig,leftArm,rightArm,leftElbow,rightElbow,leftLeg,rightLeg,leftKnee,rightKnee,body};
                knockdownBaseRotations=new Quaternion[knockdownJoints.Length];
                knockdownDisplayedRotations=new Quaternion[knockdownJoints.Length];
            }
            knockdownBasePosition=transform.localPosition;
            knockdownBaseRotation=transform.localRotation;
            for(int i=0;i<knockdownJoints.Length;i++)
                if(knockdownJoints[i]!=null)knockdownBaseRotations[i]=knockdownJoints[i].localRotation;
            knockdownApplied=true;
            // The left forearm braces first during recovery; the bent near knee follows.
            float brace=knockdownPose.Brace;
            transform.localRotation=knockdownBaseRotation*Quaternion.Euler((heavy?-78f:-82f)*weight,0,8f*weight);
            transform.localPosition=knockdownBasePosition+new Vector3(.06f,.30f,-.06f)*weight*transform.localScale.y;
            DownJoint(0,new Vector3(-12f,5f,-5f),weight);
            DownJoint(1,new Vector3(4f,-8f,7f),weight);
            DownJoint(2,new Vector3(12f,-14f,10f),weight);
            DownJoint(3,new Vector3(-8f-brace*24f,0,-24f),weight);
            DownJoint(4,new Vector3(heavy?45f:-15f,10f,28f),weight);
            DownJoint(5,new Vector3(-24f+brace*16f,0,0),weight);
            DownJoint(6,new Vector3(-38f,0,0),weight);
            DownJoint(7,new Vector3((heavy?-12f:-18f)-brace*18f,0,-13f),weight);
            DownJoint(8,new Vector3(heavy?13.6f:8f,0,16f),weight);
            DownJoint(9,new Vector3(38f+brace*25f,0,0),weight);
            DownJoint(10,new Vector3(19f,0,0),weight);
            knockdownDisplayedPosition=transform.localPosition;
            knockdownDisplayedRotation=transform.localRotation;
            for(int i=0;i<knockdownJoints.Length;i++)
                if(knockdownJoints[i]!=null)knockdownDisplayedRotations[i]=knockdownJoints[i].localRotation;
            knockdownDisplayed=true;
            return true;
        }
        private void RestoreKnockdownForDeath()
        {
            // Animate may already have restored the base this Update while status LateUpdate
            // has not run yet. Hand death the last complete displayed pose, never that scratch pose.
            if(!knockdownDisplayed)return;
            transform.localPosition=knockdownDisplayedPosition;
            transform.localRotation=knockdownDisplayedRotation;
            for(int i=0;i<knockdownJoints.Length;i++)
                if(knockdownJoints[i]!=null)knockdownJoints[i].localRotation=knockdownDisplayedRotations[i];
        }
        private void DownJoint(int index,Vector3 degrees,float weight)
        {
            var joint=knockdownJoints[index];if(joint==null)return;
            joint.localRotation=Quaternion.Slerp(knockdownBaseRotations[index],Quaternion.Euler(degrees.x,degrees.y,degrees.z),weight);
        }
    }
    // Small allocation-free envelope. Airborne supersedes the grounded pose smoothly;
    // repeated controls reverse the current recovery without restarting from an upright snap.
    internal sealed class EnemyKnockdownPose
    {
        internal float Weight {get;private set;}
        internal float Brace {get;private set;}
        internal void Advance(float remaining,bool airborne,float delta,bool heavy)
        {
            if(delta<=0 || float.IsNaN(delta) || float.IsInfinity(delta))return;
            float recovery=heavy?.34f:.26f;
            float target=airborne || float.IsNaN(remaining) || float.IsInfinity(remaining)?0:Mathf.SmoothStep(0,1,Mathf.Clamp01(remaining/recovery));
            Weight=Mathf.MoveTowards(Weight,target,delta/(heavy?.18f:.13f));
            float braceTarget=airborne?0:4f*Weight*(1f-Weight);
            Brace=Mathf.MoveTowards(Brace,braceTarget,delta*9f);
        }
    }
}
