#!/usr/bin/env python3
"""Actual cast gate/resource transaction plus healing and contact identity routes.

Rendering endpoints are explicit recorders here. Current physical mesh/material,
contact geometry and real Transform acceptance execute in separate production
and native Unity suites; this suite does not model those implementations.
"""
from pathlib import Path
import os
import subprocess
import sys
import tempfile

root = Path(os.environ.get('EMBERFALL_TEST_ROOT', Path(__file__).resolve().parents[1]))
dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('DOTNET', 'dotnet')


def member(path, marker, body_only=False):
    text = (root / 'Assets/Scripts' / path).read_text()
    start = text.index(marker)
    brace = text.index('{', start)
    cursor, depth = brace + 1, 1
    while depth:
        depth += (text[cursor] == '{') - (text[cursor] == '}')
        cursor += 1
    return text[brace + 1:cursor - 1] if body_only else text[start:cursor]


fixture = r'''
using System;using System.Collections.Generic;using System.Linq;using UnityEngine;using Emberfall;
namespace UnityEngine{
 public class Transform{public Vector3 position,forward=Vector3.forward;}
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public struct Vector3{
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 zero=>default;public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward=>new Vector3(0,0,1);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>0?this/magnitude:zero;
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float n)=>new Vector3(a.x*n,a.y*n,a.z*n);public static Vector3 operator/(Vector3 a,float n)=>new Vector3(a.x/n,a.y/n,a.z/n);
  public static float Angle(Vector3 a,Vector3 b)=>(float)(Math.Acos(Math.Clamp((a.x*b.x+a.y*b.y+a.z*b.z)/Math.Max(.00001f,a.magnitude*b.magnitude),-1,1))*180/Math.PI);
 }
 public static class Mathf{public static float Min(float a,float b)=>Math.Min(a,b);public static int CeilToInt(float n)=>(int)Math.Ceiling(n);}
}
namespace Emberfall{
 public class GameSession{public PlayerController Player;public float Healed;public List<EnemyController> Enemies=new List<EnemyController>();public void RecordActualHealing(float h){Healed+=h;}public void SpawnFloatingText(Vector3 at,string text,Color color){}}
 public class PlayerController{
  public bool IsDead;public float Health=1,MaxHealth=100;public Transform transform=new Transform();public GameSession session;public SkillRuntime skillRuntime;public HeroClass HeroClass;
  public PlayerController(HeroClass hero){HeroClass=hero;skillRuntime=new SkillRuntime(hero);session=new GameSession{Player=this};}
  public CombatDamage RollDirectDamage(float n)=>new CombatDamage(n,false);public void RegisterSkillHit(int id){}public void ApplySpellDodgeBoon(EnemyController enemy){}public void HitArea(Vector3 p,float r,CombatDamage d,float k,float s,int castId){}
  HEAL
  ENERGY
 }
 public class EnemyController{public bool IsDead,Invulnerable;public Transform transform=new Transform();public float Health=100;public int Attempts;public void TakeDamage(float n,Vector3 force,float knockback=0,float stun=0,bool critical=false,int practiceCastId=0){Attempts++;if(!IsDead&&!Invulnerable){Health-=n;if(Health<=0)IsDead=true;}}}
 public static class CombatFx{public static Vector3 Flat(Vector3 p)=>new Vector3(p.x,0,p.z);public static void Slash(params object[] args){}}
 public static class CombatSight{public static bool Chain(Vector3 a,Vector3 b)=>true;public static bool Melee(Vector3 a,Vector3 b)=>true;}
 public static class DestructibleProp{public static void StrikeCone(params object[] args){}}
 public enum CombatVisualPriority{ActionBody,RealContact}
 // Pure presentation endpoints: no payment, target choice or health mutation.
 public static class FilledSkillVfx{
  public static int Release,Contact,Pulses,WrongIdentity;
  public static bool IdentityContact(PlayerController h,Vector3 p,Vector3 f,float r,Color color,int identity,CombatVisualPriority priority=CombatVisualPriority.ActionBody){if(identity!=(h.HeroClass==HeroClass.Arcanist?2:3))WrongIdentity++;if(priority==CombatVisualPriority.RealContact)Contact++;else Release++;return true;}
  public static void HealingPulse(PlayerController h,Color color){Pulses++;}
 }
 public static class AdvancedSkillVfx{public static void Beam(params object[] args){}}
 public static class ElementalCombatVfx{public static int Discharges;public static void Lightning(params object[] args){Discharges++;}}
 public static class ChainElectrifiedVisual{public static int Contacts;public static void Attach(PlayerController p,EnemyController e){Contacts++;}}
 public static class SummonedCompanion{public static float Healed;public static void HealAll(PlayerController p,float fraction){Healed+=fraction;}}
 public class RouteProbe{
  PlayerController owner;GameSession session;Vector3 target=new Vector3(0,0,2),forward=Vector3.forward;CombatDamage damage;int rank=1,skill=4,step,castId=5;float range=1;HeroClass heroClass;
  public RouteProbe(PlayerController player,float attack){owner=player;session=player.session;heroClass=player.HeroClass;damage=new CombatDamage(attack,false);}
  CHAIN
  public void RunChain()=>ChainLightning(2);
  public void RunImpulse(float strength){var player=owner;var game=session;int propCast=castId;float damage=strength;Color color=new Color(1,1,1);
   IMPULSE
  }
 }
 public class HealingProbe{
  PlayerController owner;HeroClass heroClass;bool restrictedHealing;int rank,step=4,steps=5;Color color=new Color(1,1,1);
  public HealingProbe(PlayerController owner,bool restricted,int level){this.owner=owner;heroClass=owner.HeroClass;restrictedHealing=restricted;rank=level;}
  HEALING
  public void Tick()=>Healing();
 }
}
static class IdentityTests{
 static int checks;static void Check(bool yes,string reason){checks++;if(!yes)throw new Exception(reason);}static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
 static void Reset(){FilledSkillVfx.Release=FilledSkillVfx.Contact=FilledSkillVfx.Pulses=FilledSkillVfx.WrongIdentity=ElementalCombatVfx.Discharges=ChainElectrifiedVisual.Contacts=0;SummonedCompanion.Healed=0;}
 static void Main(){
  foreach(string route in new[]{"chain","impulse"})foreach(string scenario in new[]{"damage","immune","zero","corpse","kill","empty"}){
   Reset();var h=new PlayerController(route=="chain"?HeroClass.Arcanist:HeroClass.Summoner);var enemy=new EnemyController();enemy.transform.position=new Vector3(0,0,2);enemy.Invulnerable=scenario=="immune";enemy.IsDead=scenario=="corpse";if(scenario=="kill")enemy.Health=5;if(scenario!="empty")h.session.Enemies.Add(enemy);float strength=scenario=="zero"?0:10;var probe=new RouteProbe(h,strength);if(route=="chain")probe.RunChain();else probe.RunImpulse(strength);bool hit=scenario=="damage"||scenario=="kill";
   Check(FilledSkillVfx.Contact==(hit?1:0),route+" only actual health decrease routes contact");Check(FilledSkillVfx.Release==(route=="impulse"?1:0),"release priority remains distinct from confirmed contact");Check(FilledSkillVfx.WrongIdentity==0,"class owns contact identity");Check(enemy.Attempts==(scenario=="corpse"||scenario=="empty"?0:1),"existing live-target damage dispatch");
   if(route=="chain"){Check(ElementalCombatVfx.Discharges==1,"physical discharge is emitted even without damage contact");Check(ChainElectrifiedVisual.Contacts==(hit?1:0),"persistent electrification requires actual health loss");}
   if(scenario=="damage")Check(Near(enemy.Health,route=="chain"?71:82),"actual current skill coefficients reach health recipient");if(scenario=="kill")Check(enemy.IsDead&&FilledSkillVfx.Contact==1,"killing hit retains its contact");if(scenario=="immune"||scenario=="zero")Check(enemy.Health==100,"no-damage target remains unchanged");
  }
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(bool restricted in new[]{false,true})foreach(int rank in new[]{1,2,3})foreach(string state in new[]{"injured","full"}){
   Reset();var h=new PlayerController(hero);h.skillRuntime.TryConsume(7,1);float energy=h.skillRuntime.Energy;if(state=="full")h.Health=100;if(state=="dead")h.IsDead=true;float before=h.Health;new HealingProbe(h,restricted,rank).Tick();float ordinary=rank==1?.30f:rank==2?.42f:.55f,limited=rank==1?.60f:rank==2?.70f:.80f;float expected=state=="injured"?100*(restricted?limited:ordinary)/5:0;
   Check(Near(h.Health-before,expected)&&Near(h.session.Healed,expected),"actual Heal preserves five-pulse health budget and legal recipient");Check(FilledSkillVfx.Pulses==(state=="injured"?1:0),"healing contact pulse requires actual health increase");Check(Near(h.skillRuntime.Energy,energy+(rank==3?8:0)),"rank-three final continuation uses real SkillRuntime energy restoration");Check(Near(SummonedCompanion.Healed,hero==HeroClass.Summoner?ordinary/5:0),"companion budget remains ordinary healing fraction across restrictions");
  }
  Console.WriteLine("PASS "+checks+" actual healing/chain/impulse identity checks (real health/energy/budgets; pure presentation recorders)");
 }
}
'''
fixture = fixture.replace('  HEAL\n', member('Combat/PlayerController.cs', 'public void Heal('))
fixture = fixture.replace('  ENERGY\n', member('Combat/PlayerController.cs', 'internal void RestoreSkillEnergy('))
fixture = fixture.replace('  CHAIN\n', member('Combat/AdvancedSkillSequence.cs', 'private void ChainLightning('))
fixture = fixture.replace('   IMPULSE\n', member('Combat/SummonerSpell.cs', 'else if (skill == 0)', True))
fixture = fixture.replace('  HEALING\n', member('Combat/AdvancedSkillSequence.cs', 'private void Healing('))

with tempfile.TemporaryDirectory(prefix='emberfall-skill-identity-') as directory:
    work = Path(directory)
    for path in ['Core/GameTypes.cs', 'Core/CombatBalance.cs', 'Core/SkillDamageBudgets.cs',
                 'Core/SkillRuntime.cs', 'Combat/CombatDamage.cs', 'Combat/SummonerDamageRules.cs']:
        (work / Path(path).name).write_text((root / 'Assets/Scripts' / path).read_text())
    program = work / 'Program.cs'
    program.write_text(fixture)
    (work / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>')
    (work / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / 'cli'), DOTNET_NOLOGO='1', DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='true')
    build = [dotnet, 'build', str(work / 'Test.csproj'), '--configfile', str(work / 'NuGet.Config'), '-v:q']
    run = [dotnet, str(work / 'bin/Debug/net8.0/Test.dll')]
    subprocess.run(build, env=env, check=True)
    subprocess.run(run, env=env, check=True)
    for before, after, oracle in [
        ('if(nearest.Health<endpointHealth)', 'if(true)', 'chain only actual health decrease routes contact'),
        ('if(enemy.Health<contactHealth)', 'if(true)', 'impulse only actual health decrease routes contact'),
        ('if(owner.Health>healthBefore)', 'if(true)', 'healing contact pulse requires actual health increase'),
    ]:
        assert before in fixture
        program.write_text(fixture.replace(before, after))
        subprocess.run(build, env=env, check=True, stdout=subprocess.DEVNULL)
        failed = subprocess.run(run, env=env, text=True, capture_output=True)
        assert failed.returncode and 'System.Exception: ' + oracle in failed.stdout + failed.stderr, failed.stdout + failed.stderr
        print('PASS compiled contact negative control:', oracle)

# The real cast gate/paid transaction is mandatory, rather than an owner stub
# inside the route fixture claiming payment semantics it does not execute.
subprocess.run([sys.executable, str(root / 'Tests/SkillReleasePreparationProductionTests.py'), dotnet], check=True)
