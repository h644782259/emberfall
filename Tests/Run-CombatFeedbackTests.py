#!/usr/bin/env python3
"""Standalone feedback state/geometry checks; no Unity process is launched."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', default='dotnet')
args = parser.parse_args()
spec = importlib.util.spec_from_file_location('cloud_validation', ROOT / 'Tools/cloud-validation.py')
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
checks = [('MobileCombatFeedbackTests', ['Assets/Scripts/UI/MobileCombatPresentation.cs']),
          ('CombatTextLayoutTests', ['Assets/Scripts/Combat/CombatTextLayout.cs']),
          ('CombatOpportunityTests', ['Assets/Scripts/UI/CombatOpportunityPresentation.cs']),
          ('MobileControlLayoutTests', ['Assets/Scripts/UI/MobileControlLayout.cs', 'Assets/Scripts/Combat/MobileSkillPolicy.cs']),
          ('CameraVisibilityTests', ['Assets/Scripts/Core/CameraVisibilityRules.cs'])]
report = {'scope': 'Pure production state/geometry; no Unity execution or rendered validation', 'checks': []}
with tempfile.TemporaryDirectory(prefix='EmberfallFeedback-') as temporary:
    scratch = Path(temporary)
    config = scratch / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env = os.environ.copy()
    env.update(DOTNET_CLI_HOME=str(scratch / 'cli'), DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')
    for name, dependencies in checks:
        sources = [ROOT / path for path in dependencies] + [ROOT / 'Tests' / (name + '.cs')]
        project = helper.write_project(scratch / name, sources,
            program='using System; class Program {static void Main() { Console.WriteLine(' + name + '.Run());}}')
        result = subprocess.run([args.dotnet, 'run', '--project', str(project), '--configfile', str(config)],
                                env=env, capture_output=True, text=True)
        report['checks'].append({'name': name, 'passed': result.returncode == 0, 'output': result.stdout + result.stderr,
            'sourceSha256': {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in sources}})
        print(name, result.stdout, result.stderr)
output = ROOT / 'Tests/TestResults/CombatFeedback-Latest'
output.mkdir(parents=True, exist_ok=True)
(output / 'report.json').write_text(json.dumps(report, indent=2))
raise SystemExit(any(not check['passed'] for check in report['checks']))
