#!/usr/bin/env python3
"""Full elemental entry/field/lease components; managed Unity lifecycle, not Unity rendering."""
import os,sys,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def once(s,a,b):
 assert s.count(a)==1,a
 return s.replace(a,b)
s=(ROOT/'Tests/FilledVfxAllocationTests.cs').read_text();shell='using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;'+s[s.index('namespace Emberfall'):]
shell=once(shell,'public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}','public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);if(c is ParticleSystem)AddComponent<ParticleSystemRenderer>();return c;}')
shell=once(shell,'public void SetActive(bool value){if(activeSelf==value)return;activeSelf=value;Call(value?"OnEnable":"OnDisable");}', 'public void SetActive(bool value){if(activeSelf==value)return;var before=All.ToDictionary(o=>o,o=>o.activeInHierarchy);activeSelf=value;foreach(var o in All.ToArray())if(before[o]!=o.activeInHierarchy)o.Call(o.activeInHierarchy?"OnEnable":"OnDisable");}')
a=shell.index('    public sealed class ParticleSystem:');b=shell.index('    public struct Color',a);shell=shell[:a]+(ROOT/'Tests/ElementalPriorityParticleShell.cs.txt').read_text()+shell[b:]
shell=shell.replace('public static Vector3 forward=>','public static Vector3 right=>new Vector3(1,0,0);public static Vector3 forward=>')
# Keep the real status query semantics. Timers are fixture inputs here; the complete
# EnemyStatusEffects schedule/consumption lifecycle is covered by StatusFeedbackProductionTests.
def member(source,signature):
 a=source.index(signature);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
status=(ROOT/'Assets/Scripts/Combat/EnemyStatusEffects.cs').read_text()
status_boundary='using UnityEngine;namespace Emberfall{public sealed class EnemyStatusEffects:MonoBehaviour{private float burnTime,poisonTime;private int poisonStacks;public void SetTimers(float burn,float poison,int stacks){burnTime=burn;poisonTime=poison;poisonStacks=stacks;}'+member(status,'public bool IsBurning')+member(status,'public int PoisonStacks')+'}}'
source=(ROOT/'Assets/Scripts/Combat/ElementalCombatVfx.cs').read_text()
files=['Core/CombatVisualBudget','Core/DecorationBudget','Core/FilledVfxPlacement','Combat/CombatVisualLease','Combat/DecorationLease','Combat/ElementalFieldVisual','Combat/CoveredAreaParticles']
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='elemental-priority-') as temp:
 for name,expected in [('current',None),('old-area-particle-gate','full decoration pool must retain actual Area primary field'),('old-aura-parent-ownership','aura main shape must be sibling of optional particles'),('unbound-status-clock','bound live status owns aura expiry across wall-clock advance')]:
  d=Path(temp)/name;d.mkdir();(d/'Shell.cs').write_text(shell);(d/'StatusBoundary.cs').write_text(status_boundary)
  for file in files:(d/(Path(file).name+'.cs')).write_text((ROOT/('Assets/Scripts/'+file+'.cs')).read_text())
  code=source
  if name=='old-area-particle-gate':
   code=once(code,'            ElementalFieldVisual.Spawn(parent, element, radius, false,CombatVisualPriority.SustainedBackground);','')
   code=once(code,'            particles.Play();','            particles.Play();\n            ElementalFieldVisual.Spawn(parent, element, radius, false,CombatVisualPriority.SustainedBackground);')
  if name=='old-aura-parent-ownership':
   code=once(code,'            particles.gameObject.SetActive(true);','            if(activeShape!=null)activeShape.transform.SetParent(particles.transform,false);\n            particles.gameObject.SetActive(true);')
  if name=='unbound-status-clock':code=once(code,'            aura.BindStatus(enemy);','')
  (d/'ElementalCombatVfx.cs').write_text(code);(d/'Tests.cs').write_text((ROOT/'Tests/ElementalPriorityProductionTests.cs').read_text());(d/'Program.cs').write_text('System.Console.WriteLine(ElementalPriorityProductionTests.Run());')
  (d/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');project=d/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
  build=subprocess.run([dotnet,'build',str(project),'--configfile',str(d/'NuGet.Config'),'-v:q'],capture_output=True,text=True)
  if build.returncode:print(build.stdout+build.stderr);build.check_returncode()
  run=subprocess.run([dotnet,str(d/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True)
  if expected:
   assert run.returncode and 'System.Exception: '+expected in run.stdout+run.stderr,run.stdout+run.stderr
   print('PASS: '+name+' compiled and failed exact assertion: '+expected)
  else:print(run.stdout+run.stderr);run.check_returncode()
