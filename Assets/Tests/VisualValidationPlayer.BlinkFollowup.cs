#if EMBERFALL_VISUAL_VALIDATION
using System.Collections;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class VisualValidationPlayer
    {
        private IEnumerator VerifyBlinkFollowups()
        {
            result.captureScope="Four live blink follow-ups, paid windups, single-use/expiry and mobile button dispatch; isolated saves.";
            for(int h=0;h<4;h++)
            {
                MobileControls.SimulationEnabled=false;
                if(session.HasStarted){ResetPanels();session.QuitToTitle();}
                yield return SetResolution(1600,900);SetField("selectedClass",(HeroClass)h);Invoke("StartSelectedHero");yield return WaitForSceneReady();
                foreach(var e in session.Enemies)if(e!=null)e.gameObject.SetActive(false);session.Enemies.Clear();
                var player=session.Player;session.Progression.Profile.level=4;player.RefreshStats(true);
                Check(player.TryBlink(Vector3.forward)&&player.BlinkFollowupRemaining==0&&!player.TryBlinkFollowup(),"Level four blink has no followup: "+h);
                yield return new WaitForSeconds(2.2f);session.Progression.Profile.level=5;player.RefreshStats(true);
                Check(player.TryBlink(Vector3.back)&&player.BlinkFollowupLearned&&player.BlinkFollowupRemaining>0,"Level five automatically learns followup: "+h);
                yield return new WaitForSeconds(2.2f);session.Progression.Profile.level=100;player.RefreshStats(true);
                player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,-5),.45f));player.transform.forward=Vector3.forward;
                Check(player.TryBlink(Vector3.forward),"Blink opens follow-up for class "+h);
                Check(player.BlinkFollowupRemaining>0&&player.BlinkFollowupRemaining<=player.DodgeCooldown,"Opportunity never exceeds blink cooldown");
                yield return null;
                var host=new GameObject("Followup target");host.transform.position=player.transform.position+Vector3.forward*1.5f;
                var enemy=host.AddComponent<EnemyController>();enemy.Initialize(session,EnemyKind.Wisp,100);enemy.enabled=false;session.Enemies.Add(enemy);
                typeof(PlayerController).GetProperty("AimTarget").SetValue(player,enemy);
                typeof(PlayerController).GetField("aimPoint",PrivateInstance).SetValue(player,enemy.transform.position);
                float health=enemy.Health,energy=player.Energy,cooldown=player.DodgeCooldown;
                Check(player.TryBlinkFollowup(),"Follow-up accepts fresh action");
                Check(!player.TryBlinkFollowup(),"Same blink cannot queue a second follow-up");
                Check(enemy.Health==health&&player.Energy==energy,"Follow-up has windup and no extra energy charge");
                yield return new WaitForSeconds(.2f);
                Check(enemy.Health<health,"Class follow-up actually hits after windup "+h);
                Check(player.DodgeCooldown>0&&player.DodgeCooldown<cooldown,"Follow-up does not reset or extend blink cooldown");
                yield return Capture("blink-followup-"+h,0);
                session.Enemies.Remove(enemy);Destroy(host);
                yield return new WaitForSeconds(2.2f);
                Check(player.TryBlink(Vector3.back),"Second blink opens a new opportunity");
                yield return new WaitForSeconds(1.3f);
                Check(player.BlinkFollowupRemaining==0&&!player.TryBlinkFollowup(),"Expired window cannot activate");
                yield return new WaitForSeconds(1);
                MobileControls.SimulationEnabled=true;yield return SetResolution(813,421);
                Check(player.TryBlink(Vector3.forward),"Mobile blink opens temporary action");yield return null;
                yield return Capture("blink-followup-mobile-"+h,0);
                typeof(MobileControls).GetField("dodge",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,true);
                yield return null;yield return null;
                Check(player.BlinkFollowupRemaining==0,"Mobile dodge-button input consumes follow-up instead of trying a cooldown blink");
                yield return new WaitForSeconds(.3f);
            }
            MobileControls.SimulationEnabled=false;
        }
    }
}
#endif
