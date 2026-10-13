using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall
{
    // Thin physical deposits follow the real support surface. They never select
    // targets, introduce colliders or change the authoritative damage footprint.
    internal sealed class GroundSkillSurface : MonoBehaviour
    {
        internal enum Deposit { WetPoison, Scorch }
        private static readonly HashSet<GroundSkillSurface> live=new HashSet<GroundSkillSurface>();
        private static Material shared;
        private Mesh mesh;
        private Renderer surface;
        private MaterialPropertyBlock block;
        private Deposit kind;
        private PlayerController owner;
        private GameSession session;
        private int epoch,revision;
        private Vector3 center;
        private float radius,age;
        private System.Func<float> remainingLifetime;
        private bool retired;
        private readonly struct Edge
        {
            internal readonly int A,B;
            internal Edge(int a,int b){A=Mathf.Min(a,b);B=Mathf.Max(a,b);}
            public override int GetHashCode(){return A*397^B;}
            public override bool Equals(object value){return value is Edge edge&&A==edge.A&&B==edge.B;}
        }
        private struct Boundary {internal int Count,A,B;internal Boundary(int a,int b){Count=1;A=a;B=b;}}
        internal static float HorizontalRadius(Mesh shape)
        {
            float radius=.01f;foreach(var point in shape.vertices)radius=Mathf.Max(radius,CombatFx.Flat(point).magnitude);return radius;
        }
        // A rooted body needs dry support for its complete horizontal envelope.
        // This only fits cosmetic matter inside the existing damage radius.
        internal static bool TryPlaceAttached(Transform root,Mesh shape,ref Vector3 at,ref Vector3 dimensions,Quaternion rotation,float extent,float inflation=1)
        {
            float footprint=.03f;
            foreach(var vertex in shape.vertices)
                footprint=Mathf.Max(footprint,CombatFx.Flat(rotation*Vector3.Scale(vertex,dimensions)).magnitude*inflation);
            float scale=Mathf.Max(Mathf.Abs(root.lossyScale.x),Mathf.Abs(root.lossyScale.z)),x,z,fit;
            if(!FilledVfxPlacement.TryPlace(at.x,at.z,extent,footprint,(px,pz,r)=>{
                Vector3 point=root.TransformPoint(new Vector3(px,0,pz));
                return CombatSight.Area(root.position,point)&&AttachedFootprintDry(point,r*scale);
            },out x,out z,out fit))return false;
            at.x=x;at.z=z;dimensions*=fit;return true;
        }
        internal static bool AttachedFootprintDry(Vector3 point,float extent)
        {
            if(WorldTraversal.IsOpenWater(point,Mathf.Max(.01f,extent)))return false;
            // IsOpenWater deliberately treats solid support as ground. Also test
            // the perimeter so a root on a small river platform cannot hang over
            // the neighbouring water after its center overlaps that support.
            for(int ring=1;ring<=2;ring++)for(int i=0;i<12;i++)
            {
                float angle=i*Mathf.PI/6;Vector3 edge=point+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*extent*ring*.5f;
                if(WorldTraversal.IsOpenWater(edge,.01f))return false;
            }
            return true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach(var layer in new List<GroundSkillSurface>(live))if(layer!=null)layer.Retire();
            live.Clear();if(shared!=null)Destroy(shared);shared=null;
        }
        internal static GroundSkillSurface Ensure(Transform parent,PlayerController player,Vector3 point,float extent,Deposit deposit,System.Func<float> remaining=null,bool independentLifetime=false)
        {
            if(parent==null||player==null||player.IsDead||extent<=0)return null;
            point.y=WorldTraversal.SurfaceHeight(point,.012f);
            // Sustained fields and their real contact pulse share one deposit.
            // Its lifetime stays with the existing authoritative visual parent.
            foreach(var layer in live)
                if(layer!=null&&!layer.retired&&(!independentLifetime||layer.transform.parent==parent)&&layer.gameObject.activeInHierarchy&&layer.owner==player&&layer.epoch==player.CombatEpoch&&layer.kind==deposit&&
                    CombatFx.Flat(layer.center-point).sqrMagnitude<.0225f&&Mathf.Abs(layer.radius-extent)<.08f)return layer;
            if(shared==null){shared=new Material(Resources.Load<Shader>("GroundSkillSurface"));SkillBodyMaterialLibrary.Bind(shared);}
            if(Camera.main!=null)Camera.main.depthTextureMode|=DepthTextureMode.Depth;
            var root=new GameObject(deposit==Deposit.WetPoison?"Wet poisonous soil / scattered puddles":"Fire contact / scattered ash deposits");
            root.transform.SetParent(parent,false);root.transform.SetPositionAndRotation(new Vector3(point.x,WorldTraversal.SurfaceHeight(point,.015f),point.z),Quaternion.identity);
            var fx=root.AddComponent<GroundSkillSurface>();fx.owner=player;fx.session=GameSession.Instance;fx.epoch=player.CombatEpoch;
            fx.center=point;fx.radius=extent;fx.kind=deposit;fx.remainingLifetime=remaining;fx.revision=WorldTraversal.Revision;fx.block=new MaterialPropertyBlock();
            var filter=root.AddComponent<MeshFilter>();fx.surface=root.AddComponent<MeshRenderer>();fx.surface.sharedMaterial=shared;
            fx.surface.shadowCastingMode=ShadowCastingMode.Off;fx.surface.receiveShadows=true;
            fx.mesh=fx.Build();filter.sharedMesh=fx.mesh;fx.Draw();live.Add(fx);return fx;
        }
        private Mesh Build()
        {
            // Shared top vertices keep smooth shading. A closed underside and
            // short skirts give the deposit real millimetre thickness without
            // building a raised gel disk or sheets across a platform edge.
            int steps=Mathf.Clamp(Mathf.CeilToInt(radius*2/.3f),8,EffectPreferences.ReducedEffects?24:36),side=steps+1;
            int nodes=side*side;float cell=radius*2/steps;
            var vertices=new List<Vector3>(nodes*2);var normals=new List<Vector3>(nodes*2);var uv=new List<Vector2>(nodes*2);var colors=new List<Color>(nodes*2);
            var heights=new float[nodes];var cover=new float[nodes];var world=new Vector3[nodes];var triangles=new List<int>();
            for(int z=0;z<=steps;z++)for(int x=0;x<=steps;x++)
            {
                int i=z*side+x;Vector3 point=center+new Vector3(-radius+x*cell,0,-radius+z*cell);float distance=CombatFx.Flat(point-center).magnitude/radius;
                float broad=Mathf.PerlinNoise(point.x*.67f+8.31f,point.z*.67f+3.79f),fine=Mathf.PerlinNoise(point.x*2.4f+16.1f,point.z*2.4f+2.73f);
                // Irregular feathered islands fill the real area but never draw
                // a complete circular boundary or opaque carpet over its floor.
                float edge=1-Feather(.67f,.995f,distance+(broad-.5f)*.10f);
                float patch=Feather(.23f,.58f,broad*.8f+fine*.2f);
                cover[i]=Mathf.Clamp01(edge*patch);if(distance>1||!CombatSight.Area(center,point)||WorldTraversal.IsOpenWater(point,.01f))cover[i]=0;
                float h=WorldTraversal.SurfaceHeight(point,.012f);heights[i]=h;point.y=h+.005f+fine*.0015f;world[i]=point;
                vertices.Add(transform.InverseTransformPoint(point));
                normals.Add(Vector3.up);
                uv.Add(new Vector2(point.x,point.z));colors.Add(new Color(broad,fine,1,cover[i]));
            }
            // Reuse the already sampled support heights instead of four extra
            // terrain/obstacle queries for every cosmetic deposit vertex.
            for(int z=0;z<=steps;z++)for(int x=0;x<=steps;x++)
            {
                int left=Mathf.Max(0,x-1),right=Mathf.Min(steps,x+1),down=Mathf.Max(0,z-1),up=Mathf.Min(steps,z+1);
                float hx=(heights[z*side+right]-heights[z*side+left])/((right-left)*cell);
                float hz=(heights[up*side+x]-heights[down*side+x])/((up-down)*cell);
                normals[z*side+x]=new Vector3(-Mathf.Clamp(hx,-.5f,.5f),1,-Mathf.Clamp(hz,-.5f,.5f)).normalized;
            }
            var top=new List<int>();
            for(int z=0;z<steps;z++)for(int x=0;x<steps;x++)
            {
                int a=z*side+x,b=a+1,c=a+side,d=c+1;
                Face(a,c,b,cell,heights,cover,world,top);
                Face(b,c,d,cell,heights,cover,world,top);
            }
            SplitTouchingFans(top,vertices,normals,uv,colors);
            nodes=vertices.Count;
            for(int i=0;i<nodes;i++)
            {vertices.Add(vertices[i]-Vector3.up*.009f);normals.Add(-normals[i]);uv.Add(uv[i]);colors.Add(colors[i]);}
            var edges=new Dictionary<Edge,Boundary>();
            for(int i=0;i<top.Count;i+=3)
            {
                int a=top[i],b=top[i+1],c=top[i+2];
                triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(a+nodes);triangles.Add(c+nodes);triangles.Add(b+nodes);
                AddEdge(edges,a,b);AddEdge(edges,b,c);AddEdge(edges,c,a);
            }
            foreach(var pair in edges)if(pair.Value.Count==1)
            {
                int a=pair.Value.A,b=pair.Value.B;
                // Reverse the boundary's top winding once. Every welded edge
                // belongs to exactly two faces; no duplicate two-sided shell.
                triangles.Add(b);triangles.Add(a);triangles.Add(a+nodes);triangles.Add(b);triangles.Add(a+nodes);triangles.Add(b+nodes);
            }
            var result=new Mesh{name=kind==Deposit.WetPoison?"Closed thin wet soil on actual support":"Closed thin scattered ash on actual support"};
            result.SetVertices(vertices);result.SetNormals(normals);result.SetUVs(0,uv);result.SetColors(colors);result.SetTriangles(triangles,0);result.RecalculateBounds();return result;
        }
        private void Face(int a,int b,int c,float cell,float[] heights,float[] cover,Vector3[] world,List<int> triangles)
        {
            if(Mathf.Max(cover[a],Mathf.Max(cover[b],cover[c]))<.025f)return;
            float high=Mathf.Max(heights[a],Mathf.Max(heights[b],heights[c])),low=Mathf.Min(heights[a],Mathf.Min(heights[b],heights[c]));
            if(high-low>Mathf.Max(.26f,cell*.8f)||!CombatSight.VisualTriangle(center,world[a],world[b],world[c]))return;
            if(!DryFace(world[a],world[b],world[c]))return;
            triangles.Add(a);triangles.Add(b);triangles.Add(c);
        }
        private static void SplitTouchingFans(List<int> triangles,List<Vector3> vertices,List<Vector3> normals,List<Vector2> uv,List<Color> colors)
        {
            // Noisy islands can touch only at a grid corner. Sharing that one
            // vertex would join both vertical skirts into a four-face edge.
            // Keep each edge-connected top fan as its own closed physical shell.
            int originalCount=vertices.Count;var original=triangles.ToArray();var incident=new List<int>[originalCount];
            for(int i=0;i<original.Length;i++)
            {int vertex=original[i];if(incident[vertex]==null)incident[vertex]=new List<int>();incident[vertex].Add(i/3);}
            for(int vertex=0;vertex<originalCount;vertex++)
            {
                if(incident[vertex]==null)continue;
                var pending=new HashSet<int>(incident[vertex]);bool firstFan=true;
                while(pending.Count>0)
                {
                    int first=-1;foreach(int face in pending){first=face;break;}
                    int replacement=vertex;
                    if(!firstFan){replacement=vertices.Count;vertices.Add(vertices[vertex]);normals.Add(normals[vertex]);uv.Add(uv[vertex]);colors.Add(colors[vertex]);}
                    firstFan=false;var queue=new Queue<int>();queue.Enqueue(first);pending.Remove(first);
                    while(queue.Count>0)
                    {
                        int face=queue.Dequeue();
                        for(int corner=0;corner<3;corner++)if(original[face*3+corner]==vertex)triangles[face*3+corner]=replacement;
                        foreach(int neighbour in incident[vertex])
                        {
                            if(!pending.Contains(neighbour))continue;bool sharedEdge=false;
                            for(int a=0;a<3;a++)for(int b=0;b<3;b++)
                                if(original[face*3+a]!=vertex&&original[face*3+a]==original[neighbour*3+b])sharedEdge=true;
                            if(sharedEdge){pending.Remove(neighbour);queue.Enqueue(neighbour);}
                        }
                    }
                }
            }
        }
        private static bool DryFace(Vector3 a,Vector3 b,Vector3 c)
        {
            return !WorldTraversal.IsOpenWater(a,.01f)&&!WorldTraversal.IsOpenWater(b,.01f)&&!WorldTraversal.IsOpenWater(c,.01f)&&
                !WorldTraversal.IsOpenWater((a+b)*.5f,.01f)&&!WorldTraversal.IsOpenWater((b+c)*.5f,.01f)&&!WorldTraversal.IsOpenWater((c+a)*.5f,.01f)&&
                !WorldTraversal.IsOpenWater((a+b+c)/3,.01f);
        }
        private static float Feather(float low,float high,float value){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(low,high,value));}
        private static void AddEdge(Dictionary<Edge,Boundary> edges,int a,int b)
        {var edge=new Edge(a,b);Boundary boundary;if(edges.TryGetValue(edge,out boundary)){boundary.Count++;edges[edge]=boundary;}else edges.Add(edge,new Boundary(a,b));}
        private void Draw()
        {
            float remaining=remainingLifetime!=null?remainingLifetime():1;
            float opacity=Mathf.Min(.35f+.65f*Mathf.Clamp01(age/.06f),Mathf.Clamp01(remaining/.30f));
            block.SetFloat("_Deposit",(float)kind);block.SetFloat("_Opacity",opacity);block.SetFloat("_Age",age);surface.SetPropertyBlock(block);
        }
        private void LateUpdate()
        {
            if(retired)return;
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||session==null||session!=GameSession.Instance||session.Player!=owner||!session.HasStarted||session.CombatEffectsEnded){Retire();return;}
            if(session.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;
            if(revision!=WorldTraversal.Revision)
            {
                revision=WorldTraversal.Revision;var previous=mesh;mesh=Build();GetComponent<MeshFilter>().sharedMesh=mesh;if(previous!=null)Destroy(previous);
            }
            Draw();
        }
        internal void Retire(){if(retired)return;retired=true;live.Remove(this);gameObject.SetActive(false);Destroy(gameObject);}
        private void OnDisable(){Retire();}
        private void OnDestroy(){live.Remove(this);if(mesh!=null)Destroy(mesh);mesh=null;}
    }
}
