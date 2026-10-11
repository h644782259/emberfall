using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        // Pose identity follows the actual skill, never its palette or damage values.
        // Reuses the same nominal timeline and recovery sampler as committed attacks.
        private void ApplyCasterSkillPose(float t)
        { ApplyCasterSkillPose(t,actionSkill); }
        private void ApplyCasterSkillPose(float t,int skill)
        {
            CasterPoseFamily family=CasterPoseRecipe.For(heroClass,skill);
            Vector3 back=new Vector3(-7,-13,-3),release=new Vector3(9,16,3);
            Vector3 rightReady=new Vector3(-96,-12,25),rightCast=new Vector3(-73,17,12);
            Vector3 staffReady=new Vector3(55,0,-18),staffCast=new Vector3(103,0,-5);
            Vector3 leftReady=new Vector3(-48,12,-32),leftCast=new Vector3(-86,0,-13);
            Vector3 elbowReady=new Vector3(-60,0,0),elbowCast=new Vector3(-14,0,0);
            if(family==CasterPoseFamily.Ground)
            {
                back=new Vector3(-12,0,0);release=new Vector3(17,0,0);
                rightReady=new Vector3(-128,-8,23);rightCast=new Vector3(-27,-12,19);
                staffReady=new Vector3(36,0,-12);staffCast=new Vector3(8,0,-9);
                leftReady=new Vector3(-102,15,-37);leftCast=new Vector3(-34,-12,-34);
                elbowReady=new Vector3(-40,0,0);elbowCast=new Vector3(-14,0,0);
            }
            else if(family==CasterPoseFamily.SelfGuard)
            {
                back=new Vector3(-4,8,0);release=new Vector3(-3,0,0);
                rightReady=new Vector3(-25,-14,24);rightCast=new Vector3(-30,-8,24);
                staffReady=new Vector3(10,0,-14);staffCast=new Vector3(14,0,-14);
                leftReady=new Vector3(-28,-28,-28);leftCast=new Vector3(-46,-40,-11);
                elbowReady=new Vector3(-76,0,0);elbowCast=new Vector3(-102,0,0);
            }
            else if(family==CasterPoseFamily.Contract)
            {
                back=new Vector3(-8,-8,0);release=new Vector3(3,-12,-3);
                rightReady=new Vector3(-32,0,22);rightCast=new Vector3(-37,-8,24);
                staffReady=new Vector3(9,0,-18);staffCast=new Vector3(15,0,-16);
                leftReady=new Vector3(-65,20,-62);leftCast=new Vector3(-57,32,-72);
                elbowReady=new Vector3(-66,0,0);elbowCast=new Vector3(-18,0,0);
            }
            spine.localRotation*=Pose(Vector3.zero,back,release,t);
            rightArm.localRotation=Pose(new Vector3(-10,0,10),rightReady,rightCast,t);
            rightElbow.localRotation=Pose(new Vector3(-12,0,0),new Vector3(-38,0,0),new Vector3(-12,0,0),t);
            staffRig.localRotation=Pose(new Vector3(12,0,-12),staffReady,staffCast,t);
            leftArm.localRotation=Pose(new Vector3(-12,0,-10),leftReady,leftCast,t);
            leftElbow.localRotation=Pose(new Vector3(-20,0,0),elbowReady,elbowCast,t);
        }
    }
}
