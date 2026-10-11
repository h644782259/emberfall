"""Execute production black-hole rank scaling and pull against explicit scene doubles."""
from pathlib import Path
import subprocess,tempfile,sys,os
root=Path(__file__).resolve().parents[1]
def member(text,signature):
 a=text.index(signature);b=text.index('{',a)+1;depth=1
 while depth:depth+=(text[b]=='{')-(text[b]=='}');b+=1
 return text[a:b]
sequence=(root/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text()
game=(root/'Assets/Scripts/Core/GameTypes.cs').read_text()
fixture=r"""
using System;using System.Collections.Generic;
namespace UnityEngine{
 public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>this/(magnitude>0?magnitude:1);public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator/(Vector3 a,float b)=>a*(1/b);}
 public static class Mathf{public static float Sin(float f)=>(float)Math.Sin(f);public static float Clamp01(float f)=>Math.Clamp(f,0,1);public static float Min(float a,float b)=>Math.Min(a,b);}public static class Time{public static float deltaTime=.1f;}public class Transform{public Vector3 position;}
}
namespace Emberfall{using UnityEngine;
 public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}
 public static class GameBalance{SCALELEGACY SCALE}
 public static class CombatFx{public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);}
 public static class CombatSight{public static bool Clear=true;public static bool Area(Vector3 a,Vector3 b)=>Clear;}
 public class EnemyController{public bool IsDead;public Transform transform=new Transform();public int Pulls;public void ApplyPull(Vector3 move,bool physical){if(!physical)throw new Exception("physical pull flag lost");transform.position+=move;Pulls++;}}
 public class GameSession{public List<EnemyController> Enemies=new List<EnemyController>();}
 public class Sequence{GameSession session=new GameSession();float age=.3f;PULL
 public void Run(int rank,float distance,bool clear=true,bool dead=false){var enemy=new EnemyController{IsDead=dead};enemy.transform.position=new Vector3(distance,0,0);session.Enemies.Clear();session.Enemies.Add(enemy);CombatSight.Clear=clear;Pull(new Vector3(),5.2f*GameBalance.SkillRangeMultiplier(HeroClass.Arcanist,7,rank),7);bool expected=rank==3&&distance>1&&clear&&!dead;if((enemy.Pulls>0)!=expected)throw new Exception("wrong pull boundary for rank="+rank+" distance="+distance);if(enemy.Pulls>0&&enemy.transform.position.magnitude>=distance)throw new Exception("pull moved outward");}}
 public static class Program{public static void Main(){var sequence=new Sequence();sequence.Run(1,8);sequence.Run(3,8);sequence.Run(3,8,false);sequence.Run(3,8,true,true);sequence.Run(3,.5f);if(GameBalance.SkillRangeMultiplier(HeroClass.Ranger,7,3)!=GameBalance.SkillRangeMultiplier(3))throw new Exception("unrelated class range changed");Console.WriteLine("PASS: actual pull includes distant enemy only at upgraded rank, respects occlusion/death/core boundary and preserves other class scaling");}}
}
"""
fixture=fixture.replace('SCALELEGACY',member(game,'public static float SkillRangeMultiplier(int rank)')).replace('SCALE',member(game,'public static float SkillRangeMultiplier(HeroClass hero,int skill,int rank)')).replace('PULL',member(sequence,'private void Pull('))
assert 'sequence.range = GameBalance.SkillRangeMultiplier(hero.HeroClass,index,skillRank)' in sequence
assert 'Pull(target,5.2f*range' in sequence and '4.8f:5.5f)*range' in sequence
with tempfile.TemporaryDirectory(prefix='blackhole-rank-') as temporary:
 p=Path(temporary);(p/'Program.cs').write_text(fixture);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1],'run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',UseSharedCompilation='false'))
