using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using UnityEngine;
using Emberfall;
namespace Emberfall { public sealed partial class CombatModel { public void F2AdvanceWalk(){locomotion.Advance(0,.045f,.016f,3,true,false,0);} } }
class EnemySilhouetteAnimationTests
{
 static int checks;
 static void C(bool value,string text){checks++;if(!value)throw new Exception(text);}
 static void Main(string[] args)
 {
  var cases=new List<object>();
  foreach(bool enabled in new[]{false,true})foreach(var kind in new[]{EnemyKind.Slime,EnemyKind.Wisp,EnemyKind.Goblin,EnemyKind.Guardian})foreach(bool boss in kind==EnemyKind.Guardian?new[]{false,true}:new[]{false})
  {
   EnemySilhouetteArt.Enabled=enabled;var root=new GameObject("Enemy");var owner=root.AddComponent<EnemyController>();owner.Kind=kind;owner.IsBoss=boss;owner.StatusEffects=root.AddComponent<EnemyStatusEffects>();var model=CombatModel.Enemy(root.transform,kind,boss);UnityEngine.Object.Flush();
   var filters=model.GetComponentsInChildren<MeshFilter>(false).Where(f=>f.GetComponent<Renderer>().enabled).ToArray();var meshes=filters.Select(f=>f.sharedMesh).ToArray();var materials=filters.Select(f=>f.GetComponent<Renderer>().sharedMaterial).ToArray();
   foreach(string phase in new[]{"walk","windup","contact","recovery"})
   {
    model.Attack(phase=="windup"?EnemyPosePhase.Windup:phase=="contact"||phase=="recovery"?EnemyPosePhase.Recovery:EnemyPosePhase.Idle,phase=="recovery"?.8f:phase=="windup"?.8f:0);
    for(int i=0;i<12;i++){Time.frameCount++;Time.deltaTime=.016f;Time.time=i*.016f;if(phase=="walk")model.F2AdvanceWalk();model.Animate(.7f,phase=="contact"?.7f:0,false);C(meshes.SequenceEqual(filters.Select(f=>f.sharedMesh)),"animation never swaps authored body by clip");C(materials.SequenceEqual(filters.Select(f=>f.GetComponent<Renderer>().sharedMaterial)),"animation preserves palette ownership");}
    if(kind==EnemyKind.Slime||kind==EnemyKind.Wisp)C(!meshes.Any(m=>m.name.StartsWith("Enemy silhouette / ")),"preserved species never acquire humanoid F2 modules");
    var parts=filters.Select(f=>new{name=f.gameObject.name,mesh=f.sharedMesh.name,color=new[]{f.GetComponent<Renderer>().sharedMaterial.color.r,f.GetComponent<Renderer>().sharedMaterial.color.g,f.GetComponent<Renderer>().sharedMaterial.color.b},vertices=f.sharedMesh.vertices.Select(v=>{var w=f.transform.TransformPoint(v);C(float.IsFinite(w.x)&&float.IsFinite(w.y)&&float.IsFinite(w.z),"animated actual factory vertex finite");return new[]{w.x,w.y,w.z};}).ToArray(),triangles=f.sharedMesh.triangles}).ToArray();
    cases.Add(new{name=kind+(boss?"Boss":"")+"-"+phase+(enabled?"-after":"-before"),origin=new[]{0,0,0},parts});
   }
  }
  File.WriteAllText(args[0],JsonSerializer.Serialize(cases));Console.WriteLine("PASS "+checks+" actual Enemy factory + actual Animate assertions: five identities, four phases, F2 on/off, shared meshes and retained Slime/Wisp; managed, not Unity");
 }
}
