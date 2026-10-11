#!/usr/bin/env python3
"""Actual hero pose and isolated sampler, with mandatory world-clock negative control."""
from pathlib import Path
exec((Path(__file__).with_name('HeroPoseCommitTests.py')).read_text().split('with tempfile.TemporaryDirectory')[0])
with tempfile.TemporaryDirectory(prefix='preview-pose-') as directory:
 p=Path(directory)
 for name in ['GameTypes','CombatBalance','SkillDamageBudgets','BasicActionTimeline','VisualMotionEnvelope','CasterPoseRecipe','HeroMotionStyle']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for f in ['Assets/Scripts/Combat/CombatModel.Recovery.cs','Assets/Scripts/Combat/CombatModel.CastPoses.cs','Assets/Scripts/Combat/CombatModel.Preview.cs','Assets/Scripts/UI/CollectionPreviewComposition.cs','Tests/HeroPoseCommitFixture.cs','Tests/CollectionPoseIsolationProductionTests.cs']:(p/Path(f).name).write_text((root/f).read_text())
 body='using UnityEngine;namespace Emberfall {public sealed partial class CombatModel {'+'\n'.join(method(x) for x in ['public bool SwordActionActive','public bool TryClaimSwordRibbon(', 'public void PlayAction(','public void CancelAction(','public void ReleaseCharge(','private void CommitActionPose(','private static Quaternion Pose(','private void AnimateHero('])+'}}'
 production=p/'Model.cs';production.write_text(body);(p/'Program.cs').write_text('System.Console.WriteLine(CollectionPoseIsolationProductionTests.Run());')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0169</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);subprocess.run(cmd,env=env,check=True)
 assert body.count('(isolatedPreview?previewTime:Time.time)')==1;production.write_text(body.replace('(isolatedPreview?previewTime:Time.time)','Time.time'));subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);assert result.returncode and 'System.Exception: preview pose is independent of world clock pause and frames' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: old global-clock breathing compiled and rejected by exact preview isolation assertion')

 # Restore the actual pose source before isolating the formerly reset orbit angle.
 production.write_text(body)
 orbitSource=p/'CombatModel.Preview.cs';normalOrbit=orbitSource.read_text();assert normalOrbit.count('float.IsNaN(mechanicalYaw)?time*16:mechanicalYaw')==1
 orbitSource.write_text(normalOrbit.replace('float.IsNaN(mechanicalYaw)?time*16:mechanicalYaw','time*16'))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);assert result.returncode and 'System.Exception: actual mechanical orbit remains continuous across local-clock wrap' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: 16-degree orbit tied to wrapped time compiled and failed exact visual-transform continuity assertion')

 orbitSource.write_text(normalOrbit.replace('action==CollectionPreviewAction.Attack && heroClass!=HeroClass.Ranger','action==CollectionPreviewAction.Attack').replace('progress < BasicActionTimeline.BowRelease || progress >= BasicActionTimeline.ArrowReload','progress >= BasicActionTimeline.ArrowReload'))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);assert result.returncode and 'System.Exception: preview arrow visible before draw' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: old contact-only preview compiled and rejected by actual arrow visibility assertion')
