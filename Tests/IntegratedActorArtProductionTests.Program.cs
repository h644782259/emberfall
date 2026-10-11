using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Text.Json;using Emberfall;using UnityEngine;
class Program{
 static int count;static void Check(bool ok,string why){count++;if(!ok)throw new Exception(why);}
 static MeshFilter[] Visible(CombatModel m)=>m.GetComponentsInChildren<MeshFilter>(false).Where(x=>x.sharedMesh?.vertices!=null&&x.GetComponent<Renderer>().enabled).ToArray();
 static object Geo(string name,CombatModel m)=>new{name,pose=2,lines=m.GetComponentsInChildren<LineRenderer>(false).Where(l=>l.enabled).Select(l=>new{width=l.startWidth,color=new[]{l.sharedMaterial.color.r,l.sharedMaterial.color.g,l.sharedMaterial.color.b},points=l.points.Select(v=>{var w=l.transform.TransformPoint(v);return new[]{w.x,w.y,w.z};}).ToArray()}).ToArray(),parts=Visible(m).Select(x=>new{name=x.transform.name,mesh=x.sharedMesh.name,color=new[]{x.GetComponent<Renderer>().sharedMaterial.color.r,x.GetComponent<Renderer>().sharedMaterial.color.g,x.GetComponent<Renderer>().sharedMaterial.color.b},vertices=x.sharedMesh.vertices.Select(v=>{var w=x.transform.TransformPoint(v);if(!(float.IsFinite(w.x)&&float.IsFinite(w.y)&&float.IsFinite(w.z)))throw new Exception("finite integrated geometry");return new[]{w.x,w.y,w.z};}).ToArray(),triangles=x.sharedMesh.triangles}).ToArray()};
 static bool Has(CombatModel m,string prefix)=>Visible(m).Any(f=>f.sharedMesh.name.StartsWith(prefix));
 static void Reset(){foreach(var t in new[]{typeof(ActorSilhouetteF1),typeof(EnemySilhouetteArt),typeof(WeaponModules),typeof(AuthoredActorMeshes)})t.GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic)?.Invoke(null,null);}
 static CombatModel Hero(int h)=>CombatModel.Hero(new GameObject("hero").transform,(HeroClass)h);
 static CombatModel Enemy(EnemyKind k)=>CombatModel.Enemy(new GameObject("enemy").transform,k,false);
 static void Main(){var cases=new List<object>();
 for(int h=0;h<4;h++){var m=Hero(h);m.IntegratedSequence(Check);Check(Has(m,"Blender actor"),"original ActorModules remains visible");Check(Has(m,"Blender weapon"),"F3 real weapon visible");cases.Add(Geo(((HeroClass)h).ToString(),m));}
 foreach(var kind in new[]{SummonedCompanion.Kind.Spirit,SummonedCompanion.Kind.Treant}){var m=CombatModel.Companion(new GameObject("companion").transform,kind);m.SetCompanionAppearance(kind,3,true);UnityEngine.Object.Flush();m.IntegratedSequence(Check);Check(Has(m,"F1 silhouette"),"F1 companion visible");if(kind==SummonedCompanion.Kind.Spirit)Check(m.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Spirit wing").All(t=>t.parent.name=="Companion rigid body attachments"),"spirit rigid attachment");cases.Add(Geo(kind.ToString(),m));}
 foreach(var k in new[]{EnemyKind.Guardian,EnemyKind.Goblin}){var m=Enemy(k);m.IntegratedSequence(Check);Check(Has(m,"Enemy silhouette"),"F2 enemy visible");Check(Has(m,"Blender actor"),"F2 keeps original ActorModules");cases.Add(Geo(k.ToString(),m));}
 // Each missing/corrupt layer must preserve live geometry and unrelated authored layers.
 foreach(bool corrupt in new[]{false,true})foreach(string resource in new[]{"ActorSilhouettes/F1/Hood","EnemySilhouettes/GuardianChest","WeaponModules/StarGuard","VanguardActions/Swordguard","ActorModules/Cuirass"}){
  Resources.Missing=corrupt?null:resource;Resources.Corrupt=corrupt?resource:null;Reset();
  var ranger=Hero(2);var guardian=Enemy(EnemyKind.Guardian);var vanguard=Hero(0);
  Check(Visible(ranger).Length>15&&Visible(guardian).Length>15&&Visible(vanguard).Length>15,"missing layer preserves visible body");
  Check(Has(ranger,"Blender weapon")&&Has(ranger,"Blender actor"),"F1 failure preserves F3 and original mesh");
  if(resource.EndsWith("Cuirass"))Check(!Visible(guardian).Any(f=>f.sharedMesh.name=="Blender actor Cuirass")&&Has(guardian,"Enemy silhouette"),"original missing cuirass retains F2 overlay");
  if(resource.EndsWith("Hood"))Check(!Visible(ranger).Any(f=>f.transform.name=="Forest Hood"&&f.sharedMesh.name.StartsWith("F1 silhouette")),"missing hood falls back at same socket");
  else Check(Visible(ranger).Any(f=>f.transform.name=="Forest Hood"&&f.sharedMesh.name.StartsWith("F1 silhouette")),"unrelated F1 hood remains");
  if(resource.EndsWith("GuardianChest"))Check(!Visible(guardian).Any(f=>f.sharedMesh.name=="Enemy silhouette / GuardianChest")&&Has(guardian,"Enemy silhouette"),"F2 missing chest preserves other modules");
  if(resource.EndsWith("StarGuard"))Check(!Has(vanguard,"Blender weapon Star"),"F3 atomic incomplete sword fallback");
  else Check(Visible(vanguard).Count(f=>f.sharedMesh.name.StartsWith("Blender weapon Star"))==4,"unrelated F3 complete sword remains");
  if(resource.EndsWith("Swordguard")){var field=typeof(CombatModel).GetField("vanguardArt",BindingFlags.Instance|BindingFlags.NonPublic);Check(field.GetValue(vanguard)==null,"missing Vanguard library retains procedural action");vanguard.PlayAction(0,true,.46f);Check(Visible(vanguard).Length>15,"missing motion library still commits action");}
 }
 Resources.Missing=Resources.Corrupt=null;Reset();System.IO.File.WriteAllText(@"OUTPUT_PATH",JsonSerializer.Serialize(cases));Console.WriteLine("PASS "+count+" integrated assertions: all real F1/F2/F3/ActorModules/Vanguard layers, four class sequential gear/fashion/contact/cancel/recovery, companions/enemies and missing/corrupt isolation; managed TRS NOT Unity");
 }
}
