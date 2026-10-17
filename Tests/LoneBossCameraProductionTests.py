"""Execute production lone-boss selection; engine visibility is a controlled boundary."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/Core/GameSession.cs').read_text();a=s.index('private static EnemyController ResolveLoneBossCameraTarget(');b=s.index('{',a)+1;d=1
while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
method=s[a:b].replace('private','public',1)
code='''using System;using System.Collections.Generic;
struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
class Transform{public Vector3 position;}class Obj{public bool activeInHierarchy=true;}
class EnemyController{public bool IsDead,IsBoss,isActiveAndEnabled=true;public Obj gameObject=new Obj();public Transform transform=new Transform();}
class Player{public Transform transform=new Transform();}
class GameSession{public bool InDungeon=true;public Player Player=new Player();public List<EnemyController> Enemies=new List<EnemyController>();}
static class CombatFx{public static Vector3 Flat(Vector3 p)=>new Vector3(p.x,0,p.z);}
static class CombatSight{public static bool Visible=true;public static bool Direct(Vector3 a,Vector3 b)=>Visible;}
class Camera{METHOD}
class Program{static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}static void Main(){var g=new GameSession();C(Camera.ResolveLoneBossCameraTarget(g)==null,"empty encounter");var boss=new EnemyController{IsBoss=true};boss.transform.position=new Vector3(0,0,10);g.Enemies.Add(boss);C(Camera.ResolveLoneBossCameraTarget(g)==boss,"lone boss within radius selected without aim target");var mob=new EnemyController();g.Enemies.Add(mob);C(Camera.ResolveLoneBossCameraTarget(g)==null,"living mob disables assist");mob.IsDead=true;C(Camera.ResolveLoneBossCameraTarget(g)==boss,"dead mob no longer disables assist");mob.IsDead=false;mob.gameObject.activeInHierarchy=false;C(Camera.ResolveLoneBossCameraTarget(g)==boss,"retired mob ignored");mob.gameObject.activeInHierarchy=true;mob.IsBoss=true;C(Camera.ResolveLoneBossCameraTarget(g)==null,"two bosses disable assist");g.Enemies.Remove(mob);boss.transform.position=new Vector3(0,0,14);C(Camera.ResolveLoneBossCameraTarget(g)==boss,"radius boundary included");boss.transform.position=new Vector3(0,0,14.1f);C(Camera.ResolveLoneBossCameraTarget(g)==null,"outside radius disables assist");boss.transform.position=new Vector3(0,0,10);CombatSight.Visible=false;C(Camera.ResolveLoneBossCameraTarget(g)==null,"blocked sight disables assist");CombatSight.Visible=true;g.InDungeon=false;C(Camera.ResolveLoneBossCameraTarget(g)==null,"camp does not assist");g.InDungeon=true;boss.IsDead=true;C(Camera.ResolveLoneBossCameraTarget(g)==null,"dead boss disables assist");Console.WriteLine("PASS "+n+" production lone-boss camera assertions");}}
'''.replace('METHOD',method)
with tempfile.TemporaryDirectory() as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(code);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([os.environ.get('DOTNET','/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'),'run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli')))
assert 'if(!snap)AssistCombatView();' in s
assert 'orbitInput.IsHeld||MobileControls.Active&&MobileControls.CameraLookActive' in s
assert 'if(Time.unscaledTime<manualCameraUntil)' in s
print('PASS mobile/desktop invocation and manual-gesture priority wiring')
