"""Execute actual gravity pulses/pull and area boundary against numeric actor doubles."""
from pathlib import Path
import tempfile, subprocess, sys, os
root=Path(__file__).resolve().parents[1]
def member(text,key):
    a=text.index(key);b=text.index('{',a)+1;depth=1
    while depth:
        depth+=(text[b]=='{')-(text[b]=='}');b+=1
    return text[a:b]
sequence=(root/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text(encoding='utf-8')
arcanist=member(sequence,'private void Arcanist()')
event=arcanist[arcanist.index('case 7:'):arcanist.index('case 9:')]
player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text(encoding='utf-8')
condition=next(line.strip()[4:-1] for line in member(player,'internal void HitArea(').splitlines() if 'if (delta.magnitude <=' in line)
game=(root/'Assets/Scripts/Core/GameTypes.cs').read_text(encoding='utf-8')
# Reuse the existing vector fixture without running its legacy scaling assertions.
legacy=(root/'Tests/BlackHoleScalingProductionTests.py').read_text(encoding='utf-8')
math=legacy.split('namespace UnityEngine{',1)[1].split('namespace Emberfall{',1)[0]
program=r'''
using System;using System.Collections.Generic;
namespace UnityEngine{MATH
namespace Emberfall{using UnityEngine;
public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}
public static class GameBalance{public const float ArcanistFinaleRadius=16,ArcanistPulseRadius=13.6f;SCALELEGACY SCALE}
public static class CombatFx{public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);}
public static class CombatSight{public static bool Clear=true;public static bool Area(Vector3 a,Vector3 b)=>Clear;}
public class EnemyController{public bool IsDead,IsBoss;public float HitFootprintBonus;public Transform transform=new Transform();public int Pulls,Hits;public void ApplyPull(Vector3 delta,bool physical){if(!physical)throw new Exception("nonphysical pull");transform.position+=delta;Pulls++;}}
public class GameSession{public List<EnemyController> Enemies=new List<EnemyController>();}
public static class SkillDamageBudgets{public static float AdvancedImpact(HeroClass h,int s,int r,int step)=>1;}
public static class SkillPerformanceVfx{public static void CollapseGravity(Player owner,Vector3 at,float radius){}}
public class Player{public GameSession Session;public void HitArea(Vector3 at,float radius,float damage,float knockback=0,float stun=0,int castId=0,bool strictRadius=false){
if(!strictRadius)throw new Exception("gravity must opt into strict damage radius");
foreach(var enemy in Session.Enemies){if(enemy.IsDead)continue;var delta=CombatFx.Flat(enemy.transform.position-at);if(CONDITION)enemy.Hits++;}}}
public class Probe{
GameSession session=new GameSession();Player owner;float age=.3f,range=1,damage=1;int rank=1,step,steps=11,castId=1,skill=7;HeroClass heroClass=HeroClass.Arcanist;Vector3 target;
PULL
void Pulse(){switch(skill){EVENT}}
static void Check(bool value,string message){if(!value)throw new Exception(message);}
public static void Main(){int checks=0;
for(int rank=1;rank<=3;rank++)foreach(bool boss in new[]{false,true})foreach(bool finale in new[]{false,true}){
var p=new Probe{rank=rank,range=GameBalance.SkillRangeMultiplier(HeroClass.Arcanist,7,rank)};p.owner=new Player{Session=p.session};p.step=finale?p.steps-1:0;
float radius=ElementalistFieldTuning.GravityDamageRadius*p.range;
var e=new EnemyController{IsBoss=boss,HitFootprintBonus=boss?1.2f:0};p.session.Enemies.Add(e);
e.transform.position=new Vector3(radius+.01f,0,0);p.Pulse();Check(e.Hits==0,"outside visual core has no damage, including boss footprint and finale");
e.transform.position=new Vector3(radius-.01f,0,0);p.Pulse();Check(e.Hits==1,"inside visual core takes damage");
e.Hits=0;e.transform.position=new Vector3(ElementalistFieldTuning.GravityPullRadius*p.range-.1f,0,0);float before=e.transform.position.magnitude;
p.Pull(p.target,ElementalistFieldTuning.GravityPullRadius*p.range,ElementalistFieldTuning.GravityPullStrength(rank));p.Pulse();Check(e.Pulls==1&&e.transform.position.magnitude<before&&e.Hits==0,"outer ring pulls without damage");
for(int i=0;i<100&&e.transform.position.magnitude>radius;i++)p.Pull(p.target,ElementalistFieldTuning.GravityPullRadius*p.range,ElementalistFieldTuning.GravityPullStrength(rank));
p.Pulse();Check(e.Hits==1,"damage begins only after pull enters visual core");
CombatSight.Clear=false;int hits=e.Hits,pulls=e.Pulls;p.Pull(p.target,ElementalistFieldTuning.GravityPullRadius*p.range,12);p.Pulse();Check(e.Hits==hits&&e.Pulls==pulls,"walls block both effects");CombatSight.Clear=true;checks+=5;
}Console.WriteLine("PASS "+checks+" gravity pulse, finale, boss footprint, pull and occlusion boundary checks");}}
}
'''.replace('MATH',math).replace('SCALELEGACY',member(game,'public static float SkillRangeMultiplier(int rank)')).replace('SCALE',member(game,'public static float SkillRangeMultiplier(HeroClass hero,int skill,int rank)')).replace('PULL',member(sequence,'private void Pull(')).replace('EVENT',event).replace('CONDITION',condition)
with tempfile.TemporaryDirectory(prefix='gravity-boundary-') as tmp:
    p=Path(tmp);(p/'Program.cs').write_text(program,encoding='utf-8');(p/'ElementalistFieldTuning.cs').write_text((root/'Assets/Scripts/Core/ElementalistFieldTuning.cs').read_text(encoding='utf-8'),encoding='utf-8')
    (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
    (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
