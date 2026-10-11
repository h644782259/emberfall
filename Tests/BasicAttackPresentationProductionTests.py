#!/usr/bin/env python3
"""Execute real basic attack release/factories and finite sword-body ownership.

Socket pose, terrain/cover and rendering calls are managed boundaries. Authored
mesh bytes, attack entry, launch checks, scales, flight and lease code execute.
This is behavior evidence, not Unity/GPU or simulator acceptance.
"""
from pathlib import Path
import os
import subprocess
import sys
import tempfile
from CastReceiptFixtureSources import include_cast_receipt_source

root = Path(__file__).resolve().parents[1]
ns = {'__file__': str(root / 'Tests/AuthoredProjectileProductionTests.py')}
exec((root / 'Tests/AuthoredProjectileProductionTests.py').read_text().split('\nwith tempfile.TemporaryDirectory', 1)[0], ns)
member = ns['member']
shell = ns['s']
catalog = (root / 'Assets/Scripts/Core/GameTypes.cs').read_text()
shell += 'namespace Emberfall{' + member(catalog, 'public enum EnemyKind') + '}'
shell = shell.replace('public static class BuildCatalog{', 'public static class BuildCatalog{' + member(catalog, 'public static float CinderTrailTickMultiplier('))
for declaration in ['public sealed class PlayerController:', 'public sealed class GameSession', 'public static class CombatFx']:
    shell = shell.replace(member(shell, declaration), '')
shell = shell.replace('public void SetFloat(string n,float v){}', 'public readonly Dictionary<string,float> Values=new Dictionary<string,float>();public void SetFloat(string n,float v){Values[n]=v;}')
shell = shell.replace('public static class WorldTraversal {public static int Revision;public static float SurfaceHeight(Vector3 at,float radius=.45f)=>0;}', 'public static class WorldTraversal {public static int Revision;public static float SurfaceHeight(Vector3 at,float radius=.45f)=>at.x>50?6f:0;}')
shell = shell.replace('public T GetComponentInChildren<T>()where T:Component=>gameObject.GetComponent<T>();', 'public T GetComponentInChildren<T>()where T:Component=>gameObject.GetComponent<T>();public T[] GetComponentsInChildren<T>(bool include)where T:Component=>GameObject.All.Where(o=>o==gameObject||o.transform.parent==transform).Select(o=>o.GetComponent<T>()).Where(c=>c!=null).ToArray();')
shell = shell.replace('public Vector3 normalized=>this*(1/magnitude);', 'public Vector3 normalized=>magnitude>0?this*(1/magnitude):zero;')
player_source = (root / 'Assets/Scripts/Combat/PlayerController.cs').read_text()
effect_source = (root / 'Assets/Scripts/Combat/FilledSkillVfx.cs').read_text()
methods = 'using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{' + ''.join(member(player_source, marker) for marker in ['private void BasicAttack(', 'private Vector3 BasicAttackMuzzle(']) + '}}'
filled_methods = '\n'.join(member(effect_source, marker) for marker in ['private static FilledSkillVfx Create(', 'public static void Crescent(', 'private void Update()', 'internal void Retire()', 'private void OnDisable()', 'private void OnEnable()', 'private void Register()', 'private void Release()', 'private static bool Finite('])
fx_source = ns['source']
combat_fx = 'using UnityEngine;namespace Emberfall{public static partial class CombatFx{' + ''.join(member(fx_source, marker) for marker in ['public static void WeaponSlash(', 'public static void Slash(']) + '}}'

with tempfile.TemporaryDirectory(prefix='emberfall-basic-presentation-') as directory:
    work = Path(directory)
    (work / 'Shell.cs').write_text(shell)
    (work / 'Projectile.cs').write_text(ns['projectile'])
    (work / 'Player.cs').write_text(methods)
    (work / 'CombatFx.cs').write_text(combat_fx)
    fixture = (root / 'Tests/BasicAttackPresentationProductionTests.cs').read_text().replace('FILLED_METHODS', filled_methods)
    (work / 'Test.cs').write_text(fixture)
    for name in ['Core/CombatImpactBatch', 'Core/SkillDamageBudgets', 'Core/CombatBalance', 'Core/CombatVisualBudget', 'Core/FilledVfxRecipes', 'Combat/ConcentratedVenomRules', 'Combat/AuthoredProjectileMeshes', 'Combat/AuthoredActorMeshes', 'Combat/AuthoredSpellBases', 'Combat/VisualMeshRecipes', 'Combat/CombatVisualLease', 'Combat/AnchoredImpactMesh']:
        (work / (Path(name).name + '.cs')).write_text((root / ('Assets/Scripts/' + name + '.cs')).read_text())
    project = work / 'Tests.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0169;0649;0414</NoWarn></PropertyGroup></Project>')
    include_cast_receipt_source(project)
    config = work / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('DOTNET', 'dotnet')
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / 'cli'), DOTNET_NOLOGO='1')
    subprocess.run([dotnet, 'restore', str(project), '--configfile', str(config)], env=env, check=True, stdout=subprocess.DEVNULL)
    command = [dotnet, 'run', '--project', str(project), '--no-restore', '-c', 'Release', '--', str(root / 'Assets/Resources')]
    subprocess.run(command, env=env, check=True)
    controls = [
        ('socket-origin', 'Player.cs', 'return release;', 'return transform.position+Vector3.up*1.15f;', 'release begins at the posed weapon socket'),
        ('elevated-empty-shot', 'Player.cs', 'new Vector3(aimPoint.x,muzzle.y,aimPoint.z)', 'aimPoint+Vector3.up*1.15f', 'empty shot keeps the elevated weapon height'),
        ('cover-launch', 'Projectile.cs', 'if (!CanLaunchFromMuzzle(player,muzzle,amount,attackId))', 'if (false)', 'blocked physical socket cannot launch through cover'),
        ('readable-mage', 'Projectile.cs', 'new Vector3(1.95f,2.8f,1.85f)', 'new Vector3(.95f,1.35f,.95f)', 'mage solid volume remains readable'),
        ('essential-sword-body', 'CombatFx.cs', 'essentialBody:true', 'essentialBody:false', 'basic sword body survives a saturated optional budget'),
        ('finite-sword-body', 'Test.cs', 'if(age>=life)', 'if(false)', 'basic sword body retires after its finite lifetime'),
    ]
    for name, filename, before, after, expected in controls:
        path = work / filename
        original = path.read_text()
        if before not in original:
            raise AssertionError('missing production mutation: ' + name)
        path.write_text(original.replace(before, after, 1))
        rejected = subprocess.run(command, env=env, capture_output=True, text=True)
        path.write_text(original)
        if rejected.returncode == 0 or expected not in rejected.stdout + rejected.stderr:
            raise AssertionError('mutation not rejected: ' + name + '\n' + rejected.stdout + rejected.stderr)
        print('PASS compiled negative: ' + name, flush=True)
