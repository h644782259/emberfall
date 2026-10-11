using System;using System.Linq;using Emberfall;using UnityEngine;
class AttachmentProgram
{
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
 static MeshRenderer Part(string name)=>GameObject.All.Last(o=>o.name=="Tactical attachment / "+name).GetComponent<MeshRenderer>();
 static void Main()
 {
  TacticalAttachmentArt.Enabled=false;Resources.Root="RESOURCE_ROOT";
  Console.WriteLine(TacticalLiveVisualTests.Run());TacticalAttachmentArt.Enabled=true;
  var owner=new GameObject("E05 owner").AddComponent<PlayerController>();var session=new GameSession{Player=owner,Supplier=true,Hunt=true,Supported=true};
  var enemy=new GameObject("E05 enemy").AddComponent<EnemyController>();enemy.session=session;
  var torso=new GameObject("Breastplate");torso.transform.SetParent(enemy.transform,false);torso.transform.localScale=new Vector3(2,3,4);torso.AddComponent<MeshFilter>().sharedMesh=new Mesh{bounds=new Bounds{size=new Vector3(1,2,1)}};
  TacticalEnemyVisual.Attach(enemy,session);var live=enemy.GetComponentInChildren<TacticalEnemyVisual>(true);
  foreach(var name in new[]{"SupplierBackpack","HuntBadge","SupportMantle"}){var part=Part(name);Check(part.enabled,"actual loaded live state "+name);Check(part.GetComponent<MeshFilter>().sharedMesh.triangles.Length>=36,"nonempty actual authored geometry");Check(part.transform.parent==torso.transform,"body animation socket");Check(part.transform.lossyScale.y==6,"rig and mesh envelope scale inherited");}
  Check(GameObject.All.Where(o=>o.transform.parent?.parent==live.transform&&o.GetComponent<LineRenderer>()!=null).All(o=>!o.GetComponent<LineRenderer>().enabled),"authored identity never stacks its corresponding line marker");
  int count=GameObject.All.Count;for(int i=0;i<300;i++)live.gameObject.Call("LateUpdate");Check(GameObject.All.Count==count,"steady live refresh allocates no GameObjects");
  session.Supplier=false;session.Hunt=false;session.Supported=false;live.gameObject.Call("LateUpdate");Check(!Part("SupplierBackpack").enabled&&!Part("HuntBadge").enabled&&!Part("SupportMantle").enabled,"all actual state removals immediate");
  session.Supported=true;live.gameObject.Call("LateUpdate");Check(Part("SupportMantle").enabled,"support recovery reuses attachment");session.Player=new GameObject("replacement owner").AddComponent<PlayerController>();live.gameObject.Call("LateUpdate");Check(!Part("SupportMantle").enabled,"same epoch replacement owner hides old fittings");session.Player=owner;
  live.gameObject.Call("LateUpdate");enemy.Health=0;live.gameObject.Call("LateUpdate");Check(!Part("SupportMantle").enabled,"death hides body fitting");enemy.Health=100;live.gameObject.Call("LateUpdate");live.gameObject.SetActive(false);Check(!Part("SupportMantle").enabled,"component disable hides attachments parented outside component");
  var retained=Part("SupportMantle").GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.Destroy(live.gameObject);Check(Part("SupportMantle").gameObject.Destroyed&&!retained.Destroyed,"destroy owned fitting retains shared mesh");
  var reset=typeof(TacticalAttachmentArt).GetMethod("Reset",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);reset.Invoke(null,null);Check(retained.Destroyed,"subsystem reset releases shared meshes");
  string valid=Resources.Root;string corrupt=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"e05-corrupt-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(System.IO.Path.Combine(corrupt,"TacticalAttachments"));
  foreach(var name in new[]{"SupplierBackpack","HuntBadge","SupportMantle"})System.IO.File.Copy(System.IO.Path.Combine(valid,"TacticalAttachments",name+".bytes"),System.IO.Path.Combine(corrupt,"TacticalAttachments",name+".bytes"));
  System.IO.File.WriteAllBytes(System.IO.Path.Combine(corrupt,"TacticalAttachments/HuntBadge.bytes"),new byte[]{1,2,3});Resources.Root=corrupt;
  var mixed=TacticalAttachmentArt.Create(enemy);Check(mixed.Set(0,true)&&!mixed.Set(1,true)&&mixed.Set(2,true),"corrupt badge alone falls back while independent authored identities remain");mixed.Destroy();reset.Invoke(null,null);System.IO.Directory.Delete(corrupt,true);
  var unknown=new GameObject("unsupported rig").AddComponent<EnemyController>();Resources.Root=valid;Check(!TacticalAttachmentArt.Create(unknown).Set(0,true),"unknown rig retains original presentation");
  Resources.Root="/tmp/emberfall-e05-missing-resource-root";var fallback=TacticalAttachmentArt.Create(enemy);Check(!fallback.Set(0,true)&&!fallback.Set(1,true)&&!fallback.Set(2,true),"missing assets select each original marker fallback");reset.Invoke(null,null);
  Console.WriteLine("PASS E05 actual tactical state/component/loader/decoder/assets chain: three sockets, no duplicate markers, no steady object growth, owner/death/disable/destroy, cache reset and missing fallback. Managed, NOT Unity.");
 }
}
