"""Execute the production ballistic/contact solver at different frame rates."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[1]
program=r'''
using System;using Emberfall;
class Program {
 static int checks;static void C(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Main(){
  foreach(int fps in new[]{15,30,60,120}){
   var m=new EnemyImpactMotion();m.Launch(2.2f);float initial=m.Velocity;
   for(int i=0;i<fps;i++)m.Advance(.5f/fps);
   C(Math.Abs(m.Height-(initial*.5f-EnemyImpactMotion.Gravity*.125f))<.00002f,"analytic gravity is frame-rate independent");
   C(!m.CanLift(.7f),"spikes cannot relaunch a target above their visible height");
   while(!m.CanLift(.7f))m.Advance(1f/fps);
   C(m.Velocity<=0&&m.Height<=.7f,"relaunch requires descent into contact volume");
   float before=m.Height;m.Launch(2.2f);C(m.Height==before&&m.Velocity>0,"relaunch preserves position without teleport");
   m.PressDown();C(m.Velocity<=-2,"suppression applies downward velocity");
   m.Advance(5);C(!m.Airborne&&m.Height==0,"long frame lands without sinking");
   float speed=11,travel=0;for(int i=0;i<fps;i++){travel+=EnemyImpactMotion.KnockbackTravel(speed,1f/fps);speed*=EnemyImpactMotion.KnockbackDecay(1f/fps);}
   C(Math.Abs(travel-EnemyImpactMotion.KnockbackTravel(11,1))<.00001,"knockback travel is frame-rate independent");
   C(travel>2&&travel<2.2f,"impact visibly separates bodies without unbounded drift");
  }
  var invalid=new EnemyImpactMotion();invalid.Launch(float.NaN);invalid.Launch(float.PositiveInfinity);C(!invalid.Airborne,"invalid launch rejected");
  invalid.Launch(2);var h=invalid.Height;var v=invalid.Velocity;invalid.Advance(0);invalid.Advance(float.NaN);C(invalid.Height==h&&invalid.Velocity==v,"paused/invalid clocks do not advance");
  Console.WriteLine("PASS "+checks+" production gravity, contact-height, repeated launch, suppression and impulse assertions");
 }
}'''
with tempfile.TemporaryDirectory(prefix='emberfall-impact-') as temp:
 p=Path(temp)
 (p/'Motion.cs').write_text((root/'Assets/Scripts/Combat/EnemyImpactMotion.cs').read_text(encoding='utf-8'),encoding='utf-8')
 (p/'Program.cs').write_text(program,encoding='utf-8')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--configuration','Release'],env=env,check=True)
