"""Exercise the actual skill dispatcher: every mobile skill commits without a preview."""
from pathlib import Path
import subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
source=(root/'Assets/Scripts/Combat/SkillTargetingController.cs').read_text()
def member(signature):
    a=source.index(signature);b=source.index('{',a)+1;depth=1
    while depth:
        depth+=(source[b]=='{')-(source[b]=='}');b+=1
    return source[a:b]
code='''using System;
enum HeroClass { Vanguard,Arcanist,Ranger,Summoner }
static class MobileControls { public static bool Active=true; }
static class Time { public static int frameCount; }
static class GameBalance { public const int SkillCount=10; public static bool IsPassive(int s)=>s==3||s==8; }
struct V { public static V operator +(V a,V b)=>a;public static V operator *(V a,float b)=>a; }
class Transform { public V position,forward; }
class Profile { public int[] skillRanks=new int[10]; }
class Progression { public Profile Profile=new Profile(); }
class Session { public Progression Progression=new Progression(); }
class Owner {
 public HeroClass HeroClass;public int CombatEpoch,Prepared,Casts;public bool Ready=true,Pinned=true;
 public V AimPoint;public Transform transform=new Transform();
 public void ClearMobilePinnedTarget(){Pinned=false;}public void PrepareMobileSkillAim(int s){Prepared++;}
 public bool CanBeginSkillTargeting(int s)=>Ready;
 public bool ConfirmTargetedSkill(int s,V p){Casts++;return true;}public bool CastImmediateSkill(int s){Casts++;return true;}
}
class Visual {public void SetActive(bool b){throw new Exception("Mobile placement preview opened");}}
class Fixture {
 Owner owner=new Owner();Session session=new Session();int castFrame=-1,skill=-1,epoch;bool previewDirty;object CurrentPreview;Visual visuals=new Visual();
 void Cancel(){skill=-1;}void EnsureVisuals(){throw new Exception("Mobile placement geometry allocated");}
 void SetTarget(V p){}object Describe(HeroClass h,int i,int r)=>null;
''' + member('public static bool RequiresConfirmation(')+member('public bool Begin(')+'''
 static void Main(){int checks=0;
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))for(int s=0;s<10;s++){
   Time.frameCount++;var f=new Fixture();f.owner.HeroClass=hero;
   if(GameBalance.IsPassive(s)){if(f.Begin(s))throw new Exception("Passive cast");continue;}
   if(!f.Begin(s)||f.owner.Casts!=1||f.owner.Pinned||f.skill!=-1||f.owner.Prepared!=1)throw new Exception("Not a single direct cast: "+hero+"/"+s);
   if(f.Begin(s)||f.owner.Casts!=1)throw new Exception("Double dispatch");
   Time.frameCount++;f.owner.Ready=false;if(f.Begin(s)||f.owner.Casts!=1||f.skill!=-1)throw new Exception("Unavailable skill committed");checks+=3;
  }
  MobileControls.Active=false;Time.frameCount++;var desktop=new Fixture();desktop.owner.HeroClass=HeroClass.Vanguard;
  if(!desktop.Begin(0)||desktop.owner.Casts!=1)throw new Exception("Desktop immediate cast regressed");
  Console.WriteLine("PASS: "+checks+" mobile one-tap skill assertions plus desktop immediate cast");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='emberfall-one-tap-') as folder:
    p=Path(folder);(p/'Program.cs').write_text(code)
    (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>')
    subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],check=True)
