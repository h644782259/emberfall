using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        // Accepted jump trajectory stays controller-owned. The rig loads,
        // tucks, extends for contact and absorbs landing through several joints.
        private void ApplyHeroJumpPose(bool casting)
        {
            if (!locomotion.Airborne && locomotion.Landing <= 0) return;
            float t = locomotion.JumpProgress;
            float push = locomotion.Airborne ? 1 - Mathf.SmoothStep(0, 1, t / .20f) : 0;
            float tuck = locomotion.JumpTuck;
            float reach = locomotion.Airborne ? Mathf.SmoothStep(0, 1, (t - .64f) / .36f) : 0;
            float land = locomotion.Landing;
            float weight = push * .12f + land * .16f;
            pelvis.localPosition += new Vector3(0, -weight, push * .04f);
            spine.localPosition += new Vector3(0, -weight * .65f, push * .035f);
            spine.localRotation *= Quaternion.Euler(push * 19 - tuck * 9 + reach * 8 + land * 22, 0, 0);
            headRig.localRotation *= Quaternion.Euler(-push * 8 + tuck * 4 - land * 11, 0, 0);
            leftLeg.localRotation *= Quaternion.Euler(-push * 14 - tuck * 11 + reach * 12 - land * 8, 0, -tuck * 4);
            rightLeg.localRotation *= Quaternion.Euler(-push * 12 - tuck * 15 + reach * 10 - land * 8, 0, tuck * 4);
            leftKnee.localRotation *= Quaternion.Euler(push * 23 + tuck * 17 - reach * 8 + land * 18, 0, 0);
            rightKnee.localRotation *= Quaternion.Euler(push * 19 + tuck * 21 - reach * 8 + land * 16, 0, 0);
            if (leftFoot != null) leftFoot.localRotation *= Quaternion.Euler(push * 12 - tuck * 19 + reach * 17 - land * 10, 0, 0);
            if (rightFoot != null) rightFoot.localRotation *= Quaternion.Euler(push * 10 - tuck * 16 + reach * 15 - land * 10, 0, 0);
            if (!casting)
            {
                leftArm.localRotation *= Quaternion.Euler(-push * 25 - tuck * 21 + reach * 15 + land * 17, -tuck * 8, -tuck * 17);
                rightArm.localRotation *= Quaternion.Euler(-push * 20 - tuck * 18 + reach * 12 + land * 14, tuck * 6, tuck * 14);
                leftElbow.localRotation *= Quaternion.Euler(-tuck * 23 - push * 14, 0, 0);
                rightElbow.localRotation *= Quaternion.Euler(-tuck * 20 - push * 12, 0, 0);
            }
            cloak.localRotation *= Quaternion.Euler(-push * 7 + tuck * 16 - reach * 7 + land * 13, 0, 0);
        }
    }
}
