#!/usr/bin/env python3
"""Execute real camp recovery, teleport, enemy cancellation and cast ownership methods.

Unity objects, persistence admission, scene construction and camera/physics are explicit
host boundaries. SkillRuntime and all changed recovery methods execute unchanged.
DeathLootPersistenceProductionTests separately exercises actual durable file writes.
"""
from pathlib import Path
import os
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def member(path, marker):
    source = (ROOT / path).read_text()
    start = source.index(marker)
    end = source.index('{', start) + 1
    depth = 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


session = 'Assets/Scripts/Core/GameSession.cs'
player = 'Assets/Scripts/Combat/PlayerController.cs'
enemy = 'Assets/Scripts/Combat/EnemyController.cs'
hubs = 'Assets/Scripts/Core/GameSession.Hubs.cs'
production = 'using System.Collections.Generic;using UnityEngine;namespace Emberfall{'
production += 'public partial class GameSession{' + ''.join(member(session, marker) for marker in [
    'private bool ChangeZone(bool dungeon)', 'private bool RecoverInCurrentCamp()',
    'public void Respawn()', 'public void ReturnToCamp()', 'private void SuspendInputs()',
]) + ''.join(member(hubs, marker) for marker in [
    'public int CurrentHub', 'private bool CanTravelNow()', 'public bool TravelToHub(int hub)',
]) + '}'
production += 'public partial class PlayerController{' + ''.join(member(player, marker) for marker in [
    'public void RefreshStats(bool heal)', 'public void Teleport(Vector3 position)', 'internal void CancelCombatPose()',
]) + '}'
production += 'public partial class EnemyController{' + ''.join(member(enemy, marker) for marker in [
    'internal void RetireAttackForCampRecovery()', 'private void CancelAttack(bool interrupted = false)', 'private void ClearWarning()',
]) + '}'
production += 'public partial class CombatArea{' + member('Assets/Scripts/Combat/CombatEffects.cs', 'private bool IsCurrentCast') + '}'
production += 'public static class SkillStockRules{' + ''.join(member('Assets/Scripts/Core/GameTypes.cs', marker) for marker in [
    'public static int Skill(HeroClass hero)', 'public static float Seconds(HeroClass hero)',
]) + '}}'

dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('DOTNET', 'dotnet')
with tempfile.TemporaryDirectory(prefix='emberfall-camp-recovery-') as directory:
    work = Path(directory)
    adapter = work / 'ProductionMethods.cs'
    adapter.write_text(production)
    (work / 'Boundary.cs').write_bytes((ROOT / 'Tests/CampRecoveryBoundary.cs').read_bytes())
    for name in ['SkillRuntime', 'ScenePreparationState', 'HubTravelRules']:
        (work / (name + '.cs')).write_bytes((ROOT / 'Assets/Scripts/Core' / (name + '.cs')).read_bytes())
    project = work / 'Tests.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>')
    config = work / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / 'cli'), DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')
    restore = subprocess.run([dotnet, 'restore', str(project), '--configfile', str(config), '-v:q'], env=env, capture_output=True, text=True)
    assert restore.returncode == 0, restore.stdout + restore.stderr
    build = [dotnet, 'build', str(project), '--no-restore', '-v:q']
    run = [dotnet, str(work / 'bin/Debug/net8.0/Tests.dll')]
    mutations = [
        ('saved-state-gate', 'if (!SaveBeforeLeaving()) return false;', '// regression: recovery proceeds after refused save', 'failed save leaves death and recovery state intact'),
        ('foreign-hub-reuse', '!dungeon && !InDungeon && !loadingSaveSnapshot && world != null', '!dungeon && !InDungeon && world != null', 'committed hub travel prepares destination instead of reusing old hub'),
        ('stale-cast-owner', 'CombatEpoch++;relicGems.Reset();', 'relicGems.Reset();', 'old epoch damaging field cannot survive camp revival'),
        ('unfinished-hostile-attack', 'CancelAttack();\n            aggro = false;', '// regression: existing hostile windup continues\n            aggro = false;', 'living enemy attack and warning are retired without replacement'),
    ]
    for mutation in [None] + mutations:
        text = production
        if mutation:
            name, before, after, expected = mutation
            assert text.count(before) == 1, (name, text.count(before))
            text = text.replace(before, after, 1)
        adapter.write_text(text)
        built = subprocess.run(build, env=env, capture_output=True, text=True)
        assert built.returncode == 0, built.stdout + built.stderr
        result = subprocess.run(run, env=env, capture_output=True, text=True)
        if mutation:
            assert result.returncode != 0 and expected in result.stdout + result.stderr, (name, result.stdout, result.stderr)
            print('PASS: ' + name + ' production mutation rejected', flush=True)
        else:
            assert result.returncode == 0, result.stdout + result.stderr
            print(result.stdout, end='', flush=True)
