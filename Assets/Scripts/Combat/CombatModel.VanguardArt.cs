using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private VanguardActionLibrary vanguardArt;
        private Transform[] vanguardArtBones;
        private bool vanguardHurt;
        private float vanguardHurtStart=-10;
        private Quaternion[] vanguardDeathRest;
        private void ConfigureVanguardArt()
        {
            if(heroClass!=HeroClass.Vanguard||!AuthoredActorMeshes.Enabled)return;
            var joints=new[]{pelvis,spine,headRig,leftArm,rightArm,leftKnee,rightKnee,cloak,swordRig};
            foreach(var joint in joints)if(joint==null)return;
            // Selected once at construction, independent of rarity, upgrade, or equipment changes.
            // Unsupported/missing curve library preserves all of the original rig's actions.
            vanguardArt=VanguardActionLibrary.Load();
            if(vanguardArt!=null)vanguardArtBones=joints;
        }
        private void VanguardLayer(VanguardArtPose pose,float progress,float weight)
        {
            if(weight<=0)return;
            for(int i=0;i<vanguardArtBones.Length;i++)
                vanguardArtBones[i].localRotation*=Quaternion.Euler(vanguardArt.Sample(pose,i,progress)*weight);
        }
        private void ApplyAuthoredVanguardPose(bool acting,float progress,bool hurt)
        {
            if(vanguardArt==null||pilotOwnerDead||dying)return;
            float clock=isolatedPreview?previewTime:Time.time;
            float phase=locomotion.Phase/(Mathf.PI*2);
            VanguardLayer(VanguardArtPose.Idle,Mathf.Repeat(clock*.5f,1),1-locomotion.Speed);
            if(!pilotAirborne)
            {
                float x=locomotion.Side,z=locomotion.Forward;
                float normalization=Mathf.Max(1,Mathf.Abs(x)+Mathf.Abs(z));
                VanguardLayer(z>=0?VanguardArtPose.Forward:VanguardArtPose.Backward,phase,Mathf.Abs(z)/normalization);
                VanguardLayer(x>=0?VanguardArtPose.Right:VanguardArtPose.Left,phase,Mathf.Abs(x)/normalization);
                VanguardLayer(visualMotion.Turn>=0?VanguardArtPose.TurnRight:VanguardArtPose.TurnLeft,Mathf.Abs(visualMotion.Turn),1);
            }
            VanguardLayer(VanguardArtPose.Jump,locomotion.JumpTuck,1);
            VanguardLayer(VanguardArtPose.Landing,locomotion.Landing,1);
            // Keep exact existing action clocks: these clips add torso,
            // free-arm and cloak counterbalance; sword/right arm offsets are zero in both.
            if(acting&&((actionBasic&&actionSkill!=-2)||actionSkill==0))VanguardLayer(actionBasic?VanguardArtPose.Basic:VanguardArtPose.Skill,progress,1);
            if(hurt&&!vanguardHurt)vanguardHurtStart=clock;
            vanguardHurt=hurt;
            float age=clock-vanguardHurtStart;
            // Damage recoil is additive, does not cancel or restart a committed action.
            if(age>=0&&age<.22f)VanguardLayer(VanguardArtPose.Hit,age/.22f,1);
        }
        private void SampleVanguardDeath()
        {
            if(vanguardArt==null)return;
            if(!pilotOwnerDead){vanguardDeathRest=null;return;}
            if(vanguardDeathRest==null)
            {
                vanguardDeathRest=new Quaternion[vanguardArtBones.Length];
                for(int i=0;i<vanguardArtBones.Length;i++)vanguardDeathRest[i]=vanguardArtBones[i].localRotation;
            }
            for(int i=0;i<vanguardArtBones.Length;i++)vanguardArtBones[i].localRotation=vanguardDeathRest[i];
            // Player death freezes scaled time immediately. Apply the authored terminal
            // local pose once, then re-sample from the snapshot without accumulation.
            // Root fall and the pause remain PlayerController/GameSession-owned.
            VanguardLayer(VanguardArtPose.Death,1,1);
        }
    }
}
