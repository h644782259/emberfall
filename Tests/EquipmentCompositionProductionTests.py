#!/usr/bin/env python3
"""Actual Hero/equipment/fashion builders with managed hierarchy/resource doubles; no Unity rendering."""
import os,sys,tempfile,subprocess,re
from pathlib import Path
harness_root=Path(__file__).resolve().parents[1]
root=Path(os.environ.get("EMBERFALL_TEST_SOURCE_ROOT",str(harness_root))).resolve()
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def extract(source,signature):
 start=source.index(signature);i=source.index('{',start)+1;depth=1
 while depth:
  depth+=(source[i]=='{')-(source[i]=='}');i+=1
 return source[start:i]
model=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
methods=['public static CombatModel Hero(', 'public void ApplyFashion(', 'public void ApplyEquipment(', 'private static string EquipmentKey(', 'private static void SetVisible(', 'private void SetBaseWeaponVisible(', 'private Transform GearRoot(', 'private Transform GlowingPart(', 'private void BuildEquipmentWeapon(', 'private void BuildEquipmentArmor(', 'private void BuildEquipmentRelic(', 'private void SummonerCrown(', 'private static CombatModel Create(', 'private Material Mat(', 'private Transform Part(', 'private Transform Joint(', 'private void Humanoid(', 'private void Cape(', 'private void BuildHero(', 'private void EnhanceHero(', 'private static Transform NewJoint(', 'private static void RemovePart(', 'private Transform ArticulateArm(', 'private Transform ArticulateLeg(', 'private Transform MeshPart(', 'private Transform Blade(', 'private Transform Tapered(', 'private void OnDestroy(']
fields=model[model.index('        private struct SurfaceKey'):model.index('        public int WeaponActionId')]
# Identity fields above are construction-compatible; remaining animation state isn't needed.
fields+='private float phase; private EnemyController enemyOwner; private bool treantCompanion;'
body='using System.Collections.Generic;using UnityEngine;namespace Emberfall {public sealed partial class CombatModel:MonoBehaviour {'+fields+'\n'.join(extract(model,m) for m in methods)+'}\n'+extract(model,'internal sealed class OwnedCombatMesh')+'}'
body+=(root/'Assets/Scripts/Combat/RearSilhouette.cs').read_text().replace('using UnityEngine;','')
types=(root/'Assets/Scripts/Core/GameTypes.cs').read_text()
data='using System;namespace Emberfall {'+'\n'.join(re.findall(r'public enum (?:HeroClass|ItemSlot|Rarity|FashionSlot|EquipmentMechanic)\s*\{[^}]*\}',types))+extract(types,'public class FashionData')+extract(types,'public class ItemData')+'}'
with tempfile.TemporaryDirectory(prefix='equipment-composition-') as temp:
 p=Path(temp)
 for folder,names in [('Core',['WeaponStructure','EquipmentAppearance','EquipmentAttachmentRecipe','CostumeRecipes','CostumeLayers']),('Combat',['CombatModel.Costumes','CombatModel.CostumeLayers','CombatModel.WeaponRig','CostumeMeshLibrary','ProceduralVisuals','VisualMeshRecipes'])]:
  for n in names:(p/(n+'.cs')).write_text((root/'Assets/Scripts'/folder/(n+'.cs')).read_text())
 # This suite owns the default procedural gear path only. Keep actual builder and
 # WeaponRig calls intact, but explicitly forbid enabling the unrelated optional
 # imported visual boundary. Imported-rig behavior needs its own production suite.
 if (root/'Assets/Scripts/Combat/CombatModel.BlenderPilot.cs').exists():
  (p/'OptionalPilotBoundary.cs').write_text((harness_root/'Tests/EquipmentCompositionPilotBoundary.cs').read_text())
  print('SCOPE: optional Blender visual disabled boundary; no imported-rig coverage',flush=True)
 (p/'Model.cs').write_text(body);(p/'Types.cs').write_text(data)
 (p/'Fixture.cs').write_text((root/'Tests/EquipmentCompositionProductionTests.Fixture.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0169</NoWarn></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(cmd,env=env,check=True)
 original=(p/'Model.cs').read_text()
 mutations=[
  ('Cape(accent * .48f, hero);','Cape(accent * .48f, HeroClass.Vanguard);','actual Hero factory passes class to rear silhouette before EnhanceHero'),
  ('equipmentRelic.localPosition = Vector3.down * 1.12f;','equipmentRelic.localPosition = Vector3.zero;','actual relic vertices preserve face/chest/grip/back envelope'),
  ('new Vector3(.09f + look.Tier * .012f, weaponStructure.StaffShaftHalfLength, .09f + look.Tier * .012f)','new Vector3(.09f + look.Tier * .012f, weaponStructure.StaffShaftHalfLength * 1.5f, .09f + look.Tier * .012f)','T4 staff lower end stays at authored safe bound'),
  ('foreach(Material material in palette.Values) if(material!=null) Destroy(material);','foreach(Material material in palette.Values) if(material!=null) { }','model disposal releases hierarchy and own palette')]
 for before,after,expected in mutations:
  if original.count(before)!=1:raise AssertionError('negative control must match exactly once: '+before)
  (p/'Model.cs').write_text(original.replace(before,after))
  subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
  if result.returncode==0 or 'System.Exception: '+expected not in result.stdout+result.stderr:raise AssertionError(result.stdout+result.stderr)
 (p/'Model.cs').write_text(original)
 print('PASS: 4 compiled production negative controls fail exact attachment-offset, staff-length and owned-palette assertions')
