#!/usr/bin/env python3
"""Run the production incremental physical-assets warmer with counted resources."""
from pathlib import Path
import os, subprocess, sys, tempfile

root=Path(__file__).resolve().parents[1]
source=(root/'Assets/Scripts/Combat/PhysicalSkillVfx.cs').read_text()
def method(marker):
 a=source.index(marker);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
fixture=r'''
using System;using Emberfall;using UnityEngine;
namespace UnityEngine {
 public class Object{public bool destroyed;public static void Destroy(Object value){if(value!=null)value.destroyed=true;}}
 public class MonoBehaviour:Object{}
 public class Shader:Object{}
 public class Mesh:Object{public Mesh(){Counts.Meshes++;}}
 public class Material:Object{public string name;public Material(Shader s){Counts.Materials++;}public Material(Material m){Counts.Materials++;}}
 public static class Resources{public static T Load<T>(string key)where T:new(){Counts.Loads++;return new T();}}
}
namespace Emberfall {
 public static class Counts{public static int Meshes,Materials,Loads;public static int Builds=>Meshes+Materials;}
 internal static class EnemySkillPhysicalVfx{static Mesh gel;static Mesh[] crust=new Mesh[3];internal static Mesh GelMesh=>gel??(gel=new Mesh());internal static Mesh CrustMesh(int v)=>crust[v]??(crust[v]=new Mesh());}
 internal static class AuthoredProjectileMeshes{static Mesh rock;internal static Mesh Load(string n)=>rock??(rock=new Mesh());}
 internal static class ProceduralVisuals{static Mesh rock;internal static Mesh WeatheredRock=>rock??(rock=new Mesh());}
 internal static class SkillBodyMaterialLibrary{internal enum Matter{Rock,Mud}internal static void Apply(Material m,Matter matter){}}
 internal class PhysicalSkillVfx:MonoBehaviour{private static Material stone,liquid;private static bool assetsReady;METHODS public static void ResetBoundary()=>Reset();}
}
class Program {
static int checks;static void Check(bool v,string message){checks++;if(!v)throw new Exception(message);}
static void Main(){int slices=0;var first=PhysicalSkillVfx.PrewarmAssets();while(true){int before=Counts.Builds;bool pending=first.MoveNext();Check(Counts.Builds-before<=1,"each loading slice builds at most one physical asset");if(!pending)break;slices++;}
Check(slices==7&&Counts.Meshes==5&&Counts.Materials==2,"gel, three crusts, projectile rock and two contact materials are prewarmed before gameplay");int builds=Counts.Builds,loads=Counts.Loads;
Check(!PhysicalSkillVfx.PrewarmAssets().MoveNext()&&Counts.Builds==builds&&Counts.Loads==loads,"complete physical warmup is a zero-work fast path");
PhysicalSkillVfx.ResetBoundary();var interrupted=PhysicalSkillVfx.PrewarmAssets();Check(interrupted.MoveNext(),"cancelled load can stop after a cached mesh slice");
var resumed=PhysicalSkillVfx.PrewarmAssets();while(resumed.MoveNext()){}Check(Counts.Meshes==5&&Counts.Materials==4,"restart reuses all mesh caches and only rebuilds reset materials");Check(!PhysicalSkillVfx.PrewarmAssets().MoveNext(),"resumed warmup restores its immediate completion fast path");Console.WriteLine("PASS "+checks+" production physical-resource prewarm slice/cache/cancellation checks; resource construction boundaries.");}}
'''
fixture=fixture.replace('METHODS','\n'.join(method(k) for k in ['        private static void Reset()', '        private static void StoneMaterial()', '        private static void LiquidMaterial()', '        internal static System.Collections.IEnumerator PrewarmAssets()']))
with tempfile.TemporaryDirectory(prefix='physical-prewarm-') as directory:
 p=Path(directory);(p/'Test.cs').write_text(fixture);project=p/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(project)];subprocess.run(cmd,env=env,check=True)
 original=(p/'Test.cs').read_text();mutated=original.replace('for(int variant=0;variant<3;variant++){var crust=EnemySkillPhysicalVfx.CrustMesh(variant);yield return null;}','for(int variant=0;variant<3;variant++){var crust=EnemySkillPhysicalVfx.CrustMesh(variant);}')
 assert mutated!=original;(p/'Test.cs').write_text(mutated);result=subprocess.run(cmd,env=env,capture_output=True,text=True);assert result.returncode!=0 and 'each loading slice builds at most one physical asset' in result.stdout+result.stderr,(result.stdout,result.stderr)
 print('PASS removing the per-crust yield fails the actual resource-slice budget.')
