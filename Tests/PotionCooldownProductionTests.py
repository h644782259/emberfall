"""Compile actual potion consumption and timer against small engine boundaries."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os
root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('cv', root/'Tools/cloud-validation.py')
cv = importlib.util.module_from_spec(spec); spec.loader.exec_module(cv)
def member(source, key):
    a=source.index(key); b=source.index('{',a)+1; depth=1
    while depth:
        depth+=(source[b]=='{')-(source[b]=='}'); b+=1
    return source[a:b]
session=(root/'Assets/Scripts/Core/GameSession.cs').read_text()
fixture=r'''using System;
namespace UnityEngine{public struct Vector3{public static Vector3 up=>new Vector3();public static Vector3 operator *(Vector3 x,int y)=>x;public static Vector3 operator +(Vector3 x,Vector3 y)=>x;}public struct Color{public Color(float a,float b,float c){}}public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static int CeilToInt(float x)=>(int)Math.Ceiling(x);}}
namespace Emberfall{
public class Transform{public UnityEngine.Vector3 position;}
public partial class PlayerController{public float Health=1,MaxHealth=100;public Transform transform=new Transform();public void Heal(float amount){Health=Math.Min(MaxHealth,Health+amount);}public void Advance(float dt)=>TickPotionCooldown(dt);}
public class Inventory{public int Count=3;public bool Fail;public string LastError="failed";public bool UsePotion(){if(Fail||Count<=0)return false;Count--;return true;}}
public class Practice{public bool Started=true,Finished;}
public class GameSession{public bool PracticeActive,InputBlocked,HasStarted=true,IsDead,ChallengeRun,InDungeon;public Practice PracticeRecord=new Practice();public PlayerController Player=new PlayerController();public Inventory Progression=new Inventory();public int Charges=3;public string Notice;public void Notify(string x){Notice=x;}public bool TrySpendHealingCharge(){if(Charges<=0)return false;Charges--;return true;}public void SpawnFloatingText(UnityEngine.Vector3 p,string s,UnityEngine.Color c){}
'''+member(session,'public void DrinkPotion()')+'}}'
program=r'''using System;using Emberfall;class Program{static int n;static void C(bool ok,string s){n++;if(!ok)throw new Exception(s);}static void Main(){var s=new GameSession();s.DrinkPotion();C(s.Player.Health==51&&s.Progression.Count==2&&s.Player.PotionCooldownRemaining==60,"successful use starts sixty seconds");s.Player.Health=1;s.DrinkPotion();C(s.Player.Health==1&&s.Progression.Count==2,"repeat does not heal or spend");s.Player.Advance(59.9f);s.DrinkPotion();C(s.Progression.Count==2,"before boundary rejected");s.Player.Advance(0);C(s.Player.PotionCooldownRemaining>0,"pause preserves timer");s.Player.Advance(.1f);s.DrinkPotion();C(s.Progression.Count==1&&s.Player.Health==51,"available after sixty seconds");s.Player.Advance(100);C(s.Player.PotionCooldownRemaining==0,"timer clamps to zero");s.Player.Health=100;s.DrinkPotion();C(s.Player.PotionCooldownRemaining==0&&s.Progression.Count==1,"full health does not consume or start timer");s.Player.Health=1;s.Progression.Fail=true;s.DrinkPotion();C(s.Player.PotionCooldownRemaining==0&&s.Player.Health==1,"failed save does not start timer or heal");s.ChallengeRun=s.InDungeon=true;s.DrinkPotion();s.Player.Health=1;s.DrinkPotion();C(s.Charges==2&&s.Player.Health==1,"challenge charges share cooldown");var other=new PlayerController();C(other.PotionCooldownRemaining==0,"new practice player independent");Console.WriteLine("PASS "+n+" potion cooldown/transaction assertions");}}'''
with tempfile.TemporaryDirectory(prefix='potion-cooldown-') as tmp:
    p=Path(tmp);(p/'Fixture.cs').write_text('using UnityEngine;'+fixture)
    project=cv.write_project(p/'project',[p/'Fixture.cs',root/'Assets/Scripts/Combat/PlayerController.Potion.cs'],program)
    (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    sdk='/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
    subprocess.run([sdk,'run','--project',str(project)],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
