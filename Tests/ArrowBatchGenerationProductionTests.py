#!/usr/bin/env python3
"""Actual retained sequence fields/Configure/Update/arrow event/OnDisable against actual pooled VFX."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import tempfile,subprocess,sys,os
root=Path(__file__).resolve().parents[1]
from SustainedArrowBodyProductionTests import shell,boundaries,fixture
shell=shell.replace('public Vector3[] vertices;', 'public Vector3[] vertices,normals;')
shell=shell.replace('public static class Resources{','public static class Resources{public static string Root;')
with tempfile.TemporaryDirectory(prefix='arrow-batch-generation-') as d:
 p=Path(d);(p/'Stubs.cs').write_text(shell);(p/'SequenceProbe.cs').write_text(fixture);(p/'ResourceBoundaries.cs').write_text('using System;using System.Linq;using System.Collections.Generic;using UnityEngine;'+boundaries)
 for f in ['Assets/Scripts/Core/CombatImpactBatch.cs','Assets/Scripts/Core/SkillDamageBudgets.cs','Assets/Scripts/Combat/PhysicalSkillMotion.cs','Assets/Scripts/Core/FilledVfxRecipes.cs','Assets/Scripts/Core/FilledVfxPlacement.cs','Assets/Scripts/Core/CombatVisualBudget.cs','Assets/Scripts/Combat/CombatVisualLease.cs','Assets/Scripts/Combat/AnchoredImpactMesh.cs','Assets/Scripts/Combat/FilledSkillVfx.cs','Tests/ArrowBatchGenerationProductionTests.cs']:(p/Path(f).name).write_text((root/f).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(p/'Test.csproj');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');command=[sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources')]
 subprocess.run(command,env=env,check=True)
 target=p/'FilledSkillVfx.cs';original=target.read_text();guard='effect.rentGeneration==generation &&';assert guard in original;target.write_text(original.replace(guard,''));result=subprocess.run(command,env=env,capture_output=True,text=True)
 assert result.returncode!=0 and 'stale sequence Update cannot mutate new rental snapshot' in result.stdout+result.stderr,(result.stdout,result.stderr)
 print('PASS removing only rental-generation guard fails actual stale sequence Update/full snapshot despite same owner and same castId.')

 target.write_text(original.replace('if(fx.rentGeneration==generation)fx.Retire();','fx.Retire();'));result=subprocess.run(command,env=env,capture_output=True,text=True)
 assert result.returncode!=0 and 'stale lease callback cannot mutate new rental snapshot' in result.stdout+result.stderr,(result.stdout,result.stderr)
 print('PASS removing lease callback generation guard fails full protected new-rental snapshot.')
