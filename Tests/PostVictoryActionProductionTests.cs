using System;using Emberfall;using UnityEngine;
class PostVictoryActionProductionTests
{
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Main()
 {
  foreach(bool special in new[]{false,true})foreach(HeroClass kind in Enum.GetValues(typeof(HeroClass)))
  {
   Time.frameCount++;var game=new GameSession{InDungeon=true,DungeonCleared=true,ModeFinished=special};GameSession.Instance=game;
   var player=new PlayerController(game){HeroClass=kind,skillRuntime=new SkillRuntime(kind)};int slot=kind==HeroClass.Vanguard?2:0;
   game.Progression.Profile.skillRanks[slot]=3;
   Check(player.skillRuntime.TryConsume(5,1),"fixture records existing paid cooldown");player.skillRuntime.RestoreEnergy(100);
   float before=player.Energy,cooldown=player.skillRuntime.Remaining(5);int epoch=player.CombatEpoch;
   Check(game.InputBlocked&&game.CombatEffectsEnded&&game.CombatEnded&&!player.CanBeginSkillTargeting(slot)&&!player.CastImmediateSkill(slot)&&player.Casts==0&&player.Energy==before,"result page blocks cast before dismissal and spends no budget");
   game.BossDeathPresenting=true;game.DismissFinishedResult();Check(player.CombatEpoch==epoch&&!game.FinishedResultDismissed,"boss death presentation cannot dismiss results early");game.BossDeathPresenting=false;
   game.DismissFinishedResult();Check(game.FinishedResultDismissed&&!game.InputBlocked&&!game.CombatEffectsEnded&&game.CombatEnded,"dismissed success opens player effects while enemy combat stays terminal");
   Check(player.CombatEpoch==epoch+1&&player.skillRuntime.Remaining(5)==cooldown&&player.Energy==before,"dismiss result retires old producer epoch once without cooldown reset");
   epoch=player.CombatEpoch;game.DismissFinishedResult();Check(player.CombatEpoch==epoch&&player.skillRuntime.Remaining(5)==cooldown,"duplicate dismissal cannot invalidate a new cast");
   Check(player.CanBeginSkillTargeting(slot)&&player.SkillTargetingReady(slot),"dismissed victory permits actual new skill readiness");
   Check(player.CastImmediateSkill(slot),"dismissed victory accepts actual player preparation");player.AdvanceSkillReleaseForFixture(.2f);
   Check(player.Casts>0,"post-result paid cast reaches actual skill emitter");
   Check(player.skillRuntime.Remaining(slot)>0&&player.Energy<before,"post-result new cast spends normal cooldown and energy once");
   int emitted=player.Casts;float energy=player.Energy;game.DismissFinishedResult();Check(player.CombatEpoch==epoch&&player.Casts==emitted&&player.Energy==energy,"repeated result action leaves new paid generation untouched");
   Check(!AdventureResultPolicy.AcceptsDamage(game.HasStarted,game.CombatEnded)&&!AdventureResultPolicy.AcceptsKill(game.HasStarted,game.CombatEnded,true),"post-result casts cannot reopen enemy damage or kill economy");
   player.CombatEpoch++;Check(!game.FinishedResultDismissed&&game.InputBlocked&&game.CombatEffectsEnded,"owner epoch replacement closes stale dismissed result admission");
  }
  foreach(bool practice in new[]{false,true})
  {
   var game=new GameSession{InDungeon=true,ModeFinished=true,PracticeActive=practice};GameSession.Instance=game;var player=new PlayerController(game);
   game.DismissFinishedResult();Check(game.CombatEffectsEnded&&!player.CanBeginSkillTargeting(0),"failed or practice completion never reopens player world actions");
  }
  Console.WriteLine("PASS: "+checks+" actual post-victory session/readiness/payment/emission/epoch assertions (managed emission boundaries)");
 }
}
