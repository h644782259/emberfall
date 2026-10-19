"""Exercise the production ray-height selection and projectile flight-height methods."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(source,signature):
 a=source.index(signature);b=source.index('{',a)+1;depth=1
 while depth:
  depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
ray=member((root/'Assets/Scripts/Combat/PlayerController.cs').read_text(),'private Vector3 BasicAttackRayTarget(').replace('private Vector3','public Vector3',1)
flight=member((root/'Assets/Scripts/Combat/CombatEffects.cs').read_text(),'private float FlightHeight(').replace('private float','public float',1)
source=r'''using System;using System.Collections.Generic;
struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>0?this*(1/magnitude):new Vector3();public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f);public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;}
static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp01(float x)=>Math.Max(0,Math.Min(1,x));public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);}
static class CombatFx{public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);}
static class CombatSight{public static bool Clear=true;public static bool Direct(Vector3 a,Vector3 b)=>Clear;}
static class PhysicalSkillMotion{public const float Gravity=9.81f;}
class Enemy{public Vector3 Body;public float ProjectileHitRadius=.3f;public bool Alive=true;}
class Session{public List<Enemy> Enemies=new List<Enemy>();}
class Player{public Session session=new Session();bool ValidAimTarget(Enemy e)=>e!=null&&e.Alive;Vector3 EnemyBodyPoint(Enemy e)=>e.Body;
RAY
}
class Projectile{public bool arrowShape;public float launchHeight,impactHeight,aimedDistance,speed=16;
FLIGHT
}
class Program{static int checks;static void C(bool v,string m){checks++;if(!v)throw new Exception(m);}static void Main(){
foreach(float height in new[]{.35f,1f,4f,8f,-2f})foreach(bool arrow in new[]{false,true}){
var p=new Player();p.session.Enemies.Add(new Enemy{Body=new Vector3(0,height,6)});
var muzzle=new Vector3(0,2,0);var target=p.BasicAttackRayTarget(muzzle,new Vector3(0,0,1));C(Math.Abs(target.y-height)<.001f&&target.x==0&&target.z==6,"body elevation selected without horizontal steering");
var shot=new Projectile{arrowShape=arrow,launchHeight=muzzle.y,impactHeight=target.y,aimedDistance=6};C(Math.Abs(shot.FlightHeight(6)-height)<.001f,"production trajectory crosses body height");}
var player=new Player();player.session.Enemies.Add(new Enemy{Body=new Vector3(0,5,10)});player.session.Enemies.Add(new Enemy{Body=new Vector3(0,.5f,3)});player.session.Enemies.Add(new Enemy{Body=new Vector3(2,7,2)});
C(player.BasicAttackRayTarget(new Vector3(0,2,0),new Vector3(0,0,1)).y==.5f,"nearest intersecting body, not off-axis enemy");
player.session.Enemies[1].Alive=false;C(player.BasicAttackRayTarget(new Vector3(0,2,0),new Vector3(0,0,1)).y==5,"dead targets excluded");
CombatSight.Clear=false;C(player.BasicAttackRayTarget(new Vector3(0,2,0),new Vector3(0,0,1)).y==2,"occluded bodies do not redirect elevation");
Console.WriteLine("PASS "+checks+" elevation assertions");}}
'''.replace('RAY',ray).replace('FLIGHT',flight)
with tempfile.TemporaryDirectory(prefix='basic-elevation-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 project=cv.write_project(p/'project',[],source)
 subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project)],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',MSBuildEnableWorkloadResolver='false'))
