#!/usr/bin/env python3
"""Exercise central production identities, visual recipes and real arrival branch.

Uses the complete pure production tables and the actual SummonerSpell branch;
presentation endpoints are recorders, not Unity/GPU acceptance.
"""
from pathlib import Path
import os
import subprocess
import sys
import tempfile

root = Path(os.environ.get('EMBERFALL_TEST_ROOT', Path(__file__).resolve().parents[1]))


def block(text, marker):
    start = text.index(marker)
    begin = text.index('{', start)
    end, depth = begin + 1, 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]


arrival = block((root / 'Assets/Scripts/Combat/SummonerSpell.cs').read_text(), 'if (skill == 2 || skill == 4 || skill == 6 || skill == 9)')
fixture = r'''
using System;using System.Collections.Generic;using Emberfall;using UnityEngine;
namespace UnityEngine{
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 down=>new Vector3(0,-1,0);}
 public class Transform{public Vector3 position;}
}
namespace Emberfall{
 public class PlayerController{public Transform transform=new Transform();public EnemyController AimTarget;}
 public class EnemyController{}
 public class ProgressionService{public GameProfile Profile=new GameProfile();}
 public class GameSession{public ProgressionService Progression=new ProgressionService();}
 public static class CombatSight{public static Vector3 GroundPoint(Vector3 a,Vector3 b)=>b;}
 public class SummonedCompanion{public enum Kind{Wolf,Spirit,Treant,Wisp}public Transform transform=new Transform();public Kind Form;public static SummonedCompanion Last;public static bool ReturnNull;public static SummonedCompanion CastContract(PlayerController player,GameSession game,Kind form,int rank,Vector3 at,float damage,bool pack,EnemyController commanded,bool preserve){Last=ReturnNull?null:new SummonedCompanion{Form=form,transform=new Transform{position=at}};return Last;}}
 public enum SkillPerformanceBody{Spirit}
 public enum CombatVisualPriority{ActionBody}
 public static class SkillPerformanceVfx{public static int Wolves;public static void Sustain(PlayerController h,Vector3 at,float radius,float life,SkillPerformanceBody type){if(type!=SkillPerformanceBody.Spirit)throw new Exception("wrong animal");Wolves++;}}
 public static class FilledSkillVfx{public static int Wolves;public static void Impact(PlayerController h,Vector3 at,float radius,FilledVfxKind type,Color color,CombatVisualPriority priority){if(type!=FilledVfxKind.Summon)throw new Exception("wrong animal");Wolves++;}}
 public static class PhysicalSkillVfx{public static int GroundContacts;public static void Impact(Vector3 at,Vector3 direction,Color color,float radius){if(direction.y!=-1||radius!=1.4f)throw new Exception("physical summon contact");GroundContacts++;}}
 public static class ArrivalProbe{
  public static void Cast(int skill){var player=new PlayerController();var game=new GameSession();int rank=3;var target=new Vector3(2,1,3);float damage=10;EnemyController commandTarget=null;bool preserveTargetPoint=false;Color color=GameBalance.ClassColor(HeroClass.Summoner);
   ARRIVAL
  }
 }
}
static class Program{
 static int checks;static void C(bool value,string message){checks++;if(!value)throw new Exception(message);}static void Throws(Action action){try{action();}catch(ArgumentOutOfRangeException){checks++;return;}throw new Exception("invalid identity accepted");}
 static void Main(){
  var ids=new HashSet<string>();var resources=new HashSet<string>();int casterCount=0;
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))for(int slot=0;slot<GameBalance.SkillCount;slot++){
   var recipe=SkillIdentity.Describe(hero,slot);C(recipe.Hero==hero&&recipe.Slot==slot,"stable role and slot");C(ids.Add(recipe.Id)&&resources.Add(recipe.IconResourceKey),"all forty skills retain separate identity and icon resources");C(recipe.Name==GameBalance.SkillName(hero,slot)&&recipe.Description==GameBalance.SkillDescription(hero,slot),"copy reads real gameplay authority");C(recipe.IconResourceKey.StartsWith("skill-entities-")&&!recipe.IconResourceKey.StartsWith("skills-"),"new photography does not overwrite existing paintings");C(float.IsFinite(recipe.PhotographSeconds)&&recipe.PhotographSeconds>=0&&recipe.PhotographSeconds<1.2,"photograph selects a live production stage");
   if(recipe.IncludeCaster)casterCount++;if(slot==3)C(recipe.IncludeCaster&&recipe.PhotographSeconds==0&&GameBalance.IsPassive(slot),"permanent mastery is an actual actor without invented cast");if(slot==9)C(GameBalance.SkillEnergyCost(hero,slot)==0&&recipe.Description.Contains("无需"),"zero-cost ultimate copy follows actual resource rule");
  }
  C(ids.Count==40&&resources.Count==40&&casterCount==10,"full identity/actor photograph coverage");
  C(SkillIdentity.Describe(HeroClass.Vanguard,6).Primary==SkillPrimaryBody.LifeBanner,"healing has an actual battle-flag identity");
  C(SkillIdentity.Describe(HeroClass.Ranger,8).Primary==SkillPrimaryBody.WindWard,"ranger defense identity cannot be a wolf");
  C(SkillIdentity.Describe(HeroClass.Summoner,2).Primary==SkillPrimaryBody.Wolf&&SkillIdentity.Describe(HeroClass.Summoner,4).Primary==SkillPrimaryBody.TimberTurret&&SkillIdentity.Describe(HeroClass.Summoner,6).Primary==SkillPrimaryBody.Ember&&SkillIdentity.Describe(HeroClass.Summoner,9).Primary==SkillPrimaryBody.Treant,"all four contracts name their actual production partners");
  C(SkillIdentity.Describe(HeroClass.Arcanist,2).Primary==SkillPrimaryBody.Lightning&&SkillIdentity.Describe(HeroClass.Arcanist,9).Primary==SkillPrimaryBody.Meteor,"storm and ultimate keep actual discharge and meteor subjects");
  Throws(()=>SkillIdentity.Describe((HeroClass)99,0));Throws(()=>SkillIdentity.IconResourceKey(HeroClass.Ranger,-1));Throws(()=>SkillIdentity.Id(HeroClass.Ranger,10));
  foreach(int skill in new[]{2,4,6,9})foreach(bool missing in new[]{false,true}){
   SummonedCompanion.ReturnNull=missing;SkillPerformanceVfx.Wolves=FilledSkillVfx.Wolves=PhysicalSkillVfx.GroundContacts=0;ArrivalProbe.Cast(skill);
   bool wolf=skill==2&&!missing;C(SkillPerformanceVfx.Wolves==(wolf?1:0)&&FilledSkillVfx.Wolves==(wolf?1:0),"only real wolf arrival owns animal apparitions");C(PhysicalSkillVfx.GroundContacts==(!missing&&skill!=2?1:0),"other living partners own their actual physical contact");if(!missing)C((int)SummonedCompanion.Last.Form==(skill==2?0:skill==4?1:skill==6?3:2),"arrival preserves actual contract form");
  }
  Console.WriteLine(SkillVisualRecipeTests.Run());Console.WriteLine("PASS "+checks+" complete production identity/copy/resource and actual summon-arrival checks; managed, not GPU acceptance");
 }
}
'''.replace('ARRIVAL', arrival)
paths = {
    'GameTypes.cs': root / 'Assets/Scripts/Core/GameTypes.cs',
    'SkillIdentity.cs': root / 'Assets/Scripts/Core/SkillIdentity.cs',
    'FilledVfxRecipes.cs': root / 'Assets/Scripts/Core/FilledVfxRecipes.cs',
    'SkillVisualRecipe.cs': root / 'Assets/Scripts/Core/SkillVisualRecipe.cs',
    'SkillVisualRecipeTests.cs': root / 'Tests/SkillVisualRecipeTests.cs',
}
with tempfile.TemporaryDirectory(prefix='emberfall-skill-identities-') as directory:
    work = Path(directory)
    for name, path in paths.items():
        (work / name).write_text(path.read_text())
    (work / 'Program.cs').write_text(fixture)
    project = work / 'Validation.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
    config = work / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('DOTNET', 'dotnet')
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / 'cli'), DOTNET_NOLOGO='1')
    subprocess.run([dotnet, 'restore', str(project), '--configfile', str(config)], env=env, check=True, stdout=subprocess.DEVNULL)
    command = [dotnet, 'run', '--project', str(project), '--no-restore', '-c', 'Release']
    subprocess.run(command, env=env, check=True)
    controls = [
        ('neutral-wolves', 'SkillVisualRecipe.cs', 'default: return FilledVfxKind.Fault;', 'default: return FilledVfxKind.Summon;', 'only spirit makes a summoned animal'),
        ('all-partners-wolves', 'Program.cs', 'partner != null && form == SummonedCompanion.Kind.Wolf', 'partner != null', 'only real wolf arrival owns animal apparitions'),
        ('wrong-life-banner-identity', 'SkillIdentity.cs', 'SkillPrimaryBody.LifeBanner, SkillPrimaryBody.GroundFault', 'SkillPrimaryBody.GuardArmor, SkillPrimaryBody.GroundFault', 'healing has an actual battle-flag identity'),
    ]
    for name, target, before, after, expected in controls:
        original = fixture if target == 'Program.cs' else paths[target].read_text()
        if before not in original:
            raise AssertionError('mutation target missing: ' + name)
        (work / target).write_text(original.replace(before, after, 1))
        result = subprocess.run(command, env=env, capture_output=True, text=True)
        if result.returncode == 0 or expected not in result.stdout + result.stderr:
            raise AssertionError('mutation was not rejected: ' + name + '\n' + result.stdout + result.stderr)
        print('PASS: ' + name + ' production mutation rejected', flush=True)
        (work / target).write_text(original)
