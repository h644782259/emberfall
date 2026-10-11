using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private Transform previewOrbit;
        private Quaternion previewOrbitRest,previewDecorationRest;
        // Only the isolated mannequin opts in. No action dispatch, controller, random draw,
        // damage, leases or world clock is used while sampling a presentation pose.
        public void ConfigurePreview()
        {
            isolatedPreview=true;previewTime=0;
            if(blenderPilot==null)ConfigureBlenderPilot();
            previewOrbit=fashionWings==null?null:fashionWings.Find("Mechanical star-ring orbit");
            if(previewOrbit!=null)previewOrbitRest=previewOrbit.localRotation;
            if(decoration!=null)previewDecorationRest=decoration.localRotation;
        }
        public void SamplePreview(float time,CollectionPreviewAction action,float progress,float mechanicalYaw=float.NaN)
        {
            if(!isolatedPreview||!isHero||float.IsNaN(time)||float.IsInfinity(time)||float.IsNaN(progress)||float.IsInfinity(progress))return;
            previewTime=time;
            actionBasic=action==CollectionPreviewAction.Attack && heroClass!=HeroClass.Ranger;
            actionSkill=action==CollectionPreviewAction.Move?-3:heroClass==HeroClass.Summoner?2:heroClass==HeroClass.Arcanist?1:heroClass==HeroClass.Ranger?2:0;
            actionDuration=action==CollectionPreviewAction.Idle||action==CollectionPreviewAction.Move?0:1;
            actionAge=Mathf.Clamp01(progress);
            AnimateHero(0,0,false,0);
            if(action==CollectionPreviewAction.Move)
            {
                // Bounded pose-only two-step demonstration; no controller/root-motion dispatch.
                float step=Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI*4);
                leftLeg.localRotation=Quaternion.Euler(step*28,0,-2);rightLeg.localRotation=Quaternion.Euler(-step*28,0,2);
                leftKnee.localRotation=Quaternion.Euler(Mathf.Max(0,-step)*38,0,0);rightKnee.localRotation=Quaternion.Euler(Mathf.Max(0,step)*38,0,0);
                leftArm.localRotation=Quaternion.Euler(-step*14,0,-8);rightArm.localRotation=Quaternion.Euler(step*14,0,8);
                transform.localPosition+=Vector3.forward*(Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI)*.45f);
            }
            // Preview shows a complete draw/release/reload; live basic commits at contact.
            if(heroClass==HeroClass.Ranger && arrowRig!=null && action==CollectionPreviewAction.Attack)
                arrowRig.gameObject.SetActive(progress < BasicActionTimeline.BowRelease || progress >= BasicActionTimeline.ArrowReload);
            if(decoration!=null)decoration.localRotation=previewDecorationRest*Quaternion.Euler(0,time*42,0);
            // The host owns a separate wrapped angle: its 16-degree orbit must not
            // inherit the 120s breathing/cloth clock reset. Framing uses static time=0.
            if(previewOrbit!=null)previewOrbit.localRotation=previewOrbitRest*Quaternion.Euler(0,0,float.IsNaN(mechanicalYaw)?time*16:mechanicalYaw);
            if(tailoredCloth!=null)tailoredCloth.SamplePreview(time,action==CollectionPreviewAction.Idle?0:Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI));
        }
    }
}
