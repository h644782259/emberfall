"""Execute actual AnimateHero + locomotion + authored layer on production factory.
Fresh-frame visual recovery is inactive (no cancellation). Other-class paths throw.
"""
from pathlib import Path
import sys,subprocess,os
root=Path(__file__).resolve().parents[2];out=Path(sys.argv[1]).resolve();dotnet=sys.argv[2]
import shutil
if (out/'export').exists():shutil.rmtree(out/'export')
subprocess.run([sys.executable,str(root/'ArtSource/VanguardActions/export_rig.py'),str(out),dotnet],check=True)
sys.argv=['x',dotnet];ns={'__file__':str(root/'Tests/EquipmentCompositionProductionTests.py')};exec((root/'Tests/EquipmentCompositionProductionTests.py').read_text().split('with tempfile.TemporaryDirectory')[0],ns);extract=ns['extract'];p=out/'export'
for file in ['Combat/VanguardActionLibrary','Combat/CombatModel.VanguardArt','Core/LocomotionPoseState','Core/HeroMotionStyle','Core/VisualMotionEnvelope','Core/BasicActionTimeline']:
 (p/(file.split('/')[-1]+'.cs')).write_text((root/'Assets/Scripts'/(file+'.cs')).read_text())
# Remove only the named optional pose boundary; this exporter loads the real adapter.
boundary=p/'OptionalPilotBoundary.cs';boundary.write_text(boundary.read_text().replace('private void ConfigureVanguardArt() {}',''))
source=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text();motion=(root/'Assets/Scripts/Combat/CombatModel.Motion.cs').read_text()
body='using System;using UnityEngine;namespace Emberfall {public sealed partial class CombatModel {private bool isolatedPreview,pilotOwnerDead,pilotAirborne,dying;private float previewTime,actionAge,actionDuration,gaitPhase;private int actionStartedFrame;private readonly LocomotionPoseState locomotion=new LocomotionPoseState();private readonly VisualMotionEnvelope visualMotion=new VisualMotionEnvelope();private bool SampleBlenderPilot(bool a,float b,bool c){return false;}private void AdvanceVisualMotion(float dt){throw new Exception("clock must be frozen");}private void ApplyVisualRecovery(float dt){}private void ApplyCasterSkillPose(float t){throw new Exception("not a vanguard");}private void AimArm(Transform a,Transform b,Vector3 c,Vector3 d){throw new Exception("not a vanguard");}'
for sig in ['private static Quaternion Pose(','private void AnimateHero(']:body+=extract(source,sig)
body+=extract(motion,'private void ApplyHeroLocomotion(')
body+='''public void ExportPose(int mode){ ConfigureVanguardArt();Time.time=1;Time.frameCount=1;actionStartedFrame=1;isolatedPreview=true;previewTime=1;bool hurt=mode==11;
if(mode>=1&&mode<=4){float x=mode==3?-1:mode==4?1:0,z=mode==1?1:mode==2?-1:0;for(int i=0;i<6;i++)locomotion.Advance(x*.1f,z*.1f,.05f,2,true,false,0);}
if(mode==5||mode==6)for(int i=0;i<12;i++)visualMotion.Advance(mode==5?-12:12,0,0,.05f);
if(mode==7||mode==8){actionDuration=1;actionAge=.52f;actionBasic=mode==7;actionSkill=0;}
if(mode==9||mode==10){pilotAirborne=mode==9;locomotion.Advance(0,0,.05f,2,false,true,.5f);if(mode==10)locomotion.Advance(0,0,.05f,2,true,false,0);}
AnimateHero(locomotion.Speed,0,hurt,0);
if(mode==12){pilotOwnerDead=true;transform.localRotation=Quaternion.Euler(0,0,75f);SampleVanguardDeath();SampleVanguardDeath();}
}}}'''
(p/'MotionExport.cs').write_text(body)
f=(p/'Fixture.cs').read_text().replace('public static float deltaTime=.016f,time;','public static float deltaTime=.016f,time;public static int frameCount;')
f=f.replace('public Quaternion rotation=>parent==null?localRotation:parent.rotation*localRotation;','public Quaternion rotation{get=>parent==null?localRotation:parent.rotation*localRotation;set=>localRotation=parent==null?value:Quaternion.Inverse(parent.rotation)*value;}public void Rotate(float x,float y,float z,Space s){localRotation=localRotation*Quaternion.Euler(x,y,z);}')
f=f.replace('public struct Vector3 {','public enum Space{Self,World}public struct Vector3 {public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);')
f=f.replace('public struct Quaternion {','public struct Quaternion {public static Quaternion Euler(Vector3 v)=>Euler(v.x,v.y,v.z);public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>new Quaternion{q=System.Numerics.Quaternion.Slerp(a.q,b.q,t)};')
f=f.replace('public static class Mathf{','public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}')
(p/'Fixture.cs').write_text(f)
(p/'Exporter.cs').write_text(r'''using System;using System.Linq;using System.Collections.Generic;using System.Text.Json;using UnityEngine;using Emberfall;
class Exporter {static void Main(){var cases=new List<object>();for(int mode=0;mode<13;mode++){var host=new GameObject("source");var m=CombatModel.Hero(host.transform,HeroClass.Vanguard);m.ExportPose(mode);var all=m.GetComponentsInChildren<Transform>(true);var parts=all.Select((t,i)=>new{id=i,name=t.name,parent=Array.IndexOf(all,t.parent),p=new[]{t.localPosition.x,t.localPosition.y,t.localPosition.z},q=new[]{t.localRotation.q.X,t.localRotation.q.Y,t.localRotation.q.Z,t.localRotation.q.W},s=new[]{t.localScale.x,t.localScale.y,t.localScale.z},vertices=t.GetComponent<MeshFilter>()?.sharedMesh?.vertices?.Select(v=>new[]{v.x,v.y,v.z}).ToArray(),triangles=t.GetComponent<MeshFilter>()?.sharedMesh?.triangles,visible=t.gameObject.activeInHierarchy&&(t.GetComponent<Renderer>()==null||t.GetComponent<Renderer>().enabled)}).ToArray();cases.Add(parts);}System.IO.File.WriteAllText(@"OUTPUT",JsonSerializer.Serialize(cases));}}'''.replace('OUTPUT',str(out/'actions.json')))
subprocess.run([dotnet,'run','--project',str(p/'Export.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli')))
print('PASS executed production Hero, AnimateHero, ApplyHeroLocomotion, ApplyAuthoredVanguardPose and SampleVanguardDeath; managed numerical transforms')

import json,math
poses=json.loads((out/'actions.json').read_text())
assert len(poses)==13
signatures=[]
for index,parts in enumerate(poses):
 assert len(parts)==len(poses[0]),'stable body topology through all actions'
 for part in parts:
  assert all(math.isfinite(v) for key in ['p','q','s'] for v in part[key])
  assert abs(sum(v*v for v in part['q'])-1)<.0002,'unit local quaternion'
 signatures.append(tuple(round(v,6) for part in parts for v in part['q']))
assert len(set(signatures))==13,'each requested action has a distinct actual final pose'
print('PASS 13 distinct actual final poses; fixed body hierarchy, finite TRS and unit rotations')
