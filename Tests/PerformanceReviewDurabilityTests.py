#!/usr/bin/env python3
"""Execute the production review-only durability method with minimal session doubles."""
import os,pathlib,subprocess,sys,tempfile
root=pathlib.Path(__file__).resolve().parents[1]
code=r'''
using System;
namespace Emberfall {
public class Progression { public bool IsPracticeOnly=true; }
public class Record { public bool Started=false,UsesEnemyAI=true; }
public class Session { public bool PracticeActive=true; public Progression Progression=new Progression(); public Record PracticeRecord=new Record(); }
public sealed partial class EnemyController {
 private Session session; public float Health=100,MaxHealth=100; public bool IsDead=>Health<=0;
 public EnemyController(Session owner){session=owner;}
}
}
class Program {
 static int checks;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static void Reject(Emberfall.Session owner,string label) {var e=new Emberfall.EnemyController(owner);Check(!e.PreparePerformanceReviewDurability()&&e.Health==100&&e.MaxHealth==100,label);}
 static void Main(){
 var key="EMBERFALL_PERF_REVIEW";var previous=Environment.GetEnvironmentVariable(key);
 try {
  Environment.SetEnvironmentVariable(key,null);Reject(new Emberfall.Session(),"ordinary launch unchanged");
  Environment.SetEnvironmentVariable(key,"performance-soak-combat");Reject(new Emberfall.Session(),"ordinary combat review unchanged");
  Environment.SetEnvironmentVariable(key,"performance-soak-combat-crowd");
  Reject(null,"missing owner");Reject(new Emberfall.Session{PracticeActive=false},"outside practice");
  Reject(new Emberfall.Session{Progression=null},"missing progression");Reject(new Emberfall.Session{Progression=new Emberfall.Progression{IsPracticeOnly=false}},"saved adventure protected");
  Reject(new Emberfall.Session{PracticeRecord=null},"missing record");Reject(new Emberfall.Session{PracticeRecord=new Emberfall.Record{Started=true}},"live fight cannot be rewritten");
  Reject(new Emberfall.Session{PracticeRecord=new Emberfall.Record{UsesEnemyAI=false}},"normal dummy practice unchanged");
  var dead=new Emberfall.EnemyController(new Emberfall.Session()){Health=0};Check(!dead.PreparePerformanceReviewDurability()&&dead.Health==0,"dead enemy not revived");
  var e=new Emberfall.EnemyController(new Emberfall.Session());Check(e.PreparePerformanceReviewDurability()&&e.Health==100000000&&e.MaxHealth==100000000,"explicit stress workload applied");
  e.MaxHealth=e.Health=200000000;Check(e.PreparePerformanceReviewDurability()&&e.Health==200000000,"never weaken stronger targets");
  Console.WriteLine("PASS: "+checks+" production durability gate checks");
 } finally{Environment.SetEnvironmentVariable(key,previous);}
 }
}
'''
with tempfile.TemporaryDirectory(prefix='emberfall-review-durability-') as tmp:
 p=pathlib.Path(tmp);(p/'Program.cs').write_text(code);(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 source=root/'Assets/Scripts/Combat/EnemyController.PerformanceReview.cs'
 (p/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup><ItemGroup><Compile Include="'+str(source)+'" /></ItemGroup></Project>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'home'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES=str(p/'nuget'))
 subprocess.run([sys.argv[1],'run','--project',str(p/'Tests.csproj'),'--configuration','Release'],env=env,check=True)
