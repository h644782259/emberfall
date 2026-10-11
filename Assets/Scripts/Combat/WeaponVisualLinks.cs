using UnityEngine;
namespace Emberfall
{
    // This component owns only a renderer child. The projectile's simulation transform,
    // collision history, target and hit clock remain under CombatProjectile alone.
    internal sealed class ProjectileVisualBridge : MonoBehaviour
    {
        private Transform simulation;
        private CombatModel model;
        private WeaponVisualAnchor anchor;
        private Vector3 offset;
        private TrailRenderer trail;
        private float age;
        private int bornFrame;
        private const float Duration=.09f,MaximumOffset=2.5f;
        public static void Bind(Transform body,Transform simulation,CombatModel model,bool arrow,TrailRenderer trail)
        {
            if(body==null||simulation==null)return;
            if(model==null){body.localPosition=Vector3.zero;if(trail!=null){trail.Clear();trail.emitting=true;}return;}
            var bridge=body.gameObject.AddComponent<ProjectileVisualBridge>();
            bridge.simulation=simulation;bridge.model=model;bridge.anchor=arrow?WeaponVisualAnchor.BowArrowRest:WeaponVisualAnchor.StaffCore;
            bridge.trail=trail;bridge.bornFrame=Time.frameCount;bridge.Capture();
            if(trail!=null){trail.Clear();trail.emitting=true;}
        }
        private void Capture()
        {
            Vector3 point;
            offset=Vector3.zero;
            if(model!=null&&model.TryGetWeaponVisualAnchor(anchor,out point)&&Finite(point)&&
                (point-simulation.position).sqrMagnitude<=MaximumOffset*MaximumOffset&&CombatSight.Direct(simulation.position,point))
                offset=point-simulation.position;
            transform.position=simulation.position+offset;
        }
        private void LateUpdate()
        {
            if(simulation==null){Destroy(gameObject);return;}
            // Player animation finishes during Update; read the actual release-pose anchor
            // again before the first rendered frame, then detach from the moving weapon.
            if(Time.frameCount==bornFrame){Capture();if(trail!=null)trail.Clear();return;}
            if(Time.deltaTime<=0)return;
            age+=Time.deltaTime;
            float t=Mathf.Clamp01(age/Duration),weight=1-t*t*(3-2*t);
            Vector3 candidate=simulation.position+offset*weight;
            if(!CombatSight.Direct(simulation.position,candidate)){candidate=simulation.position;offset=Vector3.zero;if(trail!=null)trail.Clear();}
            transform.position=candidate;
            if(t>=1){transform.localPosition=Vector3.zero;Destroy(this);}
        }
        private static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
    }

    // A small filled ribbon follows both real sword endpoints; it never selects targets.
    internal sealed class WeaponSlashRibbon : MonoBehaviour
    {
        private static int active;
        private CombatModel model;private PlayerController owner;private int epoch,side,action;private bool sampling=true;
        private Vector3 previousRoot,previousTip;
        private readonly Vector3[] vertices=new Vector3[8];
        private Mesh mesh;private Material material;private float age,alpha;private bool registered;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset(){active=0;}
        public static void Spawn(PlayerController owner,CombatModel model,Color color)
        {
            Vector3 root,tip;
            if(owner==null||owner.IsDead||model==null||!model.SwordActionActive||active>=(Application.isMobilePlatform?12:20)||
                !model.TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordRoot,out root)||!model.TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordTip,out tip))return;
            if(!model.TryClaimSwordRibbon(model.WeaponActionId))return;
            var obj=new GameObject("Sword endpoint visual ribbon");
            var ribbon=obj.AddComponent<WeaponSlashRibbon>();ribbon.owner=owner;ribbon.epoch=owner.CombatEpoch;ribbon.model=model;ribbon.side=model.WeaponSwingSide;ribbon.action=model.WeaponActionId;
            ribbon.previousRoot=root-model.transform.right*.06f*ribbon.side;ribbon.previousTip=tip-model.transform.right*.06f*ribbon.side;
            ribbon.mesh=new Mesh{name="Weapon root-tip swept volume"};ribbon.mesh.vertices=ribbon.vertices;
            ribbon.mesh.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
            obj.AddComponent<MeshFilter>().sharedMesh=ribbon.mesh;
            ribbon.material=new Material(Resources.Load<Shader>("FilledSpell")??Shader.Find("Sprites/Default"));ribbon.material.color=color;ribbon.alpha=color.a;ribbon.material.renderQueue=3070;
            var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=ribbon.material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            ribbon.registered=true;active++;ribbon.Sample(root,tip);
        }
        private void LateUpdate()
        {
            var game=GameSession.Instance;
            if(owner==null||owner.IsDead||model==null||owner.CombatEpoch!=epoch||game==null||game.Player!=owner||!game.HasStarted||game.ModeFinished){Destroy(gameObject);return;}
            if(model.WeaponActionId!=action||!model.SwordActionActive)sampling=false;
            if(game.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=.22f){Destroy(gameObject);return;}
            Vector3 root,tip;
            if(!model.TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordRoot,out root)||!model.TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordTip,out tip)){Destroy(gameObject);return;}
            if((root-previousRoot).sqrMagnitude>9||(tip-previousTip).sqrMagnitude>9)sampling=false;
            if(sampling)Sample(root,tip);Color color=material.color;color.a=alpha*(1-age/.22f);material.color=color;
        }
        private void Sample(Vector3 root,Vector3 tip)
        {
            if((root-previousRoot).sqrMagnitude+(tip-previousTip).sqrMagnitude<.00001f)
            {previousRoot=root-model.transform.right*.025f*side;previousTip=tip-model.transform.right*.025f*side;}
            Vector3 normal=Vector3.Cross(tip-root,model.transform.right);
            if(normal.sqrMagnitude<.00001f)normal=Vector3.Cross(tip-root,Vector3.forward);
            Vector3 thickness=(normal.sqrMagnitude>.00001f?normal.normalized:Vector3.up)*.012f;
            vertices[0]=root-thickness;vertices[1]=tip-thickness;vertices[2]=previousTip-thickness;vertices[3]=previousRoot-thickness;
            vertices[4]=root+thickness;vertices[5]=tip+thickness;vertices[6]=previousTip+thickness;vertices[7]=previousRoot+thickness;
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();previousRoot=root;previousTip=tip;
        }
        private void Release(){if(registered){registered=false;active=Mathf.Max(0,active-1);}}
        private void OnDisable(){Release();}
        private void OnEnable(){if(owner==null||registered)return;if(active>=(Application.isMobilePlatform?12:20)){Destroy(gameObject);return;}registered=true;active++;}
        private void OnDestroy(){Release();if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
