using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        static readonly Dictionary<int,Mesh[]> naturalTrees=new Dictionary<int,Mesh[]>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetNaturalTrees(){foreach(var pair in naturalTrees)foreach(var mesh in pair.Value)if(mesh!=null)Object.Destroy(mesh);naturalTrees.Clear();}
        static bool BuildNaturalTree(Transform parent,WorldResources r,Vector3 p,float size,int seed)
        {
            if(!NaturalWorldMaterial.Enabled||!SurfaceTextureLibrary.Enabled)return false;
            int variant=(seed%3+3)%3;Mesh[] meshes;
            if(!naturalTrees.TryGetValue(variant,out meshes))
            {
                meshes=CreateNaturalTreeMeshes(variant,Application.isMobilePlatform && System.Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW")!="performance-tree-geometry-ab");naturalTrees.Add(variant,meshes);
            }
            Transform root=Region(parent,"Natural branching woodland tree");root.localPosition=p;root.localScale=Vector3.one*size;root.localRotation=Quaternion.Euler(0,seed*31,0);
            for(int i=0;i<2;i++)
            {
                var go=new GameObject(i==0?"Tapered trunk roots and twigs":"Open individual leaf canopy");go.transform.SetParent(root,false);
                go.AddComponent<MeshFilter>().sharedMesh=meshes[i];var renderer=go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=r.Material(i==0?new Color(.34f,.25f,.17f):new Color(.40f,.53f,.29f),false,i==0?VisualSurface.Wood:VisualSurface.Foliage);
                if(i==1&&renderer.sharedMaterial.HasProperty("_VertexTint")){NaturalWorldMaterial.LivingLeaf(renderer.sharedMaterial);renderer.sharedMaterial.SetFloat("_VertexTint",1);renderer.sharedMaterial.SetFloat("_Wind",1);}
            }
            CameraOcclusionSurface.MarkHierarchy(root.gameObject);return true;
        }
        internal static Mesh[] CreateNaturalTreeMeshes(int variant,bool compact)
        {
            var bark=new DetailBatch(compact);var leaf=new DetailBatch(compact);var random=new System.Random(18943+variant);
            bark.Branch(Vector3.zero,new Vector3(.06f,1.6f,0),.18f,.12f,true);
            bark.Branch(new Vector3(.06f,1.6f,0),new Vector3(.10f,2.9f,.05f),.12f,.035f,true);
            for(int i=0;i<5;i++)
            {
                float a=i*2.39996f+variant*.3f;Vector3 direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                float height=1.65f+i*.28f;Vector3 fork=direction*(.72f+i%2*.12f)+Vector3.up*height;
                bark.Branch(new Vector3(.06f,height-.55f,0),fork,.065f,.025f);
                for(int branch=0;branch<3;branch++)
                {
                    float turn=a+(branch-1)*.8f;Vector3 spread=new Vector3(Mathf.Cos(turn),0,Mathf.Sin(turn));
                    Vector3 tip=fork+spread*.46f+Vector3.up*.26f;bark.Branch(fork,tip,.023f,.009f);
                    for(int j=0;j<9;j++)
                    {
                        Vector3 at=tip+new Vector3(Next(random,-.42f,.42f),Next(random,-.12f,.20f),Next(random,-.34f,.34f));
                        float shade=Next(random,.80f,1.15f);
                        leaf.Leaf(at,Next(random,0,6.28f),Next(random,.23f,.36f),new Color(shade,shade,shade,.8f),compact);
                    }
                }
                Vector3 rootTip=direction*.56f;bark.Branch(rootTip+Vector3.up*.025f,Vector3.up*.32f,.045f,.10f);
            }
            return new[]{bark.Mesh("Shared tapered tree wood"),leaf.Mesh("Shared individual canopy leaves")};
        }
        // Fixed seeded decoration, merged into three renderers. Traversal remains authored.
        static void BuildNaturalGroundDetail(Transform parent,WorldResources r,bool dungeon,int hub)
        {
            if(!NaturalWorldMaterial.Enabled)return;
            var fern=new DetailBatch(Application.isMobilePlatform);var litter=new DetailBatch(Application.isMobilePlatform);var gravel=new DetailBatch(Application.isMobilePlatform);
            var random=new System.Random(59117+(dungeon?97:hub*31));
            int count=dungeon?300:hub==0?850:360;float extent=WorldTraversal.SceneLocalArenaRadius-1;
            for(int i=0;i<count;i++)
            {
                Vector3 p=new Vector3(Next(random,-extent,extent),.055f,Next(random,-extent,extent));
                if(!WorldTraversal.IsWalkable(parent.TransformPoint(p),.35f))continue;
                // Main approaches, shop fronts, combat center and bridge stay easy to read.
                if(Mathf.Abs(p.x)<3.3f||(!dungeon&&p.z<-8&&Mathf.Abs(p.x)<9))continue;
                if(dungeon&&p.sqrMagnitude<85)continue;
                float size=Next(random,.055f,.15f);
                gravel.Pebble(p,new Vector3(size,size*.42f,size*.72f),Next(random,0,6.28f));
                if(!dungeon&&hub==0)
                {
                    litter.Leaf(p+new Vector3(.2f,.005f,0),Next(random,0,6.28f),Next(random,.07f,.16f),new Color(.74f,.66f,.47f,0));
                    if(p.x<-7&&i%3==0)
                    {
                        for(int frond=0;frond<5;frond++)
                        {
                            float a=frond*1.2566f+i;Vector3 direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                            for(int leaf=1;leaf<=5;leaf++)
                            {
                                float t=leaf/6f;Vector3 at=p+direction*(t*.46f)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.32f);
                                fern.Leaf(at,a+.65f,.13f*(1-t*.55f),new Color(.72f,.90f,.66f,t));
                                fern.Leaf(at,a-.65f,.13f*(1-t*.55f),new Color(.72f,.90f,.66f,t));
                            }
                        }
                    }
                }
            }
            gravel.Build(parent,r,"Merged natural gravel",r.Material(new Color(.46f,.44f,.38f),false,VisualSurface.Stone),false);
            Material foliage=r.Material(new Color(.37f,.49f,.26f),false,VisualSurface.Foliage);
            if(foliage.HasProperty("_VertexTint")){NaturalWorldMaterial.LivingLeaf(foliage);foliage.SetFloat("_VertexTint",1);foliage.SetFloat("_Wind",1);}
            fern.Build(parent,r,"Merged woodland fern fronds",foliage,true);
            Material leaves=r.Material(new Color(.43f,.35f,.20f),false,VisualSurface.Foliage);
            if(leaves.HasProperty("_VertexTint")){NaturalWorldMaterial.LivingLeaf(leaves);leaves.SetFloat("_VertexTint",1);}
            litter.Build(parent,r,"Merged forest leaf litter",leaves,true);
        }
        static float Next(System.Random random,float min,float max){return min+(float)random.NextDouble()*(max-min);}
        sealed class DetailBatch
        {
            // Compact geometry is opt-in. Main tree trunks retain the original
            // topology so collision bounds remain identical.
            readonly bool compact;
            internal DetailBatch(bool compact=false) { this.compact=compact; }
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<int> triangles=new List<int>();readonly List<Color> colors=new List<Color>();readonly List<Vector2> uv=new List<Vector2>();
            void Vertex(Vector3 point,Color color,Vector2 tex=default(Vector2)){vertices.Add(point);colors.Add(color);uv.Add(tex);}
            void Face(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
            void EdgeQuad(int a,int b,int c,int d)
            {
                // Keep the thin edge's lit normals separate from the curved
                // upper/lower skin. Welded positions still close the volume.
                int n=vertices.Count;
                Vertex(vertices[a],colors[a],uv[a]);Vertex(vertices[b],colors[b],uv[b]);Vertex(vertices[c],colors[c],uv[c]);Vertex(vertices[d],colors[d],uv[d]);
                Face(n,n+1,n+2);Face(n+2,n+1,n+3);
            }
            internal void Branch(Vector3 from,Vector3 to,float radius,float tipRadius,bool preserveTrunk=false)
            {
                Vector3 axis=(to-from).normalized;Vector3 tangent=Vector3.Cross(axis,Mathf.Abs(axis.y)>.95f?Vector3.forward:Vector3.up).normalized;Vector3 bitangent=Vector3.Cross(axis,tangent);
                int n=vertices.Count;int sides=compact&&!preserveTrunk?6:12,rings=compact&&!preserveTrunk?4:7;
                float phase=from.x*17+from.z*9+to.y*3;
                Vector3 bend=tangent*Mathf.Sin(phase)*radius*.42f+bitangent*Mathf.Cos(phase*1.7f)*radius*.3f;
                for(int ring=0;ring<rings;ring++)for(int i=0;i<sides;i++)
                {
                    float t=ring/(float)(rings-1),a=i*Mathf.PI*2/sides;
                    float r=Mathf.Lerp(radius,tipRadius,t)*(1+Mathf.Sin(a*3+phase)*.065f+Mathf.Sin(a*5-phase)*.028f);
                    Vector3 at=Vector3.Lerp(from,to,t)+bend*Mathf.Sin(t*Mathf.PI);
                    Vertex(at+(tangent*Mathf.Cos(a)+bitangent*Mathf.Sin(a))*r,Color.white,new Vector2(i/(float)sides,t));
                }
                for(int ring=0;ring<rings-1;ring++)for(int i=0;i<sides;i++)
                {int next=(i+1)%sides,a=n+ring*sides+i,b=n+ring*sides+next;Face(a,b,a+sides);Face(b,b+sides,a+sides);}
                int bottom=vertices.Count;Vertex(from,Color.white);int top=vertices.Count;Vertex(to,Color.white);
                for(int i=0;i<sides;i++){int j=(i+1)%sides;Face(bottom,n+j,n+i);Face(top,n+(rings-1)*sides+i,n+(rings-1)*sides+j);}
            }
            internal Mesh Mesh(string name)
            {
                var mesh=new Mesh{name=name};if(vertices.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                return mesh;
            }
            internal void Leaf(Vector3 p,float angle,float length,Color color,bool preserveOutline=false)
            {
                // Closed curved blade: narrow stem, rounded shoulders, pointed
                // tip, a raised midrib and thin solid edge. Each leaf owns UVs.
                int n=vertices.Count;int rows=compact&&!preserveOutline?3:5;const int cols=3;int layer=rows*cols;
                float tilt=Mathf.Sin(angle*3+p.x*7+p.z*11)*.48f;
                Vector3 forward=new Vector3(Mathf.Cos(angle),tilt,Mathf.Sin(angle)).normalized*length;
                Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
                Vector3 normal=Vector3.Cross(forward,side).normalized;
                for(int skin=0;skin<2;skin++)for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)
                {
                    float t=row/(float)(rows-1),s=col/(float)(cols-1)*2-1;
                    float width=(.018f+Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.8f)*.21f)*length;
                    float arch=Mathf.Sin(t*Mathf.PI)*length*(.13f-.10f*s*s)-t*t*length*.07f;
                    Vector3 at=p+forward*t+side*(s*width)+normal*(arch+(skin==0?1:-1)*length*.009f);
                    Vertex(at,color,new Vector2((s+1)*.5f,t));
                }
                if(preserveOutline)
                {
                    // Keep every perimeter point and the central rib endpoints/midpoint.
                    // Omit only the two redundant inner samples on each skin.
                    for(int row=0;row<rows-1;row+=2)for(int sideIndex=0;sideIndex<2;sideIndex++)
                    {
                        int column=sideIndex*2,a=n+row*cols+1,b=n+row*cols+column,c=b+cols,d=b+cols*2,e=a+cols*2;
                        if(sideIndex==0){LeafFace(a,b,c,layer);LeafFace(a,c,e,layer);LeafFace(e,c,d,layer);}
                        else{LeafFace(a,c,b,layer);LeafFace(a,e,c,layer);LeafFace(e,d,c,layer);}
                    }
                }
                else for(int row=0;row<rows-1;row++)for(int col=0;col<cols-1;col++)
                {int a=n+row*cols+col,b=a+1,c=a+cols,d=c+1;Face(a,c,b);Face(b,c,d);Face(a+layer,b+layer,c+layer);Face(b+layer,d+layer,c+layer);}
                for(int row=0;row<rows-1;row++)
                {int a=n+row*cols,b=a+cols;EdgeQuad(a,a+layer,b,b+layer);a+=cols-1;b+=cols-1;EdgeQuad(a,b,a+layer,b+layer);}
                for(int col=0;col<cols-1;col++)
                {int a=n+col,b=a+1;EdgeQuad(a,b,a+layer,b+layer);a+=layer-cols;b+=layer-cols;EdgeQuad(a,a+layer,b,b+layer);}
            }
            void LeafFace(int a,int b,int c,int layer){Face(a,b,c);Face(a+layer,c+layer,b+layer);}
            internal void Pebble(Vector3 p,Vector3 scale,float angle)
            {
                int n=vertices.Count;int rings=compact?3:5,sides=compact?8:12;
                for(int ring=0;ring<rings;ring++)for(int i=0;i<sides;i++)
                {
                    float t=(ring+1)/(float)(rings+1)*Mathf.PI,a=angle+i*Mathf.PI*2/sides;
                    float irregular=1+.075f*Mathf.Sin(a*3+angle)+.045f*Mathf.Cos(a*5-ring);
                    Vertex(p+new Vector3(Mathf.Cos(a)*Mathf.Sin(t)*scale.x*irregular,Mathf.Cos(t)*scale.y,Mathf.Sin(a)*Mathf.Sin(t)*scale.z*irregular),Color.white);
                }
                for(int ring=0;ring<rings-1;ring++)for(int i=0;i<sides;i++)
                {int a=n+ring*sides+i,b=n+ring*sides+(i+1)%sides;Face(a,b,a+sides);Face(b,b+sides,a+sides);}
                int top=vertices.Count;Vertex(p+Vector3.up*scale.y,Color.white);int bottom=vertices.Count;Vertex(p-Vector3.up*scale.y,Color.white);
                for(int i=0;i<sides;i++){int j=(i+1)%sides;Face(top,n+j,n+i);Face(bottom,n+(rings-1)*sides+i,n+(rings-1)*sides+j);}
            }
            internal void Grass(Vector3 p,float angle,float height,Color color)
            {
                int rings=compact?5:7,sides=compact?4:6;int n=vertices.Count;
                Vector3 across=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),bend=new Vector3(-across.z,0,across.x);
                for(int ring=0;ring<rings;ring++)for(int side=0;side<sides;side++)
                {
                    float t=ring/(float)(rings-1),a=side*Mathf.PI*2/sides;
                    float width=Mathf.Lerp(.009f,.0006f,t)+Mathf.Sin(t*Mathf.PI)*.006f;
                    Vector3 at=p+Vector3.up*(height*t)+bend*(height*.38f*t*t);
                    Color tint=color;tint.a=t;
                    Vertex(at+across*(Mathf.Cos(a)*width)+bend*(Mathf.Sin(a)*.0013f),tint,new Vector2(side/(float)sides,t));
                }
                for(int ring=0;ring<rings-1;ring++)for(int side=0;side<sides;side++)
                {int a=n+ring*sides+side,b=n+ring*sides+(side+1)%sides;Face(a,a+sides,b);Face(b,a+sides,b+sides);}
                int bottom=vertices.Count;Vertex(p,color);int top=vertices.Count;Vertex(p+Vector3.up*height+bend*(height*.38f),color);
                for(int side=0;side<sides;side++){int j=(side+1)%sides;Face(bottom,n+side,n+j);Face(top,n+(rings-1)*sides+j,n+(rings-1)*sides+side);}
            }
            internal void Build(Transform parent,WorldResources r,string name,Material material,bool vegetation)
            {
                if(vertices.Count==0)return;
                GameObject go=Geometry(parent,r,name,vertices,triangles,material);Mesh mesh=go.GetComponent<MeshFilter>().sharedMesh;mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.RecalculateTangents();
            }
        }
    }
}
