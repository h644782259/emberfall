#!/usr/bin/env python3
"""Compile production pose policy, then prove contact/clock regression controls fail."""
import importlib.util,os,subprocess,tempfile,json
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
spec=importlib.util.spec_from_file_location('cloud',ROOT/'Tools/cloud-validation.py'); cloud=importlib.util.module_from_spec(spec);spec.loader.exec_module(cloud)
dotnet=os.environ.get('DOTNET','/workspace/scratch/dotnet/dotnet'); results=[]
with tempfile.TemporaryDirectory(prefix='BlenderPilotTest-') as t:
 w=Path(t);(w/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 source=ROOT/'Assets/Scripts/Core/BlenderPilotPosePolicy.cs'; timeline=ROOT/'Assets/Scripts/Core/BasicActionTimeline.cs'
 if not timeline.exists(): timeline=next((ROOT/'Assets/Scripts').rglob('BasicActionTimeline.cs'))
 for name,mutation in [('production',None),('zero-contact',('Clamp(actionProgress)','0')),('wall-clock-action',('Clamp(actionProgress)','Repeat(time)'))]:
  src=source
  if mutation:
   src=w/(name+'.cs');src.write_text(source.read_text().replace(*mutation))
  project=cloud.write_project(w/name,[src,timeline,ROOT/'Tests/BlenderPilotPoseTests.cs'],program='using System; class Program { static void Main(){Console.WriteLine(BlenderPilotPoseTests.Run());} }')
  run=subprocess.run([dotnet,'run','--project',str(project),'--configfile',str(w/'NuGet.Config')],capture_output=True,text=True)
  okay=run.returncode==0 if not mutation else run.returncode!=0 and ('no hidden windup' in run.stderr or 'action sampling' in run.stderr)
  results.append({'name':name,'passed':okay,'exit':run.returncode,'output':(run.stdout+run.stderr)[-2000:]})
print(json.dumps(results,indent=2));assert all(r['passed'] for r in results)
