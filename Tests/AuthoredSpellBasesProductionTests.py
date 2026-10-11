#!/usr/bin/env python3
"""Execute production adapter + production strict decoder against every committed buffer."""
from pathlib import Path
import tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
fixture=(root/'Tests/ActorModulesProductionTests.py').read_text().split("(p/'Test.cs').write_text(r'''",1)[1].split('class Program',1)[0]
with tempfile.TemporaryDirectory(prefix='authored-spell-bases-') as d:
 p=Path(d)
 for name in ['AuthoredActorMeshes','AuthoredSpellBases']:(p/(name+'.cs')).write_text((root/('Assets/Scripts/Combat/'+name+'.cs')).read_text())
 (p/'Test.cs').write_text(fixture+r'''
class Program {
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
 static void Main(string[] args){Resources.Root=args[0];int count=0;foreach(var file in Directory.GetFiles(Path.Combine(args[0],"BlenderSpellBases"),"*.bytes")){
 string name=Path.GetFileNameWithoutExtension(file);var mesh=AuthoredSpellBases.Load(name);Check(mesh!=null&&mesh.vertices.Length>0,"actual authored load "+name);Check(mesh.name=="Authored spell / "+name,"runtime name");Check(mesh.triangles.Length<=612,"bounded source triangles");count++;
 }Check(count==11,"finite inventory");Check(AuthoredSpellBases.Load("missing")==null,"individual missing fallback");
 var original=Resources.Root;Resources.Root=args[1];Directory.CreateDirectory(Path.Combine(args[1],"BlenderSpellBases"));File.WriteAllBytes(Path.Combine(args[1],"BlenderSpellBases/Crystal.bytes"),new byte[]{1,2,3});Check(AuthoredSpellBases.Load("Crystal")==null,"individual malformed fallback");Resources.Root=original;Check(AuthoredSpellBases.Load("Crystal")!=null,"good resource remains available after malformed other request");Console.WriteLine("PASS actual AuthoredSpellBases loader + decoder 11 assets, missing/malformed fallback. Managed, NOT Unity.");}}
''')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources'),str(p/'bad')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
