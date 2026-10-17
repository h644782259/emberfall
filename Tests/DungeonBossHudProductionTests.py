from pathlib import Path
import tempfile,subprocess,os
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/UI/GameUI.cs').read_text();a=s.index('private void DrawDungeonStatus()');b=s.index('{',a)+1;d=1
while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
method=s[a:b].replace('private','public',1)
code='''using System;using System.Collections.Generic;
class Obj{public bool activeInHierarchy=true;}
class EnemyController{public bool IsBoss,IsDead;public Obj gameObject=new Obj();}
class Player{public EnemyController AimTarget;}
class Session{public bool InDungeon=true,DungeonCleared;public Player Player=new Player();public List<EnemyController> Enemies=new List<EnemyController>();}
class Hud{public Session session=new Session();public EnemyController shown;void DrawEnemyTargetPanel(EnemyController e){shown=e;}METHOD}
class Program{static void Check(bool v){if(!v)throw new Exception("boss HUD regression");}static void Main(){var h=new Hud();var mob=new EnemyController();var boss=new EnemyController{IsBoss=true};h.session.Enemies.AddRange(new[]{mob,boss});h.session.Player.AimTarget=mob;h.DrawDungeonStatus();Check(h.shown==boss);h.session.Player.AimTarget=boss;h.DrawDungeonStatus();Check(h.shown==boss);boss.IsDead=true;h.shown=null;h.DrawDungeonStatus();Check(h.shown==null);boss.IsDead=false;boss.gameObject.activeInHierarchy=false;h.DrawDungeonStatus();Check(h.shown==null);Console.WriteLine("PASS boss persists while mob is targeted; dead and inactive bosses hidden");}}
'''.replace('METHOD',method)
with tempfile.TemporaryDirectory() as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(code);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli')))

assert 'if (!session.InDungeon && target != null && !target.IsDead)' in (root/'Assets/Scripts/UI/GameUI.cs').read_text()
assert 'if(session.InDungeon)DrawDungeonStatus();' in (root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text()
print('PASS desktop and mobile dungeon HUD entry wiring')
