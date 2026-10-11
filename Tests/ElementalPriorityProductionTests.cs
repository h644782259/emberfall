using System;using System.Linq;using UnityEngine;using Emberfall;
namespace Emberfall
{
 public enum EnemyKind{Slime,Other}public sealed class EnemyController:MonoBehaviour{public bool IsDead,IsBoss;public EnemyKind Kind;public EnemyStatusEffects StatusEffects;}
 public static class MobileControls{public static bool Active=true;}public static class WorldTraversal{public static int Revision;}
 public static class ProceduralVisuals{public static GameObject Create(string name,PrimitiveType type,Material material){var o=new GameObject(name);o.AddComponent<MeshRenderer>().sharedMaterial=material;return o;}}
 public sealed class FadingCombatEffect:MonoBehaviour{public void Setup(LineRenderer l,Color c,float s,float d,bool b){}}
}
public static class ElementalPriorityProductionTests
{
 static int checks;static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static EnemyController Enemy(string name){var enemy=new GameObject(name).AddComponent<EnemyController>();enemy.StatusEffects=enemy.gameObject.AddComponent<EnemyStatusEffects>();enemy.StatusEffects.SetTimers(2,2,1);return enemy;}
 static GameObject[] Fields()=>GameObject.All.Where(o=>!o.Destroyed&&o.activeInHierarchy&&o.GetComponent<ElementalFieldVisual>()!=null).ToArray();
 static GameObject[] Particles()=>GameObject.All.Where(o=>!o.Destroyed&&o.activeInHierarchy&&o.GetComponent<ParticleSystem>()!=null).ToArray();
 static void Reset(){foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);Check(CombatVisualLease.Active==0,"cleanup returns every global lease");GameObject.All.Clear();Time.time=0;Application.isMobilePlatform=true;EffectPreferences.ReducedEffects=false;}
 static void Fill(int count,CombatVisualPriority p){for(int i=0;i<count;i++)Check(CombatVisualLease.Attach(new GameObject("filler"),p)!=null,"filler lease accepted");}
 public static string Run()
 {
  Reset();Fill(20,CombatVisualPriority.Decoration);var root=new GameObject("area");ElementalCombatVfx.Area(root.transform,3,ElementalCombatVfx.Element.Poison);
  Check(Fields().Length==1,"full decoration pool must retain actual Area primary field");Check(Particles().Length==0&&CombatVisualLease.Active==20,"failed optional particle cannot displace field or exceed budget");
  root.SetActive(false);Check(CombatVisualLease.Active==19,"parent disable immediately releases field lease");root.SetActive(true);Check(CombatVisualLease.Active==20,"parent reenable reacquires field lease once");UnityEngine.Object.Destroy(root);Check(CombatVisualLease.Active==19,"destroy after disable is idempotent");
  Reset();Fill(20,CombatVisualPriority.Decoration);var enemy=Enemy("enemy");ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Fire,2);
  Check(Fields().Length==1&&Particles().Length==0,"full decoration pool retains actual OnEnemy body shape without particles");Time.time=3;enemy.gameObject.Call("Update");Check(Fields().Length==1&&CombatVisualLease.Active==20,"bound live status owns aura expiry across wall-clock advance");enemy.StatusEffects.SetTimers(0,2,1);enemy.gameObject.Call("Update");Check(Fields().Length==0&&CombatVisualLease.Active==19,"expired actual burn query retires body shape even without particles");
  Reset();enemy=Enemy("enemy");ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Poison,2);var shape=Fields().Single();var particle=Particles().Single();var material=particle.GetComponent<ParticleSystemRenderer>().sharedMaterial;
  Check(shape.transform.parent==enemy.transform&&particle.transform.parent==enemy.transform,"aura main shape must be sibling of optional particles");
  UnityEngine.Object.Destroy(particle);Check(!shape.Destroyed&&shape.activeInHierarchy&&CombatVisualLease.Active==1&&material.Destroyed,"optional particle destruction retains shape and releases owned material");
  enemy.gameObject.SetActive(false);Check(CombatVisualLease.Active==0,"enemy disable releases child shape lease immediately");UnityEngine.Object.Destroy(enemy.gameObject);Check(shape.Destroyed&&CombatVisualLease.Active==0,"enemy destroy retires all visual children once");
  Reset();enemy=Enemy("eviction enemy");ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Poison,2);shape=Fields().Single();particle=Particles().Single();Fill(18,CombatVisualPriority.Primary);
  Check(CombatVisualLease.Attach(new GameObject("finisher"),CombatVisualPriority.Finale)!=null,"finisher preempts a full pool");
  Check(particle.Destroyed&&!shape.Destroyed&&shape.activeInHierarchy&&CombatVisualLease.Active==20,"budget eviction of optional child cannot destroy main sibling");
  Check(CombatVisualLease.Attach(new GameObject("second finisher"),CombatVisualPriority.Finale)!=null&&shape.Destroyed&&CombatVisualLease.Active==20,"later higher priority eviction releases body shape exactly once");
  Reset();Fill(20,CombatVisualPriority.Finale);root=new GameObject("blocked area");ElementalCombatVfx.Area(root.transform,3,ElementalCombatVfx.Element.Poison);enemy=Enemy("blocked enemy");ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Fire,2);
  Check(Fields().Length==0&&Particles().Length==0&&CombatVisualLease.Active==20,"higher priority saturation fails closed without leaked child leases");Reset();
  // Actual per-kind particle cap also must not block a new primary shape.
  EffectPreferences.ReducedEffects=true;for(int i=0;i<10;i++)Check(ElementalCombatVfx.Create(null,"particle",ElementalCombatVfx.Element.Poison,10,1)!=null,"reduced particle quota fill");root=new GameObject("area");ElementalCombatVfx.Area(root.transform,3,ElementalCombatVfx.Element.Poison);Check(Fields().Length==1&&Particles().Length==10,"local particle cap never gates field silhouette");Reset();
  Reset();enemy=Enemy("dual status enemy");ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Fire,2);ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Poison,2);
  Check(Fields().Length==2&&Particles().Length==2,"independent burn and poison channels exist");enemy.StatusEffects.SetTimers(2,0,3);enemy.gameObject.Call("Update");
  Check(enemy.StatusEffects.PoisonStacks==0&&enemy.StatusEffects.IsBurning&&Fields().Length==1&&Particles().Length==1&&CombatVisualLease.Active==2,"expired poison time overrides stale stacks and preserves fire channel");
  enemy.StatusEffects.SetTimers(2,2,1);ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Poison,2);Check(Fields().Length==2&&Particles().Length==2,"same aura can reacquire cleared status channel");enemy.IsDead=true;enemy.gameObject.Call("Update");Check(Fields().Length==0&&Particles().Length==0&&CombatVisualLease.Active==0,"dead status owner retires both channels");Reset();
  enemy=Enemy("missing status enemy");ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Poison,2);enemy.StatusEffects=null;enemy.gameObject.Call("Update");Check(Fields().Length==0&&Particles().Length==0&&CombatVisualLease.Active==0,"missing bound status fails closed without lease leak");Reset();
  return "PASS: "+checks+" actual elemental entry/lease/lifecycle checks (managed Unity substitute, not rendered)";
 }
}
