"""Run production gem form rules/runtime: all attributes and both trigger variants."""
from pathlib import Path
import os,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
types=(root/'Assets/Scripts/Core/GameTypes.cs').read_text(encoding='utf-8-sig')
runtime=(root/'Assets/Scripts/Core/SkillRuntime.cs').read_text(encoding='utf-8-sig')
def extract(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
parts=[extract(types,'public enum EquipmentMechanic'),extract(types,'public enum GemFormTrigger'),extract(types,'public static class GemFormRules'),extract(runtime,'public sealed class GemFormRuntime')]
code='using System;using System.Collections.Generic;\n'+ '\n'.join(parts)+'''
class MechanicAttachment{public bool mounted=true;public EquipmentMechanic mechanic;public int ascensionRank,variant;}
struct StatBlock{public float Damage,CritChance,CritDamageBonus,MaxHealth,Armor,DamageReduction,EnergyRecovery,CooldownReduction,MoveSpeed,LifeSteal;}
static class BuildCatalog{
'''+extract(types,'public static int GemAttribute(')+'''
public static bool IsAttributeGem(EquipmentMechanic g)=>g>=EquipmentMechanic.WeaponPower&&g<=EquipmentMechanic.RelicLeech;
public static int MechanicAttribute(EquipmentMechanic g)=>0;public static string GemAttributeLabel(EquipmentMechanic g)=>GemAttribute(g).ToString();}
class Program{
static int checks;static void C(bool b,string s){checks++;if(!b)throw new Exception(s);}
static float[] Fields(StatBlock s)=>new[]{s.Damage,s.CritChance,s.CritDamageBonus,s.MaxHealth,s.Armor,s.DamageReduction,s.EnergyRecovery,s.CooldownReduction,s.MoveSpeed,s.LifeSteal};
static void Main(){var basis=new StatBlock{Damage=100,MaxHealth=100,Armor=100,MoveSpeed=5};
for(int i=0;i<16;i++)for(int v=0;v<2;v++){
var g=(EquipmentMechanic)((int)EquipmentMechanic.WeaponPower+i);var t=GemFormRules.Trigger(g,v);var r=new GemFormRuntime();var a=new MechanicAttachment{mechanic=g,ascensionRank=2,variant=v};r.Configure(new List<MechanicAttachment>{a});
float hp=t==GemFormTrigger.LowHealth?.4f:1,energy=t==GemFormTrigger.LowEnergy?.2f:1;bool moving=t==GemFormTrigger.Moving;r.Trigger(t);
var actual=Fields(r.Evaluate(basis,hp,energy,moving));var old=Fields(basis);int attr=BuildCatalog.GemAttribute(g);
for(int f=0;f<10;f++)C(f==attr?actual[f]>old[f]:actual[f]==old[f],"own attribute only "+g+"/"+v+"/"+f);
C(GemFormRules.Trigger(g,0)!=GemFormRules.Trigger(g,1),"distinct triggers");C(t!=GemFormTrigger.Dodge,"no dodge activation");
if((int)t>=6){r.Advance(3);C(Fields(r.Evaluate(basis,hp,energy,moving))[attr]>old[attr],"active at 3s");r.Trigger(t);r.Advance(3);C(Fields(r.Evaluate(basis,hp,energy,moving))[attr]==actual[attr],"refresh without stacking");r.Advance(1);C(Fields(r.Evaluate(basis,hp,energy,moving))[attr]==old[attr],"expires at 4s");r.Trigger(t);r.Reset();C(Fields(r.Evaluate(basis,hp,energy,moving))[attr]==old[attr],"reset");}
a.mounted=false;r.Configure(new List<MechanicAttachment>{a});r.Trigger(t);C(Fields(r.Evaluate(basis,hp,energy,moving))[attr]==old[attr],"unmounted");
a.mounted=true;a.ascensionRank=0;r.Configure(new List<MechanicAttachment>{a});r.Trigger(t);C(Fields(r.Evaluate(basis,hp,energy,moving))[attr]==old[attr],"not ascended");}
Console.WriteLine("PASS "+checks+" gem form checks");}}
'''
# Extracted production classes remain public, so fixture data must also be public.
code=code.replace('class MechanicAttachment','public class MechanicAttachment').replace('struct StatBlock','public struct StatBlock')
with tempfile.TemporaryDirectory(prefix='gem-forms-') as d:
 p=Path(d);(p/'Program.cs').write_text(code);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');(p/'Check.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
 subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(p/'Check.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',MSBuildEnableWorkloadResolver='false'))
