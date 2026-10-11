using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        // A full step remains visible at strolling speed; distance controls cadence.
        private void ApplyEnemyFootsteps()
        {
            if(isHero||!articulatedEnemy||leftFoot==null||rightFoot==null||enemyActionPhase!=EnemyPosePhase.Idle)return;
            float weight=enemyStepWeight;
            float bob=Mathf.Cos(enemyStepPhase*Mathf.PI*4)*.012f*weight;
            transform.localPosition=Vector3.up*((-.045f-.070f*weight+bob)*transform.localScale.y);
            pelvis.localRotation=Quaternion.Euler(0,Mathf.Sin(enemyStepPhase*Mathf.PI*2)*3*weight,Mathf.Sin(enemyStepPhase*Mathf.PI*2)*1.6f*weight);
            PoseEnemyFoot(leftLeg,leftKnee,leftFoot,enemyStepPhase,weight,bob);
            PoseEnemyFoot(rightLeg,rightKnee,rightFoot,Mathf.Repeat(enemyStepPhase+.5f,1),weight,bob);
            float arm=Mathf.Sin(enemyStepPhase*Mathf.PI*2)*weight;
            leftArm.localRotation=Quaternion.Euler(arm*25,0,-8);rightArm.localRotation=Quaternion.Euler(-arm*25,0,8);
            leftElbow.localRotation=Quaternion.Euler(-18-Mathf.Max(0,-arm)*22,0,0);
            rightElbow.localRotation=Quaternion.Euler(-18-Mathf.Max(0,arm)*22,0,0);
        }
        private void PoseEnemyFoot(Transform hip,Transform knee,Transform ankle,float phase,float weight,float bob)
        {
            const float stance=.6f,cycleDistance=.96f,upper=.37f,lower=.27f;
            float z,lift=0,toe=0;
            if(phase<stance)
            {
                z=cycleDistance*(stance*.5f-phase);
                toe=phase<.08f?Mathf.Lerp(-10,0,phase/.08f):phase>.5f?Mathf.Lerp(0,18,(phase-.5f)/.1f):0;
            }
            else
            {
                float t=(phase-stance)/(1-stance),smooth=t*t*(3-2*t);
                z=cycleDistance*stance*(smooth-.5f);lift=Mathf.Sin(t*Mathf.PI)*.17f;
                toe=18*(1-t)-10*t-Mathf.Sin(t*Mathf.PI)*16;
            }
            float scale=Mathf.Max(.1f,transform.lossyScale.x);
            Vector3 probe=hip.position+transform.forward*(z*scale);
            float ground=enemyOwner==null?0:(WorldTraversal.SurfaceHeight(probe)-WorldTraversal.SurfaceHeight(enemyOwner.transform.position))/scale;
            float pitch=toe*Mathf.Deg2Rad;
            float soleRise=.145f*Mathf.Cos(pitch)+(toe>=0?.27f:.15f)*Mathf.Sin(Mathf.Abs(pitch))-.145f;
            float down=.57f+bob-lift-soleRise-Mathf.Clamp(ground,-.16f,.16f);
            float reach=Mathf.Clamp(Mathf.Sqrt(down*down+z*z),.12f,upper+lower-.002f);
            float aim=Mathf.Atan2(z,down)*Mathf.Rad2Deg;
            float hipAngle=-aim-Mathf.Acos(Mathf.Clamp((upper*upper+reach*reach-lower*lower)/(2*upper*reach),-1,1))*Mathf.Rad2Deg;
            float kneeAngle=180-Mathf.Acos(Mathf.Clamp((upper*upper+lower*lower-reach*reach)/(2*upper*lower),-1,1))*Mathf.Rad2Deg;
            hip.localRotation=Quaternion.Slerp(hip.localRotation,Quaternion.Euler(hipAngle,0,0),weight);
            knee.localRotation=Quaternion.Slerp(knee.localRotation,Quaternion.Euler(kneeAngle,0,0),weight);
            ankle.localRotation=Quaternion.Slerp(ankle.localRotation,Quaternion.Euler(-hipAngle-kneeAngle+toe,0,0),weight);
        }
    }
}
