#!/usr/bin/env python3
"""Actual normal producer completion, visual tails and pooled rental generations.

Drawing/resource APIs are doubles; real ownership, completion, pause, terminal,
receipt release and actual FilledSkillVfx pool methods execute unchanged.
"""
from pathlib import Path
import os, subprocess, sys, tempfile

root=Path(os.environ.get('EMBERFALL_TEST_ROOT',Path(__file__).resolve().parents[1]))
ns={'__file__':str(root/'Tests/FilledVfxPoolProductionTests.py')}
exec((root/'Tests/FilledVfxPoolProductionTests.py').read_text().split('with tempfile.TemporaryDirectory',1)[0],ns)
shell,boundaries=ns['s'],ns['boundaries']
boundaries+='namespace UnityEngine{public static partial class Mathf{public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}}}'
shell=shell.replace('public T[] GetComponentsInChildren<T>(bool include)', 'public T[] GetComponentsInChildren<T>(bool include=false)')
shell=shell.replace('public sealed class Transform\n', 'public sealed partial class Transform\n')
shell=shell.replace('public class Renderer:Component{', 'public class Renderer:Component{public void GetPropertyBlock(MaterialPropertyBlock b){b.Opacity=Opacity;}')
shell=shell.replace('public sealed class ParticleSystem:Component{', 'public sealed class ParticleSystem:Component{public int Stops,Clears;public void Stop(bool child,ParticleSystemStopBehavior reason){Stops++;if(reason==ParticleSystemStopBehavior.StopEmittingAndClear)Clears++;}')
shell=shell.replace('public static Quaternion LookRotation(Vector3 forward)=>', 'public static Quaternion LookRotation(Vector3 forward,Vector3 up)=>identity;public static Quaternion LookRotation(Vector3 forward)=>')
boundaries+='''namespace UnityEngine{public enum ParticleSystemStopBehavior{StopEmitting,StopEmittingAndClear}public sealed class DefaultExecutionOrderAttribute:Attribute{public DefaultExecutionOrderAttribute(int n){}}public class Light:Component{public float intensity;}public sealed partial class Transform{public int childCount=>GameObject.All.Count(o=>o.transform.parent==this&&!o.Destroyed);public Transform GetChild(int i)=>GameObject.All.Where(o=>o.transform.parent==this&&!o.Destroyed).ElementAt(i).transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T[] GetComponentsInChildren<T>()where T:Component=>GameObject.All.Where(o=>o==gameObject||o.transform.parent==this).Select(o=>o.GetComponent<T>()).Where(c=>c!=null).ToArray();}}
namespace Emberfall{public static class GameBalance{public const float ArcanistPulseRadius=4,ArcanistFinaleRadius=5;}internal sealed class ElementalFieldVisual:MonoBehaviour{}}
'''

def member(source,token):
    start=source.index(token);end=source.index('{',start)+1;depth=1
    while depth:depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
seq=(root/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text()
area=(root/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
perf=(root/'Assets/Scripts/Combat/SkillPerformanceVfx.cs').read_text()
fixture=r'''
using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using Emberfall;
namespace Emberfall {
 public class TestReceipt {public int Releases;public void Release(){Releases++;}}
 public struct CombatDamage {public float Amount;}
 public class AdvancedSkillVfx {public bool Settling,Stopped;public void CompleteAftermath(){Settling=true;}public void Stop(){Stopped=true;}}
 internal enum SkillPerformanceBody{Tornado,Gravity,Meteor,ArrowVolley,Poison,Spirit,Frost,Fault,Fire,Lightning,GravityCollapse,Pressure}
 internal sealed class SkillPerformanceVfx:MonoBehaviour {
  PlayerController owner;GameSession session;int epoch,beatPreparedFrame=-1,windLayers;Transform authority;bool bound,retired,settling,flightArmed,awaitingContact;Func<bool> stateActive;
  SkillPerformanceBody kind=SkillPerformanceBody.ArrowVolley;float age,life,beatAge;Transform[] parts=Array.Empty<Transform>();Renderer[] surfaces=Array.Empty<Renderer>();Material body;MaterialPropertyBlock block=new MaterialPropertyBlock();ParticleSystem particles;Light glow;
  public void Setup(PlayerController h,Transform producer){owner=h;session=GameSession.Instance;epoch=h.CombatEpoch;authority=producer;bound=true;stateActive=()=>producer.gameObject.activeInHierarchy;life=3;particles=gameObject.AddComponent<ParticleSystem>();}
  public float Age=>age;public float Life=>life;public bool Settling=>settling;public ParticleSystem Particles=>particles;
  void AnimateGravityCollapse(){}void AnimateWind(int i){}void AnimateSpirit(int i){}void AnimateFlight(){}float DischargeOpacity(int i)=>1;
  PERFORMANCE
 }
 internal sealed class SequenceProbe:MonoBehaviour {
  PlayerController owner;int step,steps=4,skill=9;SkillPerformanceVfx performance;AdvancedSkillVfx healingAura,ultimateField;FilledSkillVfx.ArrowBatchHandle arrowBatch;TestReceipt castReceipt;
  public TestReceipt Receipt;public SkillPerformanceVfx LastPerformance;public FilledSkillVfx.ArrowBatchHandle LastBatch;
  public void Setup(PlayerController h){owner=h;castReceipt=Receipt=new TestReceipt();performance=LastPerformance=new GameObject("real performance completion").AddComponent<SkillPerformanceVfx>();performance.Setup(h,transform);arrowBatch=LastBatch=FilledSkillVfx.BeginArrowBatch(h,Vector3.zero,3,new Color(1,.7f,.1f),actionActive:()=>gameObject.activeInHierarchy&&step<steps);arrowBatch.ArrowBeat(Vector3.zero,3,false);LastBatchEffect().transform.SetParent(transform,false);}private FilledSkillVfx LastBatchEffect()=>(FilledSkillVfx)typeof(FilledSkillVfx.ArrowBatchHandle).GetField("effect",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(LastBatch);
  public void Complete(){step=steps;SEQUENCE_COMPLETE}
  SEQUENCE_DISABLE
 }
 internal sealed class AreaProbe:MonoBehaviour {
  PlayerController owner;SkillPerformanceVfx performance;FilledSkillVfx.ArrowBatchHandle arrowRainVisual;TestReceipt castReceipt;List<EnemyController> pendingTickTargets=new List<EnemyController>();CombatDamage finalDamage;float age=4,delay=0,duration=3;bool ticksDrained=true;
  public TestReceipt Receipt;public SkillPerformanceVfx LastPerformance;public GameObject Cloud,Field;public ParticleSystem Particles;
  public void Setup(PlayerController h){owner=h;castReceipt=Receipt=new TestReceipt();performance=LastPerformance=new GameObject("field performance").AddComponent<SkillPerformanceVfx>();performance.Setup(h,transform);Cloud=new GameObject("existing particle cloud");Cloud.transform.SetParent(transform,false);Particles=Cloud.AddComponent<ParticleSystem>();Cloud.AddComponent<MeshRenderer>();Field=new GameObject("existing elemental body");Field.transform.SetParent(transform,false);Field.AddComponent<ElementalFieldVisual>();Field.AddComponent<MeshRenderer>();}
  public void Complete(){AREA_COMPLETE}
  AREA_RETIRE
  AREA_DISABLE
 }
}
static class Program {
 static int checks;static readonly BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic;static object F(object o,string n)=>o.GetType().GetField(n,I).GetValue(o);static void C(bool yes,string msg){checks++;if(!yes)throw new Exception(msg);}static FilledSkillVfx E(FilledSkillVfx.ArrowBatchHandle h)=>(FilledSkillVfx)F(h,"effect");
 static PlayerController Hero(){var hero=new GameObject("isolated owner").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=hero,HasStarted=true};return hero;}
 static void Tick(GameObject obj,string method,float dt){Time.deltaTime=Time.unscaledDeltaTime=dt;Time.frameCount++;obj.Call(method);}
 static SequenceProbe Sequence(PlayerController h){var s=new GameObject("actual completed sequence probe").AddComponent<SequenceProbe>();s.Setup(h);return s;}
 static void Main(){Resources.Root=Environment.GetEnvironmentVariable("RESOURCE_ROOT");EffectPreferences.ReducedEffects=true;
  var h=Hero();var s=Sequence(h);var old=s.LastBatch;var fx=E(old);Tick(fx.gameObject,"Update",.11f);float age=(float)F(fx,"age");int count=(int)F(fx,"count");s.Complete();
  C(s.gameObject.Destroyed&&s.Receipt.Releases==1,"actual completion retires producer and releases cast receipt exactly once");
  C(old.IsValid&&fx.transform.parent==null&&(float)F(fx,"age")==age&&(int)F(fx,"count")==count,"normal completion detaches the same current contact without emitting another beat");
  C(s.LastPerformance.Settling&&!s.LastPerformance.gameObject.Destroyed&&s.LastPerformance.Particles.Stops==1&&s.LastPerformance.Particles.Clears==0,"normal completion keeps existing body and particles without clearing or launching");
  C(F(fx,"actionActive")==null,"completed damage producer callback cannot revive its last contact");
  GameSession.Instance.InputBlocked=true;float life=(float)F(fx,"life");Tick(fx.gameObject,"Update",2);Tick(s.LastPerformance.gameObject,"LateUpdate",2);
  C((float)F(fx,"age")==age&&s.LastPerformance.Age==0&&old.IsValid,"pause freezes completed pooled contact and performance clock");
  GameSession.Instance.InputBlocked=false;Tick(fx.gameObject,"Update",.1f);old.CompleteAftermath(true);C((float)F(fx,"life")==life,"duplicate completion cannot extend the pool rental indefinitely");
  old.Retire();var replacement=FilledSkillVfx.BeginArrowBatch(h,Vector3.zero,3,new Color(1,.7f,.1f),actionActive:()=>true);C(E(replacement)==fx,"next actual batch reuses retired rental root");
  float freshLife=(float)F(fx,"life");old.CompleteAftermath(true);old.Retire();C(replacement.IsValid&&!(bool)F(fx,"settling")&&(float)F(fx,"life")==freshLife&&F(fx,"actionActive")!=null,"stale completion/retire handles cannot mutate a newer rental");replacement.Retire();
  var cancelled=Sequence(h);var cancelledBatch=cancelled.LastBatch;GameSession.Instance.InputBlocked=true;cancelled.gameObject.SetActive(false);
  C(!cancelledBatch.IsValid&&cancelled.LastPerformance.gameObject.Destroyed&&cancelled.Receipt.Releases==1,"cancel remains immediate and receipt is released even during pause");GameSession.Instance.InputBlocked=false;
  foreach(string exit in new[]{"death","epoch","session","result"}){
   h=Hero();var terminal=Sequence(h);terminal.Complete();GameSession.Instance.InputBlocked=true;
   if(exit=="death")h.IsDead=true;if(exit=="epoch")h.CombatEpoch++;if(exit=="session")GameSession.Instance=new GameSession{Player=h,HasStarted=true,InputBlocked=true};if(exit=="result")GameSession.Instance.ModeFinished=true;
   Tick(E(terminal.LastBatch).gameObject,"Update",.1f);Tick(terminal.LastPerformance.gameObject,"LateUpdate",.1f);
   C(!terminal.LastBatch.IsValid&&terminal.LastPerformance.gameObject.Destroyed,"completed tail immediately retires at "+exit+" before pause gate");
  }
  h=Hero();var field=new GameObject("actual finite area completion").AddComponent<AreaProbe>();field.Setup(h);field.Complete();
  C(field.gameObject.Destroyed&&field.Receipt.Releases==1&&field.Cloud.transform.parent==null&&field.Field.transform.parent==null,"area completion detaches only the existing visual children then destroys damage producer");
  C(field.Particles.Stops==1&&field.Particles.Clears==0,"existing field particles stop emission without clearing residue");
  GameSession.Instance.InputBlocked=true;Tick(field.Cloud,"LateUpdate",2);C(!field.Cloud.Destroyed&&field.Cloud.GetComponent<MeshRenderer>().Opacity==0,"pause freezes detached child before a new fade sample");
  GameSession.Instance.InputBlocked=false;Tick(field.Cloud,"LateUpdate",.6f);C(!field.Cloud.Destroyed&&field.Cloud.GetComponent<MeshRenderer>().Opacity>0&&field.Cloud.GetComponent<MeshRenderer>().Opacity<1,"detached field fades gradually through actual timing coverage");Tick(field.Cloud,"LateUpdate",SkillVisualAftermathTiming.FieldTail);C(field.Cloud.Destroyed,"detached child has finite normal-completion lifetime");
  GameSession.Instance.InputBlocked=true;h.CombatEpoch++;Tick(field.Field,"LateUpdate",.1f);C(field.Field.Destroyed,"detached field cancellation validates epoch before pause");
  foreach(float n in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,0,-1})C(SkillVisualAftermathTiming.Coverage(n)==0,"invalid visual clock cannot create opaque infinite tail");
  Console.WriteLine("PASS "+checks+" actual producer completion/receipt release/no new contact/pool generation/idempotence/pause/death/epoch/session/result/detached-particle assertions; managed, not rendered acceptance.");
 }
}
'''
fixture=fixture.replace('PERFORMANCE','\n'.join(member(perf,k) for k in ['internal void CompleteAftermath(', 'private void LateUpdate()', 'internal void Retire()']))
fixture=fixture.replace('SEQUENCE_COMPLETE',member(seq,'if (step >= steps)')).replace('SEQUENCE_DISABLE',member(seq,'private void OnDisable()'))
fixture=fixture.replace('AREA_COMPLETE',member(area,'if (age > delay + duration + .1f && ticksDrained)')).replace('AREA_RETIRE',member(area,'private void Retire() { pendingTickTargets')).replace('AREA_DISABLE',member(area,'private void OnDisable() { if(performance'))
with tempfile.TemporaryDirectory(prefix='emberfall-visual-aftermath-') as directory:
    work=Path(directory);(work/'Stubs.cs').write_text(shell);(work/'Boundaries.cs').write_text('using System;using System.Linq;using System.Collections.Generic;using UnityEngine;'+boundaries);(work/'Program.cs').write_text(fixture)
    for file in ['Core/FilledVfxRecipes','Core/FilledVfxPlacement','Core/CombatVisualBudget','Core/SkillVisualAftermathTiming','Core/ElementalistFieldTuning','Combat/CombatVisualLease','Combat/AnchoredImpactMesh','Combat/PhysicalSkillMotion','Combat/FilledSkillVfx','Combat/AuthoredActorMeshes','Combat/AuthoredSpellBases','Combat/SkillVisualAftermath']:
        (work/(Path(file).name+'.cs')).write_text((root/('Assets/Scripts/'+file+'.cs')).read_text())
    project=work/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>')
    config=work/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
    env=dict(os.environ,DOTNET_CLI_HOME=str(work/'cli'),DOTNET_NOLOGO='1',RESOURCE_ROOT=str(root/'Assets/Resources'))
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],env=env,check=True,stdout=subprocess.DEVNULL)
    command=[dotnet,'run','--project',str(project),'--no-restore','-c','Release'];subprocess.run(command,env=env,check=True)
    for file,name,before,after,expected in [
        ('FilledSkillVfx.cs','repeat-completion','||contactTrack||settling)return;','||contactTrack)return;','duplicate completion cannot extend'),
        ('FilledSkillVfx.cs','stale-handle-completion','effect.rentGeneration==generation &&','true &&','stale completion/retire handles cannot mutate'),
        ('Program.cs','normal-completion-retire','arrowBatch.CompleteAftermath(skill==9);arrowBatch=default;','arrowBatch.Retire();arrowBatch=default;','normal completion detaches the same'),
        ('SkillVisualAftermath.cs','stale-detached-child','owner.CombatEpoch != epoch ||','false ||','detached field cancellation validates epoch'),
    ]:
        target=work/file;original=target.read_text()
        if before not in original:raise AssertionError('mutation target missing '+name)
        target.write_text(original.replace(before,after,1));result=subprocess.run(command,env=env,text=True,capture_output=True);target.write_text(original)
        if result.returncode==0 or expected not in result.stdout+result.stderr:raise AssertionError('mutation not rejected '+name+'\n'+result.stdout+result.stderr)
        print('PASS '+name+' negative control rejected',flush=True)
