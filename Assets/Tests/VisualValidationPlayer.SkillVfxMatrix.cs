#if EMBERFALL_VISUAL_VALIDATION
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class VisualValidationPlayer
    {
        private IEnumerator VerifySkillVfxMatrix()
        {
            PerformanceRun=true;
            var args=Environment.GetCommandLineArgs();int hero=0;
            bool benchmark=Array.IndexOf(args,"--vfx-benchmark")>=0;
            var frameTimes=new System.Collections.Generic.List<float>(8192);
            int option=Array.IndexOf(args,"--matrix-hero");if(option>=0)hero=int.Parse(args[option+1]);
            result.captureScope="Live skill releases for all three ranks; world camera frames, isolated saves. Passive slots are enabled with their matching rank.";
            yield return SetResolution(benchmark?1920:960,benchmark?1080:540);
            SetField("selectedClass",(HeroClass)hero);Invoke("StartSelectedHero");yield return WaitForSceneReady();
            var player=session.Player;var runtime=(SkillRuntime)typeof(PlayerController).GetField("skillRuntime",PrivateInstance).GetValue(player);
            var view=Camera.main.GetComponent<AdventureCamera>();
            typeof(AdventureCamera).GetField("yaw",PrivateInstance).SetValue(view,0f);
            typeof(AdventureCamera).GetField("manualCameraUntil",PrivateInstance).SetValue(view,Time.unscaledTime+1000);
            session.Progression.Profile.level=100;
            if(hero==(int)HeroClass.Vanguard)
            {
                player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,5),.45f));
                player.transform.rotation=Quaternion.identity;view.Snap();yield return new WaitForSeconds(.2f);
                typeof(PlayerController).GetMethod("BasicAttack",PrivateInstance).Invoke(player,null);
                Check(UnityEngine.Object.FindFirstObjectByType<WeaponSlashRibbon>()!=null,"Vanguard basic attack creates its weapon ribbon without a hit");
                string basicFolder=Path.Combine(outputDirectory,"Vanguard-basic");Directory.CreateDirectory(basicFolder);
                Time.captureFramerate=30;
                try
                {
                    for(int frame=0;frame<12;frame++)
                    {
                        yield return new WaitForEndOfFrame();
                        var image=CaptureGroundFlameCamera();File.WriteAllBytes(Path.Combine(basicFolder,frame.ToString("D3")+".png"),image.EncodeToPNG());Destroy(image);
                    }
                }
                finally{Time.captureFramerate=0;}
            }
            for(int rank=1;rank<=3;rank++)for(int slot=0;slot<GameBalance.SkillCount;slot++)
            {
                if(GameBalance.IsPassive(slot))continue;
                foreach(var enemy in session.Enemies)if(enemy!=null)enemy.gameObject.SetActive(false);session.Enemies.Clear();
                player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,5),.45f));
                player.transform.rotation=Quaternion.identity;
                for(int index=0;index<GameBalance.SkillCount;index++)session.Progression.Profile.skillRanks[index]=rank;
                player.RefreshStats(true);runtime.ResetCooldowns();runtime.FillEnergy();PerformanceMove=Vector2.zero;
                var host=new GameObject("VFX matrix target");host.transform.position=WorldTraversal.NearestWalkable(player.transform.position+Vector3.forward*3,.5f);
                var target=host.AddComponent<EnemyController>();target.Initialize(session,hero==(int)HeroClass.Vanguard&&slot==4&&rank==1?EnemyKind.Slime:EnemyKind.Guardian,100);target.enabled=false;session.Enemies.Add(target);
                float targetHealth=target.Health;
                typeof(PlayerController).GetProperty("AimTarget").SetValue(player,target);
                typeof(PlayerController).GetField("aimPoint",PrivateInstance).SetValue(player,host.transform.position);
                view.Snap();yield return new WaitForSeconds(.2f);
                // Desktop mouse aiming runs during the settle frame; set the test
                // aim immediately before the normal input captures its snapshot.
                typeof(PlayerController).GetProperty("AimTarget").SetValue(player,target);
                typeof(PlayerController).GetField("aimPoint",PrivateInstance).SetValue(player,host.transform.position);
                int before=(int)typeof(PlayerController).GetField("nextCastId",PrivateInstance).GetValue(player);
                typeof(PlayerController).GetMethod("CastSkill",PrivateInstance).Invoke(player,new object[]{slot});
                float deadline=Time.realtimeSinceStartup+3;
                while((int)typeof(PlayerController).GetField("nextCastId",PrivateInstance).GetValue(player)==before&&Time.realtimeSinceStartup<deadline)yield return null;
                string label=((HeroClass)hero)+"-skill-"+slot+"-rank-"+rank;
                Check((int)typeof(PlayerController).GetField("nextCastId",PrivateInstance).GetValue(player)>before,"Actual skill released: "+label);
                if(hero==(int)HeroClass.Vanguard&&slot==4)
                    Check(UnityEngine.Object.FindFirstObjectByType<VanguardSwordWave>()!=null,"Vanguard fifth hotbar skill releases a travelling sword wave: "+rank);
                if(benchmark)
                {
                    QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                    var camera=Camera.main;var previous=camera.targetTexture;
                    var targetTexture=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);
                    var readback=new Texture2D(1,1,TextureFormat.RGB24,false);
                    Time.captureFramerate=20;
                    try
                    {
                        camera.targetTexture=targetTexture;
                        for(int sample=0;sample<50;sample++)
                        {
                            yield return null;
                            var active=RenderTexture.active;
                            var timer=System.Diagnostics.Stopwatch.StartNew();
                            camera.Render();RenderTexture.active=targetTexture;
                            readback.ReadPixels(new Rect(960,540,1,1),0,0,false);
                            timer.Stop();RenderTexture.active=active;
                            frameTimes.Add((float)timer.Elapsed.TotalMilliseconds);
                        }
                    }
                    finally{Time.captureFramerate=0;camera.targetTexture=previous;RenderTexture.ReleaseTemporary(targetTexture);Destroy(readback);}
                }
                else if(rank<3){yield return new WaitForSeconds(.35f);yield return Capture(label,0);}
                else
                {
                    string folder=Path.Combine(outputDirectory,label);Directory.CreateDirectory(folder);
                    Time.captureFramerate=20;
                    try
                    {
                        for(int frame=0;frame<40;frame++)
                        {
                            yield return new WaitForEndOfFrame();
                            if(frame%2!=0)continue;
                            var image=CaptureGroundFlameCamera();File.WriteAllBytes(Path.Combine(folder,(frame/2).ToString("D3")+".png"),image.EncodeToPNG());Destroy(image);
                        }
                    }
                    finally{Time.captureFramerate=0;}
                    yield return Capture(label,0);
                }
                Check(result.consoleErrors==0,"No rendering/runtime errors: "+label);
                if(hero==(int)HeroClass.Vanguard&&slot==4)
                {
                    if(!benchmark&&rank==1)
                    {
                        var wave=UnityEngine.Object.FindFirstObjectByType<VanguardSwordWave>();
                        Check(wave!=null&&Vector3.Distance(wave.transform.position,player.transform.position)>1,"Sword wave advances independently of the caster");
                        Vector3 pausedPosition=wave.transform.position;
                        expectedPause=true;session.SetPaused(true);yield return new WaitForSecondsRealtime(.15f);
                        Check(wave!=null&&wave.transform.position==pausedPosition,"Sword wave freezes while paused");
                        expectedPause=false;session.SetPaused(false);
                    }
                    yield return new WaitForSeconds(.5f);
                    Check(target==null||target.Health<targetHealth,"Sword wave damages enemies along its path: "+rank);
                    if(target!=null&&!target.IsDead)
                    {
                        Check(((Vector3)typeof(EnemyController).GetField("knockVelocity",PrivateInstance).GetValue(target)).sqrMagnitude>0,"Sword wave applies knockback: "+rank);
                        float afterHit=target.Health;yield return new WaitForSeconds(.3f);
                        Check(Mathf.Approximately(target.Health,afterHit),"Sword wave cannot damage the same enemy twice: "+rank);
                    }
                    Check((float)typeof(PlayerController).GetField("guardTime",PrivateInstance).GetValue(player)<=0,"Sword wave does not grant the retired shield buff");
                }
                session.Enemies.Remove(target);Destroy(host);
            }
            player.Teleport(player.transform.position);yield return null;yield return null;
            int remaining=0;foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if(renderer.gameObject.name.StartsWith("Continuous skill volume")&&renderer.enabled)remaining++;
            Check(remaining==0,"All continuous volumes retire with their producer epoch");
            if(benchmark)
            {
                float total=0;foreach(float ms in frameTimes)total+=ms;frameTimes.Sort();
                result.captureScope="1920x1080 forced world-camera render and GPU-synchronized 1-pixel readback, 50 fixed simulation samples per cast; rendering cost, not gameplay FPS.";
                File.WriteAllText(Path.Combine(outputDirectory,"vfx-performance.txt"),"Frames="+frameTimes.Count+" MeanRenderMs="+total/frameTimes.Count+" P95ms="+frameTimes[(int)(frameTimes.Count*.95f)]+" P99ms="+frameTimes[(int)(frameTimes.Count*.99f)]);
            }
        }
    }
}
#endif
