"""Compiled negative controls for an already successful, generated integrated journey host.
Only isolated scratch copies are mutated. A build failure never counts as a caught mutant.
"""
from pathlib import Path
import argparse,hashlib,json,os,shutil,subprocess,uuid


def _member(text, signature):
    start=text.index(signature);end=text.index('{',start)+1;depth=1
    while depth:
        depth+=(text[end]=='{')-(text[end]=='}');end+=1
    return start,end,text[start:end]


def _replace_method(text,signature,old,new):
    a,b,body=_member(text,signature)
    assert body.count(old)==1,(signature,old,body.count(old))
    return text[:a]+body.replace(old,new,1)+text[b:]


def run_negative_controls(project_dir,dotnet,env,output_dir):
    project_dir=Path(project_dir).resolve();output_dir=Path(output_dir).resolve()
    output_dir.mkdir(parents=True,exist_ok=True)
    assert project_dir!=output_dir and project_dir not in output_dir.parents,'Output must not be inside original project'
    projects=list(project_dir.glob('*.csproj'));assert len(projects)==1,projects
    originals={p.name:p.read_bytes() for p in project_dir.iterdir() if p.is_file()}
    hashes={name:hashlib.sha256(data).hexdigest() for name,data in originals.items()}
    controls=[
      ('completion-earned-shares','ProgressionService.Chapter.cs','public int CompletionExperience',
       'TotalExperience-registeredDeathShares','TotalExperience-EarnedKillExperience',
       'objective completion never redistributes skipped enemy shares'),
      ('missing-l1-migration','ProgressionService.cs',None,
       'if (profile.level == 1 && slot == 0) rank = 1;','/* deleted L1 starter migration */',
       'fresh and legacy start with exactly one allocated first skill'),
      ('reforge-free-gold','ProgressionService.Reforge.cs','public bool ReforgeMechanic(ReforgeQuote quote,bool inCamp)',
       'candidate.gold-=quote.GoldCost;','/* omitted reforge debit */',
       'partial reforge charges exact gold once and no fragments'),
      ('respec-forgets-first-ranks','ProgressionService.cs','public bool ResetBuild(bool inCamp)',
       'Math.Min(1, candidate.skillRanks[i])','0',
       'joint respec refunds exactly advanced ranks and retains learned first ranks'),
      ('preset-resets-slot-upgrade','ProgressionService.cs','public bool ApplyBuildPreset(int slot, bool inCamp)',
       'GameProfile candidate = Snapshot();','GameProfile candidate = Snapshot(); candidate.slotUpgradeRanks=new int[3];',
       'old preset references live item rather than rolling back reforge or slot upgrade'),
      ('failed-stage-discards-world','Lifecycle.cs','private bool ContinueAdventure(string slotId, bool discardUnsaved = false, bool alreadySaved = false)',
       '{ SaveLoadError = error; Notify("存档未能读取：" + error); return false; }',
       '{ DiscardTransientAdventureForLoad(); SaveLoadError = error; Notify("存档未能读取：" + error); return false; }',
       'failed load staging preserves built character and paused world'),
    ]
    manifest={'projectDir':str(project_dir),'sourceHashes':hashes,'scope':'Actual generated host methods; exact runtime assertion required after successful compilation. Managed JsonUtility/scene boundaries, isolated real filesystem save roots.','controls':[]}
    manifest_path=output_dir/'manifest.json'
    def publish():manifest_path.write_text(json.dumps(manifest,indent=2)+'\n')
    publish()
    for name,file,signature,old,new,expected in controls:
        case=output_dir/name;case.mkdir(exist_ok=False);work=case/'project';work.mkdir()
        for source,data in originals.items():(work/source).write_bytes(data)
        target=work/file;before=target.read_text()
        if signature:after=_replace_method(before,signature,old,new)
        else:
            assert before.count(old)==1,(name,before.count(old));after=before.replace(old,new,1)
        target.write_text(after)
        record={'name':name,'file':file,'method':signature,'old':old,'new':new,'expectedAssertion':expected,'mutatedSha256':hashlib.sha256(target.read_bytes()).hexdigest(),'passed':False}
        manifest['controls'].append(record);publish()
        try:
            command=[str(dotnet),'build',str(work/projects[0].name),'--configfile',str(work/'NuGet.Config'),'-v:q']
            record['buildCommand']=command
            build=subprocess.run(command,env=env,capture_output=True,text=True)
            (case/'build.stdout.log').write_text(build.stdout);(case/'build.stderr.log').write_text(build.stderr)
            record['buildExitCode']=build.returncode;publish()
            if build.returncode!=0:raise AssertionError('Mutant did not compile: '+name)
            saves=case/('saves-'+uuid.uuid4().hex)
            command=[str(dotnet),str(work/'bin/Debug/net8.0'/ (projects[0].stem+'.dll')),str(saves)]
            record['runCommand']=command
            run=subprocess.run(command,env=env,capture_output=True,text=True)
            (case/'run.stdout.log').write_text(run.stdout);(case/'run.stderr.log').write_text(run.stderr)
            record['runExitCode']=run.returncode
            record['expectedAssertionObserved']=expected in run.stderr and 'System.Exception:' in run.stderr and 'JourneyCheck' in run.stderr
            record['passed']=run.returncode!=0 and record['expectedAssertionObserved'];publish()
            if not record['passed']:raise AssertionError('Mutant missed named runtime assertion: '+name)
            print('PASS: compiled '+name+' fails exact journey assertion: '+expected,flush=True)
        finally:
            target.write_bytes(originals[file])
            record['restoredSha256']=hashlib.sha256(target.read_bytes()).hexdigest()
            record['sourceRestored']=record['restoredSha256']==hashes[file];publish()
            assert record['sourceRestored'],name
    assert all((project_dir/name).read_bytes()==data for name,data in originals.items()),'Original generated host changed'
    manifest['originalHostUnchanged']=True;manifest['passed']=all(r['passed'] and r['sourceRestored'] for r in manifest['controls']);publish()
    return manifest


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--project-dir',type=Path,required=True);parser.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));parser.add_argument('--output-dir',type=Path,required=True);args=parser.parse_args()
    environment=dict(os.environ,DOTNET_CLI_HOME=str(args.output_dir.resolve()/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    result=run_negative_controls(args.project_dir,args.dotnet,environment,args.output_dir)
    print('PASS: '+str(len(result['controls']))+' compiled journey negative controls; original host unchanged')
