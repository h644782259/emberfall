using System;using System.Reflection;using UnityEngine;using Emberfall;
class StatusFeedbackTests
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Call(object target,string name){target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);}
 static T Field<T>(object target,string name)=> (T)target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
 static EnemyController Create(out PlayerController owner){Time.time=5;Time.deltaTime=0;Time.frameCount=1;owner=new PlayerController();GameSession.Instance=new GameSession{Player=owner};var e=new EnemyController();e.StatusEffects=new EnemyStatusEffects{OwnerEnemy=e};Call(e.StatusEffects,"Awake");return e;}
 static void Main()
 {
  PlayerController owner;var enemy=Create(out owner);var status=enemy.StatusEffects;
  status.Poison(owner,4,10);status.Poison(owner,4,10);status.Poison(owner,4,10);status.Burn(owner,4,20);
  var aura=enemy.GetComponent<ElementalEnemyAura>();var poison=Field<ParticleSystem>(aura,"poison");var shape=Field<ElementalFieldVisual>(aura,"poisonShape");var fire=Field<ParticleSystem>(aura,"fire");
  var independentDetonationTail=new GameObject(); // Distinct externally-owned tail: aura clear must never retire siblings.
  float stored;Check(!status.ConsumePoison(new PlayerController(),71,out stored)&&poison.isEmitting,"foreign owner keeps poison visuals");
  Check(status.ConsumePoison(owner,71,out stored)&&stored==90&&status.PoisonStacks==0&&status.PoisonRemaining==0,"consume preserves stored damage and clears gameplay immediately");
  Check(!poison.isEmitting&&!poison.gameObject.activeSelf&&!shape.gameObject.activeSelf,"poison consumption retires sustained visuals immediately");
  Check(fire.isEmitting&&fire.gameObject.activeSelf&&independentDetonationTail.activeSelf,"fire and independent burst tail remain live");
  status.Poison(owner,4,7);Check(status.PoisonStacks==1&&ReferenceEquals(poison,Field<ParticleSystem>(aura,"poison"))&&poison.isPlaying&&shape.gameObject.activeSelf,"same frame reapply safely revives exact retained channel");
  Call(aura,"Update");Check(poison.isPlaying&&poison.gameObject.activeSelf&&shape.gameObject.activeSelf,"same frame aura update cannot delete refreshed poison");
  status.Poison(owner,4,7);status.Poison(owner,4,7);Check(!status.ConsumePoison(owner,71,out stored)&&status.PoisonStacks==3&&poison.isPlaying,"duplicate receipt never clears new application");
  Time.deltaTime=1;Time.frameCount++;Call(status,"Update");Check(enemy.Hits.Count>0,"reapplied actual status still ticks");
  enemy=Create(out owner);owner.Specialization=ElementalistSpecialization.Burn;float health=enemy.Health;
  for(int i=0;i<2;i++)owner.OnBasicAttackHitTarget(Vector3.zero,enemy,true);
  Check(!enemy.StatusEffects.IsBurning&&GameSession.Instance.Text==null,"proc timing remains third hit");
  owner.OnBasicAttackHitTarget(Vector3.zero,enemy,true);
  Check(enemy.StatusEffects.IsBurning&&!enemy.StatusEffects.HasFrostMark&&enemy.Health==health&&owner.skillRuntime.Energy==3,"real burn dispatch leaves direct damage and energy cadence unchanged");
  Check(GameSession.Instance.Text=="灼触"&&CombatFx.RingColor.r>CombatFx.RingColor.b&&GameSession.Instance.TextColor.r>GameSession.Instance.TextColor.b,"burn proc reports actual fire type");
  for(int learned=0;learned<2;learned++){
   enemy=Create(out owner);owner.Specialization=ElementalistSpecialization.Shatter;GameSession.Instance.Progression.Profile.skillRanks[0]=learned;
   for(int i=0;i<3;i++)owner.OnBasicAttackHitTarget(Vector3.zero,enemy,true);
   Check(!enemy.StatusEffects.IsBurning&&enemy.StatusEffects.HasFrostMark&&GameSession.Instance.Text=="霜触"&&CombatFx.RingColor.b>CombatFx.RingColor.r,"frost/freeze proc keeps cold type and feedback");
  }
  enemy=Create(out owner);status=enemy.StatusEffects;status.Poison(owner,4,10);status.Burn(owner,4,20);
  aura=enemy.GetComponent<ElementalEnemyAura>();poison=Field<ParticleSystem>(aura,"poison");fire=Field<ParticleSystem>(aura,"fire");
  GameSession.Instance.InputBlocked=true;Time.time+=100;Call(aura,"Update");
  Check(poison.isPlaying&&fire.isPlaying,"paused actual status must not expire on wall clock");
  GameSession.Instance.InputBlocked=false;owner.CombatEpoch++;Time.deltaTime=.1f;Time.frameCount++;Call(status,"Update");Call(aura,"Update");
  Check(!poison.gameObject.activeSelf&&!fire.gameObject.activeSelf,"invalidated actual statuses retire both sustained channels immediately");
  enemy=Create(out owner);status=enemy.StatusEffects;status.Poison(owner,4,10);status.Burn(owner,4,20);aura=enemy.GetComponent<ElementalEnemyAura>();enemy.IsDead=true;Call(aura,"Update");
  Check(!Field<ParticleSystem>(aura,"poison").gameObject.activeSelf&&!Field<ParticleSystem>(aura,"fire").gameObject.activeSelf,"death retires actual status auras");
  enemy=Create(out owner);status=enemy.StatusEffects;status.Poison(owner,4,10);status.Burn(owner,4,20);aura=enemy.GetComponent<ElementalEnemyAura>();enemy.StatusEffects=null;GameSession.Instance.InputBlocked=true;Call(aura,"Update");
  Check(!Field<ParticleSystem>(aura,"poison").gameObject.activeSelf&&!Field<ParticleSystem>(aura,"fire").gameObject.activeSelf,"missing bound state clears while paused instead of falling back to wall clock");
  Console.WriteLine("PASS actual poison->consume->aura clear->same-frame reapply chain; burn/frost basic-hit third-contact feedback. Damage budget, energy and receipt checks retained. Managed only, no Unity GPU validation.");
 }
}
