// Actual GameSession side-event host methods are extracted unchanged by the runner.
// Persistence is a failure-injecting substitute; real disk transactions are tested by Progression tests.
using System;
using System.Collections.Generic;
using Emberfall;
public static class SideEventProductionTests
{
 static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
 public static string Run()
 {
  var game=new GameSession();game.Begin();var a=new EnemyController();var b=new EnemyController();game.Register(a,b);
  game.Kill(a);Check(game.Progression.Materials==0,"one of two deaths gives no reward");
  game.Progression.FailWrite=true;game.Kill(b);
  Check(game.Progression.Materials==0,"failed durable grant must not publish materials");
  Check(game.SideEventRewardPending,"last death preserves pending receipt after failed write");
  game.Kill(b);Check(game.Progression.Materials==0,"repeated death cannot consume failed receipt");
  game.Progression.FailWrite=false;game.TrySettleSideEventRewards();
  Check(game.Progression.Materials==1&&!game.SideEventRewardPending&&game.Player.Heals==1,"retry grants material and original-fight supply exactly once");
  game.TrySettleSideEventRewards();game.Kill(a);game.Kill(b);
  Check(game.Progression.Materials==1&&game.Player.Heals==1,"duplicate and retry after completion cannot double award");
  game.Begin();game.Register(a,b);game.Kill(a);game.ChangeRoom();game.Kill(b);
  Check(game.Progression.Materials==1,"old room death cannot finish abandoned encounter");
  game.Begin();game.Register(a,b);game.Player.CombatEpoch++;game.Kill(a);game.Kill(b);
  Check(game.Progression.Materials==1,"old epoch callbacks cannot grant");
  game.Begin();game.Register(a,b);game.IsDead=true;game.Kill(a);game.Kill(b);
  Check(game.Progression.Materials==1,"death rejects unfinished side encounter callbacks");
  game.Begin();game.Register(a,b);game.Progression.FailWrite=true;game.Kill(a);game.Kill(b);game.ChangeRoom();
  game.Progression.FailWrite=false;game.TrySettleSideEventRewards();
  Check(game.Progression.Materials==2&&!game.SideEventRewardPending&&game.Player.Heals==1,"earned pending survives room travel; never heals the next room");
  game.Begin();game.Register(a,b);game.Progression.FailWrite=true;game.Kill(a);game.Kill(b);
  var old=game.Progression;game.Progression=new ProgressionService();game.TrySettleSideEventRewards();
  Check(game.Progression.Materials==0&&old.Materials==2,"explicit discard/load cannot credit another profile");
  game.DiscardForeign();Check(!game.SideEventRewardPending,"successful profile replacement clears explicitly discarded foreign pending");
  Check(SideEventRun.HasRoomCapacity(6)&&!SideEventRun.HasRoomCapacity(7),"two optional enemies respect total room limit eight");
  return "PASS: "+n+" actual side-event host failure/duplicate/death/epoch/old-room/retry checks (managed substitutes, not Unity)";
 }
}
namespace Emberfall
{
 public class EnemyController{public bool IsDead;public UnityEngine.GameObject gameObject=new UnityEngine.GameObject();}
 public class PlayerController{public int CombatEpoch,Heals;public bool IsDead;public float MaxHealth=100;public void Heal(float amount){Heals++;}}
 public class ProgressionService
 {
  public sealed class Data{public int mechanicMaterials;}public Data Profile=new Data();
  public int Materials{get{return Profile.mechanicMaterials;}set{Profile.mechanicMaterials=value;}}
  public void Save(){}
  public bool FailWrite;public string LastError="injected write failure";readonly HashSet<string> receipts=new HashSet<string>();
  public bool TryGrantSideEventReward(string receipt,out bool newlyCommitted){newlyCommitted=false;if(receipts.Contains(receipt))return true;if(FailWrite)return false;receipts.Add(receipt);Materials++;newlyCommitted=true;return true;}
 }
 public class RoomState{public object Room=new object();}
 public sealed partial class GameSession
 {
  public RoomState RoomChainRun=new RoomState();public PlayerController Player=new PlayerController();public ProgressionService Progression=new ProgressionService();
  public bool HasStarted=true,InDungeon=true,IsDead,CombatEnded,ChallengeRun;public int HealingCharges;
  private bool sideEventStarted;private UnityEngine.GameObject sideCrystal;private HashSet<EnemyController> sideEventEnemies=new HashSet<EnemyController>();
  public bool SideEventAvailable{get{return false;}}
  private void Notify(string text){}private void RecordCombatAction(string key){}private void LogSystem(string text){}
  public void Begin(){IsDead=false;RoomChainRun=new RoomState();sideEventRun=new SideEventRun(SideEventContext,Player,Player.CombatEpoch,Guid.NewGuid().ToString("N"));}
  public void Register(EnemyController a,EnemyController b){sideEventEnemies.Add(a);sideEventEnemies.Add(b);sideEventRun.Register(a);sideEventRun.Register(b);}
  public void DiscardForeign(){DiscardForeignSideEventRewards();}
  public void Kill(EnemyController enemy){RecordSideEventDefeat(enemy);}
  public void ChangeRoom(){AbandonSideEvent();RoomChainRun=new RoomState();Player.CombatEpoch++;}
 }
}
namespace UnityEngine
{
 public class GameObject{public bool activeInHierarchy=true;}public static class Time{public static float unscaledTime;}
 public static class Mathf{public static int Min(int a,int b){return Math.Min(a,b);}}
}
