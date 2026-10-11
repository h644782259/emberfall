using System;
using System.Reflection;
using Emberfall;
using UnityEngine;
public static class RestrictedHealingProductionTests
{
 static int checks;
 static void C(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Near(float value,float expected,string message){C(Math.Abs(value-expected)<.015f,message+" actual="+value+" expected="+expected);}
 static PlayerController New(HeroClass hero,int rank,bool restricted=true,float health=1)
 {
  SummonedCompanion.active.Clear();AdvancedSkillSequence.Last=null;var s=new GameSession{ChallengeRun=restricted,InDungeon=true};var p=new PlayerController(s){HeroClass=hero,Health=health,MaxHealth=1000,skillRuntime=new SkillRuntime(hero)};s.Progression.Profile.skillRanks[6]=rank;return p;
 }
 static SummonedCompanion Pet(PlayerController p,float health=1){var pet=new SummonedCompanion{Owner=p,session=p.session,epoch=p.CombatEpoch,Health=health,MaxHealth=1000};SummonedCompanion.active.Add(pet);return pet;}
 public static string Run()
 {
  foreach(HeroClass hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner})foreach(bool restricted in new[]{false,true})for(int rank=1;rank<=3;rank++)
  {
   var p=New(hero,rank,restricted);var pet=Pet(p);float energy=p.Energy;float total=restricted?(rank==1?.6f:rank==2?.7f:.8f):(rank==1?.3f:rank==2?.42f:.55f);p.CastHeal();var sequence=AdvancedSkillSequence.Last;
   C(sequence!=null,"full actual cast emits healing sequence");Near(p.Health,1,"no instant skill healing");Near(p.Energy,energy-GameBalance.SkillEnergyCost(hero,6),"original energy charged once");Near(p.skillRuntime.Remaining(6),GameBalance.EffectiveCooldown(hero,6,rank),"original cooldown unchanged");C(p.session.HealingCharges==(restricted?2:3),"one restricted charge only");
   p.CastHeal();C(AdvancedSkillSequence.Last==sequence&&p.session.HealingCharges==(restricted?2:3),"cooldown retry cannot pay or refresh");
   for(int tick=1;tick<=5;tick++){sequence.Tick(.5f);Near(p.Health,1+1000*total*(tick-1)/5,"no early half-second healing");sequence.Tick(.5f);Near(p.Health,1+1000*total*tick/5,"five actual one-second self budgets");}
   C(sequence.gameObject.destroyed,"finite sequence retires after fifth event");sequence.Tick(10);Near(p.Health,1+1000*total,"finished sequence cannot repeat");
   Near(pet.Health,hero==HeroClass.Summoner?1+1000*(rank==1?.3f:rank==2?.42f:.55f):1,"companion old budget unchanged and summoner-only");
   Near(p.healingProtectionTime,rank>=2?5.2f:0,"original high rank protection");Near(p.healingReduction,rank==3?.25f:rank==2?.18f:0,"original defense magnitude");Near(p.Energy,energy-GameBalance.SkillEnergyCost(hero,6)+(rank==3?8:0),"original final rank-three energy refund");
  }
  foreach(HeroClass hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner})
  {
   var p=New(hero,1,true,1000);var pet=Pet(p,1000);float energy=p.Energy;p.CastHeal();C(AdvancedSkillSequence.Last==null,"full self and full legal companions reject rank one");Near(p.Energy,energy,"empty healing no energy payment");Near(p.skillRuntime.Remaining(6),0,"empty healing no cooldown payment");C(p.session.HealingCharges==3,"empty healing no charge payment");
   p.Health=999.9f;p.CastHeal();C(AdvancedSkillSequence.Last!=null,"partial health even below potion epsilon is legal");AdvancedSkillSequence.Last.Tick(5);AdvancedSkillSequence.Last.Tick(.01f);Near(p.Health,1000,"actual Heal clamps overheal");
  }
  foreach(string invalid in new[]{"dead","withdrawn","otherowner","oldepoch","othersession","inactiveowner","notstarted"})
  {
   var p=New(HeroClass.Summoner,1,true,1000);var pet=Pet(p);
   switch(invalid){case "dead":pet.Health=0;break;case "withdrawn":pet.gameObject.activeInHierarchy=false;break;case "otherowner":pet.Owner=new PlayerController(new GameSession());break;case "oldepoch":pet.epoch--;break;case "othersession":pet.session=new GameSession();break;case "inactiveowner":p.gameObject.activeInHierarchy=false;break;case "notstarted":pet.session.HasStarted=false;break;}
   C(!SummonedCompanion.HasHealingTarget(p),"actual IsAlive excludes "+invalid);p.CastHeal();C(AdvancedSkillSequence.Last==null&&p.session.HealingCharges==3,"illegal pet never authorizes payment "+invalid);
  }
  {
   var p=New(HeroClass.Summoner,1,true,1000);var pet=Pet(p);p.CastHeal();C(AdvancedSkillSequence.Last!=null&&p.session.HealingCharges==2,"wounded legal companion permits full self rank one");AdvancedSkillSequence.Last.Tick(1);Near(p.Health,1000,"full self remains capped");Near(pet.Health,61,"legal companion receives old six-percent pulse");
  }
  for(int rank=2;rank<=3;rank++){var p=New(HeroClass.Vanguard,rank,true,1000);p.CastHeal();C(AdvancedSkillSequence.Last!=null&&p.healingProtectionTime>0&&p.session.HealingCharges==2,"high rank defense may pre-use at full health");}
  {
   var p=New(HeroClass.Vanguard,1,false,1000);p.CastHeal();C(AdvancedSkillSequence.Last!=null,"normal full-health behavior unchanged");
   p=New(HeroClass.Vanguard,1,true,1000);p.session.InDungeon=false;p.CastHeal();C(AdvancedSkillSequence.Last!=null&&p.session.HealingCharges==3,"challenge flag outside dungeon uses old behavior");AdvancedSkillSequence.Last.Tick(1);Near(p.Health,1000,"outside dungeon remains capped");
  }
  foreach(string stop in new[]{"death","epoch","replacement","ended","notstarted"})
  {
   var p=New(HeroClass.Summoner,2);var pet=Pet(p);p.CastHeal();var sequence=AdvancedSkillSequence.Last;sequence.Tick(1);float health=p.Health,petHealth=pet.Health;
   switch(stop){case "death":p.IsDead=true;break;case "epoch":p.CombatEpoch++;break;case "replacement":p.session.Player=new PlayerController(new GameSession());break;case "ended":p.session.CombatEnded=true;break;case "notstarted":p.session.HasStarted=false;break;}
   sequence.Tick(1);Near(p.Health,health,"lifecycle stops self "+stop);Near(pet.Health,petHealth,"lifecycle stops companion "+stop);C(sequence.gameObject.destroyed,"lifecycle retires sequence "+stop);
  }
  {
   var p=New(HeroClass.Vanguard,1);p.CastHeal();var sequence=AdvancedSkillSequence.Last;p.session.InputBlocked=true;sequence.Tick(20);Near(p.Health,1,"pause doesn't heal or advance");p.session.InputBlocked=false;sequence.Tick(0);sequence.Tick(-1);Near(p.Health,1,"nonpositive delta no progress");sequence.Tick(1);Near(p.Health,121,"resume first tick after one gameplay second");
   p.session.ChallengeRun=false;sequence.Tick(1);Near(p.Health,241,"cast snapshots mode across session flag refresh");p.session.Progression.Profile.skillRanks[6]=3;sequence.Tick(1);Near(p.Health,361,"skill rank refresh cannot inflate active cast");
   p.Health=490;p.session.Progression.RefreshedMaxHealth=500;p.RefreshStats(false);sequence.Tick(1);Near(p.Health,500,"maximum-health refresh retains existing per-pulse percentage and clamps");sequence.Tick(1);C(sequence.gameObject.destroyed,"refresh does not extend five-event budget");
  }
  {
   var p=New(HeroClass.Vanguard,1);p.CastHeal();var sequence=AdvancedSkillSequence.Last;sequence.Tick(1);Near(p.Health,121,"current-max first pulse before refresh");
   p.session.Progression.RefreshedMaxHealth=500;p.RefreshStats(false);Near(p.Health,121,"stat refresh itself never heals");
   sequence.Tick(1);Near(p.Health,181,"noncapped pulse uses refreshed current max");sequence.Tick(1);Near(p.Health,241,"later pulse keeps refreshed current max");
   p.session.Progression.RefreshedMaxHealth=2000;p.RefreshStats(false);sequence.Tick(1);Near(p.Health,481,"noncapped pulse follows increased current max");
   sequence.Tick(1);Near(p.Health,721,"fifth pulse uses current max without restarting");C(sequence.gameObject.destroyed,"changing max cannot extend sequence");
  }
  {
   var p=New(HeroClass.Vanguard,1,true,1);p.session.InDungeon=false;p.CastHeal();var sequence=AdvancedSkillSequence.Last;
   C(sequence!=null&&p.session.HealingCharges==3,"wounded challenge outside dungeon does not pay limited charge");
   for(int tick=1;tick<=5;tick++){sequence.Tick(1);Near(p.Health,1+60*tick,"wounded challenge outside dungeon retains ordinary thirty percent");}
  }
  foreach(string stop in new[]{"death","epoch","replacement","ended","notstarted"})
  {
   var p=New(HeroClass.Summoner,3);p.CastHeal();var sequence=AdvancedSkillSequence.Last;sequence.Tick(1);sequence.Tick(1);float energy=p.Energy;
   switch(stop){case "death":p.IsDead=true;break;case "epoch":p.CombatEpoch++;break;case "replacement":p.session.Player=new PlayerController(new GameSession());break;case "ended":p.session.CombatEnded=true;break;case "notstarted":p.session.HasStarted=false;break;}
   sequence.Tick(3);Near(p.Energy,energy,"cancelled rank three never emits final energy refund "+stop);C(sequence.gameObject.destroyed,"cancelled rank three retires "+stop);
   sequence.Tick(10);Near(p.Energy,energy,"retired rank three cannot later emit energy refund "+stop);
  }
  foreach(bool empty in new[]{false,true}){var p=New(HeroClass.Vanguard,1);if(empty)p.session.HealingCharges=0;else typeof(SkillRuntime).GetProperty("Energy").SetValue(p.skillRuntime,0f);p.CastHeal();C(AdvancedSkillSequence.Last==null,"insufficient charge or energy refuses sequence");C(p.session.HealingCharges==(empty?0:3),"failure cannot spend charge");Near(p.skillRuntime.Remaining(6),0,"failure cannot start cooldown");}
  foreach(bool restricted in new[]{false,true}){var p=New(HeroClass.Vanguard,1,restricted);p.session.DrinkPotion();Near(p.Health,501,"potion remains instant fifty percent");C(AdvancedSkillSequence.Last==null,"potion creates no delayed sequence");C(p.session.HealingCharges==(restricted?2:3),"potion restricted charge unchanged");C(p.session.Progression.Potions==(restricted?3:2),"normal potion inventory unchanged");p.Health=1000;p.session.DrinkPotion();C(p.session.HealingCharges==(restricted?2:3)&&p.session.Progression.Potions==(restricted?3:2),"full-health potion never pays");}
  return "PASS "+checks+" actual cast/sequence/Heal/potion/companion-predicate assertions; managed engine shell, not Unity";
 }
}
