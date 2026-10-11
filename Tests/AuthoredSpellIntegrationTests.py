#!/usr/bin/env python3
"""Production chain with only Unity/scene APIs doubled; actual authored bytes and clipping execute."""
from pathlib import Path
import tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
s=(root/'Tests/FilledVfxAllocationTests.cs').read_text();s='using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;'+s[s.index('namespace Emberfall'):]
s=s.replace('    internal static class AuthoredSpellBases { internal static UnityEngine.Mesh Load(string name){return null;} internal static UnityEngine.Mesh Identity(string name){return null;} }','')
s=s.replace('public float Opacity;public void SetPropertyBlock(MaterialPropertyBlock block){Opacity=block.Opacity;}', 'public float Opacity,TintAlpha;public void SetPropertyBlock(MaterialPropertyBlock block){Opacity=block.Opacity;TintAlpha=block.TintAlpha;}').replace('public float Opacity;public void SetColor(string name,Color value){}','public float Opacity,TintAlpha=1;public void SetColor(string name,Color value){TintAlpha=value.a;}')
s=s.replace('public Vector3[] vertices;','public Vector3[] vertices,normals;').replace('public enum PrimitiveType{Sphere}','public enum PrimitiveType{Sphere,Capsule,Cube,Cylinder}')
s=s.replace('public static class Resources{public static T Load<T>(string name)where T:new()=>new T();}',r'''public class TextAsset{public byte[] bytes;}
public static class Resources{public static string Root,Bad;public static string BadName="Crystal",BadGroup="BlenderSpellBases";public static T Load<T>(string name)where T:new(){if(typeof(T)==typeof(TextAsset)){if(name==BadGroup+"/"+BadName&&Bad=="missing")return default(T);var path=Path.Combine(Root,name+".bytes");if(!File.Exists(path))return default(T);return (T)(object)new TextAsset{bytes=name==BadGroup+"/"+BadName&&Bad=="malformed"?new byte[]{1,2,3}:File.ReadAllBytes(path)};}return new T();}}''')
with tempfile.TemporaryDirectory(prefix='spell-integration-') as d:
 p=Path(d);(p/'Stubs.cs').write_text(s)
 for f in ['Assets/Scripts/Core/FilledVfxRecipes.cs','Assets/Scripts/Core/FilledVfxPlacement.cs','Assets/Scripts/Core/CombatVisualBudget.cs','Assets/Scripts/Combat/CombatVisualLease.cs','Assets/Scripts/Combat/AnchoredImpactMesh.cs','Assets/Scripts/Combat/FilledSkillVfx.cs','Assets/Scripts/Combat/AuthoredActorMeshes.cs','Assets/Scripts/Combat/AuthoredSpellBases.cs','Tests/AuthoredSpellIntegrationTests.cs']:(p/Path(f).name).write_text((root/f).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources')]+([os.environ.get('SPELL_SAMPLE_OUTPUT',''),os.environ['IDENTITY_SAMPLE_OUTPUT']] if os.environ.get('IDENTITY_SAMPLE_OUTPUT') else [os.environ['SPELL_SAMPLE_OUTPUT']] if os.environ.get('SPELL_SAMPLE_OUTPUT') else []),env=env,check=True)
 # Negative controls prove this suite needs real resource wiring, clipping and tier budgets.
 for filename,old,new,expected in [
  ('AuthoredSpellBases.cs','return data==null?null:','return true?null:','real loaded anchored landing'),
  ('FilledSkillVfx.cs','ReducedEffects?FilledVfxRecipes.ReducedParts','ReducedEffects?FilledVfxRecipes.MaximumParts','actual authored tier cap'),
  ('AnchoredImpactMesh.cs','Vector3 clipped=CombatSight.BoundaryPoint(CombatSightKind.Area,origin,world);','Vector3 clipped=world;','authored anchored vertex stays on visible side')]:
  target=p/filename;original=target.read_text();mutated=original.replace(old,new)
  if filename=='AnchoredImpactMesh.cs':mutated=mutated.replace('if(CombatSight.VisualTriangle(origin,a,b,c))','if(true)')
  assert mutated!=original;target.write_text(mutated)
  result=subprocess.run([sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources')],env=env,capture_output=True,text=True)
  target.write_text(original)
  assert result.returncode!=0 and expected in result.stdout+result.stderr,(filename,result.stdout,result.stderr)
  print('PASS negative control:',filename,expected)
