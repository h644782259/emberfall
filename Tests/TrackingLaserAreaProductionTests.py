"""Execute the actual HitArea loop against grouped, edge and obscured targets."""
from pathlib import Path
import tempfile, subprocess, os
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text(encoding='utf-8-sig')
a=s.index('        internal void HitArea(');end=s.index('{',a)+1;depth=1
while depth:
    depth+=(s[end]=='{')-(s[end]=='}');end+=1
method=s[a:end]
code='''using System;using System.Collections.Generic;
struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);public Vector3 normalized=>this;public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
struct CombatDamage{public float Amount;public bool IsCritical;public static implicit operator CombatDamage(float v)=>new CombatDamage{Amount=v};}
class Transform{public Vector3 position;}
class EnemyController{public Transform transform=new Transform();public bool IsDead,IsBoss;public float HitFootprintBonus;public float Health=100;public int Hits;public void TakeDamage(float amount,Vector3 direction,float knockback,float stun,bool critical,int practiceCastId){Health-=amount;Hits++;}}
class ProjectileVolleyBudget<T>{public CombatDamage Apply(T e,CombatDamage d,bool a)=>d;}
static class CombatFx{public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);}
static class CombatImpactBatch{public static void Begin(){}public static void End(){}}
static class CombatSight{public static bool Area(Vector3 a,Vector3 b)=>b.x<1;}
static class DestructibleProp{public static void StrikeArea(object a,Vector3 b,float c,CombatDamage d,int e){}}
static class FilledSkillVfx{public static void ConfirmFinale(object a,int b){}}
class Session{public List<EnemyController> Enemies=new List<EnemyController>();}
class PlayerController{public Session session=new Session();int NewCastId()=>1;void RegisterSkillHit(int i){}void ApplySpellDodgeBoon(EnemyController e){}
'''+method+'''
}
class Program{static void Require(bool v,string m){if(!v)throw new Exception(m);}static void Main(){var p=new PlayerController();foreach(float x in new[]{0f,1f,2.99f,3f,3.01f})p.session.Enemies.Add(new EnemyController{transform=new Transform{position=new Vector3(x,0,0)}});for(int tick=0;tick<2;tick++)p.HitArea(new Vector3(),3,10,castId:7,strictRadius:true,requireSight:false);for(int i=0;i<4;i++)Require(p.session.Enemies[i].Hits==2,"every target inside circle receives each pulse");Require(p.session.Enemies[4].Hits==0,"outside circle excluded");p.HitArea(new Vector3(),3,10,strictRadius:true);Require(p.session.Enemies[0].Hits==3&&p.session.Enemies[1].Hits==2,"other spells preserve sight checks");Console.WriteLine("PASS production area loop: grouped hits, exact boundary, repeated ticks and unchanged default sight policy");}}
'''
with tempfile.TemporaryDirectory(prefix='laser-area-') as temp:
    p=Path(temp);(p/'Program.cs').write_text(code)
    (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    (p/'Check.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
    subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(p/'Check.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
