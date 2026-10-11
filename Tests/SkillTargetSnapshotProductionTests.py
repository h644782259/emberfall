#!/usr/bin/env python3
"""Compile live target dispatches + Summoner Cast/Update and compare warm managed allocations.
Unity/VFX/status side effects are explicit boundaries; this does not measure device FPS.
"""
from pathlib import Path
import os,sys,tempfile,subprocess
r=Path(__file__).resolve().parents[1]
def body(s,a):
 b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[s.index('{',a):b]
def member(s,key):
 a=s.index(key);return s[a:s.index('{',a)]+body(s,a)
pc=(r/'Assets/Scripts/Combat/PlayerController.cs').read_text()
companion=(r/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text()
sequence=(r/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text()
def query(s,key):
 a=s.index(key);return s[a:s.index('{',a)]+body(s,a)
methods=''.join(member(pc,k) for k in ['private static bool ValidAimTarget(','internal float ResolveSkillImpact(','internal void ElementalAdvancedArea(','internal void ApplySpellDodgeBoon('])
methods+='internal void RunFrostPulse(){'+query(pc,'using (var pulseTargets =')+'}'
# The real summoner branch includes impulse dispatch BEFORE capturing interrupts.
methods+='internal void RunSummonerDispatch(int slot,int rank,Vector3 target,float range,int castId){float power=1f+(rank-1)*.3f;'+body(pc,pc.index('if (HeroClass == HeroClass.Summoner)',pc.index('private void CastSkillCore(')))[1:]
companion_method='internal void RunTreant(float attackDamage) {'+query(companion,'using (var impactTargets =')+'}'
sequence_methods=''.join(member(sequence,k) for k in ['private void ChainLightning(','private void Pull('])
with tempfile.TemporaryDirectory(prefix='skill-target-snapshot-') as d:
 p=Path(d)
 for f in ['Assets/Scripts/Core/CombatTargetSnapshot.cs','Assets/Scripts/Core/CombatImpactBatch.cs','Assets/Scripts/Core/ScheduledTickWindow.cs','Assets/Scripts/Combat/SummonerDamageRules.cs','Tests/SkillTargetSnapshotFixture.cs','Tests/SkillTargetSnapshotProductionTests.cs']:(p/Path(f).name).write_bytes((r/f).read_bytes())
 (p/'PlayerDispatch.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+methods+'}}')
 (p/'CompanionDispatch.cs').write_text('using UnityEngine;namespace Emberfall{internal sealed partial class SummonedCompanion{'+companion_method+'}}')
 (p/'SequenceDispatch.cs').write_text('using UnityEngine;namespace Emberfall{internal sealed partial class AdvancedSkillSequence{'+sequence_methods+'}}')
 spell=(r/'Assets/Scripts/Combat/SummonerSpell.cs').read_text();key='        private void Update()'
 assert spell.count(key)==1
 spell=spell.replace(key,'        internal void RunForFixture(){Update();}\n        internal void BindForFixture(PlayerController p,GameSession game){owner=p;session=game;rank=1;damage=10;epoch=p.CombatEpoch;castId=7;finisherDamage=26;}\n'+key)
 (p/'SummonerSpell.cs').write_text(spell)
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414;0169</NoWarn></PropertyGroup></Project>')
 cfg=p/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 build=[dotnet,'build',str(project),'--configfile',str(cfg),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 # Each negative is recompiled against the same behavioral/allocation oracle.
 mutations=[
 ('CombatTargetSnapshot.cs','if (!buffers[i].InUse) { buffer = buffers[i]; break; }','if (true) { buffer = buffers[i]; break; }','nested target snapshots'),
 ('SequenceDispatch.cs','using (var struck = CombatTargetSet<EnemyController>.Capture())','using (var struck = new AllocatingSet<EnemyController>())','warm chain managed allocation'),
 ('PlayerDispatch.cs','CombatTargetSnapshot<EnemyController>.Capture(session.Enemies)','AllocatingSnapshot<EnemyController>.Capture(session.Enemies)','warm frost managed allocation')]
 for file,before,after,oracle in mutations:
  f=p/file;good=f.read_text();assert before in good;f.write_text(good.replace(before,after))
  subprocess.run(build+['--no-restore'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(run,env=env,text=True,capture_output=True);f.write_text(good)
  assert result.returncode and oracle in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled skill target negative: '+oracle,flush=True)
 print('PASS: 3 compiled snapshot/chain/allocation negatives rejected',flush=True)
