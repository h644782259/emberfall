from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
vfx=(root/'Assets/Scripts/Combat/EnemySkillPhysicalVfx.cs').read_text()
vfx=vfx[vfx.index('internal sealed class EnemySweepPhysicalVfx'):]
contains=member(vfx,'internal bool Contains(').replace('internal','public')
a=vfx.index('                spanStarts[i]=');b=vfx.index('                bodies[i].gameObject.SetActive',a)
mask=vfx[a:b].replace('bodies.Length','clearSpans.Length')
fixture=(root/'Tests/DestructibleTraversalTests.cs').read_text();fixture='using System;using UnityEngine;'+fixture[fixture.index('namespace Emberfall'):]

body='using UnityEngine;namespace Emberfall{class Sweep{float radius=.95f;bool[] clearSpans=new bool[32];Vector3[] spanStarts=new Vector3[32],spanEnds=new Vector3[32];public void Update(Vector3 from,Vector3 to){for(int i=0;i<clearSpans.Length;i++){'+mask+'}}'+contains+'}}'
enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
bolt='\n'.join(member(enemy,k) for k in ['private Vector3 BoltMuzzle(', 'private Vector3 BoltDirection(']).replace('private ','public ')
contact=member((root/'Assets/Scripts/Combat/PhysicalSkillMotion.cs').read_text(),'internal static bool BodyContact(').replace('internal','public')
body+='namespace Emberfall{class Fan{public enum AttackType{Fan,Bolt}AttackType attackType=AttackType.Fan;bool IsBoss=true;public float BodyScale=3;public object largeBoss;public class Socket{public float y;}public class SocketTransform{public Vector3 position;}public SocketTransform bossBoltSocket;public class Companion{public bool IsAlive;public float PhysicalBodyHeight;}public Companion companionTarget;public Vector3 targetPoint;'+bolt+'}class Contact{'+contact+'}}'
rows=(root/'Assets/Scripts/Combat/BossLayeredStrikes.cs').read_text()
constants='\n'.join(line.strip().replace('internal','public') for line in rows.splitlines() if 'internal const ' in line)
body+='namespace Emberfall{class RowRules{'+constants+member(rows,'internal static float GapCenter(').replace('internal','public')+member(rows,'internal static bool InGap(').replace('internal','public')+'}}'
test='''using System;using Emberfall;using UnityEngine;
class Test{static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}static void Main(){
var s=new Sweep();var a=new Vector3(0,0,0);var b=new Vector3(0,0,18);
WorldTraversal.Reset(ZoneKind.Dungeon);s.Update(a,b);C(s.Contains(new Vector3(0,0,15)),"doubled reach active");
WorldTraversal.AddDynamicCircle(new Vector3(0,0,7),.8f);s.Update(a,b);C(s.Contains(new Vector3(0,0,3)),"before cover active");C(!s.Contains(new Vector3(0,0,7)),"cover has no hidden damage");C(s.Contains(new Vector3(0,0,12)),"after cover remains active");
WorldTraversal.AddDynamicCircle(a,.8f);s.Update(a,b);C(!s.Contains(a),"blocked origin inactive");C(s.Contains(new Vector3(0,0,12)),"blocked origin does not disable far beam");
s.Update(a,new Vector3(18,0,0));C(s.Contains(new Vector3(12,0,0)),"rotated beam reopens");C(!s.Contains(new Vector3(0,0,12)),"old orientation retired");
C(LargeBossPhaseState.BeamLength==18,"normal reach doubled");
C(CombatNumberFormat.Compact(9999)=="9999","small number intact");C(CombatNumberFormat.Compact(123456)=="12.35万","ten thousand units");C(CombatNumberFormat.Compact(123456789)=="1.23亿","hundred million units");C(CombatNumberFormat.Compact(int.MaxValue).Length<=7,"large card number bounded");
foreach(float scale in new[]{1f,2.9f,3f}){var fan=new Fan{BodyScale=scale};var muzzle=fan.BoltMuzzle(a,new Vector3(0,0,1));var direction=fan.BoltDirection(muzzle,new Vector3(0,0,1));C(Math.Abs(muzzle.y-1.1f)<.001f&&direction.y==0,"fan stays at victim height regardless of boss scale");foreach(float distance in new[]{4f,8f,15f,24f}){float hit;C(Contact.BodyContact(muzzle,muzzle+direction*30,new Vector3(0,0,distance),1.8f,.46f,.32f,out hit),"fan contacts near and far bodies");}}
for(int row=0;row<3;row++){float center=RowRules.GapCenter(row);C(RowRules.InGap(center,row,.46f),"row gap safely passes player body");C(!RowRules.InGap(center+2.1f,row,.46f),"gap edge accounts for body radius");}C(RowRules.RowSpacing>2*(RowRules.HalfDepth+.46f),"rows have traversable spacing");C(RowRules.RowDelay>1f&&RowRules.WarningSeconds>=1.5f,"fair reaction and layer timing");
float safeWindow=0;for(int i=0;i<550;i++){float t=(i+.5f)/550f;if(WorldTraversal.JumpPosition(a,a,t).y>RowRules.JumpClearance)safeWindow+=.001f;}C(safeWindow>RowRules.ActiveSeconds,"normal jump clears the full active pulse");
Console.WriteLine("PASS "+n+" sweep gap, rotation, reach and recap number checks");}}
'''
with tempfile.TemporaryDirectory(prefix='ember-gap-test-') as d:
 p=Path(d)
 for name,content in [('Fixture.cs',fixture),('Sweep.cs',body),('Test.cs',test),('World.cs',(root/'Assets/Scripts/World/WorldTraversal.cs').read_text()),('Phase.cs',(root/'Assets/Scripts/Core/LargeBossPhaseState.cs').read_text()),('Numbers.cs',(root/'Assets/Scripts/Core/CombatNumberFormat.cs').read_text())]: (p/name).write_text(content)
 (p/'t.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 import sys
 dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
 subprocess.run([dotnet,'build',str(p/'t.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 subprocess.run([dotnet,str(p/'bin/Debug/net8.0/t.dll')],env=env,check=True)
