using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Branching plasma has a stable path during each strike. It never sways like a rod.
    internal static class VolumetricDischarge
    {
        private static readonly Mesh[] variants=new Mesh[8];private static Material surface;
        internal static Material Surface
        {get{if(surface==null)surface=new Material(Resources.Load<Shader>("LightningDischarge")){hideFlags=HideFlags.HideAndDontSave};return surface;}}
        internal static Mesh Body(int variant)
        {
            variant=Mathf.Abs(variant)%variants.Length;if(variants[variant]!=null)return variants[variant];
            var random=new System.Random(4117+variant*173);var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            var path=new Vector3[11];
            for(int i=0;i<path.Length;i++){float t=i/(float)(path.Length-1),spread=i==0||i==path.Length-1?0:.22f;path[i]=new Vector3(((float)random.NextDouble()-.5f)*spread,t,((float)random.NextDouble()-.5f)*spread);}
            for(int i=0;i<path.Length-1;i++)
            {
                Segment(vertices,uv,triangles,path[i],path[i+1],.028f,.02f);
                if(i==2||i==5||i==7)
                {
                    float sign=(i%2==0?1:-1);Vector3 elbow=path[i]+new Vector3(sign*.18f,.14f,.09f),tip=elbow+new Vector3(sign*.12f,.13f,-.06f);
                    Segment(vertices,uv,triangles,path[i],elbow,.016f,.009f);Segment(vertices,uv,triangles,elbow,tip,.009f,.001f);
                }
            }
            var mesh=new Mesh{name="Organic branching plasma / "+variant};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();variants[variant]=mesh;return mesh;
        }
        private static void Segment(List<Vector3> v,List<Vector2> uv,List<int> triangles,Vector3 from,Vector3 to,float r0,float r1)
        {
            Vector3 axis=(to-from).normalized,side=Vector3.Cross(axis,Vector3.forward).normalized;if(side.sqrMagnitude<.01f)side=Vector3.right;
            Vector3 second=Vector3.Cross(axis,side);int first=v.Count;
            for(int ring=0;ring<2;ring++)for(int j=0;j<6;j++){float a=j*Mathf.PI/3;v.Add((ring==0?from:to)+(side*Mathf.Cos(a)+second*Mathf.Sin(a))*(ring==0?r0:r1));uv.Add(new Vector2(j/6f,ring));}
            for(int j=0;j<6;j++){int next=(j+1)%6;triangles.Add(first+j);triangles.Add(first+6+j);triangles.Add(first+next);triangles.Add(first+next);triangles.Add(first+6+j);triangles.Add(first+6+next);}
        }
        internal static void Strike(Vector3 from,Vector3 to)
        {
            var game=GameSession.Instance;if(game==null||game.Player==null||game.CombatEffectsEnded)return;
            var root=new GameObject("Transient branching plasma");if(CombatVisualLease.Attach(root,CombatVisualPriority.RealContact)==null)return;
            Vector3 delta=to-from;root.transform.position=from;root.transform.rotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);root.transform.localScale=new Vector3(1,Mathf.Max(.01f,delta.magnitude),1);
            root.AddComponent<MeshFilter>().sharedMesh=Body(Mathf.RoundToInt(from.x*13+to.z*7));root.AddComponent<MeshRenderer>().sharedMaterial=Surface;root.AddComponent<DischargeContactLifetime>().Initialize(game);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){foreach(var mesh in variants)if(mesh!=null)Object.Destroy(mesh);for(int i=0;i<variants.Length;i++)variants[i]=null;if(surface!=null)Object.Destroy(surface);surface=null;}
    }
    internal sealed class DischargeContactLifetime:MonoBehaviour
    {
        private GameSession session;private PlayerController player;private int epoch;private float age;private Renderer body;private MaterialPropertyBlock block;
        internal void Initialize(GameSession game){session=game;player=game.Player;epoch=player.CombatEpoch;body=GetComponent<Renderer>();block=new MaterialPropertyBlock();}
        private void Update()
        {
            if(session==null||session!=GameSession.Instance||player==null||session.Player!=player||player.CombatEpoch!=epoch||session.CombatEffectsEnded){Destroy(gameObject);return;}
            if(session.InputBlocked||Time.deltaTime<=0)return;age+=Time.deltaTime;if(age>.24f){Destroy(gameObject);return;}
            block.SetColor("_Tint",new Color(.08f,.5f,1));block.SetFloat("_Opacity",age<.075f?1:age<.12f?.1f:Mathf.Clamp01((.24f-age)/.12f)*.6f);block.SetFloat("_Phase",age);body.SetPropertyBlock(block);
        }
    }
}
