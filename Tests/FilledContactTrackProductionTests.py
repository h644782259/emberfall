#!/usr/bin/env python3
"""Execute committed contact-track rental/lifetime/handle methods and real lease budget.

Only scene transforms, authored-mesh availability and drawing are boundaries here.
Actual ground mesh and GPU envelopes are covered by ground/native fixtures.
"""
from pathlib import Path
import os, subprocess, sys, tempfile
root=Path(os.environ.get('EMBERFALL_TEST_ROOT',Path(__file__).resolve().parents[1]))
source=(root/'Assets/Scripts/Combat/FilledSkillVfx.cs').read_text()
def method(token):
 start=source.index(token);brace=source.index('{',start);end=brace+1;level=1
 while level:
  level+=(source[end]=='{')-(source[end]=='}');end+=1
 return source[start:end]
methods='\n'.join(method(token) for token in ['internal readonly struct ContactTrackHandle','private static FilledSkillVfx Create(','internal static ContactTrackHandle ContactTrack(','private void Update()','internal void Retire()','private void OnDisable()','private void OnEnable()','private void Register()','private void Release()','private static bool Finite('])
base=(root/'Tests/FilledVfxAllocationTests.cs').read_text();base='using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;'+base[base.index('namespace UnityEngine'):]
base=base.replace('public sealed class Mesh:Object{','public sealed class Mesh:Object{public Bounds bounds;')
base=base.replace('public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();','public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T[] GetComponentsInChildren<T>(bool include)where T:Component=>GameObject.All.Where(o=>o==gameObject||o.transform.parent==transform).Select(o=>o.GetComponent<T>()).Where(c=>c!=null).ToArray();')
base+='namespace UnityEngine {public struct Bounds {public Vector3 center,size;public Vector3 min=>center-size*.5f;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}}}'
opacity=source[source.index('float opacity=contactTrack?'):source.index(';',source.index('float opacity=contactTrack?'))+1]
fixture=r'''
using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using Emberfall;
namespace Emberfall {
 public class PlayerController:MonoBehaviour{public bool IsDead;public int CombatEpoch;}
 public class GameSession{public static GameSession Instance;public PlayerController Player;public bool HasStarted=true,ModeFinished,InputBlocked;public bool CombatEffectsEnded=>ModeFinished;}
 public static class EffectPreferences{public static bool ReducedEffects;public static float EffectsScale=1;}
 public static class WorldTraversal{public static float SurfaceHeight(Vector3 point,float clearance)=>.37f;}
 internal class GroundSkillSurface:MonoBehaviour{public enum Deposit{Scorch}public bool Retired,Independent;public Func<float> Remaining;public static float HorizontalRadius(Mesh mesh)=>.7f;public static GroundSkillSurface Ensure(Transform parent,PlayerController h,Vector3 at,float radius,Deposit kind,Func<float> remaining,bool independentLifetime=false){var fx=new GameObject("ground").AddComponent<GroundSkillSurface>();fx.transform.SetParent(parent,false);fx.Independent=independentLifetime;fx.Remaining=remaining;return fx;}public void Retire(){Retired=true;gameObject.SetActive(false);}}
 internal sealed class FilledSkillVfx:MonoBehaviour {
  private class Piece{public Vector3 At,Scale;public bool Anchored;public int Motion;public Transform Transform;}
  private readonly Piece[] pieces=new Piece[14];private int count,epoch;private PlayerController owner;private GameSession session;
  private bool pooled,disposing,registered,sculpted,confirmedFinale,contactTrack,settling;private float age,life,size,terminalAge,trackRemaining;private int finaleCast;private Color tint;private FilledVfxKind kind;private ulong rentGeneration;private Func<float> contactRemaining;private Func<bool> actionActive;private CombatVisualLease lease;
  private const int PoolCapacity=8;private static int active;private static readonly Stack<FilledSkillVfx> idle=new Stack<FilledSkillVfx>();private static readonly HashSet<FilledSkillVfx> instances=new HashSet<FilledSkillVfx>();private static readonly List<FilledSkillVfx> finales=new List<FilledSkillVfx>();
  private static Mesh flame=new Mesh{bounds=new Bounds(new Vector3(0,.6f,0),new Vector3(1.4f,1.8f,1.4f))};private float drawnOpacity;
  private static void EnsureAssets(){}private void EnsurePropertyBlock(){}private void ClearPieces(){Array.Clear(pieces);count=0;}
  private void Add(Mesh mesh,Vector3 at,Vector3 dimensions,Quaternion rotation,float delay,int motion,float phase,float footprint=0,string label="Repeated ornament",bool anchored=false){pieces[count++]=new Piece{At=at,Scale=dimensions,Anchored=anchored,Motion=motion};Animate(pieces[count-1]);}
  private void Animate(Piece p){var f=FilledVfxRecipes.Sample(kind,age,life);OPACITY drawnOpacity=opacity;}
  METHODS
 }
}
static class Program {
 static int checks;static readonly BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.NonPublic;static void C(bool yes,string message){checks++;if(!yes)throw new Exception(message);}static object F(object fx,string name)=>fx.GetType().GetField(name,I).GetValue(fx);static FilledSkillVfx E(FilledSkillVfx.ContactTrackHandle h)=>(FilledSkillVfx)F(h,"effect");static GroundSkillSurface G(FilledSkillVfx fx)=>fx.GetComponentsInChildren<GroundSkillSurface>(true).Single();static void Tick(FilledSkillVfx fx,float dt){Time.deltaTime=Time.unscaledDeltaTime=dt;fx.gameObject.Call("Update");}
 static FilledSkillVfx Ordinary(PlayerController hero,float duration=.25f)=> (FilledSkillVfx)typeof(FilledSkillVfx).GetMethod("Create",S).Invoke(null,new object[]{hero,Vector3.zero,Vector3.forward,FilledVfxKind.Fire,2.2f,new Color(1,.5f,.1f),duration,null,CombatVisualPriority.Finale,false,null,false});
 static void Main(){var hero=new GameObject("rider").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=hero};EffectPreferences.ReducedEffects=true;default(FilledSkillVfx.ContactTrackHandle).Retire();C(!default(FilledSkillVfx.ContactTrackHandle).IsValid,"default track handle is safe");
  var optional=new List<FilledSkillVfx>();for(int i=0;i<12;i++)optional.Add(Ordinary(hero));C(CombatVisualLease.Active==12,"actual optional budget is full before track creation");
  float remaining=20;var at=new Vector3(4,9,-3);var handles=new List<FilledSkillVfx.ContactTrackHandle>();for(int i=0;i<3;i++)handles.Add(FilledSkillVfx.ContactTrack(hero,at,2.2f,()=>remaining));
  C(handles.All(h=>h.IsValid)&&CombatVisualLease.Active==12&&optional.All(fx=>fx.gameObject.activeSelf),"essential real contact footprints survive a full optional visual budget without preempting other bodies");
  foreach(var h in handles){var fx=E(h);C(fx.transform.parent==null&&fx.transform.position.x==4&&fx.transform.position.z==-3&&Math.Abs(fx.transform.position.y-.37f)<.00001,"contact root is immutable world-space actual support");C((int)F(fx,"count")==2,"reduced track retains two low contact tongues");var parts=(Array)F(fx,"pieces");foreach(var part in parts){if(part==null)continue;C((bool)F(part,"Anchored")&&(int)F(part,"Motion")==27,"contact tongues reserve rooted motion rather than explosion flight");var scale=(Vector3)F(part,"Scale");C(Math.Abs(scale.y*1.8f-.28f)<.00001,"contact requested full authored vertical envelope is 0.28 metres");}C(G(fx).Independent&&Math.Abs(G(fx).Remaining()-remaining)<.001,"each damage slot owns its independent ground lifetime");}
  foreach(var h in handles)Tick(E(h),13);C(handles.All(h=>h.IsValid),"producer remaining keeps tracks alive beyond ordinary clamped initial life");C(handles.All(h=>(float)F(E(h),"drawnOpacity")==1),"long-lived current damage footprint remains fully visible");
  GameSession.Instance.InputBlocked=true;remaining=.12f;foreach(var h in handles)Tick(E(h),.3f);C(handles.All(h=>(float)F(E(h),"drawnOpacity")==1),"pause freezes current track opacity and sampled producer time");GameSession.Instance.InputBlocked=false;foreach(var h in handles)Tick(E(h),.01f);C(handles.All(h=>Math.Abs((float)F(E(h),"drawnOpacity")-.5f)<.0001),"track retires visibly against actual producer remaining");
  var retired=E(handles[0]);var ground=G(retired);handles[0].Retire();C(!handles[0].IsValid&&ground.Retired&&handles.Skip(1).All(h=>h.IsValid&&!G(E(h)).Retired),"single damage-slot replacement retires only corresponding ground and body");C(!(bool)F(retired,"contactTrack")&&F(retired,"contactRemaining")==null&&(float)F(retired,"trackRemaining")==0,"track pool return clears producer callback and clock");
  remaining=3;var replacement=FilledSkillVfx.ContactTrack(hero,new Vector3(8,0,7),2.2f,()=>remaining);C(E(replacement)==retired,"replacement reuses actual retired pool root");handles[0].Retire();C(replacement.IsValid,"stale generation handle cannot retire newly rented footprint");
  remaining=0;foreach(var h in handles.Skip(1))Tick(E(h),.01f);Tick(E(replacement),.01f);C(!replacement.IsValid&&handles.Skip(1).All(h=>!h.IsValid),"producer endpoint retires all remaining damage-slot indicators");
  foreach(var fx in optional)fx.Retire();var ordinary=Ordinary(hero);Tick(ordinary,.3f);C(!ordinary.gameObject.activeSelf&&F(ordinary,"contactRemaining")==null,"ordinary Nova rental retains finite lifetime and no track callback");
  remaining=2;var epoch=FilledSkillVfx.ContactTrack(hero,at,2.2f,()=>remaining);hero.CombatEpoch++;Tick(E(epoch),.01f);C(!epoch.IsValid,"new combat epoch invalidates real footprint");var current=FilledSkillVfx.ContactTrack(hero,at,2.2f,()=>remaining);GameSession.Instance=new GameSession{Player=hero};Tick(E(current),.01f);C(!current.IsValid,"new session cancels old producer ground");
  Console.WriteLine("PASS "+checks+" committed contact-track rental/lifetime/generation/actual-budget/3-slot isolation and requested low authored envelope checks; managed, not GPU acceptance");
 }
}
'''.replace('METHODS',methods).replace('OPACITY',opacity)
with tempfile.TemporaryDirectory(prefix='emberfall-contact-track-') as directory:
 work=Path(directory);(work/'Stubs.cs').write_text(base);(work/'Program.cs').write_text(fixture)
 for rel in ['Assets/Scripts/Core/FilledVfxRecipes.cs','Assets/Scripts/Core/CombatVisualBudget.cs','Assets/Scripts/Combat/CombatVisualLease.cs']:(work/Path(rel).name).write_text((root/rel).read_text())
 project=work/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>');config=work/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet');env=dict(os.environ,DOTNET_CLI_HOME=str(work/'cli'),DOTNET_NOLOGO='1');subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],env=env,check=True,stdout=subprocess.DEVNULL)
 command=[dotnet,'run','--project',str(project),'--no-restore','-c','Release'];subprocess.run(command,env=env,check=True)
 controls=[('optional-budget-track','essentialTrack:true','essentialTrack:false','essential real contact footprints survive'),('independent-parent-clock','age+=Time.deltaTime;\n            }','age+=Time.deltaTime;if(age>=life){Retire();return;}\n            }','producer remaining keeps tracks alive'),('generation-handle','effect.rentGeneration==generation && effect.contactTrack','effect.contactTrack','stale generation handle cannot retire'),('hard-track-retirement','Mathf.Clamp01(trackRemaining/.24f)','1f','track retires visibly against actual producer remaining'),('pooled-producer-callback','contactTrack=false;contactRemaining=null;actionActive=null;trackRemaining=0;epoch=0','contactTrack=false;actionActive=null;trackRemaining=0;epoch=0','track pool return clears producer callback')]
 for name,before,after,expected in controls:
  if before not in fixture:raise AssertionError('production mutation missing '+name)
  (work/'Program.cs').write_text(fixture.replace(before,after,1));result=subprocess.run(command,env=env,capture_output=True,text=True);(work/'Program.cs').write_text(fixture)
  if result.returncode==0 or expected not in result.stdout+result.stderr:raise AssertionError('mutation not rejected '+name+'\n'+result.stdout+result.stderr)
  print('PASS '+name+' negative control rejected',flush=True)
