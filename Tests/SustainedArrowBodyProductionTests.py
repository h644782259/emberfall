#!/usr/bin/env python3
"""Execute the actual retained sequence and main-arrow rental under VFX overload.

Resource/render construction is a boundary. Production Configure, schedule,
arrow contacts, pool, lease, body allocation and flight contact methods execute.
"""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import os, subprocess, sys, tempfile

root=Path(__file__).resolve().parents[1]
ns={'__file__':str(root/'Tests/EffectPriorityProductionTests.py')}
exec((root/'Tests/EffectPriorityProductionTests.py').read_text().split('effects=',1)[0],ns)
shell,boundaries=ns['shell'],ns['boundaries']
shell=shell.replace('public bool IsDead;public int CombatEpoch;', 'public bool IsDead;public int CombatEpoch;public int Hits;public float HitStrength;public bool ContactAtEveryHit=true;public void HitArea(Vector3 at,float radius,CombatDamage damage,float k=0,float s=0,int castId=0){Hits++;HitStrength+=damage.Amount;ContactAtEveryHit&=GameObject.All.Any(o=>o.activeInHierarchy&&o.name.EndsWith("Arrow embedded on the actual hit"));}public void HealingProtection(int rank){}')
shell=shell.replace('public bool HasStarted,ModeFinished,InputBlocked;', 'public bool HasStarted,ModeFinished,InputBlocked,CombatEnded;public bool CombatEffectsEnded=>CombatEnded;')
shell=shell.replace('public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();','public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T GetComponentInChildren<T>()where T:Component=>GetComponent<T>();')
shell=shell.replace('public Vector3 normalized=>this*(1/magnitude);','public Vector3 normalized=>magnitude>0?this*(1/magnitude):zero;')
shell=shell.replace('public static Vector3 operator-(Vector3 a,Vector3 b)=>','public static Vector3 operator-(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>')
shell=shell.replace('public sealed class ParticleSystem:Component{','public sealed class ParticleSystem:Component{public void Play(){}public void Emit(int count){}')
boundaries=boundaries.replace('public enum Element{Fire,Ice}','public enum Element{Fire,Ice,Poison}')
boundaries+='namespace UnityEngine{public class Camera:Component{public static Camera main;public DepthTextureMode depthTextureMode;}public class Light:Component{public float intensity;}public static partial class Mathf{public static float Sqrt(float x)=>(float)Math.Sqrt(x);}}'
boundaries+='namespace UnityEngine{public partial struct Color{public static Color Lerp(Color a,Color b,float t)=>new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);}}'

source=(root/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text()
performance_source=(root/'Assets/Scripts/Combat/SkillPerformanceVfx.cs').read_text()
def member(text,marker):
 a=text.index(marker);b=text.index('{',a)+1;depth=1
 while depth:depth+=(text[b]=='{')-(text[b]=='}');b+=1
 return text[a:b]
fields=source[source.index('        private PlayerController owner;'):source.index('        public static void Spawn(')]
ranger=source[source.index('        private void Ranger()'):];a=ranger.index('                case 9:')+len('                case 9:');b=ranger.index('                    break;',a);event=ranger[a:b]
fixture='''using System;using System.Linq;using UnityEngine;namespace Emberfall{
public static class CombatBalance{public static float RankPower(int rank)=>1;}public static class BuildCatalog{public static float CinderTrailTickMultiplier(bool concentrated)=>1;}
public struct CombatDamage{public float Amount;public static CombatDamage operator*(CombatDamage d,float n){d.Amount*=n;return d;}}
public static class CombatProjectile{public static void Friendly(PlayerController h,GameSession s,Vector3 a,Vector3 b,CombatDamage d,Color c,bool p,bool arrow,bool basic,float size,float speed,int castId){}}
public class AdvancedSkillVfx{public static AdvancedSkillVfx Healing(PlayerController h,float r,Color c,float life,int detail,System.Func<bool> active)=>new AdvancedSkillVfx();public void Stop(){}public static AdvancedSkillVfx Rune(PlayerController h,Vector3 at,float size,Color c,float duration,int detail,bool follow=false,int identity=0)=>new AdvancedSkillVfx();}
public enum WeaponVisualAnchor{BowArrowRest}
public class CombatModel:MonoBehaviour{public void PrepareSkillPerformanceBeat(int skill,int cast,float until,bool draw=false){}public void SkillPerformanceBeat(int skill,int cast){}public bool TryGetWeaponVisualAnchor(WeaponVisualAnchor anchor,out Vector3 point){point=transform.position+Vector3.up*1.2f;return true;}}
internal enum SkillPerformanceBody{Tornado,Gravity,Meteor,ArrowVolley,Poison,Spirit,Frost,Fault,Fire,Lightning,GravityCollapse}
internal class SkillPerformanceVfx:MonoBehaviour{
private PlayerController owner;private GameSession session;private int epoch;private Transform authority;private bool bound,retired,awaitingContact;private Func<bool> stateActive;private SkillPerformanceBody kind;private float age,life,size,beatAge,beatDuration;private Vector3 launch,landing;private CombatModel model;private Vector3[] flightFrom,flightTo;private float flightRadius;private bool flightArmed;private int beatPreparedFrame;private Transform[] parts;private Renderer[] surfaces;private MaterialPropertyBlock block;private ParticleSystem particles;private Light glow;
private static void Assets(){} // Resource/material construction has separate native validation.
private void Build(){parts=new Transform[5];surfaces=new Renderer[5];for(int i=0;i<5;i++){var o=new GameObject("Main arrow flight");o.transform.SetParent(transform,false);parts[i]=o.transform;parts[i].localScale=new Vector3(1.15f,.6f,2.5f);surfaces[i]=o.AddComponent<MeshRenderer>();surfaces[i].enabled=false;}}
private void LatchDischarges(){}
public bool Visible=>gameObject.activeInHierarchy&&surfaces.Any(s=>s.enabled);public bool Armed=>flightArmed;
public void Sample(float t){beatAge=t;AnimateFlight();}
PERFORMANCE_METHODS
}
internal sealed class SequenceProbe:MonoBehaviour{
FIELDS
public FilledSkillVfx.ArrowBatchHandle Handle=>arrowBatch;public SkillPerformanceVfx Performance=>performance;public int Step=>step;public int Steps=>steps;public float Next=>nextEvent;public float Age=>age;
public void SetHandle(FilledSkillVfx.ArrowBatchHandle handle){arrowBatch=handle;}
public void Setup(PlayerController h,int id,int level=1){owner=h;session=GameSession.Instance;heroClass=HeroClass.Ranger;skill=9;rank=level;epoch=h.CombatEpoch;castId=id;range=1;target=new Vector3(1,0,2);color=new Color(1,1,1);damage=new CombatDamage{Amount=10};Configure();}
private EnemyController Nearest(Vector3 p,float r)=>null;private void Healing(){}private void Vanguard(){}private void Arcanist(){}private void Pull(Vector3 p,float r,float s){}
private void Ranger(){EVENT}
SEQUENCE_METHODS
private static Vector3 Circle(float a,float r)=>new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
}}
'''.replace('FIELDS',fields).replace('EVENT',event)
fixture=fixture.replace('PERFORMANCE_METHODS','\n'.join(member(performance_source,key) for key in ['        internal static SkillPerformanceVfx Sustain(', '        internal void PrepareBeat(', '        internal void Beat()', '        private void AnimateFlight()', '        internal void Retire()']))
# Execute the unmodified owner/session/epoch/producer gate from LateUpdate;
# the remaining field animation belongs to the separate physical-flight suite.
lifecycle=member(performance_source,'        private void LateUpdate()').split('            if(session.InputBlocked',1)[0]+'        }'
fixture=fixture.replace('private static void Assets(){}',lifecycle+'\nprivate static void Assets(){}')
fixture=fixture.replace('SEQUENCE_METHODS','\n'.join(member(source,key) for key in ['        private void Configure()', '        private void Update()', '        private void PrepareNextPerformance()', '        private Vector3 RangerRainPoint(', '        private void OnDisable()']))
shell=shell.replace('public static bool Direct(Vector3 from,Vector3 to)=>','public static Vector3 GroundPoint(Vector3 from,Vector3 to)=>to;public static bool Direct(Vector3 from,Vector3 to)=>')
test=r'''using System;using System.Linq;using System.Reflection;using UnityEngine;using Emberfall;
class SustainedArrowBodyProductionTests{
static int checks;static void Check(bool v,string m){checks++;if(!v)throw new Exception(m);}static readonly BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic;
static FilledSkillVfx Effect(FilledSkillVfx.ArrowBatchHandle h)=>(FilledSkillVfx)typeof(FilledSkillVfx.ArrowBatchHandle).GetField("effect",I).GetValue(h);
static void Saturate(int cap,CombatVisualPriority priority){for(int i=0;i<cap+5;i++)CombatVisualLease.Attach(new GameObject("Overloaded optional VFX"),priority);Check(CombatVisualLease.Active==cap,"optional budget remains capped under overload");}
static PlayerController Reset(bool reduced){foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();typeof(FilledSkillVfx).GetMethod("ResetAssets",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);typeof(CombatVisualLease).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);EffectPreferences.ReducedEffects=reduced;Application.isMobilePlatform=true;Time.deltaTime=.01f;var h=new GameObject("Hero").AddComponent<PlayerController>();h.HeroClass=HeroClass.Ranger;GameSession.Instance=new GameSession{Player=h,HasStarted=true};return h;}
static void Main(){foreach(bool reduced in new[]{false,true})foreach(int rank in new[]{1,2,3})foreach(var seed in new[]{CombatVisualPriority.Decoration,CombatVisualPriority.RealContact,CombatVisualPriority.Finale}){
var h=Reset(reduced);int cap=reduced?12:20;Saturate(cap,seed);var seq=new GameObject("Real sequence").AddComponent<SequenceProbe>();seq.Setup(h,77,rank);var fx=Effect(seq.Handle);var flight=seq.Performance;
Check(seq.Handle.IsValid,"main retained arrow batch allocates even when optional finales fill the pool");Check(!(bool)typeof(FilledSkillVfx).GetField("contactTrack",I).GetValue(fx),"protected action body does not use contact-track timing or opacity");Check(seq.Performance!=null&&seq.Performance.Visible,"scheduled arrow flight exists when optional finales saturate the pool");
int total=seq.Steps;float strength=0;
for(int beat=0;beat<total;beat++){
Saturate(cap,CombatVisualPriority.RealContact);Check(seq.Handle.IsValid,"contact overload cannot evict the retained main arrow batch");Check(seq.Performance.Visible,"contact overload cannot erase the next physical trajectory");seq.Performance.Sample((seq.Next-seq.Age)*.5f);Check(seq.Performance.Visible,"main trajectory stays readable between release and contact");
Time.deltaTime=seq.Next-seq.Age+.001f;seq.gameObject.Call("Update");strength+=10*SkillDamageBudgets.AdvancedImpact(HeroClass.Ranger,9,rank,beat);
Check(h.Hits==beat+1&&h.ContactAtEveryHit,"each real damage pulse has its corresponding embedded main arrow on the same event");Check(seq.Handle.IsValid,"contact batch stays live through every ordinary and final pulse");
if(beat<total-1){Time.deltaTime=.8f;fx.gameObject.Call("Update");Check(seq.Handle.IsValid,"a faded contact retains producer ownership across frame-time spikes");}
}
Check(Math.Abs(h.HitStrength-strength)<.001f,"visual protection preserves the exact production damage budget");Check(!flight.Visible,"sequence completion retires flying arrows immediately");Check(seq.Handle.IsValid,"final contact keeps only its finite aftermath");Time.deltaTime=1.1f;fx.gameObject.Call("Update");Check(!seq.Handle.IsValid,"final arrow aftermath retires without a live producer");Check(CombatVisualLease.Active<=cap,"essential arrow ownership cannot expand optional lease budget");}
foreach(string exit in new[]{"abort","room","session","death"}){var h=Reset(true);var seq=new GameObject("Exit sequence").AddComponent<SequenceProbe>();seq.Setup(h,77);var old=seq.Handle;var fx=Effect(old);var flight=seq.Performance;if(exit=="abort")UnityEngine.Object.Destroy(seq.gameObject);else{if(exit=="room")h.CombatEpoch++;if(exit=="session")GameSession.Instance=new GameSession{Player=h,HasStarted=true};if(exit=="death")h.IsDead=true;seq.gameObject.Call("Update");fx.gameObject.Call("Update");flight.gameObject.Call("LateUpdate");}Check(!old.IsValid&&!fx.gameObject.activeSelf,"producer exit releases protected main arrow bodies: "+exit);Check(!seq.PerformanceVisibleBoundary(),"producer exit releases flight authority: "+exit);}
Reset(true);Console.WriteLine("PASS "+checks+" production sustained-arrow schedule/contact/overload/exit checks; managed engine/resource boundaries, not GPU.");}}
'''
fixture=fixture.replace('public void SetHandle(', 'public bool PerformanceVisibleBoundary()=>performance!=null&&performance.Visible;public void SetHandle(')

if __name__=='__main__':
 with tempfile.TemporaryDirectory(prefix='sustained-arrow-body-') as directory:
  p=Path(directory);(p/'Shell.cs').write_text(shell);(p/'ResourceBoundaries.cs').write_text('using System;using System.Linq;using System.Collections.Generic;using UnityEngine;'+boundaries);(p/'Sequence.cs').write_text(fixture);(p/'Test.cs').write_text(test)
  for f in ['Core/CombatImpactBatch','Core/SkillDamageBudgets','Core/CombatVisualBudget','Core/FilledVfxRecipes','Core/FilledVfxPlacement','Combat/CombatVisualLease','Combat/FilledSkillVfx','Combat/AnchoredImpactMesh','Combat/PhysicalSkillMotion']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
  project=p/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(project);(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
  env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(project)]
  subprocess.run(cmd,env=env,check=True)
  for file,old,new,message in [('FilledSkillVfx.cs','essentialTrack||actionActive!=null||essentialBody?null:','essentialTrack||essentialBody?null:','contact overload cannot evict'),('FilledSkillVfx.cs','actionActive!=null&&actionActive()','false','a faded contact retains producer ownership'),('Sequence.cs','!(producerBoundActionBody&&type==SkillPerformanceBody.ArrowVolley)&&','', 'contact overload cannot erase')]:
   target=p/file;original=target.read_text();assert old in original;target.write_text(original.replace(old,new));result=subprocess.run(cmd,env=env,capture_output=True,text=True);target.write_text(original);assert result.returncode!=0 and message in result.stdout+result.stderr,(result.stdout,result.stderr)
   print('PASS compiled negative:',message)
