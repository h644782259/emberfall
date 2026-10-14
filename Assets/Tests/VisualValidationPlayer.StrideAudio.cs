#if EMBERFALL_VISUAL_VALIDATION
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class VisualValidationPlayer
    {
        private Texture2D CaptureGroundFlameCamera()
        {
            var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
            var target=RenderTexture.GetTemporary(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();return image;
            }
            finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
        }
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
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--ground-flame-preview")>=0)
            {
                result.captureScope+=" World-only camera RenderTexture captures; not a UI/framebuffer test.";
                var ribbon=GameObject.Find("Continuous ground flame / connected flow surface");
                Check(ribbon!=null&&ribbon.GetComponentsInChildren<Renderer>().Length==1,"One connected flame renderer instead of stamped footprint objects");
                Check(ribbon.GetComponent<MeshFilter>().sharedMesh.vertexCount<=3456,"Bounded continuous trail mesh");
                session.SetPaused(true);
                float frozen=(float)typeof(FlameRide).GetField("age",PrivateInstance).GetValue(ride);
                typeof(AdventureCamera).GetField("yaw",PrivateInstance).SetValue(view,85f);view.Snap();
                yield return Capture("frost-flame-trail-side",0);
                Check((float)typeof(FlameRide).GetField("age",PrivateInstance).GetValue(ride)==frozen,"Paused flame animation uses the unchanged producer clock");
                session.SetPaused(false);
                yield return null; // Let the audio owner resume its paused sources.
            }
            var audio=Object.FindAnyObjectByType<GameAudio>();
            var accents=(AudioClip[])typeof(GameAudio).GetField("skillBeds",PrivateInstance).GetValue(audio);
            foreach(var accent in accents)
            {
                Check(accent!=null,"Each skill family has a prewarmed accent");
                var samples=new float[accent.samples];accent.GetData(samples,0);
                float tailPeak=0;for(int i=(int)(accent.frequency*.25f);i<samples.Length;i++)tailPeak=Mathf.Max(tailPeak,Mathf.Abs(samples[i]));
                Check(tailPeak<.0001f,"Skill accent has a silent gap instead of continuous hiss or hum");
            }
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
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--ground-flame-preview")>=0)
            {
                // Record the running game camera at a fixed simulation interval.
                // The GIF is assembled from these frames without fabricated in-betweens.
                player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,-5),.45f));
                player.transform.rotation=Quaternion.identity;
                typeof(AdventureCamera).GetField("yaw",PrivateInstance).SetValue(view,0f);view.Snap();
                var runtime=typeof(PlayerController).GetField("skillRuntime",PrivateInstance).GetValue(player);
                runtime.GetType().GetMethod("ReduceCooldowns").Invoke(runtime,new object[]{999f});
                typeof(PlayerController).GetMethod("CastSkill",PrivateInstance).Invoke(player,new object[]{5});
                while(!player.FrostStrideActive)yield return null;
                Time.captureFramerate=20;
                string frames=System.IO.Path.Combine(outputDirectory,"frames");System.IO.Directory.CreateDirectory(frames);
                try
                {
                    for(int frame=0;frame<116;frame++)
                    {
                        PerformanceMove=frame<12?Vector2.up:Vector2.right;
                        if(frame==48)
                        {
                            typeof(PlayerController).GetField("burnStrideTime",PrivateInstance).SetValue(player,0f);
                            typeof(PlayerController).GetField("guardTime",PrivateInstance).SetValue(player,0f);
                        }
                        if(frame>=48)PerformanceMove=Vector2.zero;
                        view.Snap();
                        yield return new WaitForEndOfFrame();
                        var image=CaptureGroundFlameCamera();
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(frames,frame.ToString("D4")+".png"),image.EncodeToPNG());
                        Destroy(image);
                    }
                }
                finally {Time.captureFramerate=0;}
            }
            GameAudio.Muted=true;PerformanceMove=Vector2.zero;
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--contact-stress")>=0)
            {
                player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,-5),.45f));
                int confirmed=0;
                for(int hit=0;hit<12;hit++)
                {
                    var basicHost=new GameObject("Repeated basic contact target");basicHost.transform.position=player.transform.position+Vector3.forward*2;
                    var basicTarget=basicHost.AddComponent<EnemyController>();basicTarget.Initialize(session,EnemyKind.Guardian,100);basicTarget.enabled=false;session.Enemies.Add(basicTarget);
                    float health=basicTarget.Health;
                    typeof(PlayerController).GetProperty("AimTarget").SetValue(player,basicTarget);
                    typeof(PlayerController).GetField("aimPoint",PrivateInstance).SetValue(player,basicTarget.transform.position);
                    typeof(PlayerController).GetMethod("BasicAttack",PrivateInstance).Invoke(player,null);
                    yield return new WaitForSeconds(.6f);
                    if(basicTarget==null||basicTarget.Health<health)confirmed++;
                    session.Enemies.Remove(basicTarget);if(basicHost!=null)Destroy(basicHost);
                }
                Check(confirmed==12,"Repeated actual basic attacks still confirm health loss: "+confirmed);
                yield return new WaitForSeconds(2);
                Check(FloatingNumber.ActiveCount==0,"Damage text pool retires every active caption");
                int textMeshes=0;foreach(var number in Object.FindObjectsByType<FloatingNumber>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(number.GetComponent<TextMesh>()!=null)textMeshes++;
                Check(textMeshes==0,"Screen-space damage text creates no hidden TextMesh");
                Check(result.consoleErrors==0,"Repeated contacts and pooled hit feedback have no runtime errors");
            }
        }
    }
}
#endif
