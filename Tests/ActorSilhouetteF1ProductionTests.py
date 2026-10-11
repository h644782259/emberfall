#!/usr/bin/env python3
"""Real F1 adapter/decoder/buffers and full factory/equipment/action composition. Managed, NOT Unity."""
from pathlib import Path
import os,sys,tempfile,subprocess,json
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
fixture=(root/'Tests/ActorModulesProductionTests.py').read_text().split("(p/'Test.cs').write_text(r'''",1)[1].split('class Program',1)[0]
with tempfile.TemporaryDirectory(prefix='f1-art-') as temp:
 p=Path(temp).resolve()
 for name in ['AuthoredActorMeshes','ActorSilhouetteF1']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Combat'/(name+'.cs')).read_text())
 (p/'Test.cs').write_text(fixture+r'''
class Program {static void Check(bool ok,string why){if(!ok)throw new Exception(why);}static void Reset()=>typeof(ActorSilhouetteF1).GetMethod("Reset",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
static void Main(string[] args){Resources.Root=args[0];string[] names={"Long robe front panel","Forest Hood","Quiver","Spare Arrow","Spirit antler","Star crown","Spirit wing","Leaf crown","Head","Sleeve","Glove","Boot"};int index=0;foreach(var name in names){var go=new GameObject();var original=new Mesh();go.filter.sharedMesh=original;ActorSilhouetteF1.Apply(go,name,index>=8);Check(go.filter.sharedMesh!=original,"real F1 apply "+name);Check(go.filter.sharedMesh.triangles.Length<=512*3,"triangle budget");var cached=go.filter.sharedMesh;ActorSilhouetteF1.Apply(go,name,index>=8);Check(object.ReferenceEquals(cached,go.filter.sharedMesh),"shared cached mesh");index++;}Check(Directory.GetFiles(Path.Combine(args[0],"ActorSilhouettes/F1"),"*.bytes").Length==12,"bounded12 inventory");foreach(var n in new[]{"Head","Sleeve","Glove","Boot","Trouser"})Check(ActorSilhouetteF1.Key(n,false)==null,"no other-role generic piece replacement");var rollback=new GameObject();var fallback=new Mesh();rollback.filter.sharedMesh=fallback;ActorSilhouetteF1.Enabled=false;ActorSilhouetteF1.Apply(rollback,"Forest Hood",false);Check(rollback.filter.sharedMesh==fallback,"explicit rollback preserves original");Reset();Resources.Root=args[1];Directory.CreateDirectory(Path.Combine(args[1],"ActorSilhouettes/F1"));File.WriteAllBytes(Path.Combine(args[1],"ActorSilhouettes/F1/Hood.bytes"),new byte[]{1,2,3});ActorSilhouetteF1.Apply(rollback,"Forest Hood",false);Check(rollback.filter.sharedMesh==fallback,"malformed fallback");ActorSilhouetteF1.Apply(rollback,"Quiver",false);Check(rollback.filter.sharedMesh==fallback,"missing fallback");Console.WriteLine("PASS F1 actual12 resources, strict decoder, cached mesh, role isolation, explicit/missing/malformed fallback");}}
''')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 command=[dotnet,'run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources'),str(p/'bad')];env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run(command,env=env,check=True)
 loader=p/'ActorSilhouetteF1.cs';good=loader.read_text();loader.write_text(good.replace('if(treant)switch(name)','if(true)switch(name)'))
 r=subprocess.run(command,env=env,capture_output=True,text=True);assert r.returncode and 'no other-role generic piece replacement' in r.stdout+r.stderr,r.stdout+r.stderr;loader.write_text(good)
 print('PASS compiled broad-role replacement mutant rejected',flush=True)
 subprocess.run([sys.executable,str(root/'ArtSource/ActorSilhouettes/F1/export.py'),str(p/'production'),dotnet],check=True)
 cases=json.loads((p/'production/actors.json').read_text());assert len(cases)==44
 loaded={part['mesh'].split(' / ')[-1] for c in cases for part in c['parts'] if part.get('mesh','').startswith('F1 silhouette / ')}
 expected={'RobePanel','Hood','Quiver','ArrowFeather','Antler','SpiritCrown','SpiritWing','Canopy','BarkLimb','BarkHand','RootFoot','BarkFace'};assert loaded==expected,(loaded,expected)
 for c in cases:
  for part in c['parts']:
   for v in part['vertices']:assert all(abs(x)<16 for x in v),'finite bounded actual world vertices'
 print('PASS all12 generated modules are visible in actual production compositions;44 body/equipment/action cases')

 subprocess.run([sys.executable,str(root/'ArtSource/ActorSilhouettes/F1/measure_attachments.py'),str(p/'production/actors.json'),str(p/'contacts.json')],check=True)
 model=p/'production/export/Model.cs';good=model.read_text();bad=good.replace('new Color(.67f,1f,.88f),model.CompanionRigidParent()', 'new Color(.67f,1f,.88f),model.body');assert bad!=good;model.write_text(bad)
 r=subprocess.run([dotnet,'run','--project',str(p/'production/export/Export.csproj')],env=dict(env,DOTNET_CLI_HOME=str(p/'production/cli')),capture_output=True,text=True);model.write_text(good)
 assert r.returncode and 'spirit wings follow rigid pose' in r.stdout+r.stderr,r.stdout+r.stderr
 print('PASS compiled soft-core-parent attachment regression rejected')
