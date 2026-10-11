#!/usr/bin/env python3
"""Execute real hero flight methods against clock/ground/render doubles.

Checks physical acceleration, exact arrow-tip contact, stable release snapshots,
zero-delay catch-up and contact retirement. Does not claim Unity/GPU acceptance.
"""
import os
import re
from pathlib import Path
import subprocess
import sys
import tempfile

root = Path(__file__).resolve().parents[1]
flight_source = (root / "Assets/Scripts/Combat/SkillPerformanceVfx.cs").read_text()
motion_source = (root / "Assets/Scripts/Combat/PhysicalSkillMotion.cs").read_text()


def extract(source, signature):
    start = source.index(signature)
    cursor = source.index("{", start) + 1
    depth = 1
    while depth:
        depth += (source[cursor] == "{") - (source[cursor] == "}")
        cursor += 1
    return source[start:cursor]


fixture = r'''
using System;
namespace UnityEngine {
 public struct Vector3 {
  public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public static Vector3 up=>new Vector3(0,1,0); public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
  public Vector3 normalized=>magnitude>0?this/magnitude:new Vector3();
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator -(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);
  public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
  public static Vector3 operator /(Vector3 a,float b)=>new Vector3(a.x/b,a.y/b,a.z/b);
 }
 public struct Quaternion {public Vector3 Up;public static Quaternion Euler(float x,float y,float z)=>new Quaternion{Up=Vector3.up};public static Quaternion FromToRotation(Vector3 a,Vector3 b)=>new Quaternion{Up=b.normalized};}
 public class Transform {public Vector3 position,localScale=new Vector3(.65f,.6f,.65f);public Quaternion rotation;}
 public class Renderer {public bool enabled;}
 public class ParticleSystem {public void Emit(int count){}public void Play(){}}
 public class Light {public float intensity;}
 public static class Time {public static int frameCount;}
 public static class Mathf {public const float PI=(float)Math.PI;public static float Max(float a,float b)=>Math.Max(a,b);public static float Sqrt(float v)=>(float)Math.Sqrt(v);public static float Cos(float v)=>(float)Math.Cos(v);public static float Sin(float v)=>(float)Math.Sin(v);public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));}
}
namespace Emberfall {
 using UnityEngine;
 enum SkillPerformanceBody {ArrowVolley,Meteor,Lightning,Poison,Fire}
 enum WeaponVisualAnchor {BowArrowRest}
 enum CombatSightKind {Area}
 class Hero {public Transform transform=new Transform();}
 class CombatModel {public bool TryGetWeaponVisualAnchor(WeaponVisualAnchor anchor,out Vector3 muzzle){muzzle=new Vector3(2,2.8f,1);return true;}}
 static class EffectPreferences {public static bool ReducedEffects;}
 static class WorldTraversal {public static float SurfaceHeight(Vector3 at,float radius){if(radius>.041f)throw new Exception("narrow platform contact footprint must remain small");return 1.6f;}}
 static class CombatSight {public static bool Area(Vector3 a,Vector3 b)=>true;public static Vector3 BoundaryPoint(CombatSightKind k,Vector3 a,Vector3 b)=>b;}
 static class PhysicalSkillMotion {internal const float Gravity=9.81f;BALLISTICS}
 static class GroundSkillSurface {public enum Deposit{WetPoison,Scorch}public static void Ensure(Transform t,Hero h,Vector3 p,float s,Deposit d,Func<float> remaining){throw new Exception("airborne flight cannot paint a ground field");}}
 class Flight {
  Hero owner=new Hero();CombatModel model=new CombatModel();ParticleSystem particles;Light glow;Transform transform=new Transform();
  Transform[] parts;Renderer[] surfaces;SkillPerformanceBody kind;Vector3 landing,launch;float age,life,beatAge,beatDuration,flightRadius,size=4.6f;bool flightArmed,awaitingContact;int beatPreparedFrame;
  Vector3[] flightFrom,flightTo;
  public Flight(bool meteor=false){kind=meteor?SkillPerformanceBody.Meteor:SkillPerformanceBody.ArrowVolley;parts=new Transform[meteor?3:12];surfaces=new Renderer[parts.Length];for(int i=0;i<parts.Length;i++){parts[i]=new Transform();surfaces[i]=new Renderer();}}
  void LatchDischarges(){}
  FLIGHT_METHODS
  public void Sample(float time){beatAge=time;AnimateFlight();}
  public Vector3 Tip(int i)=>kind==SkillPerformanceBody.ArrowVolley?parts[i].position+parts[i].rotation.Up*(parts[i].localScale.y*1.7f):parts[i].position;
  public Vector3 Destination(int i)=>flightTo[i];public Vector3 Start(int i)=>flightFrom[i];public bool Armed=>flightArmed;public bool Visible(int i)=>surfaces[i].enabled;
  public void MoveCaster(){owner.transform.position=new Vector3(500,100,200);}
 }
 static class Program {
  static int checks;static void Check(bool v,string message){checks++;if(!v)throw new Exception(message);}
  static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;static bool Near(Vector3 a,Vector3 b)=>(a-b).magnitude<.0003f;
  static void Main(){
   foreach(float duration in new[]{.08f,.18f,.28f,.42f,1.6f}) {
    var f=new Flight();f.PrepareBeat(new Vector3(8,0,4),duration,4.6f);
    for(int i=0;i<12;i++) {
     f.Sample(0);Check(Near(f.Tip(i),f.Start(i)),"every arrow begins at the captured physical muzzle");
     f.Sample(duration);Check(Near(f.Tip(i),f.Destination(i)),"all arrow tips contact on the scheduled damage frame");
     Check(Near(f.Tip(i).y,1.625f),"arrow tip lands on the narrow elevated platform");
     Check((f.Tip(i)-new Vector3(8,1.625f,4)).magnitude<=4.6f,"impact distribution stays within the actual damage pulse radius");
     float dt=duration*.2f;f.Sample(duration*.3f-dt);float before=f.Tip(i).y;f.Sample(duration*.3f);float at=f.Tip(i).y;f.Sample(duration*.3f+dt);float after=f.Tip(i).y;
     Check(Math.Abs((before-2*at+after)/(dt*dt)+9.81f)<.006f,"arrow vertical acceleration remains physical gravity");
    }
    f.Sample(duration*.4f);Vector3 tip=f.Tip(0);f.MoveCaster();f.Sample(duration*.4f);Check(Near(tip,f.Tip(0)),"an arrow in flight does not teleport when its caster moves");
    f.Beat();Check(!f.Armed,"real damage event retires the flight");for(int i=0;i<12;i++)Check(!f.Visible(i),"real impact hides flying body without a second launch");
    f.PrepareBeat(new Vector3(8,0,4),0);Check(!f.Armed,"overdue catch-up event cannot schedule fake future flight");for(int i=0;i<12;i++)Check(!f.Visible(i),"zero delay creates no late visible flight");
   }
   var m=new Flight(true);m.PrepareBeat(new Vector3(4,0,6),.42f);m.Sample(.42f);Check(Near(m.Tip(0),new Vector3(4,2.65f,6)),"meteor body reaches terrain at its actual contact height");m.Beat();Check(!m.Visible(0)&&!m.Armed,"meteor contact retires descending body before damage");
   Console.WriteLine("PASS "+checks+" actual hero body flight checks; managed, not Unity rendering.");
  }
 }
}
'''
fixture = fixture.replace("internal const float Gravity=9.81f;", re.search(r"internal const float Gravity=[^;]+;", motion_source).group())
fixture = fixture.replace("BALLISTICS", "\n".join(extract(motion_source, name) for name in [
    "internal static Vector3 BallisticPosition(", "internal static Vector3 BallisticVelocity("]))
fixture = fixture.replace("FLIGHT_METHODS", "\n".join(extract(flight_source, name) for name in [
    "internal void PrepareBeat(", "internal void Beat(", "private void AnimateFlight("]))
with tempfile.TemporaryDirectory(prefix="emberfall-hero-flight-") as directory:
    work = Path(directory)
    (work / "Program.cs").write_text(fixture)
    project = work / "Validation.csproj"
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
    config = work / "NuGet.Config"
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("DOTNET", "dotnet")
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / "cli"), DOTNET_NOLOGO="1")
    subprocess.run([dotnet, "restore", str(project), "--configfile", str(config)], env=env, check=True, stdout=subprocess.DEVNULL)
    command = [dotnet, "run", "--project", str(project), "--no-restore", "-c", "Release"]
    subprocess.run(command, env=env, check=True)
    # Prove the contact/clock checks reject broken production behavior.
    controls = [
        ("tip-offset", "parts[i].position=point-direction*(1.7f*parts[i].localScale.y);", "parts[i].position=point;", "every arrow begins at the captured physical muzzle"),
        ("contact-retirement", "surface.enabled=false;", "surface.enabled=true;", "real impact hides flying body"),
        ("overdue-flight", "flightArmed=until>0;", "flightArmed=true;", "overdue catch-up event cannot schedule fake future flight"),
    ]
    for name, before, after, expected in controls:
        if before not in fixture:
            raise AssertionError("mutation target missing: " + name)
        (work / "Program.cs").write_text(fixture.replace(before, after))
        rejected = subprocess.run(command, env=env, capture_output=True, text=True)
        if rejected.returncode == 0 or expected not in rejected.stdout + rejected.stderr:
            raise AssertionError("mutation was not rejected: " + name + "\n" + rejected.stdout + rejected.stderr)
        print("PASS: " + name + " production mutation rejected", flush=True)
    (work / "Program.cs").write_text(fixture)
