using System;
using UnityEngine;

namespace Emberfall
{
    // Original four-legged astrolabe silhouette; no scaled copy of the small-trial giant.
    internal sealed partial class LargeBossRig : MonoBehaviour
    {
        private Transform body, core, firstRing, secondRing, beamEmitter;
        private readonly Transform[] legs = new Transform[4], petals = new Transform[4];
        private float age, opening, locomotion,bodyHeight=1.45f,bodyPitch,bodyYaw,coreScale=1,brace;
        private readonly LargeBossMotion motion=new LargeBossMotion();
        private LargeExpeditionBoss encounter;
        public void Build(Func<Color, VisualSurface, Material> material)
        {
            encounter = transform.parent.GetComponent<LargeExpeditionBoss>();
            Material shell = material(new Color(.17f,.23f,.3f),VisualSurface.Metal);
            Material bronze = material(new Color(.65f,.43f,.2f),VisualSurface.Metal);
            Material light = material(new Color(.15f,.8f,.94f),VisualSurface.Crystal);
            body = Joint(transform,"Suspended astrolabe chassis",new Vector3(0,1.45f,0));
            Part(body,"Faceted engine housing",PrimitiveType.Cylinder,Vector3.zero,new Vector3(1.55f,.33f,1.55f),shell);
            Part(body,"Engine collar",PrimitiveType.Cylinder,new Vector3(0,.3f,0),new Vector3(1.7f,.07f,1.7f),bronze);
            beamEmitter=Joint(body,"Aim-aligned beam emitter",new Vector3(0,.53f,0));
            Part(beamEmitter,"Radial beam emitter",PrimitiveType.Cube,new Vector3(0,0,.96f),new Vector3(.26f,.22f,.62f),light);
            core = Part(body,"Exposed star heart",PrimitiveType.Sphere,new Vector3(0,.65f,0),new Vector3(.85f,1.12f,.85f),light);
            Part(body,"Split crown spindle",PrimitiveType.Cube,new Vector3(0,1.8f,0),new Vector3(.24f,.55f,.24f),bronze).localRotation=Quaternion.Euler(0,45,22);
            Mesh ring = MakeRing(); var owned=gameObject.AddComponent<OwnedCombatMesh>();owned.Value=ring;
            firstRing = Ring(body,"Bronze orbital meridian",ring,bronze,new Vector3(0,.68f,0));
            secondRing = Ring(body,"Dark counter-rotating meridian",ring,shell,new Vector3(0,.68f,0));
            secondRing.localScale=Vector3.one*.91f;
            for(int i=0;i<4;i++)
            {
                float angle=45+i*90;Quaternion yaw=Quaternion.Euler(0,angle,0);
                legs[i]=Joint(transform,"Articulated radial limb",yaw*new Vector3(0,1.12f,.55f));legs[i].localRotation=yaw;
                Part(legs[i],"Thigh armour",PrimitiveType.Capsule,new Vector3(0,-.12f,.27f),new Vector3(.35f,.38f,.34f),shell).localRotation=Quaternion.Euler(-48,0,0);
                Part(legs[i],"Bronze knee",PrimitiveType.Sphere,new Vector3(0,-.35f,.54f),Vector3.one*.33f,bronze);
                Part(legs[i],"Tapered radial shin",PrimitiveType.Capsule,new Vector3(0,-.67f,.59f),new Vector3(.2f,.39f,.23f),shell).localRotation=Quaternion.Euler(11,0,0);
                Part(legs[i],"Grounded claw",PrimitiveType.Cube,new Vector3(0,-1.02f,.7f),new Vector3(.35f,.18f,.53f),bronze);
                petals[i]=Joint(body,"Opening core armour",new Vector3(0,.32f,0));petals[i].localRotation=Quaternion.Euler(0,i*90,0);
                Part(petals[i],"Curved shell plate",PrimitiveType.Capsule,new Vector3(0,.6f,.56f),new Vector3(.46f,.61f,.2f),shell).localRotation=Quaternion.Euler(-12,0,0);
                Part(petals[i],"Armour crest inlay",PrimitiveType.Cube,new Vector3(0,1.02f,.62f),new Vector3(.12f,.32f,.09f),bronze).localRotation=Quaternion.Euler(-12,0,0);
            }
            BuildPowerChannels(material);
        }
        public void Animate(float speed,float attack)
        {
            float dt=Time.deltaTime;if(dt<=0)return;age+=dt;
            locomotion=Mathf.Lerp(locomotion,speed,1-Mathf.Exp(-dt*9));
            LargeBossPhase phase=encounter!=null&&encounter.State!=null?encounter.State.Phase:LargeBossPhase.Combat;
            float remaining=encounter!=null&&encounter.State!=null?encounter.State.Remaining:0;
            var pose=LargeBossMotion.Pose(phase,remaining);motion.Advance(dt,phase);
            float blend=1-Mathf.Exp(-dt*8);
            opening=Mathf.Lerp(opening,pose.Opening,blend);bodyHeight=Mathf.Lerp(bodyHeight,pose.Height,blend);
            bodyPitch=Mathf.Lerp(bodyPitch,pose.Pitch,blend);coreScale=Mathf.Lerp(coreScale,pose.CoreScale,blend);brace=Mathf.Lerp(brace,pose.Brace,blend);
            bool aimsBeam=phase==LargeBossPhase.Windup||phase==LargeBossPhase.Beam;
            // The chassis turns with inertia; the independently pivoted muzzle remains
            // exactly aligned with the actual damage corridor, without snapping the rings.
            bodyYaw=Mathf.LerpAngle(bodyYaw,aimsBeam?Mathf.DeltaAngle(transform.eulerAngles.y,encounter.BeamWorldAngle):0,blend);
            body.localPosition=new Vector3(0,bodyHeight+Mathf.Sin(age*2)*(.055f*(1-brace))-attack*.12f,0);
            body.localRotation=Quaternion.Euler(bodyPitch-attack*8,bodyYaw,phase==LargeBossPhase.Exposed?Mathf.Sin(age*2)*2:0);
            beamEmitter.gameObject.SetActive(aimsBeam);
            if(aimsBeam)beamEmitter.rotation=Quaternion.Euler(0,encounter.BeamWorldAngle,0);
            core.localScale=new Vector3(.85f,1.12f,.85f)*(coreScale+Mathf.Sin(age*(phase==LargeBossPhase.Exposed?5:3))*.025f);
            firstRing.localRotation=Quaternion.Euler(73,motion.FirstAngle,28);
            secondRing.localRotation=Quaternion.Euler(25,motion.SecondAngle,75);
            for(int i=0;i<4;i++)
            {
                float swing=Mathf.Sin(age*7+i*Mathf.PI*.5f)*locomotion*(1-brace);
                legs[i].localRotation=Quaternion.Euler(swing*11-brace*8,45+i*90,swing*4);
                petals[i].localRotation=Quaternion.Euler(-opening*48,i*90,0);
            }
        }
        internal static Transform Joint(Transform parent,string title,Vector3 position)
        {var obj=new GameObject(title);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;return obj.transform;}
        internal static Transform Part(Transform parent,string title,PrimitiveType shape,Vector3 position,Vector3 scale,Material material)
        {var obj=ProceduralVisuals.Create(title,shape,material);AuthoredActorMeshes.Apply(obj,title,shape);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;return obj.transform;}
        private static Transform Ring(Transform parent,string title,Mesh mesh,Material material,Vector3 at)
        {Transform obj=Joint(parent,title,at);obj.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;obj.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return obj;}
        private static Mesh MakeRing()
        {
            const int segments=40,sides=6;var vertices=new Vector3[segments*sides];var normals=new Vector3[vertices.Length];var triangles=new int[segments*sides*6];
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {
                float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;int k=i*sides+j;
                Vector3 radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));normals[k]=radial*Mathf.Cos(b)+Vector3.up*Mathf.Sin(b);
                vertices[k]=radial*1.1f+normals[k]*.07f;
                int n=((i+1)%segments)*sides+j,s=i*sides+(j+1)%sides,t=((i+1)%segments)*sides+(j+1)%sides;
                int p=k*6;triangles[p]=k;triangles[p+1]=s;triangles[p+2]=n;triangles[p+3]=s;triangles[p+4]=t;triangles[p+5]=n;
            }
            var mesh=new Mesh{name="Authored astrolabe torus"};mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=triangles;mesh.RecalculateBounds();return mesh;
        }
    }
}
