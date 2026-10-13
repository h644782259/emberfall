#if EMBERFALL_VISUAL_VALIDATION
using System.Collections;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class VisualValidationPlayer
    {
        private IEnumerator VerifyCastPoses()
        {
            result.captureScope="All forty skill pose policies plus live detached black-hole movement; isolated saves.";
            yield return SetResolution(1600,900);
            for(int hero=0;hero<4;hero++)
            {
                var host=new GameObject("Pose policy mannequin");host.transform.position=Vector3.down*100;
                var model=CombatModel.Hero(host.transform,(HeroClass)hero);
                for(int skill=0;skill<10;skill++)
                {
                    model.PlayAction(skill,false);model.BeginSkillPerformance(skill,8,.5f,skill+1);
                    float remaining=(float)typeof(CombatModel).GetField("performanceRemaining",PrivateInstance).GetValue(model);
                    Check((remaining>0)==SkillCastingPolicy.Channeled((HeroClass)hero,skill),"Persistent actor pose only for actor-driven channel "+hero+" / "+skill);
                    model.SkillPerformanceBeat(skill,skill+1);
                    Check((float)typeof(CombatModel).GetField("performanceRemaining",PrivateInstance).GetValue(model)==remaining,"Detached contact never reactivates a pose");
                }
                Destroy(host);yield return null;
            }
            SetField("selectedClass",HeroClass.Arcanist);Invoke("StartSelectedHero");yield return WaitForSceneReady();
            foreach(var enemy in session.Enemies)if(enemy!=null)enemy.gameObject.SetActive(false);session.Enemies.Clear();
            var player=session.Player;session.Progression.Profile.level=100;session.Progression.Profile.skillRanks[7]=3;player.RefreshStats(true);
            player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,-3),.45f));
            typeof(PlayerController).GetField("aimPoint",PrivateInstance).SetValue(player,player.transform.position+Vector3.forward*5);
            var runtime=(SkillRuntime)typeof(PlayerController).GetField("skillRuntime",PrivateInstance).GetValue(player);
            session.Progression.Profile.skillRanks[1]=3;
            var charger=player.GetComponent<SkillChargeController>();
            float chargeEnergy=player.Energy;
            int chargeCast=(int)typeof(PlayerController).GetField("nextCastId",PrivateInstance).GetValue(player);
            Check(charger.Begin(1),"Charged skill accepts input");
            Check(Mathf.Abs(player.Energy-chargeEnergy+GameBalance.SkillEnergyCost(player.HeroClass,1))<.01f&&runtime.Remaining(1)>0,"Charged input pays immediately");
            typeof(PlayerController).GetMethod("InterruptSkillForBlink",PrivateInstance).Invoke(player,null);
            yield return new WaitForSeconds(.7f);
            Check(!charger.IsCharging&&(int)typeof(PlayerController).GetField("nextCastId",PrivateInstance).GetValue(player)==chargeCast,"Blink cancels charged release");
            runtime.ResetCooldowns();runtime.FillEnergy();yield return null;
            Check(charger.Begin(1),"Charged skill restarts after test reset");
            chargeEnergy=player.Energy;
            yield return new WaitForSeconds(.65f);
            Check((int)typeof(PlayerController).GetField("nextCastId",PrivateInstance).GetValue(player)==chargeCast+1&&player.Energy>=chargeEnergy,"Charged release executes once without duplicate payment");
            yield return new WaitForSeconds(.4f);
            runtime.ResetCooldowns();runtime.FillEnergy();
            float energy=player.Energy;
            int castBefore=(int)typeof(PlayerController).GetField("nextCastId",PrivateInstance).GetValue(player);
            typeof(PlayerController).GetMethod("CastSkill",PrivateInstance).Invoke(player,new object[]{7});
            Check(Mathf.Abs(player.Energy-(energy-GameBalance.SkillEnergyCost(player.HeroClass,7)))<.01f,"Input pays energy immediately");
            Check(runtime.Remaining(7)>0,"Input starts cooldown immediately");
            Check((int)typeof(PlayerController).GetField("nextCastId",PrivateInstance).GetValue(player)==castBefore,"Windup cannot release damage or effects");
            float paidEnergy=player.Energy,paidCooldown=runtime.Remaining(7);
            typeof(PlayerController).GetMethod("InterruptSkillForBlink",PrivateInstance).Invoke(player,null);
            Check(!player.HasPendingSkillRelease&&player.Energy==paidEnergy&&runtime.Remaining(7)==paidCooldown,"Blink cancels windup without refund or double payment");
            yield return new WaitForSeconds(.7f);
            Check((int)typeof(PlayerController).GetField("nextCastId",PrivateInstance).GetValue(player)==castBefore,"Cancelled preparation cannot release later");
            runtime.ResetCooldowns();runtime.FillEnergy();
            yield return null;
            typeof(PlayerController).GetMethod("CastSkill",PrivateInstance).Invoke(player,new object[]{7});
            float releaseEnergy=player.Energy;
            yield return new WaitForSeconds(.12f);
            Check(player.HasPendingSkillRelease,"Heavy spell shows a paid windup");
            float deadline=Time.realtimeSinceStartup+2;
            while(player.HasPendingSkillRelease&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(player.Energy>=releaseEnergy,"Release does not spend energy twice");
            yield return new WaitForSeconds(.7f);
            var actor=player.GetComponentInChildren<CombatModel>();
            Check((float)typeof(CombatModel).GetField("performanceRemaining",PrivateInstance).GetValue(actor)<=0,"Black hole releases ownership of character animation");
            Check(Object.FindObjectsByType<SkillPerformanceVfx>(FindObjectsSortMode.None).Length>0,"Detached black-hole effect remains alive after the release pose");
            PerformanceMove=Vector2.right;yield return new WaitForSeconds(.6f);
            Check((float)typeof(CombatModel).GetField("actionAge",PrivateInstance).GetValue(actor)>=(float)typeof(CombatModel).GetField("actionDuration",PrivateInstance).GetValue(actor),"Walking beside a live black hole uses locomotion after release recovery");
            yield return Capture("detached-black-hole-walking",0);PerformanceMove=Vector2.zero;
        }
    }
}
#endif
