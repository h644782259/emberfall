using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private float performanceRemaining,performanceBeatAge,performanceCadence;
        private int performanceSkill,performanceCast;
        private bool performancePrepared,performanceLaunch;
        private float performanceUntil;private int performanceBeatFrame=-1;
        internal void BeginSkillPerformance(int skill,float duration,float cadence,int castId)
        {
            performanceRemaining=0;performancePrepared=performanceLaunch=false;
            if(!isHero||!SkillCastingPolicy.Channeled(heroClass,skill)||duration<1.2f)return;
            performanceCast=castId;performanceSkill=skill;performanceRemaining=duration;
            performanceBeatAge=0;performanceCadence=Mathf.Max(.12f,cadence);performancePrepared=performanceLaunch=false;performanceUntil=0;performanceBeatFrame=Time.frameCount;
        }
        // Only the presentation anticipates this already-authorized contact.
        // Resource use, basic recovery and the sequence's event clock stay intact.
        internal void BeginDelayedSkillRelease(int skill,float until)
        {
            if(!isHero||heroClass!=HeroClass.Vanguard||skill!=9||until<=0||float.IsNaN(until)||float.IsInfinity(until))return;
            actionDuration=until/(.52f-.30f);actionAge=actionDuration*.30f;
            performancePrepared=true;performanceLaunch=false;performanceUntil=until;
            actionStartedFrame=Time.frameCount;CommitActionPose();
        }
        internal void SkillPerformanceBeat(int skill,int castId)
        {
            if(performanceCast!=castId||(skill>=0&&performanceSkill!=skill)||performanceRemaining<=0)return;
            // A flying arrow/meteor was released at Prepare, so arrival cannot
            // manufacture another draw/release or reset its emitter's clock.
            if(performancePrepared&&performanceLaunch){performancePrepared=performanceLaunch=false;return;}
            performancePrepared=false;performanceBeatAge=0;performanceBeatFrame=Time.frameCount;
            CommitActionPose();
        }
        internal void PrepareSkillPerformanceBeat(int skill,int castId,float until,bool launchNow=false)
        {
            if(performanceCast!=castId||(skill>=0&&performanceSkill!=skill)||performanceRemaining<=0||until<=0||float.IsNaN(until)||float.IsInfinity(until))return;
            performancePrepared=true;performanceLaunch=launchNow;performanceUntil=until;
            if(launchNow){performanceBeatAge=0;performanceBeatFrame=Time.frameCount;CommitActionPose();}
        }
        private bool SampleSkillPerformance(ref float t,ref bool acting,float dt)
        {
            if(performanceRemaining<=0||pilotCharging)return false;
            if(performanceBeatFrame!=Time.frameCount){performanceRemaining=Mathf.Max(0,performanceRemaining-dt);performanceBeatAge+=dt;if(performancePrepared)performanceUntil=Mathf.Max(0,performanceUntil-dt);}
            if(performanceRemaining<=0)return false;
            if(actionBasic&&actionDuration>0&&actionAge/actionDuration<.75f)return false;
            if(heroClass==HeroClass.Vanguard&&performanceSkill==9&&performancePrepared&&!performanceLaunch&&
                !actionBasic&&actionDuration>0&&actionAge/actionDuration<.52f)return false;
            float recovery=Mathf.Min(.32f,performanceCadence*.65f);
            t=Mathf.Lerp(.52f,1,Mathf.Clamp01(performanceBeatAge/Mathf.Max(.05f,recovery)));
            if(performancePrepared&&!performanceLaunch)
            {
                float anticipation=Mathf.Min(.2f,performanceCadence*.55f);
                if(performanceUntil<=anticipation)t=.52f*(1-performanceUntil/Mathf.Max(.04f,anticipation));
            }
            else if(performancePrepared&&performanceLaunch)
            {
                // Reload in the last part of a flying body's travel; the next
                // Prepare launch commits the release. Arrival itself never does.
                float anticipation=Mathf.Min(.2f,performanceCadence*.55f);
                if(performanceUntil<=anticipation)t=Mathf.Lerp(.14f,.43f,1-performanceUntil/Mathf.Max(.04f,anticipation));
            }
            // Between widely spaced emissions the caster keeps a small ready
            // stance. No free-running phase invents an unclaimed release.
            if(t>=1)t=.14f;
            acting=true;return true;
        }
    }
}
