#!/usr/bin/env python3
"""Actual three-channel builder and detached shutdown with Unity hierarchy/API shells."""
import os,sys,tempfile,subprocess
from pathlib import Path
r=Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='boss-shutdown-') as d:
 p=Path(d)
 for f in ['Assets/Scripts/Combat/LargeBossRig.Shutdown.cs','Assets/Scripts/Combat/LargeBossShutdownVisual.cs','Assets/Scripts/Core/LargeBossPhaseState.cs']:(p/Path(f).name).write_text((r/f).read_text())
 (p/'Fixture.cs').write_text((r/'Tests/LargeBossShutdownFixture.cs').read_text());(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0169</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet');subprocess.run([dotnet,'restore',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--no-restore'],env=env,check=True)
 visual=p/'LargeBossShutdownVisual.cs';visual.write_text(visual.read_text().replace('age+=Time.unscaledDeltaTime','age+=Time.deltaTime'))
 negative=subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--no-restore'],env=env,text=True,capture_output=True)
 if negative.returncode==0 or 'real chassis/core/petals dismantle separately' not in negative.stdout+negative.stderr:raise AssertionError(negative.stdout+negative.stderr)
 print('PASS scaled-clock mutation fails actual zero-timescale settlement assertion')
session=(r/'Assets/Scripts/Core/GameSession.cs').read_text()
# Check ordering in the ordinary reward-bearing branch. Practice has a separate
# no-reward BeginDeath early return; it must not become the visual-entry anchor.
a=session.index('public void OnEnemyKilled(');b=session.index('{',a)+1;depth=1
while depth:depth+=(session[b]=='{')-(session[b]=='}');b+=1
kill=session[a:b]
ordinary=kill.index('if (enemy == null || !AdventureResultPolicy.AcceptsKill')
assert 'if(PracticeActive)' in kill[:ordinary] and 'return;' in kill[:ordinary]
kill=kill[ordinary:]
assert kill.index('Enemies.Remove(enemy)')<kill.index('Progression.GrantEnemyKillReward')<kill.index('enemy.BeginDeath()')
enemy=(r/'Assets/Scripts/Combat/EnemyController.cs').read_text();assert 'if(model!=null&&model.TryBeginLargeBossShutdown()){gameObject.SetActive(false);Destroy(gameObject);return;}' in enemy
visual=(r/'Assets/Scripts/Combat/LargeBossShutdownVisual.cs').read_text();assert 'FilledSkillVfx.SkipFinales(game)' in visual;assert 'GrantEnemyKillReward' not in visual and 'TakeDamage(' not in visual
print('PASS reward/exactly-once guard remains before visual entry; large host immediately disables; shutdown has no reward/damage API')
