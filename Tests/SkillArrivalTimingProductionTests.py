#!/usr/bin/env python3
"""Execute production advanced-cast orchestration with explicit visual/contact doubles.

These checks verify event clocks, flight scheduling, pause and encounter ownership.
They do not claim Unity rendering or on-device visual acceptance.
"""
import os
from pathlib import Path
import subprocess
import sys
import tempfile

root = Path(__file__).resolve().parents[1]
source = (root / "Assets/Scripts/Combat/AdvancedSkillSequence.cs").read_text()


def method(marker):
    start = source.index(marker)
    body = source.index("{", start)
    depth, cursor = 1, body + 1
    while depth:
        depth += (source[cursor] == "{") - (source[cursor] == "}")
        cursor += 1
    return source[start:cursor]


fixture = r"""
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine{
 public class GameObject{public bool activeInHierarchy=true;}
 public class MonoBehaviour{public GameObject gameObject=new GameObject();public bool Destroyed;protected void Destroy(GameObject o){Destroyed=true;o.activeInHierarchy=false;}}
 public class Transform{public Vector3 position;}
 public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public static class Mathf{public static float Cos(float a)=>(float)Math.Cos(a);public static float Sin(float a)=>(float)Math.Sin(a);}
 public static class Time{public static float deltaTime;}
}
namespace Emberfall{
 public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}
 public static class CombatBalance{public static float RankPower(int rank)=>1+(rank-1)*.3f;}
 public static class BuildCatalog{public static float CinderTrailTickMultiplier(bool v)=>1;}
 public class PlayerController{public bool IsDead;public int CombatEpoch;}
 public class GameSession{public PlayerController Player;public bool HasStarted=true,CombatEffectsEnded,InputBlocked;}
 public static class CombatImpactBatch{public static void BeginAction(){}public static void EndAction(){}}
 public static class CombatSight{
  public static float Wall=float.PositiveInfinity;
  public static Vector3 GroundPoint(Vector3 from,Vector3 at){at.x=Math.Min(at.x,Wall);at.y=1.6f;return at;}
 }
 public class CombatModel{public int Beats,Prepares;public bool LastLaunch;public void PrepareSkillPerformanceBeat(int s,int c,float until,bool launchNow=false){Prepares++;LastLaunch=launchNow;}public void SkillPerformanceBeat(int s,int c){Beats++;}}
 public class SkillPerformanceVfx{
  public readonly List<float> Flights=new List<float>(),Radii=new List<float>();public readonly List<Vector3> Destinations=new List<Vector3>();public int Beats;
  public void PrepareBeat(Vector3 at,float until,float impactRadius=-1){Flights.Add(until);Destinations.Add(at);Radii.Add(impactRadius);}
  public bool Settled;public void Beat(){Beats++;}public void CompleteAftermath(bool final){Settled=true;}
 }
 public class AdvancedSkillVfx{public void CompleteAftermath(){}}
 public static class FilledSkillVfx{public struct ArrowBatchHandle{public void CompleteAftermath(bool final){}}}
 public static class CombatReviewObjectId{public static string Get(object o)=>"isolated-timing-hero";}
 public static class CombatReviewEvents{public static bool Enabled=true;public static int Beats;public static void Emit(string kind,string actor,string target="0",float amount=0,int skill=-1,string detail=""){if(kind=="skillbeat")Beats++;}}
 public class AdvancedSkillSequence:MonoBehaviour{
  PlayerController owner;GameSession session;HeroClass heroClass;int skill=9,rank,epoch,step,steps,castId=1;
  float range=1,interval,age,nextEvent;Vector3 target=new Vector3(0,1.6f,0);CombatModel performanceModel=new CombatModel();
  AdvancedSkillVfx healingAura;FilledSkillVfx.ArrowBatchHandle arrowBatch;public SkillPerformanceVfx LastPerformance;public SkillPerformanceVfx performance=new SkillPerformanceVfx();public int Hits;public float Coefficient;
  public readonly List<Vector3> Contacts=new List<Vector3>();
  public AdvancedSkillSequence(HeroClass hero,int level){LastPerformance=performance;heroClass=hero;rank=level;owner=new PlayerController();session=new GameSession{Player=owner};steps=SkillDamageBudgets.AdvancedSteps(hero,9,rank);interval=SkillDamageBudgets.AdvancedInterval(hero,9);nextEvent=SkillDamageBudgets.AdvancedFirstEvent(hero,9);PrepareNextPerformance();}
  void Contact(){if(performanceModel.Beats!=Hits+1)throw new Exception("body must release before damage");if(performance.Beats!=Hits+1)throw new Exception("flight body must retire before damage");Hits++;Coefficient+=SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step);Contacts.Add(heroClass==HeroClass.Ranger?RangerRainPoint(step):target);}
  void Ranger(){Contact();}void Arcanist(){Contact();}void Vanguard(){Contact();}void Healing(){}void Pull(Vector3 at,float r,float s){}
  METHODS
  public void Tick(float dt){Time.deltaTime=dt;Update();}public int ModelBeats=>performanceModel.Beats;public int ModelPrepares=>performanceModel.Prepares;public bool LastLaunch=>performanceModel.LastLaunch;public float Age=>age;public int Step=>step;public float Next=>nextEvent;public void Pause(bool value){session.InputBlocked=value;}public void Stale(){owner.CombatEpoch++;}
 }
 class Program{
  static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
  static void Main(){
   foreach(var hero in new[]{HeroClass.Ranger,HeroClass.Arcanist})foreach(int rank in new[]{1,2,3})foreach(int fps in new[]{30,60,120}){
    int observedBefore=CombatReviewEvents.Beats;var s=new AdvancedSkillSequence(hero,rank);float total=0;int count=SkillDamageBudgets.AdvancedSteps(hero,9,rank);for(int i=0;i<count;i++)total+=SkillDamageBudgets.AdvancedImpact(hero,9,rank,i);
    for(int frame=0;frame<fps*12&&!s.Destroyed;frame++)s.Tick(1f/fps);
    Check(s.LastPerformance.Settled&&CombatReviewEvents.Beats-observedBefore==count,"actual budget completion keeps a finite visual tail and reports each real claimed beat once");
    Check(s.Destroyed&&s.Hits==count&&Near(s.Coefficient,total),"frame rate cannot drop or duplicate impact budget");
    Check(s.LastPerformance.Flights.Count==count&&s.LastPerformance.Beats==count,"exactly one future flight per contact");Check(s.ModelBeats==count&&s.ModelPrepares==count&&s.LastLaunch,"each airborne body launches from matching skeletal beat before landing");
    if(hero==HeroClass.Ranger)for(int i=0;i<count;i++)Check(Near(s.LastPerformance.Radii[i],i<count-1?SkillDamageBudgets.RangerUltimatePulseRadius:SkillDamageBudgets.RangerUltimateRadius),"arrows cover ordinary pulse and final damage radius respectively");
    foreach(var flight in s.LastPerformance.Flights)Check(flight>0&&flight<=Math.Max(SkillDamageBudgets.AdvancedFirstEvent(hero,9),SkillDamageBudgets.AdvancedInterval(hero,9))+.0001f,"flight reaches the next authoritative event");
    foreach(var point in s.Contacts)Check(Near(point.y,1.6f),"flight and contact preserve raised terrain");
   }
   var normal=new AdvancedSkillSequence(HeroClass.Ranger,1);normal.Tick(.1f);normal.Tick(.1f);Check(normal.LastPerformance.Flights.Count==1,"flight is not restarted every update");normal.Tick(.41f);Check(normal.Hits==1&&Near(normal.LastPerformance.Flights[1],normal.Next-normal.Age),"late first frame shortens remaining flight rather than delaying damage");
   var stalled=new AdvancedSkillSequence(HeroClass.Ranger,3);stalled.Tick(2f);Check(stalled.Hits==3&&stalled.LastPerformance.Flights.Count==1,"catch-up is bounded and overdue contacts do not spawn late flights");stalled.Tick(.01f);Check(stalled.Hits==4&&Near(stalled.LastPerformance.Flights[1],.27f),"catch-up schedules the next future event using remaining time");
   float before=stalled.Age;int hits=stalled.Hits,flights=stalled.LastPerformance.Flights.Count;stalled.Pause(true);stalled.Tick(20);Check(Near(stalled.Age,before)&&stalled.Hits==hits&&stalled.LastPerformance.Flights.Count==flights,"pause freezes both flight clock and remaining contact budget");stalled.Pause(false);stalled.Stale();stalled.Tick(.1f);Check(stalled.Destroyed&&stalled.Hits==hits,"old encounter cannot land another hit");
   CombatSight.Wall=.5f;var clipped=new AdvancedSkillSequence(HeroClass.Ranger,1);clipped.Tick(.61f);Check(Near(clipped.LastPerformance.Destinations[0].x,.5f)&&Near(clipped.Contacts[0].x,.5f),"preview and damage share the same clipped rain center");
   Console.WriteLine("PASS "+checks+" advanced skill arrival checks (production clock methods; visual/contact doubles)");
  }
 }
}
"""
fixture = fixture.replace(
    "METHODS",
    "\n".join(method(marker) for marker in [
        "        private void Update()",
        "        private void PrepareNextPerformance()",
        "        private Vector3 RangerRainPoint(",
        "        private static Vector3 Circle(",
    ]),
)
with tempfile.TemporaryDirectory(prefix="emberfall-skill-arrival-") as directory:
    work = Path(directory)
    (work / "Program.cs").write_text(fixture)
    (work / "Test.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup>'
        '<Compile Include="' + str(root / "Assets/Scripts/Core/SkillDamageBudgets.cs") + '" /><Compile Include="' + str(root / "Assets/Scripts/Core/ElementalistFieldTuning.cs") + '" />'
        '</ItemGroup></Project>'
    )
    (work / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / "cli"), DOTNET_NOLOGO="1")
    subprocess.run([
        sys.argv[1] if len(sys.argv) > 1 else os.environ.get("DOTNET", "dotnet"),
        "run", "--project", str(work / "Test.csproj"), "--configuration", "Release",
    ], env=env, check=True)
