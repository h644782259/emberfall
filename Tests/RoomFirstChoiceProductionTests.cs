// Real first-room choice flow; Unity scene and persistence services are doubles.
using System;using System.Linq;using UnityEngine;
namespace Emberfall {public sealed partial class GameSession {
static int checks;static void C(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
public static void Probe(){
 foreach(int seed in new[]{0,3,6,9,12,15}){
  Time.timeScale=1;var s=new GameSession(seed);var plan=s.RoomChainRun.Room;int epoch=s.Player.CombatEpoch;
  C(s.RunChoices.CompletedWave==0&&!s.RunChoices.AwaitingChoice&&!s.pendingRoomChoice.Pending,"first room begins without any chosen blessing");
  foreach(var e in s.Enemies)e.transform.position=new Vector3(0,0,-10);
  s.Player.Teleport(s.Markers[1].transform.position);for(int i=0;i<12;i++)s.Tick();
  C(s.RoomChainRun.SealComplete(1)&&!s.RoomChainRun.SealComplete(0)&&!s.pendingRoomChoice.Pending,"B-first completion alone does not request blessing");
  s.Player.Teleport(s.Markers[0].transform.position);for(int i=0;i<12;i++)s.Tick();
  C(s.RoomChainRun.DoorUnlocked&&s.RoomChainRun.Seals==2&&s.pendingRoomChoice.Pending&&!s.RunChoices.AwaitingChoice,"both seals request deferred blessing without synchronous UI");
  C(s.Enemies.Count==6&&s.RunChoices.CompletedWave==0,"completion bypasses live guards without inventing selected wave");
  s.Player.Teleport(new Vector3(0,0,14));C(!s.EnterNextRoom()&&ReferenceEquals(s.RoomChainRun.Room,plan)&&s.Progression.Saves==0,"pending blessing blocks actual EnterNextRoom before save or transition");
  s.TickRoomTactics();C(s.pendingRoomChoice.Pending&&!s.RunChoices.AwaitingChoice,"same frame cannot claim deferred request");
  s.Tick();C(!s.pendingRoomChoice.Pending&&s.RunChoices.AwaitingChoice&&s.RunChoices.CompletedWave==1&&s.RunChoices.Offer.Length==3,"next frame claims deferred blessing through real offer generation");
  C(s.InputBlocked&&Time.timeScale==0,"real InputBlocked and UpdateTimeScale pause during blessing choice");
  C(!s.EnterNextRoom()&&ReferenceEquals(s.RoomChainRun.Room,plan)&&s.Player.CombatEpoch==epoch,"awaiting choice blocks room exit even after request is claimed");
  C(!s.ConfirmRoomInterlude(-1)&&s.RunChoices.AwaitingChoice,"invalid choice cannot release gate");
  s.Paused=true;C(!s.ConfirmRoomInterlude(0),"manual pause blocks blessing confirmation");s.Paused=false;
  C(s.ConfirmRoomInterlude(0)&&!s.RunChoices.AwaitingChoice&&s.RunChoices.Active.Count()==1&&!s.InputBlocked&&Time.timeScale==1,"real confirm selects one offered blessing and resumes combat");
  C(!s.ConfirmRoomInterlude(0)&&s.RunChoices.Active.Count()==1,"duplicate confirmation cannot choose twice");
  C(s.EnterNextRoom()&&s.RoomChainRun.Room.Index==1&&s.Player.CombatEpoch==epoch+1&&s.Progression.Saves==1,"only completed choice permits saved epoch-retiring transition");
  s.Tick();C(!s.pendingRoomChoice.Pending&&!s.RunChoices.AwaitingChoice&&s.RunChoices.Active.Count()==1,"new room cannot reclaim old first-room choice");
 }
 Console.WriteLine("PASS: "+checks+" actual first-room B-first deferred-choice/real RunChoices/pause/transition assertions; managed scene and save-service doubles");
}}}
class Program {static void Main(){Emberfall.GameSession.Probe();}}
