"""Production C# helpers with engine shells; does not launch Unity or test rendered UI."""
from pathlib import Path
import importlib.util,tempfile,os,subprocess,sys
dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
r=Path(__file__).resolve().parents[1]
s=importlib.util.spec_from_file_location('cv',r/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(s);s.loader.exec_module(cv)
for name,files in [('MobileSkillNavigationTests',['UI/GameUI.MobileSkillNavigation.cs','UI/TouchScrollGesture.cs','UI/SkillIconPresentation.cs']),('SkillIconAtlasTests',['UI/UIIconAtlas.cs','UI/SkillIconPresentation.cs','Core/GameTypes.cs','Core/CombatBalance.cs']),('MechanicBadgePresentationTests',['Core/GameTypes.cs','Core/SkillRuntime.cs','UI/EquipmentComparisonPresentation.cs','UI/MechanicBadgePresentation.cs'])]:
 with tempfile.TemporaryDirectory(prefix='a07-') as t:
  o=Path(t);p=cv.write_project(o/'p',[r/'Assets/Scripts'/f for f in files]+[r/'Tests'/f'{name}.cs'],program=f'using System;class Program{{static void Main(){{Console.WriteLine({name}.Run());}}}}')
  if name=='MechanicBadgePresentationTests':
   text=p.read_text();p.write_text(text.replace('</ItemGroup>', '<Compile Include="'+str(r/'Tests/SkillRuntimeTests.cs')+'" /></ItemGroup>'))
  c=o/'NuGet.Config';c.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(o/'cli'),DOTNET_NOLOGO='1')
  subprocess.run([dotnet,'build',str(p),'--configfile',str(c),'-v:q'],env=env,check=True)
  subprocess.run([dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')],env=env,check=True)
