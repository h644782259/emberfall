"""Exercise actual boss selection/eligibility and strike hit methods without a renderer."""
from pathlib import Path
import tempfile, subprocess, os
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
def member(key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:
  n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b].replace('private ', 'public ',1)
code=r'''
using System;
namespace Emberfall {
struct Vector3 { public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude<.00001f?new Vector3():new Vector3(x/magnitude,y/magnitude,z/magnitude);public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;}
static class CombatFx { public static Vector3 Flat(Vector3 p)=>new Vector3(p.x,0,p.z);public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b)=>0; }
static class WorldTraversal {public static bool ground=true,sight=true;public static bool HasGroundPath(Vector3 a,Vector3 b,float r)=>ground;public static bool HasLineOfSight(Vector3 a,Vector3 b)=>sight;}
static class EnemyImpactRegion { public static bool Contains(Vector3 a,Vector3 b,Vector3 c,float r)=>false; }
class Transform {public Vector3 position;}
class Harness {
public float BodyScale=1,BossChargeHalfWidth=1.3f,ImpactRadius=2.5f,BladeContactHalfWidth=.2f; public Transform transform=new Transform();public BossAdvanceBudget advanceBudget=new BossAdvanceBudget();public int arenaBossPattern,repeatedMove;public BossAttackPolicy.Move previousMove;public enum AttackType {Strike,Melee};public enum EnemyKind{Goblin};public AttackType attackType=AttackType.Strike;public EnemyKind Kind;public Vector3 attackForward=new Vector3(0,0,1),attackOrigin,targetPoint,BladeStart,BladeEnd;
Vector3 ClipPath(Vector3 a,Vector3 b,bool c)=>b;
METHODS
}
class Program {
static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}
static void Main(){
var h=new Harness();
foreach(int pattern in new[]{0,1,2,3})foreach(float height in new[]{0f,1f,3f}){
h.arenaBossPattern=pattern;h.repeatedMove=0;WorldTraversal.ground=true;WorldTraversal.sight=true;
var p=new Vector3(0,height,1);
C(h.SelectBossMove(1,p)==BossAttackPolicy.Move.Strike,"close target overrides ranged/charge preference");
h.previousMove=BossAttackPolicy.Move.Strike;h.repeatedMove=1;
C(h.SelectBossMove(1,p)==BossAttackPolicy.Move.Spin,"close rotation remains varied");
WorldTraversal.ground=false;
C(h.SelectBossMove(1,p)==BossAttackPolicy.Move.Strike,"visible target on low obstacle retains strike");
C(h.CanUseBossAttack(BossAttackPolicy.Move.Strike,p),"jump height cannot prevent visible strike");
WorldTraversal.sight=false;
C(!h.CanUseBossAttack(BossAttackPolicy.Move.Strike,p),"wall still blocks strike");
}
WorldTraversal.sight=true;
C(h.InsideImpact(new Vector3()),"exact overlap is hittable");
C(h.InsideImpact(new Vector3(0,0,1)),"forward strike hits");
C(!h.InsideImpact(new Vector3(0,0,-1)),"rear escape remains valid");
C(!h.InsideImpact(new Vector3(0,0,3)),"range is unchanged");
WorldTraversal.sight=false;C(!h.InsideImpact(new Vector3()),"overlap does not bypass cover");
Console.WriteLine("PASS "+n+" boss close response assertions");}
}}
'''
code=code.replace('METHODS','\n'.join(member(k) for k in ['private bool CanUseBossAttack(', 'private BossAttackPolicy.Move SelectBossMove(', 'private bool InsideImpact(']))
with tempfile.TemporaryDirectory(prefix='boss-close-') as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(code)
 for name in ['BossAttackPolicy','ArenaBossPatternPolicy']:
  (p/(name+'.cs')).write_bytes((root/'Assets/Scripts/Combat'/(name+'.cs')).read_bytes())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,MSBuildEnableWorkloadResolver='false',DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
