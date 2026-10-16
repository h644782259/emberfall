#!/usr/bin/env python3
"""Run the production visual pool at overload; preserve current-hit readability."""
import os
from pathlib import Path
import subprocess
import sys
import tempfile

root = Path(__file__).resolve().parents[1]
source = (root / "Assets/Scripts/Core/CombatVisualBudget.cs").read_text()
fixture = r"""
using System;
using System.Collections.Generic;
using System.Reflection;
using Emberfall;
using UnityEngine;
namespace UnityEngine {
 public enum RuntimeInitializeLoadType{SubsystemRegistration}
 [AttributeUsage(AttributeTargets.Method)]public class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
 public class MonoBehaviour{public GameObject gameObject;protected static void Destroy(GameObject root){root.SetActive(false);root.Call("OnDestroy");}}
 public class GameObject {
  readonly List<MonoBehaviour> components=new List<MonoBehaviour>();public bool activeInHierarchy=true;
  public T GetComponent<T>()where T:MonoBehaviour{foreach(var c in components)if(c is T)return (T)c;return null;}
  public T AddComponent<T>()where T:MonoBehaviour{var c=(T)Activator.CreateInstance(typeof(T),true);c.gameObject=this;components.Add(c);c.GetType().GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);return c;}
  public void Call(string name){foreach(var c in components)c.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);}
  public void SetActive(bool value){if(value==activeInHierarchy)return;activeInHierarchy=value;Call(value?"OnEnable":"OnDisable");}
 }
 public static class Application{public static bool isMobilePlatform=true;}
}
namespace Emberfall{public static class EffectPreferences{public static bool ReducedEffects;}}
class Program {
 static int checks;
 static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
 static void Main(){
  foreach(int cap in new[]{12,20,24,32,48}){
   var pool=new CombatVisualBudget();int retired=0;
   for(int i=0;i<cap;i++)pool.Acquire(CombatVisualPriority.Decoration,cap,()=>retired++);
   Check(pool.Acquire(CombatVisualPriority.RealContact,cap,()=>{})!=null&&retired==1&&pool.Active==cap,"real hit preempts older decoration within the profile cap");
   pool=new CombatVisualBudget();retired=0;
   var first=pool.Acquire(CombatVisualPriority.RealContact,cap,()=>retired++);
   for(int i=1;i<cap;i++)pool.Acquire(CombatVisualPriority.RealContact,cap,()=>{});
   Check(pool.Acquire(CombatVisualPriority.RealContact,cap,()=>{})!=null&&retired==1&&!first.Active&&pool.Active==cap,"latest physical contact remains readable when old contact rubble fills the pool");
   pool=new CombatVisualBudget();retired=0;
   for(int i=0;i<cap;i++)pool.Acquire(CombatVisualPriority.Finale,cap,()=>retired++);
   Check(pool.Acquire(CombatVisualPriority.RealContact,cap,()=>{})==null&&retired==0&&pool.Active==cap,"ordinary hit cannot erase an active finale");
   Check(pool.Acquire(CombatVisualPriority.Finale,cap,()=>{})!=null&&retired==1&&pool.Active==cap,"latest finale retains its existing bounded replacement");
   pool=new CombatVisualBudget();retired=0;
   for(int i=0;i<cap;i++)pool.Acquire(CombatVisualPriority.ActionBody,cap,()=>retired++);
   Check(pool.Acquire(CombatVisualPriority.Decoration,cap,()=>{})==null&&retired==0&&pool.Active==cap,"lower-quality ornament cannot evict a skill body");
  }
  {
   var pool=new CombatVisualBudget();int retired=0;int protectedRetired=0;
   for(int i=0;i<10;i++)pool.Acquire(CombatVisualPriority.Decoration,24,()=>retired++);
   foreach(var priority in new[]{CombatVisualPriority.SustainedBackground,CombatVisualPriority.ActionBody,CombatVisualPriority.RealContact,CombatVisualPriority.Finale})pool.Acquire(priority,24,()=>protectedRetired++);
   pool.SetDecorationLimit(6);
   Check(pool.ActiveDecorations==6&&pool.Active==10&&retired==4&&protectedRetired==0,"warm budget trims only ornaments, preserving persistent fields, action bodies, hits and finales");
   Check(pool.Acquire(CombatVisualPriority.Decoration,24,()=>{})==null,"new ornaments cannot refill the warm budget");
   Check(pool.Acquire(CombatVisualPriority.RealContact,24,()=>{})!=null,"warm budget leaves important feedback capacity intact");
   pool.SetDecorationLimit(int.MaxValue);
   Check(pool.Acquire(CombatVisualPriority.Decoration,24,()=>{})!=null,"cooldown restores ornament admission");
   pool.SetDecorationLimit(0);
   Check(pool.ActiveDecorations==0&&protectedRetired==0,"zero decorative budget never retires protected feedback");
  }
  CombatVisualBudget.ConfiguredLimit=1;
  var older=new GameObject();var latest=new GameObject();
  Check(CombatVisualLease.Attach(older,CombatVisualPriority.RealContact)!=null,"initial native lease owns its profile slot");
  Check(CombatVisualLease.Attach(latest,CombatVisualPriority.RealContact)!=null&&!older.activeInHierarchy&&latest.activeInHierarchy&&CombatVisualLease.Active==1,"same-contact replacement retires the old root and safely reenters OnDisable");
  latest.SetActive(false);Check(CombatVisualLease.Active==0,"OnDisable releases a current contact once");
  latest.SetActive(true);Check(CombatVisualLease.Active==1,"reenabling a live lease acquires its profile slot again");latest.SetActive(false);latest.Call("OnDestroy");Check(CombatVisualLease.Active==0,"OnDestroy after OnDisable cannot double-release its budget");
  var finale=new GameObject();var suppressed=new GameObject();CombatVisualLease.Attach(finale,CombatVisualPriority.Finale);
  Check(CombatVisualLease.Attach(suppressed,CombatVisualPriority.RealContact)==null&&!suppressed.activeInHierarchy&&CombatVisualLease.Active==1,"rejected contact destroys its own root without releasing another finale");finale.SetActive(false);
  CombatVisualBudget.ConfiguredLimit=2;
  var profileRoots=new[]{new GameObject(),new GameObject(),new GameObject()};
  Check(CombatVisualLease.Attach(profileRoots[0],CombatVisualPriority.Decoration)!=null&&CombatVisualLease.Attach(profileRoots[1],CombatVisualPriority.Decoration)!=null&&CombatVisualLease.Attach(profileRoots[2],CombatVisualPriority.Decoration)==null&&CombatVisualLease.Active==2,"lease honors the newly configured graphics capacity");
  CombatVisualLease.SetDecorationLimit(1);
  Check(!profileRoots[0].activeInHierarchy&&profileRoots[1].activeInHierarchy&&CombatVisualLease.Active==1,"thermal trim safely reenters native lease release without retiring the surviving ornament");
  CombatVisualLease.SetDecorationLimit(int.MaxValue);
  profileRoots[0].SetActive(false);profileRoots[1].SetActive(false);Check(CombatVisualLease.Active==0,"profile-cap tests leave no orphaned leases");
  Console.WriteLine("PASS "+checks+" actual contact/decorative/finale overload and full lease lifecycle checks across five graphics caps");
 }
}
"""
with tempfile.TemporaryDirectory(prefix="emberfall-contact-budget-") as directory:
    work = Path(directory)
    (work / "Program.cs").write_text(fixture)
    production = work / "CombatVisualBudget.cs"
    production.write_text(source)
    (work / "CombatVisualLease.cs").write_text((root / "Assets/Scripts/Combat/CombatVisualLease.cs").read_text())
    project = work / "Validation.csproj"
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
    (work / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("DOTNET", "dotnet")
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / "cli"), DOTNET_NOLOGO="1")
    command = [dotnet, "run", "--project", str(project)]
    subprocess.run(command, env=env, check=True)
    original = "priority==CombatVisualPriority.RealContact||priority==CombatVisualPriority.Finale"
    assert original in source
    production.write_text(source.replace(original, "priority==CombatVisualPriority.Finale"))
    rejected = subprocess.run(command, env=env, capture_output=True, text=True)
    assert rejected.returncode and "latest physical contact remains readable" in rejected.stdout + rejected.stderr, rejected.stdout + rejected.stderr
    print("PASS compiled dropped-latest-contact negative control rejected")
