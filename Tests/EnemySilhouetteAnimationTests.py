#!/usr/bin/env python3
"""Full Enemy factories + actual Animate for preserved Slime/Wisp and new rigid silhouettes."""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
# Reuse the real-factory managed fixture setup; this suite supplies its own program.
ns={'__file__':str(root/'Tests/EnemyKnockdownGeometryTests.py')}
exec((root/'Tests/EnemyKnockdownGeometryTests.py').read_text().split('with tempfile.TemporaryDirectory')[0],ns)
source=ns['source'];member=ns['member'];output=ns['output'];dotnet=ns['dotnet']
with tempfile.TemporaryDirectory(prefix='enemy-f2-animation-') as directory:
 p=Path(directory)
 for name in ['ActorSilhouetteF1','CombatModel.Knockdown','EnemySilhouetteArt','AuthoredActorMeshes','ProceduralVisuals','VisualMeshRecipes']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Combat'/(name+'.cs')).read_text())
 (p/'LocomotionPoseState.cs').write_text((root/'Assets/Scripts/Core/LocomotionPoseState.cs').read_text())
 (p/'ThreatAdmissionPolicy.cs').write_text((root/'Assets/Scripts/Core/ThreatAdmissionPolicy.cs').read_text())
 (p/'Fields.cs').write_text((root/'Tests/EnemyKnockdownProductionTests.cs').read_text().split('public static class EnemyKnockdownProductionTests')[0])
 methods=['public static CombatModel Enemy(','private static CombatModel Create(','private Material Mat(','private Transform Part(','private Transform Joint(','private void Humanoid(','private static Transform NewJoint(','private static void RemovePart(','private Transform ArticulateArm(','private Transform ArticulateLeg(','private Transform MeshPart(','private Transform Tapered(','private void BuildEnemy(','private void EnhanceEnemy(','public void Animate(','public void Recoil(','private void ApplyRecoil(']
 fields=source[source.index('        private struct SurfaceKey'):source.index('        private TailoredCloth')]
 (p/'Factory.cs').write_text('using UnityEngine;using System.Collections.Generic;namespace Emberfall{public sealed partial class CombatModel{'+fields+''.join(member(source,m) for m in methods)+'}'+member(source,'internal sealed class OwnedCombatMesh')+'public partial class EnemyStatusEffects{'+member(ns['status'],'private void LateUpdate(')+'}}')
 (p/'Fixture.cs').write_text(ns['f']);(p/'Test.cs').write_text((root/'Tests/EnemySilhouetteAnimationTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0169;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([dotnet,'run','--project',str(project),'--',str(output)],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
