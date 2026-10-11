using System;using System.IO;using System.Linq;using System.Reflection;using Emberfall;using UnityEngine;
class AuthoredSpellIntegrationTests
{
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static readonly BindingFlags Static=BindingFlags.NonPublic|BindingFlags.Static,Instance=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Reset(){foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();typeof(FilledSkillVfx).GetMethod("ResetAssets",Static).Invoke(null,null);typeof(CombatVisualLease).GetMethod("Reset",Static).Invoke(null,null);}
 static PlayerController Start(bool mobile,bool reduced,float wall=float.PositiveInfinity){Reset();Application.isMobilePlatform=mobile;EffectPreferences.ReducedEffects=reduced;CombatSight.Wall=wall;var hero=new GameObject("Hero").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=hero,HasStarted=true};return hero;}
 static FilledSkillVfx Cast(PlayerController hero,FilledVfxKind kind){FilledSkillVfx.Impact(hero,Vector3.zero,4,kind,new Color(1,1,1));return GameObject.All.Last(o=>o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null).GetComponent<FilledSkillVfx>();}
 static GameObject[] Pieces(FilledSkillVfx fx)=>GameObject.All.Where(o=>o.transform.parent==fx.transform&&o.GetComponent<MeshFilter>()!=null).ToArray();
 static Mesh Cached(string field)=>(Mesh)typeof(FilledSkillVfx).GetField(field,Static).GetValue(null);
 static void Tick(FilledSkillVfx fx,float dt){Time.deltaTime=dt;Time.unscaledDeltaTime=dt;typeof(FilledSkillVfx).GetMethod("Update",Instance).Invoke(fx,null);}
 static void Main(string[] args)
 {
  Resources.Root=args[0];
  foreach(bool mobile in new[]{false,true})foreach(bool reduced in new[]{false,true})foreach(float wall in new[]{float.PositiveInfinity,.25f})foreach(var kind in new[]{FilledVfxKind.Ice,FilledVfxKind.Fire,FilledVfxKind.Sword,FilledVfxKind.Lightning,FilledVfxKind.Summon,FilledVfxKind.Arcane})
  {
   var hero=Start(mobile,reduced,wall);var fx=Cast(hero,kind);var pieces=Pieces(fx);int cap=reduced?7:mobile?10:14;
   Check(pieces.Length<=cap,"actual authored tier cap");Check(pieces[0].name.EndsWith("Landing base")&&pieces[1].name.EndsWith("Primary "+kind)&&pieces[2].name.EndsWith("Contact flash"),"actual authored primary ordering");
   Check(pieces[0].GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Authored spell / Rupture"),"real loaded anchored landing");
   string key=kind==FilledVfxKind.Ice?"IcePrimary":kind==FilledVfxKind.Fire?"FirePrimary":kind==FilledVfxKind.Sword?"Sword":kind==FilledVfxKind.Lightning?"ForkPulse":kind==FilledVfxKind.Summon?"ContractSigil":null;
   if(key!=null)Check(pieces[1].GetComponent<MeshFilter>().sharedMesh.name.StartsWith((kind==FilledVfxKind.Lightning||kind==FilledVfxKind.Summon?"Skill identity / ":"Authored spell / ")+key),"real authored primary resource");
   var owned=pieces.Take(3).Select(p=>p.GetComponent<MeshFilter>().sharedMesh).ToArray();var source=Cached("rupture");Check(!source.Destroyed,"cached source alive");
   foreach(float t in new[]{0f,.12f,.35f,.65f})
   {
    typeof(FilledSkillVfx).GetField("age",Instance).SetValue(fx,t);Tick(fx,0);
    // Force production Animate through a positive update, checking real clipped vertices.
    Tick(fx,.001f);
    foreach(var p in pieces.Take(3))foreach(var v in p.GetComponent<MeshFilter>().sharedMesh.vertices){var world=p.transform.TransformPoint(v);Check(world.x<=wall+.001f,"authored anchored vertex stays on visible side throughout motion");}
   }
   GameSession.Instance.InputBlocked=true;float old=(float)typeof(FilledSkillVfx).GetField("age",Instance).GetValue(fx);Tick(fx,.1f);Check((float)typeof(FilledSkillVfx).GetField("age",Instance).GetValue(fx)==old,"authored pause");GameSession.Instance.InputBlocked=false;
   hero.CombatEpoch++;Tick(fx,.1f);Check(!fx.gameObject.activeSelf&&typeof(FilledSkillVfx).GetField("owner",Instance).GetValue(fx)==null,"authored epoch retires");Check(owned.All(m=>m.Destroyed),"anchored owned meshes destroyed on retire");Check(!source.Destroyed,"retire preserves shared source mesh");Check(CombatVisualLease.Active==0,"retire releases lease");
   typeof(FilledSkillVfx).GetMethod("ResetAssets",Static).Invoke(null,null);Check(source.Destroyed,"reset destroys cached authored source");
  }
  foreach(bool reduced in new[]{false,true}){var h=Start(true,reduced);for(int i=0;i<40;i++)Cast(h,FilledVfxKind.Ice);Check(CombatVisualLease.Active<= (reduced?12:20),"authored global lease cap");Check(GameObject.All.Count(o=>o.GetComponent<FilledSkillVfx>()!=null&&o.activeInHierarchy)<= (reduced?12:20),"global cap bounds actual active effects");}
  foreach(string mode in new[]{"missing","malformed"}){
   Resources.Bad=mode;var h=Start(true,false);var fx=Cast(h,FilledVfxKind.Ice);Check(Cached("crystal").name=="Faceted ice spear","individual resource falls back through production EnsureAssets");Check(Cached("flame").name.StartsWith("Authored spell / Flame"),"other resources still authored when crystal unavailable");Check(Pieces(fx).Length==10,"fallback retains mobile budget");Resources.Bad=null;
   Resources.BadName="IcePrimary";Resources.Bad=mode;h=Start(true,true);fx=Cast(h,FilledVfxKind.Ice);Check(Cached("icePrimary")==null,"dedicated primary failure remains null");Check(Pieces(fx)[1].GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Authored spell / Crystal"),"dedicated failure uses actual base fallback");Resources.Bad=null;Resources.BadName="Crystal";
  }
  foreach(string why in new[]{"death","session","end","expiry"}){var h=Start(false,false);var fx=Cast(h,FilledVfxKind.Fire);if(why=="death")h.IsDead=true;if(why=="session")GameSession.Instance=new GameSession();if(why=="end")GameSession.Instance.ModeFinished=true;Tick(fx,why=="expiry"?2:.1f);Check(!fx.gameObject.activeSelf&&typeof(FilledSkillVfx).GetField("owner",Instance).GetValue(fx)==null&&CombatVisualLease.Active==0,"authored lifecycle "+why);}
  if(args.Length>1&&!string.IsNullOrEmpty(args[1])){var samples=new System.Collections.Generic.List<object>();foreach(var kind in new[]{FilledVfxKind.Ice,FilledVfxKind.Fire})foreach(float age in new[]{0f,.10f,.30f,.65f}){var h=Start(true,true);var fx=Cast(h,kind);typeof(FilledSkillVfx).GetField("age",Instance).SetValue(fx,age);foreach(var piece in (Array)typeof(FilledSkillVfx).GetField("pieces",Instance).GetValue(fx)){if(piece!=null)typeof(FilledSkillVfx).GetMethod("Animate",Instance).Invoke(fx,new[]{piece});}var objects=Pieces(fx).Where(o=>o.activeInHierarchy).Select(o=>new{label=o.name,opacity=o.GetComponent<MeshRenderer>().Opacity*o.GetComponent<MeshRenderer>().TintAlpha,vertices=o.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>{var w=o.transform.TransformPoint(v);return new[]{w.x,w.y,w.z};}).ToArray(),triangles=o.GetComponent<MeshFilter>().sharedMesh.triangles}).ToArray();samples.Add(new{kind=kind.ToString(),age,objects});}File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(samples));}
  var identitySamples=new System.Collections.Generic.List<object>();
  for(int identity=1;identity<=4;identity++)foreach(bool reduced in new[]{false,true})foreach(float wall in new[]{float.PositiveInfinity,.25f})foreach(string phase in new[]{"prepare","release","hold","fade"}){
   var h=Start(true,reduced,wall);string name=new[]{"","BladeSlices","ForkPulse","ContractSigil","ProtectionCage"}[identity];float at=phase=="prepare"?.12f:phase=="release"?0:phase=="hold"?.18f:.5f;
   if(phase=="prepare")FilledSkillVfx.Charge(null,h,Vector3.zero,1.8f,new Color(1,1,1),.7f,identity);
   else if(identity==1)FilledSkillVfx.Crescent(h,Vector3.zero,Vector3.forward,1.8f,new Color(1,1,1));
   else Check(FilledSkillVfx.IdentityContact(h,Vector3.zero,Vector3.forward,1.6f,new Color(1,1,1),identity),"identity resource accepted");
   var fx=GameObject.All.Last(o=>o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null).GetComponent<FilledSkillVfx>();var parts=Pieces(fx);Check(parts[0].GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Skill identity / "+name),"distinct loaded family primary");Check(parts.Length<=(identity==1&&phase!="prepare"?5:1),"reduced identity retention without renderer growth");
   typeof(FilledSkillVfx).GetField("age",Instance).SetValue(fx,at);foreach(var piece in (Array)typeof(FilledSkillVfx).GetField("pieces",Instance).GetValue(fx)){if(piece!=null&&piece.GetType().GetField("Renderer").GetValue(piece)!=null)typeof(FilledSkillVfx).GetMethod("Animate",Instance).Invoke(fx,new[]{piece});}
   foreach(var v in parts[0].GetComponent<MeshFilter>().sharedMesh.vertices)Check(parts[0].transform.TransformPoint(v).x<=wall+.001f,"family primary certified during phase");
   if(reduced&&float.IsPositiveInfinity(wall)){var objects=parts.Where(o=>o.activeInHierarchy).Select(o=>new{label=o.name,opacity=o.GetComponent<MeshRenderer>().Opacity*o.GetComponent<MeshRenderer>().TintAlpha,vertices=o.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>{var w=o.transform.TransformPoint(v);return new[]{w.x,w.y,w.z};}).ToArray(),triangles=o.GetComponent<MeshFilter>().sharedMesh.triangles}).ToArray();identitySamples.Add(new{kind=name,phase,age=at,objects});}
   var shared=(Mesh)typeof(FilledSkillVfx).GetField(new[]{"","identityBlade","identityFork","identityContract","identityProtection"}[identity],Static).GetValue(null);fx.Retire();Check(!fx.gameObject.activeSelf,"family retirement");Check(!shared.Destroyed,"family shared ownership survives retire");typeof(FilledSkillVfx).GetMethod("ResetAssets",Static).Invoke(null,null);Check(shared.Destroyed,"family shared ownership reset");
  }
  for(int identity=1;identity<=4;identity++){Resources.BadGroup="BlenderSkillIdentities";Resources.BadName=new[]{"","BladeSlices","ForkPulse","ContractSigil","ProtectionCage"}[identity];Resources.Bad="missing";var h=Start(true,true);Check(!FilledSkillVfx.IdentityContact(h,Vector3.zero,Vector3.forward,1,new Color(1,1,1),identity),"missing identity lets caller run old presentation");FilledSkillVfx.Charge(null,h,Vector3.zero,1,new Color(1,1,1),.7f,identity);var fx=GameObject.All.Last(o=>o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null).GetComponent<FilledSkillVfx>();Check(Pieces(fx).Length==3,"missing typed preparation keeps old three-band fallback");Resources.Bad=null;Resources.BadGroup="BlenderSpellBases";Resources.BadName="Crystal";}
  if(args.Length>2)File.WriteAllText(args[2],System.Text.Json.JsonSerializer.Serialize(identitySamples));
  Reset();Console.WriteLine("PASS "+checks+" actual authored resource -> decoder -> FilledSkillVfx -> lease/clip/motion/tier/fallback/cleanup checks; managed, not Unity.");
 }
}
