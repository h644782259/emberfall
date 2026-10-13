#if EMBERFALL_VISUAL_VALIDATION
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class VisualValidationPlayer
    {
        private IEnumerator VerifyDungeonTactics()
        {
            result.captureScope="Live dungeon AI and casts, immutable mesh A/B timings, wave role and terrain checks; isolated saves.";
            yield return SetResolution(1600,900);
            for(int seed=0;seed<32;seed++)
            {
                var seen=new HashSet<DungeonTactic>();
                for(int wave=1;wave<=4;wave++)seen.Add(DungeonWaveTactics.Select(seed,seed%4,wave));
                Check(seen.Count==4,"Four distinct tactics per run: "+seed);
            }
            SetField("selectedClass",HeroClass.Arcanist);Invoke("StartSelectedHero");yield return WaitForSceneReady();
            double[] cost=new double[2];
            Mesh retained=null;
            for(int pass=0;pass<2;pass++)
            {
                EnemyNaturalMeshCache.Enabled=pass==1;
                for(int i=0;i<16;i++)
                {
                    var host=new GameObject("Mesh benchmark");host.transform.position=new Vector3(0,-100,0);
                    var watch=System.Diagnostics.Stopwatch.StartNew();
                    var rig=CombatModel.Enemy(host.transform,(EnemyKind)(i%4),false);
                    watch.Stop();if(i>=4)cost[pass]+=watch.Elapsed.TotalMilliseconds;
                    if(pass==1)foreach(var filter in rig.GetComponentsInChildren<MeshFilter>())
                        if(EnemyNaturalMeshCache.Contains(filter.sharedMesh)){retained=filter.sharedMesh;break;}
                    Destroy(host);yield return null;
                    if(pass==1)Check(retained!=null,"Actor destruction preserves shared geometry");
                }
            }
            EnemyNaturalMeshCache.Enabled=true;
            Debug.Log("DUNGEON mesh benchmark 12 rigs uncached="+cost[0]+"ms cached="+cost[1]+"ms");
            Check(cost[1]<cost[0],"Cached enemy construction is faster than uncached on this machine");
            session.Progression.Profile.level=100;session.Progression.Profile.classTutorialCompleted=true;
            session.Progression.Profile.skillRanks[0]=1;session.Progression.Profile.skillRanks[1]=1;
            session.Player.RefreshStats(true);
            var entryCamera=Camera.main.GetComponent<AdventureCamera>();
            typeof(AdventureCamera).GetField("yaw",PrivateInstance).SetValue(entryCamera,137f);
            typeof(AdventureCamera).GetField("pitch",PrivateInstance).SetValue(entryCamera,65f);
            session.Player.Teleport(new Vector3(0,0,11));session.EnterDungeon();session.SelectedArenaMode=-1;session.Progression.Profile.adventureBestTiers[0]=9;session.SelectedDungeonTier=10;
            session.ConfirmDungeonSelection();yield return WaitForSceneReady();
            Check(Mathf.Abs(entryCamera.Yaw)<.01f&&Mathf.Abs(entryCamera.Pitch-38f)<.01f,"Entering dungeon resets prior scene camera orientation");
            Check(session.UsesWaveTactics,"Default dungeon uses new wave tactics");
            int expected=(int)typeof(GameSession).GetField("wavePopulation",PrivateInstance).GetValue(session);
            Check(session.Enemies.Count==expected,"Whole wave present before combat: "+session.Enemies.Count+" / "+expected);
            foreach(var enemy in session.Enemies)Check(enemy.gameObject.activeInHierarchy,"Every prepared enemy becomes visible together");
            // Real skills and replenishment with AI alive; no screenshot readback in sample.
            session.Player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,3),.5f));
            float warmUntil=Time.realtimeSinceStartup+3;
            while(Time.realtimeSinceStartup<warmUntil){session.Player.RefreshStats(true);yield return null;}
            int frame=Time.frameCount;double start=Time.realtimeSinceStartupAsDouble;
            var frames=new List<double>();double previous=start;float castAt=0;int casts=0;
            while(Time.realtimeSinceStartupAsDouble-start<20&&!session.IsDead)
            {
                session.Player.RefreshStats(true);
                double now=Time.realtimeSinceStartupAsDouble;frames.Add((now-previous)*1000);previous=now;
                if(Time.time>=castAt)
                {
                    castAt=Time.time+2;
                    typeof(PlayerController).GetMethod("CastSkill",PrivateInstance).Invoke(session.Player,new object[]{casts++%2});
                    session.Player.RefreshStats(true);
                }
                PerformanceMove=new Vector2(Mathf.Sin(Time.time*.5f),Mathf.Cos(Time.time*.5f))*.4f;
                yield return null;
            }
            PerformanceMove=Vector2.zero;frames.Sort();
            result.stationaryFps=(float)((Time.frameCount-frame)/(Time.realtimeSinceStartupAsDouble-start));
            result.performanceTargetMet=result.stationaryFps>=55;
            Debug.Log("DUNGEON live fps="+((Time.frameCount-frame)/(Time.realtimeSinceStartupAsDouble-start))+" p99ms="+frames[(int)(frames.Count*.99)]+" maxms="+frames[frames.Count-1]);
            Check(!session.IsDead&&!session.InputBlocked,"Live sample remained in active combat");
            yield return Capture("dungeon-wave-tactics",0);
            // Probe role behavior independently of randomly chosen first-wave order.
            foreach(var enemy in session.Enemies)if(enemy!=null){enemy.enabled=false;enemy.ConfigureDungeonRole(DungeonEnemyRole.Ordinary);}
            var protectorHost=new GameObject("Protector test");protectorHost.transform.position=session.Player.transform.position+Vector3.right*3;
            var protector=protectorHost.AddComponent<EnemyController>();protector.Initialize(session,EnemyKind.Wisp,100);protector.ConfigureDungeonRole(DungeonEnemyRole.Protector);protector.enabled=false;session.Enemies.Add(protector);
            var targetHost=new GameObject("Protected test");targetHost.transform.position=protectorHost.transform.position+Vector3.forward;
            var target=targetHost.AddComponent<EnemyController>();target.Initialize(session,EnemyKind.Guardian,100);target.enabled=false;session.Enemies.Add(target);
            Check(session.WaveSupportMultiplier(target)==.7f,"Nearby visible protector reduces incoming damage");
            protector.ApplyControl(1);
            Check(session.WaveSupportMultiplier(target)==1,"Stunning protector disables support");
            protector.ConfigureDungeonRole(DungeonEnemyRole.Artillery);
            Check((bool)typeof(EnemyController).GetMethod("BeginAttack",PrivateInstance).Invoke(protector,null),"Artillery begins a real attack");
            Check(Mathf.Abs((float)typeof(EnemyController).GetField("windup",PrivateInstance).GetValue(protector)-1.25f)<.001f,"Artillery grants 1.25 seconds warning");
            Vector3 locked=(Vector3)typeof(EnemyController).GetField("targetPoint",PrivateInstance).GetValue(protector);
            Check((locked-session.Player.transform.position).sqrMagnitude<.01f,"Artillery locks player's ground location");
            session.Enemies.Remove(protector);Destroy(protectorHost);session.Enemies.Remove(target);Destroy(targetHost);
            int currentSeed=(int)typeof(GameSession).GetField("runSeed",PrivateInstance).GetValue(session);
            for(int wave=1;wave<=4;wave++)if(DungeonWaveTactics.Select(currentSeed,session.DungeonLayout,wave)==DungeonTactic.Upheaval)
                typeof(GameSession).GetProperty("DungeonWave").SetValue(session,wave);
            typeof(GameSession).GetField("nextWaveFault",PrivateInstance).SetValue(session,0f);
            yield return null;yield return null;
            var fault=(EnemyAttackTelegraph)typeof(GameSession).GetField("waveFault",PrivateInstance).GetValue(session);
            Check(fault!=null,"Upheaval creates a visible pre-impact terrain warning");
            session.Player.Teleport(session.Player.transform.position+Vector3.left*5);
            yield return Capture("dungeon-ground-upheaval",.2f);
            yield return new WaitForSecondsRealtime(2);
            Check(typeof(GameSession).GetField("waveFault",PrivateInstance).GetValue(session)==null,"Ground warning resolves and cleans up");
            // Verify the player-visible wave contract through real death callbacks.
            typeof(GameSession).GetProperty("DungeonWave").SetValue(session,2);
            var alive=new List<EnemyController>(session.Enemies);
            foreach(var enemy in alive)if(enemy!=null&&!enemy.IsDead)enemy.TakeDamage(1e20f,Vector3.forward,impact:false);
            double deadline=Time.realtimeSinceStartupAsDouble+30;
            Vector3 transitionStart=session.Player.transform.position;
            PerformanceMove=Vector2.right;int preparingFrames=0;
            while((session.DungeonWave==2||session.WavePreparing)&&Time.realtimeSinceStartupAsDouble<deadline)
            {
                if(session.WavePreparing)
                {
                    preparingFrames++;
                    Check(!session.InputBlocked&&Time.timeScale>0,"Preparing a wave never freezes player input or combat time");
                    Check(session.Enemies.Count==0,"Hidden actors cannot be targeted or damaged before arrival");
                }
                yield return null;
            }
            PerformanceMove=Vector2.zero;
            Check(preparingFrames>0&&(session.Player.transform.position-transitionStart).magnitude>.1f,"Player keeps walking through wave transition");
            Check(session.DungeonWave==3&&!session.WavePreparing,"Killing the last visible enemy advances exactly one wave");
            int nextCount=(int)typeof(GameSession).GetField("wavePopulation",PrivateInstance).GetValue(session);
            Check(session.Enemies.Count==nextCount,"Next wave also arrives complete: "+session.Enemies.Count+" / "+nextCount);
            foreach(var enemy in session.Enemies)enemy.enabled=false;
            session.Player.enabled=false;
            yield return new WaitForSecondsRealtime(1);
            Check(session.Enemies.Count==nextCount,"No delayed replacement enemies after arrival");
            // Fix the target to isolate camera turning from mouse-driven aim picking.
            var camera=Camera.main.GetComponent<AdventureCamera>();
            var viewType=typeof(AdventureCamera);
            var cameraEnemy=session.Enemies[0];cameraEnemy.enabled=true;
            cameraEnemy.transform.position=session.Player.transform.position+Vector3.right*4;
            typeof(PlayerController).GetProperty("AimTarget").SetValue(session.Player,cameraEnemy);
            viewType.GetField("yaw",PrivateInstance).SetValue(camera,270f);
            viewType.GetField("manualCameraUntil",PrivateInstance).SetValue(camera,0f);
            session.RecordCombatEngagement();
            float turnStart=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup-turnStart<5.5f)
            {
                cameraEnemy.transform.position=session.Player.transform.position+Vector3.right*4;
                typeof(PlayerController).GetProperty("AimTarget").SetValue(session.Player,cameraEnemy);
                if(Time.realtimeSinceStartup-turnStart<.5f)Check(Mathf.Abs(Mathf.DeltaAngle(camera.Yaw,270))<20,"Camera gently accelerates instead of snapping toward target");
                yield return null;
            }
            Debug.Log("CAMERA yaw="+camera.Yaw+" focus="+Application.isFocused+" blocked="+session.InputBlocked+" combat="+session.InCombat+" los="+CombatSight.Direct(session.Player.transform.position,cameraEnemy.transform.position));
            Check(Mathf.Abs(Mathf.DeltaAngle(camera.Yaw,90))<12,"Camera faces the selected enemy instead of remaining behind the hero");
            typeof(PlayerController).GetProperty("AimTarget").SetValue(session.Player,null);
            yield return null;yield return null;
            float heldYaw=camera.Yaw,heldSmooth=(float)viewType.GetField("smoothYaw",PrivateInstance).GetValue(camera);
            yield return new WaitForSecondsRealtime(.5f);
            Check(Mathf.Abs(Mathf.DeltaAngle(camera.Yaw,heldYaw))<.01f&&Mathf.Abs(Mathf.DeltaAngle((float)viewType.GetField("smoothYaw",PrivateInstance).GetValue(camera),heldSmooth))<.01f,"No target means no residual camera rotation");
            typeof(PlayerController).GetProperty("AimTarget").SetValue(session.Player,cameraEnemy);
            cameraEnemy.TakeDamage(1e20f,Vector3.forward,impact:false);
            yield return null;heldYaw=camera.Yaw;yield return new WaitForSecondsRealtime(.4f);
            Check(Mathf.Abs(Mathf.DeltaAngle(camera.Yaw,heldYaw))<.01f,"Dead target cannot keep rotating camera");
            viewType.GetField("manualCameraUntil",PrivateInstance).SetValue(camera,Time.unscaledTime+4);
            viewType.GetField("yaw",PrivateInstance).SetValue(camera,180f);
            yield return new WaitForSecondsRealtime(.3f);
            Check(Mathf.Abs(Mathf.DeltaAngle(camera.Yaw,180))<1,"Manual camera input retains priority over combat assistance");

        }
    }
}
#endif
