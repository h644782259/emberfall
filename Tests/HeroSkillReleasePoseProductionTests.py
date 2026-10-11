#!/usr/bin/env python3
"""Execute actual body poses and emission/arrival clocks with managed transforms.

This verifies skeletal movement, draw/release, repeat sampling and cast ownership.
Unity rendering, rooted foot contact and actual effect sockets remain engine checks.
"""
from pathlib import Path
import os, subprocess, sys, tempfile

root = Path(__file__).resolve().parents[1]
model = (root / 'Assets/Scripts/Combat/CombatModel.cs').read_text()

def member(source, signature):
    start = source.index(signature)
    cursor = source.index('{', start) + 1
    depth = 1
    while depth:
        depth += (source[cursor] == '{') - (source[cursor] == '}')
        cursor += 1
    return source[start:cursor]

methods = '\n'.join(member(model, name) for name in [
    'public bool SwordActionActive', 'public bool TryClaimSwordRibbon(',
    'public void PlayAction(', 'public void CancelAction(', 'public void ReleaseCharge(',
    'private void CommitActionPose(', 'private static Quaternion Pose(',
    'private void AnimateHero(', 'public void AnimateCharge(float progress,int skill)'])
fixture = (root / 'Tests/HeroPoseCommitFixture.cs').read_text()
fixture = fixture.replace('public class LineRenderer {public void SetPosition(int i,Vector3 p){}}',
    'public class LineRenderer {public Vector3 nock;public void SetPosition(int i,Vector3 p){if(i==1)nock=p;}}')
fixture = fixture.replace('public enum WeaponVisualAnchor {BowGrip,BowNock}', 'public enum WeaponVisualAnchor {BowGrip,BowNock,SwordPommel}')
fixture = fixture.replace('public enum Space {Self}', 'public enum PrimitiveType {Cube}\n public enum Space {Self}')
fixture = fixture.replace('public const float PI=', 'public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public const float PI=')
fixture = fixture.replace('public CombatModel(HeroClass hero){heroClass=hero;}', r'''
  private Transform leftFoot=new Transform(),rightFoot=new Transform();
  private Transform Part(string name,PrimitiveType shape,Vector3 at,Vector3 scale,Color color,Transform parent=null,VisualSurface? surface=null,string meshModule=null)=>new Transform{parent=parent,localPosition=at,localScale=scale};
  public CombatModel(HeroClass hero){heroClass=hero;}
  public Quaternion[] Bones()=>new[]{spine.localRotation,headRig.localRotation,leftArm.localRotation,rightArm.localRotation,leftElbow.localRotation,rightElbow.localRotation,leftLeg.localRotation,rightLeg.localRotation,leftKnee.localRotation,rightKnee.localRotation,swordRig.localRotation,staffRig.localRotation,bowRig.localRotation};
  public Vector3 Hip=>pelvis.localPosition;public Vector3 ModelRoot=>transform.localPosition;public float Nock=>bowstring.nock.z;public bool Arrow=>arrowRig.gameObject.active;public float BeatAge=>performanceBeatAge;
  public float PerformancePhase(){float t=1;bool acting=false;SampleSkillPerformance(ref t,ref acting,0);return t;}
''')
driver = r'''
using System;using Emberfall;using UnityEngine;
static class ReleaseTests {
 static int checks;
 static void Check(bool valid,string message){checks++;if(!valid)throw new Exception(message);}
 static int Difference(Quaternion[] a,Quaternion[] b){int n=0;for(int i=0;i<a.Length;i++)if(Quaternion.Difference(a[i],b[i])>.0001f)n++;return n;}
 static void Main(){
  foreach(HeroClass hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner}) {
   Check(CombatModel.SkillReleaseGesture(hero,3)==CombatModel.ReleaseGesture.None,"attribute passive cannot invent a cast gesture");
   for(int slot=0;slot<10;slot++)if(slot!=3){
    Time.frameCount++;var m=new CombatModel(hero);m.AnimateCharge(0,slot);var idle=m.Bones();
    m.AnimateCharge(.58f,slot);var prepared=m.Bones();var hip=m.Hip;
    Check(Difference(idle,prepared)>=4,"preparation moves limbs and torso, not only a model sway");
    for(int sample=0;sample<12;sample++){m.AnimateCharge(.58f,slot);Check(Difference(prepared,m.Bones())==0&&Math.Abs(m.Hip.y-hip.y)<.00001f,"repeated pending pose sampling cannot accumulate a crouch or rotation");}
    if(hero==HeroClass.Ranger&&slot!=1&&slot!=5&&slot!=8){Check(m.Nock<-.10f&&m.Arrow,"drawn arrow stays attached to the tense string before release");m.AnimateCharge(1,slot);Check(m.Nock<-.24f&&m.Arrow,"completed pending charge keeps a fully drawn arrow until the paid release");}
    m.PlayAction(slot,false);var contact=m.Bones();float age=m.Age;
    Check(Difference(prepared,contact)>=3,"actual release commits a distinct force pose immediately");
    Check(m.Hip.y<.83f,"release loads the character's lower body");
    if(hero==HeroClass.Ranger&&slot!=1&&slot!=5&&slot!=8)Check(Math.Abs(m.Nock-.05f)<.00001f&&!m.Arrow,"actual release relaxes string and removes nocked arrow in the same frame");
    if(hero==HeroClass.Ranger&&(slot==1||slot==5||slot==8))Check(!m.Arrow&&Math.Abs(m.Nock-.05f)<.00001f,"trap, poison placement and defense do not pretend to fire a bow");
    for(int sample=0;sample<5;sample++){m.PoseAgain();Check(Difference(contact,m.Bones())==0&&m.Age==age,"same-frame release resampling preserves skeleton and clocks");}
    Time.frameCount++;m.Frame(.27f);Check(Difference(contact,m.Bones())>=3,"follow-through returns several joints toward the ready stance");
    Check(Math.Abs(m.ModelRoot.x)<.0001f&&Math.Abs(m.ModelRoot.z)<.0001f,"gesture does not move the collision actor or invent root motion");
   }
  }
  foreach(bool launchNow in new[]{false,true}){
   Time.frameCount++;var m=new CombatModel(HeroClass.Ranger);m.PlayAction(9,false);m.BeginSkillPerformance(9,4,.42f,91);m.SkillPerformanceBeat(9,91);
   m.PrepareSkillPerformanceBeat(9,91,.42f,launchNow);Time.frameCount++;m.Frame(.16f);float age=m.BeatAge;
   m.SkillPerformanceBeat(9,90);m.PrepareSkillPerformanceBeat(9,90,.02f,true);Check(Math.Abs(m.BeatAge-age)<.00001f,"an old cast cannot restart the emitter or prepare an unrelated action");
   if(launchNow){m.SkillPerformanceBeat(9,91);Check(Math.Abs(m.BeatAge-age)<.00001f,"flying-body arrival cannot manufacture another draw/release");}
   else {Time.frameCount++;m.Frame(.24f);Check(m.PerformancePhase()<.52f,"future ground contact first receives a genuine anticipation");m.SkillPerformanceBeat(9,91);Check(Math.Abs(m.PerformancePhase()-.52f)<.00001f,"authoritative contact immediately commits its force pose");}
  }
  Time.frameCount++;var execution=new CombatModel(HeroClass.Vanguard);execution.AnimateCharge(1,9);execution.ReleaseCharge(9);
  execution.BeginSkillPerformance(9,4,.48f,105);execution.PrepareSkillPerformanceBeat(9,105,.15f);
  Time.frameCount++;execution.Frame(.075f);
  Check(Math.Abs(execution.Age/execution.Duration-.41f)<.0001f,"charged execution keeps the real downward strike before its delayed contact");
  execution.SkillPerformanceBeat(9,105);Check(Math.Abs(execution.PerformancePhase()-.52f)<.00001f,"execution contact synchronously submits its full force pose");
  Time.frameCount++;var instant=new CombatModel(HeroClass.Vanguard);instant.PlayAction(9,false);instant.BeginSkillPerformance(9,4,.48f,106);
  instant.BeginDelayedSkillRelease(9,.15f);instant.PrepareSkillPerformanceBeat(9,106,.15f);var loaded=instant.Bones();
  Check(Math.Abs(instant.Age/instant.Duration-.30f)<.0001f,"tap execution visibly loads before its existing delayed contact");
  Time.frameCount++;instant.Frame(.075f);Check(Math.Abs(instant.Age/instant.Duration-.41f)<.0001f&&Difference(loaded,instant.Bones())>=3,"tap execution advances multiple joints through the actual .15 second strike");
  instant.SkillPerformanceBeat(9,106);Check(Math.Abs(instant.PerformancePhase()-.52f)<.00001f,"tap contact submits the same decisive force pose");
  Console.WriteLine("PASS "+checks+" actual skill body preparation/release/recovery checks; managed, not Unity rendering.");
 }
}
'''
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='emberfall-skill-body-poses-') as directory:
    work=Path(directory)
    for name in ['HeroMotionStyle','GameTypes','CombatBalance','SkillDamageBudgets','BasicActionTimeline','VisualMotionEnvelope','CasterPoseRecipe']:
        (work/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
    for name in ['CombatModel.Recovery','CombatModel.CastPoses','CombatModel.SkillPerformance','CombatModel.SkillReleasePose']:
        (work/(name+'.cs')).write_text((root/'Assets/Scripts/Combat'/(name+'.cs')).read_text())
    (work/'Fixture.cs').write_text(fixture+'\nnamespace Emberfall{public enum VisualSurface{Metal}}')
    production=work/'Production.cs'
    original='using UnityEngine;namespace Emberfall{public sealed partial class CombatModel{'+methods+'}}'
    production.write_text(original);(work/'Program.cs').write_text(driver)
    project=work/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0169</NoWarn></PropertyGroup></Project>')
    config=work/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=dict(os.environ,DOTNET_CLI_HOME=str(work/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config),'-v:q'],env=env,check=True)
    command=[dotnet,'run','--project',str(project),'--no-restore','-c','Release']
    subprocess.run(command,env=env,check=True)
    for name,file,before,after,expected in [
        ('body-hook',production,'if(acting&&!sampleBasic&&!pilotCharging)ApplySkillReleasePose(t,poseSkill);','','release loads the character'),
        ('string-release',work/'CombatModel.SkillReleasePose.cs','Vector3 nock=new Vector3(0,0,.05f-draw*.30f);','Vector3 nock=new Vector3(0,0,-.25f);','actual release relaxes string'),
        ('stale-cast',work/'CombatModel.SkillPerformance.cs','performanceCast!=castId','false','an old cast cannot restart the emitter'),
    ]:
        current=file.read_text();file.write_text(current.replace(before,after))
        result=subprocess.run(command,env=env,capture_output=True,text=True)
        if result.returncode==0 or expected not in result.stdout+result.stderr:raise AssertionError(name+' mutation was not rejected\n'+result.stdout+result.stderr)
        file.write_text(current)
        print('PASS: '+name+' production mutation rejected',flush=True)
