#!/usr/bin/env python3
"""All authored actor layers in one real factory/equipment/action/recovery project. Not Unity."""
from pathlib import Path
import sys,os,tempfile,subprocess,re,shutil
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
owned=len(sys.argv)<3
out=Path(sys.argv[2]).resolve() if not owned else Path(tempfile.mkdtemp(prefix='integrated-actor-art-'))
out.mkdir(parents=True,exist_ok=True)
# Reuse only assembly preparation: do not run the isolated F1 exporter or its assertions.
ns={'__file__':str(root/'ArtSource/ActorSilhouettes/F1/export.py')}
sys.argv=['export',str(out),dotnet]
exec((root/'ArtSource/ActorSilhouettes/F1/export.py').read_text().rsplit('\nsubprocess.run(',1)[0],ns)
p=out/'export';extract=ns['extract'];source=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
# The actor assembly executes real skill budgets; include the exact catalog helper
# they now call instead of copying a coefficient into the managed boundary.
(p/'BuildCatalogDamage.cs').write_text('namespace Emberfall{public static class BuildCatalog{'+extract((root/'Assets/Scripts/Core/GameTypes.cs').read_text(),'public static float CinderTrailTickMultiplier(')+'}}')
for path in ['Combat/WeaponModules','Combat/CombatModel.WeaponArt','Combat/VanguardActionLibrary','Combat/CombatModel.VanguardArt','Combat/CombatModel.Recovery','Combat/EnemySilhouetteArt','Combat/CombatModel.Knockdown','Core/SkillDamageBudgets','Core/CombatBalance']:
 (p/(path.split('/')[-1]+'.cs')).write_text((root/'Assets/Scripts'/(path+'.cs')).read_text())
b=p/'OptionalPilotBoundary.cs';s=b.read_text()
for stub in ['private void ConfigureVanguardArt() {}','private void ApplyWeaponFashionArt() {}','private void ApplyWeaponArt(ItemData weapon) {}']:
 assert stub in s;s=s.replace(stub,'')
s=s.replace('private static bool PilotStarterCompatible(ItemData item,ItemSlot slot) { return false; }',extract((root/'Assets/Scripts/Combat/CombatModel.BlenderPilot.cs').read_text(),'private static bool PilotStarterCompatible('));b.write_text(s)
m=p/'Motion.cs';s=m.read_text()
for stub in ['private readonly VisualMotionEnvelope visualMotion=new VisualMotionEnvelope();','private void AdvanceVisualMotion(float dt){throw new Exception("frozen review clock");}','private void ApplyVisualRecovery(float dt){}','private void ApplyAuthoredVanguardPose(bool a,float b,bool c){}']:
 assert stub in s;s=s.replace(stub,'')
s=s.replace('private bool isolatedPreview,pilotAirborne;','private bool isolatedPreview,pilotAirborne,pilotOwnerDead,pilotCharging,dying,slime;private EnemyPosePhase enemyActionPhase;private float enemyActionProgress;')
s=s.replace(extract(s,'public void ReviewPose('),'')
s=s[:-2]+''.join(extract(source,k) for k in ['public void PlayAction(','public void CancelAction(','private void CommitActionPose(','public static CombatModel Enemy(','private void BuildEnemy(','private void EnhanceEnemy(','public void Animate(','public void Recoil(','private void ApplyRecoil('])+'private float recoilStarted=-10,recoilStrength;private Vector3 recoilDirection;'+(root/'Tests/IntegratedActorArtProductionTests.Sequence.cs').read_text()+'}}'
m.write_text('using System.Linq;using System.Collections.Generic;'+s)
f=p/'Fixture.cs';s=f.read_text().replace('public static class Resources {','public static class Resources {public static string Missing,Corrupt;').replace('var path=System.IO.Path.Combine','if(name==Missing)return null;if(name==Corrupt)return new TextAsset{bytes=new byte[16]} as T;var path=System.IO.Path.Combine')
s=s.replace('public new string name=>gameObject.name;','public Vector3 eulerAngles=>new Vector3(0,0,0);public new string name=>gameObject.name;').replace('public static class Mathf{','public static class Mathf{public static int FloorToInt(float f)=>(int)Math.Floor(f);public static float DeltaAngle(float a,float b){float d=Repeat(b-a,360);return d>180?d-360:d;}public static float MoveTowards(float a,float b,float d)=>Math.Abs(b-a)<=d?b:a+Math.Sign(b-a)*d;')
s=s.replace('public class EnemyController:MonoBehaviour{}','public class EnemyController:MonoBehaviour{public bool IsBoss,IsDead;public EnemyKind Kind;public Status StatusEffects;public class Status{public float AirborneHeight;}}')
s=s.replace('public Vector3 position{','public Vector3 InverseTransformDirection(Vector3 v)=>Quaternion.Inverse(rotation)*v;public Vector3 position{');f.write_text(s)
b=p/'Boss.cs';s=b.read_text().replace('private Transform body,core','public void Animate(float speed,float attack){throw new System.Exception("large encounter outside eight-subject scope");}private Transform body,core');b.write_text(s)
t=p/'Types.cs';s=t.read_text();s+='namespace Emberfall {'+re.search(r'public enum EnemyKind\s*\{[^}]*\}',(root/'Assets/Scripts/Core/GameTypes.cs').read_text()).group(0)+'}';t.write_text(s)
(p/'Exporter.cs').write_text((root/'Tests/IntegratedActorArtProductionTests.Program.cs').read_text().replace('OUTPUT_PATH',str(out/'actors.json')))
cmd=[dotnet,'run','--project',str(p/'Export.csproj')];env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
subprocess.run(cmd,env=env,check=True)
for file,before,after,label in [('Motion.cs','ApplyVisualRecovery(dt);','','cancel preserves torso contact'),('Motion.cs','ApplyAuthoredVanguardPose(acting,t,hurt);','','real Vanguard authored contact'),('ActorSilhouetteF1.cs','if(!Enabled)return;','return;','F1 hero visible'),('EnemySilhouetteArt.cs','if(!Enabled||root==null)return;','return;','F2 enemy visible'),('CombatModel.WeaponArt.cs','if(swordRig!=null)','if(false)','F3 starter complete')]:
 target=p/file;original=target.read_text();assert before in original
 target.write_text(original.replace(before,after,1));r=subprocess.run(cmd,env=env,capture_output=True,text=True)
 (out/('negative-'+file.replace('.cs','')+('-authored' if 'Vanguard' in label else '')+'.log')).write_text(r.stdout+r.stderr)
 target.write_text(original)
 assert r.returncode!=0 and label in r.stdout+r.stderr,(file,label,r.stdout,r.stderr)
 print('PASS compiled cross-layer negative:',label,flush=True)
subprocess.run([sys.executable,str(root/'ArtSource/IntegratedActorArt/check_contacts.py'),str(out/'actors.json'),str(out/'contacts.json')],check=True)
if owned:shutil.rmtree(out)
