#!/usr/bin/env python3
"""Actual authored cloak deformation and mesh lifetime, with managed mesh/resource doubles."""
from pathlib import Path
exec((Path(__file__).with_name('EquipmentCompositionProductionTests.py')).read_text().split('with tempfile.TemporaryDirectory')[0])
with tempfile.TemporaryDirectory(prefix='preview-cloth-') as directory:
 p=Path(directory)
 fixture=(root/'Tests/EquipmentCompositionProductionTests.Fixture.cs').read_text().split('public static class EquipmentCompositionProductionTests')[0]
 fixture=fixture.replace('public class TailoredCloth:MonoBehaviour{public HeroClass Profile;public void Initialize(Material m,HeroClass hero=HeroClass.Vanguard){Profile=hero;}}','').replace('public static float deltaTime=.016f;','public static float deltaTime=.016f,time;').replace('public void RecalculateNormals(){}','public void MarkDynamic(){}public void RecalculateNormals(){}').replace('public const float PI=(float)Math.PI,Deg2Rad=PI/180;','public const float PI=(float)Math.PI,Deg2Rad=PI/180;public static float Exp(float a)=>(float)Math.Exp(a);public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);')
 (p/'Fixture.cs').write_text(fixture);(p/'Types.cs').write_text(data)
 for f in ['Assets/Scripts/Combat/RearSilhouette.cs','Assets/Scripts/Combat/TailoredCloth.cs','Assets/Scripts/Combat/VisualMeshRecipes.cs','Tests/PreviewClothProductionTests.cs']:(p/Path(f).name).write_text((root/f).read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);subprocess.run(cmd,env=env,check=True)
 source=p/'TailoredCloth.cs';original=source.read_text();assert original.count('Deform(time);')==1;source.write_text(original.replace('Deform(time);','Deform(Time.time);'));subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);assert result.returncode and 'System.Exception: manual preview cloth uses supplied clock rather than world time' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: global-clock cloak mutation compiled and rejected by the exact local-clock assertion')

 source.write_text(original.replace('silhouette = hero;','silhouette = HeroClass.Vanguard;'))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);assert result.returncode and 'System.Exception: each class has distinct actual cloth geometry' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: universal cloak old-behavior control compiled and rejected by actual geometry assertion')
