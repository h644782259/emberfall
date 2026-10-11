using System;
using System.Linq;
using Emberfall;
using UnityEngine;
public static class WeaponVisualLinkTests
{
    static int checks;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static bool Near(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<.000001f;
    public static string Run()
    {
        foreach(bool arrow in new[]{false,true})
        {
            CombatSight.Wall=float.PositiveInfinity;Time.frameCount=10;Time.deltaTime=.045f;
            var simulation=new GameObject("Simulation");simulation.transform.position=new Vector3(2,0,3);simulation.transform.rotation=Quaternion.Euler(25,50,10);simulation.transform.localScale=new Vector3(.1f,.43f,.1f);
            var body=new GameObject("Visual child");body.transform.SetParent(simulation.transform,false);var trail=body.AddComponent<TrailRenderer>();
            var model=new GameObject("Weapon").AddComponent<CombatModel>();model.Staff=new Vector3(2.4f,1,3.2f);model.Bow=new Vector3(1.6f,.8f,3.1f);
            var before=simulation.transform.TransformPoint(Vector3.one);var anchor=arrow?model.Bow:model.Staff;
            ProjectileVisualBridge.Bind(body.transform,simulation.transform,model,arrow,trail);
            Check(Near(body.transform.position,anchor),"render child starts at actual selected weapon anchor");
            Check(Near(before,simulation.transform.TransformPoint(Vector3.one)),"binding does not alter logical root pose or scale");
            Check(trail.emitting&&trail.ClearCount==1,"trail starts only after visual origin is set");
            model.Staff+=new Vector3(0,.1f,0);model.Bow+=new Vector3(0,.1f,0);anchor=arrow?model.Bow:model.Staff;
            body.Call("LateUpdate");Check(Near(body.transform.position,anchor),"first render recaptures completed release pose");
            var offset=anchor-simulation.transform.position;
            Time.frameCount++;simulation.transform.position+=new Vector3(0,0,.7f);var expected=simulation.transform.position+offset*.5f;before=simulation.transform.TransformPoint(Vector3.one);
            body.Call("LateUpdate");Check(Near(body.transform.position,expected),"visual child converges while simulation advances independently");
            Check(Near(before,simulation.transform.TransformPoint(Vector3.one)),"visual convergence never changes trajectory root");
            Time.frameCount++;body.Call("LateUpdate");Check(Near(body.transform.localPosition,Vector3.zero),"visual exactly rejoins logical trajectory at bounded deadline");
            Check(body.GetComponent<ProjectileVisualBridge>().Destroyed,"temporary attachment retires after convergence");
            CombatSight.Wall=2.1f;model.Staff=new Vector3(2.4f,1,3);model.Bow=model.Staff;
            var blocked=new GameObject("Blocked visual");blocked.transform.SetParent(simulation.transform,false);
            ProjectileVisualBridge.Bind(blocked.transform,simulation.transform,model,arrow,null);
            Check(Near(blocked.transform.position,simulation.transform.position),"covered anchor cannot pull visual through solid boundary");
        }
        foreach(int side in new[]{-1,1})
        {
            Time.deltaTime=.02f;CombatSight.Wall=float.PositiveInfinity;
            var owner=new GameObject("Owner").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=owner,HasStarted=true};
            var model=new GameObject("Sword").AddComponent<CombatModel>();model.Side=side;model.Root=new Vector3(1,1,0);model.Tip=new Vector3(1,2.2f,0);
            WeaponSlashRibbon.Spawn(owner,model,new Color(1,1,1,.4f));
            var ribbon=GameObject.All.Last(o=>o.GetComponent<WeaponSlashRibbon>()!=null);var mesh=ribbon.GetComponent<MeshFilter>().sharedMesh;
            Check(Near((mesh.vertices[0]+mesh.vertices[4])*.5f,model.Root)&&Near((mesh.vertices[1]+mesh.vertices[5])*.5f,model.Tip),"ribbon live edge joins both actual weapon anchors");
            Check(Math.Sign(model.Root.x-(mesh.vertices[3]+mesh.vertices[7]).x*.5f)==side,"initial ribbon trail uses shared left/right swing side");
            model.Root+=new Vector3(.15f,0,0);model.Tip+=new Vector3(.25f,0,0);ribbon.Call("LateUpdate");
            Check(Near((mesh.vertices[0]+mesh.vertices[4])*.5f,model.Root)&&Near((mesh.vertices[1]+mesh.vertices[5])*.5f,model.Tip),"ribbon follows new endpoints after animation");
            Check(ribbon.GetComponent<MeshRenderer>().sharedMaterial.color.a<=.4f,"fade preserves requested alpha");
            int beforeDuplicate=GameObject.All.Count;WeaponSlashRibbon.Spawn(owner,model,new Color(1,1,1,.4f));
            Check(GameObject.All.Count==beforeDuplicate,"one physical swing can create only one endpoint ribbon");
            var frozen=(Vector3[])mesh.vertices.Clone();model.WeaponActionId++;model.Root+=new Vector3(.3f,0,0);model.Tip+=new Vector3(.3f,0,0);
            ribbon.Call("LateUpdate");Check(mesh.vertices.SequenceEqual(frozen)&&ribbon.activeSelf,"action change stops sampling while the existing afterimage fades");
            model.WeaponActionId--;ribbon.Call("LateUpdate");Check(mesh.vertices.SequenceEqual(frozen),"stopped sampling cannot resume when an old identity reappears");
            owner.CombatEpoch++;Time.deltaTime=0;ribbon.Call("LateUpdate");Check(!ribbon.activeSelf,"stale sword ribbon retires even during pause");
        }
        return "PASS: "+checks+" production visual-child/root-invariance and sword-endpoint checks (managed, not GPU)";
    }
}
namespace Emberfall
{
    // Explicit fake anchor provider; the actual weapon rig has separate structure tests.
    public sealed class CombatModel:MonoBehaviour
    {
        public Vector3 Staff,Bow,Root,Tip;public int Side=1;public int WeaponSwingSide=>Side;
        public bool SwordActionActive=true;public int WeaponActionId=1;private int claimed;
        public bool TryClaimSwordRibbon(int action){if(!SwordActionActive||action!=WeaponActionId||claimed==action)return false;claimed=action;return true;}
        public bool TryGetWeaponVisualAnchor(WeaponVisualAnchor anchor,out Vector3 point)
        {point=anchor==WeaponVisualAnchor.StaffCore?Staff:anchor==WeaponVisualAnchor.BowArrowRest?Bow:anchor==WeaponVisualAnchor.SwordRoot?Root:Tip;return true;}
    }
}
