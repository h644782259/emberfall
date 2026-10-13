#if EMBERFALL_VISUAL_VALIDATION
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class VisualValidationPlayer
    {
        private IEnumerator VerifyStrideAudio()
        {
            result.captureScope="Real frost-flame movement, persistent fire damage and pooled audio lifecycle; isolated saves.";
            yield return SetResolution(1600,900);
            SetField("selectedClass",HeroClass.Arcanist);Invoke("StartSelectedHero");yield return WaitForSceneReady();
            foreach(var enemy in session.Enemies)if(enemy!=null)enemy.gameObject.SetActive(false);
            session.Enemies.Clear();
            var player=session.Player;
            session.Progression.Profile.level=100;session.Progression.Profile.skillRanks[5]=3;player.RefreshStats(true);
            player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,-5),.45f));
            player.transform.rotation=Quaternion.identity;
            var view=Camera.main.GetComponent<AdventureCamera>();
            typeof(AdventureCamera).GetField("yaw",PrivateInstance).SetValue(view,0f);view.Snap();
            typeof(AdventureCamera).GetField("manualCameraUntil",PrivateInstance).SetValue(view,Time.unscaledTime+60);
            typeof(PlayerController).GetField("aimPoint",PrivateInstance).SetValue(player,player.transform.position+Vector3.forward*8);
            GameAudio.Muted=false;GameAudio.MasterVolume=.25f;
            PerformanceMove=Vector2.zero;
            typeof(PlayerController).GetMethod("CastSkill",PrivateInstance).Invoke(player,new object[]{5});
            float releaseDeadline=Time.realtimeSinceStartup+2;
            while(!player.FrostStrideActive&&Time.realtimeSinceStartup<releaseDeadline)yield return null;
            Check(player.FrostStrideActive,"Frost-flame release enters automatic movement");
            Vector3 start=player.transform.position;float startTime=Time.time;
            yield return new WaitForSeconds(.30f);
            float idleSpeed=CombatFx.Flat(player.transform.position-start).magnitude/(Time.time-startTime);
            float expected=session.Progression.GetStats().MoveSpeed*1.5f;
            Check(Mathf.Abs(idleSpeed-expected)<expected*.15f,"No-input skill speed is 1.5x ordinary walk: "+idleSpeed+" expected "+expected);
            PerformanceMove=Vector2.up;start=player.transform.position;startTime=Time.time;
            yield return new WaitForSeconds(.30f);
            float inputSpeed=CombatFx.Flat(player.transform.position-start).magnitude/(Time.time-startTime);
            Check(Mathf.Abs(inputSpeed-idleSpeed)<expected*.15f,"Holding forward does not add a second movement speed");
            PerformanceMove=Vector2.right;yield return new WaitForSeconds(.4f);
            Check(Vector3.Dot(player.transform.forward,Vector3.right)>.85f,"Directional input steers the active skill");
            PerformanceMove=Vector2.zero;
            var host=new GameObject("Stride contact test");host.transform.position=player.transform.position+player.transform.forward*1.7f;
            var target=host.AddComponent<EnemyController>();target.Initialize(session,EnemyKind.Guardian,100);target.enabled=false;session.Enemies.Add(target);
            float before=target.Health;
            yield return new WaitForSeconds(.15f);
            Check(target.Health<before,"Moving collision deals real enemy health damage");
            var ride=Object.FindAnyObjectByType<FlameRide>();Check(ride!=null,"All elementalist specializations create the flame trail");
            var footprints=(Vector3[])typeof(FlameRide).GetField("footprints",PrivateInstance).GetValue(ride);
            target.transform.position=footprints[0];before=target.Health;
            yield return new WaitForSeconds(.7f);
            Check(target.Health<before,"Fire left behind continues to burn after the caster passes");
            yield return Capture("frost-flame-trail",0);
            var audio=Object.FindAnyObjectByType<GameAudio>();
            var beds=(IEnumerable)typeof(GameAudio).GetField("sustained",PrivateInstance).GetValue(audio);
            int playing=0;foreach(var bed in beds)if(bed!=null)
            {var source=(AudioSource)bed.GetType().GetField("Source",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(bed);if(source.isPlaying)playing++;}
            Check(playing>0&&playing<=4,"Persistent skill audio plays beyond the initial cast within four-layer budget");
            var chant=(AudioSource)typeof(GameAudio).GetField("chantSource",PrivateInstance).GetValue(audio);
            Check(chant!=null&&chant.clip!=null&&chant.clip.length>1,"Fictional chant asset is assigned to a dedicated speech channel");
            session.SetPaused(true);yield return new WaitForSecondsRealtime(.2f);
            int paused=0;foreach(var bed in beds)if(bed!=null)
            {var source=(AudioSource)bed.GetType().GetField("Source",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(bed);if(source.isPlaying)paused++;}
            Check(paused==0,"Pause suspends persistent skill audio");
            session.SetPaused(false);yield return null;
            player.Teleport(player.transform.position);yield return null;yield return null;
            int retained=0;foreach(var bed in beds)if(bed!=null)
            {var source=(AudioSource)bed.GetType().GetField("Source",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(bed);if(source.isPlaying)retained++;}
            Check(retained==0&&!player.FrostStrideActive,"Teleport retires motion and sustained audio from the old combat epoch");
            GameAudio.Muted=true;PerformanceMove=Vector2.zero;
        }
    }
}
#endif
