#!/usr/bin/env python3
"""Compile actual phone geometry and binding-caption methods, with compiled negatives.

No Unity, simulator, GUI or touch process is launched. One viewport reuses safe
dimensions from a runtime snapshot; others are device specifications/synthetic.
Reusing the measured input supplies no rendered-control or touch evidence.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
from MobileBindingFixtureSources import include_mobile_binding_sources

root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet',default='dotnet')
parser.add_argument('--output',type=Path)
args=parser.parse_args()
sources=[root/path for path in ['Assets/Scripts/UI/MobileControlLayout.cs','Assets/Scripts/Combat/MobileSkillPolicy.cs','Assets/Scripts/UI/GameUI.ControlPreferences.cs','Assets/Scripts/UI/GameUI.LootNotices.cs','Tests/MobileBindingFixtureSources.py','Tests/MobileControlLayoutTests.cs','Tests/MobileCombatClusterLayoutTests.cs','Tests/MobileCombatClusterLayoutProductionTests.py']]
hashes={str(path.relative_to(root)):hashlib.sha256(path.read_bytes()).hexdigest() for path in sources}
report={'scope':'Pure actual compact phone arc, binding-caption mapping, loot visibility getters and RenderDpi boundaries; full phone-control regression is a separate required result; no rendering/touch evidence','viewportEvidence':{'screenPixels':[1920,883],'safeAreaPixels':[136,44,1648,839],'dpi':460,'observedLayout':[628.5578,320],'observedScale':2.621875,'nativeResolutionPixels':[2622,1206],'scope':'Pre-render-DPI-correction runtime snapshot supplied by the parent; measured safe dimensions replayed with raw DPI and corrected density; corrected geometry is not a new runtime observation'},'project':str(root),'sourceSha256':hashes,'completed':False,'checks':[]}
def save_report():
    report['sourceChangedDuringRun']=[str(path.relative_to(root)) for path in sources if hashlib.sha256(path.read_bytes()).hexdigest()!=hashes[str(path.relative_to(root))]]
    report['passed']=report['completed'] and all(check['passed'] for check in report['checks']) and not report['sourceChangedDuringRun']
    if args.output:
        args.output.mkdir(parents=True,exist_ok=True)
        (args.output/'report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
with tempfile.TemporaryDirectory(prefix='emberfall-compact-caption-') as temporary:
    scratch=Path(temporary)
    include_mobile_binding_sources(scratch,root)
    loot_source=(root/'Assets/Scripts/UI/GameUI.LootNotices.cs').read_text()
    def loot_member(signature):
        start=loot_source.index(signature);end=loot_source.index('{',start)+1;depth=1
        while depth:
            depth+=(loot_source[end]=='{')-(loot_source[end]=='}');end+=1
        return loot_source[start:end]
    loot_methods='using UnityEngine;namespace Emberfall{public partial class GameUI{'+''.join(loot_member(signature) for signature in ['private bool LootNoticesVisible','private bool MobileLootReplacesInformation','private int VisibleLootNoticeCount'])+'}}'
    loot=scratch/'LootVisibilityMethods.cs';loot.write_text(loot_methods)
    (scratch/'MobileControlLayout.cs').write_bytes((root/'Assets/Scripts/UI/MobileControlLayout.cs').read_bytes())
    (scratch/'MobileControlLayoutTests.cs').write_bytes((root/'Tests/MobileControlLayoutTests.cs').read_bytes())
    (scratch/'Fixture.cs').write_bytes((root/'Tests/MobileCombatClusterLayoutTests.cs').read_bytes())
    config=scratch/'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    project=scratch/'Check.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
    env=dict(os.environ,DOTNET_CLI_HOME=str(scratch/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    build_state={'fingerprint':None,'count':0}
    def run(name,oracle=None,mode=None,allow_failure=False):
        fingerprint=tuple((path.name,hashlib.sha256(path.read_bytes()).hexdigest()) for path in sorted(scratch.glob('*.cs')))
        if fingerprint!=build_state['fingerprint']:
            built=subprocess.run([args.dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,capture_output=True,text=True)
            if built.returncode:
                print(built.stdout+built.stderr)
                built.check_returncode()
            build_state['fingerprint']=fingerprint;build_state['count']+=1
        report['compiledSourceVersions']=build_state['count']
        command=[args.dotnet,str(scratch/'bin/Debug/net8.0/Check.dll')]
        if mode:command.append(mode)
        executed=subprocess.run(command,env=env,capture_output=True,text=True)
        output=executed.stdout+executed.stderr
        passed=executed.returncode==0 if oracle is None else executed.returncode!=0 and oracle in output
        report['checks'].append({'name':name,'scope':'compiled negative' if oracle else 'actual RenderDpi boundary execution' if mode=='render-dpi' else 'actual loot visibility getters' if mode=='loot-visibility' else 'full phone-control regression' if mode else 'binding-caption positive','passed':passed,'returnCode':executed.returncode,'expectedFailure':oracle,'output':output})
        save_report()
        if not passed:
            print(output)
            if allow_failure:
                print('FAIL required separate result: '+name)
                return False
            raise AssertionError(name+' failed to reach its expected oracle')
        print(output.strip() if oracle is None else 'PASS compiled negative: '+name+' -> '+oracle)
    run('compact-phone-positive')
    run('full-phone-control-regression',mode='phone-controls',allow_failure=True)
    run('full-phone-and-ipad-control-regression',mode='all-controls',allow_failure=True)
    run('actual-loot-visibility',mode='loot-visibility')
    run('actual-render-dpi-boundaries',mode='render-dpi')
    binding=scratch/'BindingMethods.cs'
    original=binding.read_text()
    start=original.index('public MobileControlLayout.Area MobileOpportunityArea(')
    body=original.index('{',start)+1
    def with_caption_prefix(code):
        return original[:body]+'\n'+code+'\n'+original[body:]
    for page in [0,1]:
        replacement='if(mobileSkillPage=='+str(page)+')return new MobileControlLayout.Area(MobileControls.Layout.Width-2,0,52,14);'
        binding.write_text(with_caption_prefix(replacement))
        run('page-'+str(page)+'-unsafe-caption','visible hint stays in safe area on both skill pages')
        binding.write_text(original)
    binding.write_text(with_caption_prefix('for(int brokenButton=0;brokenButton<5;brokenButton++)if(BoundMobileSkill(brokenButton,mobileSkillPage)==skill){var brokenKey=MobileControls.Layout.Skills[brokenButton];return new MobileControlLayout.Area(brokenKey.X,brokenKey.Y+5,brokenKey.Width,14);}'))
    run('caption-inside-skill','visible hint never covers a skill, action or HUD target')
    binding.write_text(original)
    layout=scratch/'MobileControlLayout.cs'
    original_layout=layout.read_text()
    anchor='int[] opportunityIdentities='
    assert original_layout.count(anchor)==1
    broken_grid='if(!ipad){Skills[0]=Centered(Width-274,Height-89+shift,diameter);Skills[1]=Centered(Width-210,Height-89+shift,diameter);Skills[2]=Centered(Width-274,Height-159+shift,diameter);Skills[3]=Centered(Width-210,Height-159+shift,diameter);}'
    layout.write_text(original_layout.replace(anchor,broken_grid+'\n            '+anchor))
    run('restored-two-row-grid','four ordinary phone skills follow the restored smooth arc')
    layout.write_text(original_layout)
    dpi_anchor='return nativeDpi*Math.Min(widthRatio,heightRatio);'
    assert original_layout.count(dpi_anchor)==1
    layout.write_text(original_layout.replace(dpi_anchor,'return nativeDpi;'))
    run('unscaled-native-dpi','lower render resolution converts native DPI to the render grid',mode='render-dpi')
    layout.write_text(original_layout)
    pad_status_anchor='vitalWidth,36);'
    assert original_layout.count(pad_status_anchor)==1
    layout.write_text(original_layout.replace(pad_status_anchor,'vitalWidth+(ipad?50:0),36);'))
    run('restored-pad-status-hidden-width','iPad vitals reserve only their two actually drawn rows',mode='all-controls')
    layout.write_text(original_layout)
    notification_anchor='return !MobileControls.Active||!MobileControls.Layout.LootNotice(0).Overlaps(MobileControls.Layout.Notice)||string.IsNullOrEmpty(session.Notification);'
    assert loot_methods.count(notification_anchor)==1
    loot.write_text(loot_methods.replace(notification_anchor,'return true;'))
    run('lost-notification-priority','notification has priority only over a shared mobile pickup slot',mode='loot-visibility')
    loot.write_text(loot_methods)
    empty_queue_anchor='VisibleLootNoticeCount>0'
    assert loot_methods.count(empty_queue_anchor)==1
    loot.write_text(loot_methods.replace(empty_queue_anchor,'true'))
    run('empty-queue-hides-objectives','empty pickup queue never hides objectives and visible shared pickups replace them',mode='loot-visibility')
    loot.write_text(loot_methods)
report['completed']=True
save_report()
assert not report['sourceChangedDuringRun'], 'production sources changed while the geometry fixture ran'
print('PASS source hashes stable; '+str(sum(check['expectedFailure'] is not None for check in report['checks']))+' compiled negative controls')
raise SystemExit(0 if report['passed'] else 1)
