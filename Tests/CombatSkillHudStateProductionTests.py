#!/usr/bin/env python3
"""Execute actual HUD visibility, slot geometry, touch capture and material draws.

Unity rendering, texture import, simulator and native touch delivery are outside
this recording-GUI fixture. Existing progression and cast budgets are inputs.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', default='dotnet')
parser.add_argument('--output', type=Path)
args = parser.parse_args()
members = {
    'GameUI.cs': ['private void RefreshLayout()', 'private static int SkillAtSlot(',
        'private static int LearnedSkillAtSlot(', 'private static bool CombatSkillLearned(',
        'private static bool CombatSlotVisible(', 'private void DrawHotbar()',
        'private void HandleHotbarPointer(', 'private static void DrawIcon('],
    'GameUI.Mobile.cs': ['private float TouchRatio', 'private Rect TouchRect(MobileControlLayout.Area',
        'private Rect TouchRect(float ', 'private int TouchFont(', 'public bool MobileSkillVisible(',
        'private bool BeginMobileCast(', 'private void ContinueMobileCast(',
        'private bool OtherMobilePageReady()', 'private void DrawMobileHotbar()'],
    'GameUI.ControlPreferences.cs': ['private int BoundMobileSkill('],
}
paths = [root/'Assets/Scripts/UI'/name for name in members]
paths += [root/'Assets/Scripts/UI/GameUI.SkillIcons.cs',
    root/'Assets/Scripts/UI/AuthoredIconArt.cs', root/'Assets/Scripts/UI/MobileControlLayout.cs',
    root/'Assets/Scripts/Combat/MobileSkillPolicy.cs', Path(__file__),
    root/'Tests/CombatSkillHudStateTests.cs']
hashes = {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
report = {'scope': __doc__, 'sourceSha256': hashes, 'project': str(root), 'checks': []}

def member(filename, signature):
    source = (root/'Assets/Scripts/UI'/filename).read_text()
    start = source.index(signature)
    cursor = source.index('{', start)+1
    depth = 1
    while depth:
        depth += (source[cursor] == '{')-(source[cursor] == '}')
        cursor += 1
    # Expression-bodied members terminate at the semicolon instead.
    semicolon = source.find(';', start, source.index('{', start))
    return source[start:semicolon+1] if semicolon >= 0 else source[start:cursor]

with tempfile.TemporaryDirectory(prefix='emberfall-skill-hud-state-') as temporary:
    scratch = Path(temporary)
    methods = 'using UnityEngine;namespace Emberfall{public partial class GameUI{' + ''.join(
        member(name, signature) for name, signatures in members.items() for signature in signatures) + '}}'
    (scratch/'Methods.cs').write_text(methods)
    for relative in ['UI/GameUI.SkillIcons.cs', 'UI/MobileControlLayout.cs', 'Combat/MobileSkillPolicy.cs']:
        (scratch/Path(relative).name).write_bytes((root/'Assets/Scripts'/relative).read_bytes())
    (scratch/'AuthoredTint.cs').write_text('using UnityEngine;namespace Emberfall{public static partial class AuthoredIconArt{' + member('AuthoredIconArt.cs', 'public static Color DisplayTint(') + '}}')
    (scratch/'Fixture.cs').write_bytes((root/'Tests/CombatSkillHudStateTests.cs').read_bytes())
    config = scratch/'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    project = scratch/'Check.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(scratch/'cli'), DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')
    def run(name, oracle=None):
        built = subprocess.run([args.dotnet, 'build', str(project), '--configfile', str(config), '-v:q'], env=env, capture_output=True, text=True)
        if built.returncode:
            print(built.stdout+built.stderr)
        built.check_returncode()
        ran = subprocess.run([args.dotnet, str(scratch/'bin/Debug/net8.0/Check.dll')], env=env, capture_output=True, text=True)
        output = ran.stdout+ran.stderr
        passed = ran.returncode == 0 if oracle is None else ran.returncode != 0 and oracle in output
        report['checks'].append({'name': name, 'passed': passed, 'expectedFailure': oracle, 'output': output})
        if not passed:
            print(output)
            raise AssertionError(name)
        print(output.strip() if oracle is None else 'PASS compiled negative: '+name+' -> '+oracle)
    run('production-hud-state-and-touch')
    for filename, before, after, oracle in [
        ('Methods.cs', 'i<MobileSkillPolicy.ButtonCount&&CombatSkillLearned(session.Progression.Profile,BoundMobileSkill(i,mobileSkillPage))?', 'i<MobileSkillPolicy.ButtonCount?', 'unlearned skill has no registered hitbox'),
        ('Methods.cs', 'if(!CombatSkillLearned(p,skill))continue;', 'if(skill<0)continue;', 'only learned skills draw and reserve HUD rectangles'),
        ('Methods.cs', 'if(session==null||!CombatSkillLearned(session.Progression.Profile,skill))return false;', 'if(session==null)return false;', 'unlearned skill is invisible to opportunity input'),
        ('Methods.cs', 'if(!CombatSkillLearned(session.Progression.Profile,skill))return false;', 'if(skill<0)return false;', 'stale hidden hitbox never captures a skill touch'),
        ('Methods.cs', '&&CombatSkillLearned(session.Progression.Profile,mobileTap.Skill)', '', 'becoming unlearned before release cancels without casting'),
        ('GameUI.SkillIcons.cs', 'ready?1f:.35f', 'ready?1f:.73f', 'temporarily unavailable material is dark and neutral'),
        ('GameUI.SkillIcons.cs', ',icon,.5f)', ',icon,.64f)', 'cooldown recovery retains a restrained colored intensity'),
    ]:
        path = scratch/filename
        original = path.read_text()
        assert original.count(before) == 1, before
        path.write_text(original.replace(before, after))
        run(filename+'-'+oracle, oracle)
        path.write_text(original)
report['sourceChangedDuringRun'] = [str(p.relative_to(root)) for p in paths if hashlib.sha256(p.read_bytes()).hexdigest() != hashes[str(p.relative_to(root))]]
report['passed'] = all(c['passed'] for c in report['checks']) and not report['sourceChangedDuringRun']
if args.output:
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output/'report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n')
assert report['passed'], report['sourceChangedDuringRun']
