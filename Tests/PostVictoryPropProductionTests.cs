using System;using UnityEngine;using Emberfall;
namespace Emberfall
{
 public static class WorldTraversal
 {
  public class ObstacleHandle{public bool Released;}public static int Obstacles;public static bool SightClear=true;
  public static ObstacleHandle AddDynamicCircle(Vector3 p,float r){Obstacles++;return new ObstacleHandle();}
  public static void SetPhysicalBounds(ObstacleHandle h,Vector3 p,Vector3 size){}
  public static void RemoveDynamicObstacle(ObstacleHandle h){h.Released=true;Obstacles--;}
  public static bool HasLineOfSightIgnoringObstacle(Vector3 a,Vector3 b,ObstacleHandle h)=>SightClear;
  public static bool IsWalkable(Vector3 p,float r)=>true;public static bool IsPhysicalSpaceClear(Vector3 p,float r,ObstacleHandle h)=>SightClear;
  public static float SurfaceHeight(Vector3 p,float radius)=>0;
 }
 public static partial class CombatFx{public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);}
 public enum CombatVisualPriority{RealContact}public static class HitFeedback{public static int Contacts;public static void Spawn(Vector3 a,Vector3 b,float c,bool crit,CombatVisualPriority priority){Contacts++;}}
 public static class EffectPreferences{public static bool ReducedEffects;}
 public static class DestructibleDebrisBurst{public static int Bursts;public static void Spawn(Transform t,Material m,int n){Bursts++;}}
 public enum SoundCue{Hit}public static class GameAudio{public static void Play(SoundCue c){}}
 public class PlayerController:MonoBehaviour{public bool IsDead;public int CombatEpoch=4,nextCast;public float Health=30,MaxHealth=100,Energy=20;public int NewCastId()=>++nextCast;public void Heal(float v){Health+=v;}public void RestoreSkillEnergy(float v){Energy+=v;}}
 public sealed partial class GameSession
 {
  public static GameSession Instance;public PlayerController Player;public bool HasStarted=true,ModeFinished,InDungeon=true,DungeonCleared=true,FinishedResultDismissed,ChallengeRun,PracticeActive;
  public bool InputBlocked=>!FinishedResultDismissed;public bool CombatEnded=>ModeFinished||DungeonCleared;public void LogSystem(string s){}
 }
}
class PostVictoryPropProductionTests
{
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static DestructibleProp Pot(out GameObject visual)
 {
  var prop=new GameObject("actual scope pot").AddComponent<DestructibleProp>();visual=new GameObject("pot visible body");visual.transform.SetParent(prop.transform,false);prop.transform.position=new Vector3(0,0,1);
  prop.Initialize(DestructibleKind.Pot,1,.4f,true,PropRecovery.Health,visual.transform,null);return prop;
 }
 static bool Strike(string path,PlayerController owner,DestructibleProp prop)
 {
  CombatDamage packet=new CombatDamage(99999,false);Vector3 from=new Vector3(0,.55f,0),to=new Vector3(0,.55f,3);
  switch(path)
  {
   case "direct":prop.Impact(owner,1,packet);return true;
   case "area":DestructibleProp.StrikeArea(owner,Vector3.zero,3,packet,1);return true;
   case "cone":DestructibleProp.StrikeCone(owner,Vector3.zero,Vector3.forward,3,90,packet,1);return true;
   case "line":DestructibleProp.StrikeLine(owner,from,to,.1f,packet,1);return true;
   default:DestructibleProp hit;float fraction;bool found=DestructibleProp.FindProjectileHit(owner,from,to,.1f,out hit,out fraction);if(found)hit.Impact(owner,1,packet);return found;
  }
 }
 static void Main()
 {
  foreach(string path in new[]{"direct","area","cone","line","projectile"})foreach(bool special in new[]{false,true})
  {
   var owner=new GameObject("hero").AddComponent<PlayerController>();var game=new GameSession{Player=owner,ModeFinished=special};GameSession.Instance=game;
   GameObject visual;var prop=Pot(out visual);int obstacles=WorldTraversal.Obstacles;
   Strike(path,owner,prop);Check(!prop.Broken&&visual.activeSelf&&WorldTraversal.Obstacles==obstacles,"undismissed result cannot damage scenery through any combat path");
   game.FinishedResultDismissed=true;owner.CombatEpoch++;bool contact=Strike(path,owner,prop);
   Check(contact&&prop.Broken&&!visual.activeSelf&&WorldTraversal.Obstacles==obstacles-1,"dismissed victory breaks scene prop and releases obstruction through actual contact path");
   Check(owner.Health==33&&HitFeedback.Contacts>0&&DestructibleDebrisBurst.Bursts>0,"post-result prop produces real hit/debris callbacks and one scoped recovery");
   prop.Impact(owner,1,new CombatDamage(99999,false));Check(owner.Health==33&&WorldTraversal.Obstacles==obstacles-1,"broken prop cannot replay collision release or recovery");
   Check(game.CombatEnded&&!AdventureResultPolicy.AcceptsDamage(true,game.CombatEnded)&&!AdventureResultPolicy.AcceptsKill(true,game.CombatEnded,true),"scenery contact leaves combat and enemy economy terminal");
   UnityEngine.Object.Destroy(prop.gameObject);UnityEngine.Object.Flush();
  }
  foreach(string path in new[]{"direct","area","cone","line","projectile"})foreach(bool practice in new[]{false,true})
  {
   var owner=new GameObject("blocked hero").AddComponent<PlayerController>();var game=new GameSession{Player=owner,ModeFinished=true,DungeonCleared=practice,PracticeActive=practice,FinishedResultDismissed=true};GameSession.Instance=game;
   GameObject visual;var prop=Pot(out visual);int obstacles=WorldTraversal.Obstacles;Strike(path,owner,prop);
   Check(!prop.Broken&&visual.activeSelf&&WorldTraversal.Obstacles==obstacles,practice?"practice result cannot damage scenery through any combat path":"failed result cannot damage scenery through any combat path");
   UnityEngine.Object.Destroy(prop.gameObject);UnityEngine.Object.Flush();
  }
  foreach(string path in new[]{"direct","area","cone","line","projectile"})
  {
   var owner=new GameObject("practice hero").AddComponent<PlayerController>();var game=new GameSession{Player=owner,DungeonCleared=false,PracticeActive=true,FinishedResultDismissed=true};GameSession.Instance=game;
   GameObject visual;var prop=Pot(out visual);int obstacles=WorldTraversal.Obstacles;Strike(path,owner,prop);
   Check(!prop.Broken&&visual.activeSelf&&WorldTraversal.Obstacles==obstacles,"active practice never damages real scene props");
   UnityEngine.Object.Destroy(prop.gameObject);UnityEngine.Object.Flush();
  }
  Console.WriteLine("PASS: "+checks+" actual post-result prop damage/projectile contact/collision-release/recovery assertions (managed physical boundary)");
 }
}
