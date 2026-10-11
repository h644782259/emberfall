using System;using System.Linq;using System.Reflection;using System.Collections.Generic;using System.Text.Json;using UnityEngine;using Emberfall;
class DefenseIdentityTests
{
 static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 static readonly List<object> samples=new List<object>();
 static PlayerController New()
 {
  foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);UnityEngine.Object.Flush();
  typeof(FilledSkillVfx).GetMethod("ResetAssets",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);UnityEngine.Object.Flush();
  typeof(AdvancedSkillVfx).GetMethod("ResetCount",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
  typeof(CombatVisualLease).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
  Time.deltaTime=.01f;Resources.Bad=null;var hero=new GameObject("hero").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=hero,HasStarted=true};return hero;
 }
 static AdvancedSkillVfx Anchor()=>GameObject.All.Last(o=>!o.Destroyed&&o.activeInHierarchy&&o.GetComponent<AdvancedSkillVfx>()!=null).GetComponent<AdvancedSkillVfx>();
 static FilledSkillVfx Child(AdvancedSkillVfx anchor)=>GameObject.All.Single(o=>!o.Destroyed&&o.transform.parent==anchor.transform&&o.GetComponent<FilledSkillVfx>()!=null).GetComponent<FilledSkillVfx>();
 static void Sample(string label,AdvancedSkillVfx anchor)
 {
  var fx=Child(anchor);Time.deltaTime=.75f;fx.gameObject.Call("Update");
  var objects=GameObject.All.Where(o=>!o.Destroyed&&o.activeInHierarchy&&o.transform.parent==fx.transform&&o.GetComponent<MeshFilter>()!=null).Select(o=>new {label=o.name,vertices=o.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>{var w=o.transform.TransformPoint(v);return new[]{w.x,w.y,w.z};}).ToArray(),triangles=o.GetComponent<MeshFilter>().sharedMesh.triangles,opacity=o.GetComponent<MeshRenderer>().Opacity}).ToArray();
  var vertices=objects.SelectMany(o=>o.vertices).ToArray();
  Check(vertices.Max(v=>v[1])>3f&&vertices.Max(v=>Math.Abs(v[0]))>1.3f,"persistent protection has body envelope clear above head and shoulders");
  samples.Add(new {kind="ProtectionCage",phase=label,objects});
 }
 static void Main(string[] args)
 {
  Resources.Root=args[0];var color=new Color(1,.8f,.3f,1);
  for(int guard=0;guard<3;guard++)for(int rank=1;rank<=3;rank++){
   var h=New();h.Specialization=ElementalistSpecialization.Burn;
   if(guard==0)h.Guard0(rank,1,1,color,23);else if(guard==1)h.Guard1(rank,1,1,color,23);else h.Guard2(rank,1,1,color,23);
   var anchor=Anchor();var child=Child(anchor);
   Check(h.guardTime==6+(rank-1)*2&&h.guardCastId==23,"actual guard time and receipt unchanged");
   var part=GameObject.All.First(o=>o.transform.parent==child.transform&&o.GetComponent<MeshFilter>()!=null);
   Check(part.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Skill identity / ProtectionCage"),"actual defense selects loaded ProtectionCage");
   if(rank==1)Sample("Guard-"+guard,anchor);
   GameSession.Instance.InputBlocked=true;Time.deltaTime=0;anchor.gameObject.Call("Update");Check(anchor.gameObject.activeSelf,"pause retains active true state");
   h.guardTime=0;anchor.gameObject.Call("Update");
   Check(!anchor.gameObject.activeSelf&&!child.gameObject.activeInHierarchy,"state cancellation hides hierarchy before deferred destruction");
   Check(CombatVisualLease.Active==0,"state cancellation releases actual child lease immediately");
   UnityEngine.Object.Flush();
  }
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){
   var h=New();h.HeroClass=hero;h.TriggerPassive();Check(!GameObject.All.Any(o=>!o.Destroyed&&o.activeInHierarchy&&o.GetComponent<AdvancedSkillVfx>()!=null),"unlearned passive never creates cast visual");
   GameSession.Instance.Progression.Profile.skillRanks[8]=3;h.TriggerPassive();var a=Anchor();Check(h.passiveTime==5&&h.passiveCooldown==30,"true passive duration cooldown unchanged");
   Sample("Passive-"+hero+"-true-trigger",a);
   int count=GameObject.All.Count(o=>!o.Destroyed&&o.activeInHierarchy&&o.GetComponent<AdvancedSkillVfx>()!=null);h.TriggerPassive();Check(count==GameObject.All.Count(o=>!o.Destroyed&&o.activeInHierarchy&&o.GetComponent<AdvancedSkillVfx>()!=null),"cooldown never manufactures repeated passive casts");
   h.passiveTime=0;Time.deltaTime=0;a.gameObject.Call("Update");Check(!a.gameObject.activeSelf,"true passive end hides protection");
  }
  {var h=New();h.Guard1(1,1,1,color,1);var old=Anchor();h.Guard1(2,1,1,color,2);var fresh=Anchor();Check(!ReferenceEquals(old,fresh)&&!old.gameObject.activeSelf,"refresh replaces only prior guard anchor");h.HeroClass=HeroClass.Arcanist;GameSession.Instance.Progression.Profile.skillRanks[8]=1;h.TriggerPassive();var passive=Anchor();Check(fresh.gameObject.activeSelf&&passive.gameObject.activeSelf,"passive and guard own independent channels");h.transform.position=new Vector3(2,0,3);Time.deltaTime=.1f;fresh.gameObject.Call("Update");Check(fresh.transform.position.x==2&&fresh.transform.position.z==3,"active defense follows moving owner");h.guardTime=0;Time.deltaTime=0;fresh.gameObject.Call("Update");h.Guard1(1,1,1,color,3);var reapplied=Anchor();UnityEngine.Object.Flush();Check(reapplied.gameObject.activeSelf&&!reapplied.gameObject.Destroyed&&passive.gameObject.activeSelf,"same frame reapply survives prior anchor destruction without canceling passive");h.passiveCooldown=0;h.TriggerPassive();Check(!passive.gameObject.activeSelf&&reapplied.gameObject.activeSelf,"passive refresh replaces its own channel only");}
  // Reuse a returned child in the same frame, then destroy its old nonpooled anchor.
  {var h=New();h.Guard1(1,1,1,color,1);var old=Anchor();var returned=Child(old);returned.Retire();FilledSkillVfx.Impact(h,Vector3.zero,1,FilledVfxKind.Ice,color);var live=GameObject.All.Last(o=>!o.Destroyed&&o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null).GetComponent<FilledSkillVfx>();Check(ReferenceEquals(returned,live),"fixture actually reuses pooled former child");h.guardTime=0;Time.deltaTime=0;old.gameObject.Call("Update");UnityEngine.Object.Flush();Check(!live.gameObject.Destroyed&&live.gameObject.activeInHierarchy,"old anchor destroy cannot affect rerented detached child");}
  foreach(bool epoch in new[]{true,false}){var h=New();h.Guard1(1,1,1,color,1);var a=Anchor();GameSession.Instance.InputBlocked=true;Time.deltaTime=0;if(epoch)h.CombatEpoch++;else h.IsDead=true;a.gameObject.Call("Update");Check(!a.gameObject.activeSelf&&CombatVisualLease.Active==0,"epoch or death revokes defense while paused");}
  {var h=New();var a=AdvancedSkillVfx.Rune(h,Vector3.zero,2.1f,color,6,1,true,4);var fx=Child(a);Time.deltaTime=.75f;fx.gameObject.Call("Update");var v=GameObject.All.Where(o=>o.transform.parent==fx.transform&&o.GetComponent<MeshFilter>()!=null).SelectMany(o=>o.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>o.transform.TransformPoint(p))).ToArray();Check(v.Max(p=>p.y)<1.7f,"ordinary identity4 preparation dimensions remain unchanged");}
  {var h=New();Application.isMobilePlatform=true;EffectPreferences.ReducedEffects=true;h.Guard1(1,1,1,color,1);var a=Anchor();var fx=Child(a);Check(GameObject.All.Count(o=>o.transform.parent==fx.transform&&o.GetComponent<MeshFilter>()!=null)==1,"mobile reduced protection retains exactly one mesh part");h.guardTime=0;a.gameObject.Call("Update");Check(CombatVisualLease.Active==0,"low tier envelope follows state lifetime");Application.isMobilePlatform=false;EffectPreferences.ReducedEffects=false;}
  // Missing identity keeps original procedural charge fallback and its state contract.
  {var h=New();Resources.Bad="missing";Resources.BadName="ProtectionCage";Resources.BadGroup="BlenderSkillIdentities";h.Guard1(1,1,1,color,1);var a=Anchor();Check(Child(a)!=null,"missing defense identity keeps fallback");h.guardTime=0;a.gameObject.Call("Update");Check(!a.gameObject.activeSelf,"fallback also obeys state cancellation");}
  System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(args[1]));System.IO.File.WriteAllText(args[1],JsonSerializer.Serialize(samples));
  Console.WriteLine("PASS actual 3 guard blocks x 3 ranks and 4 passive true triggers -> actual ProtectionCage mesh; paused/state end/death/epoch; immediate lease release; same-frame child rerent isolated; missing asset fallback. Managed not Unity.");
 }
}
