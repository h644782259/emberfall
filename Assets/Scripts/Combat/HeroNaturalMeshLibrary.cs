using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Actual closed meshes, shared per shape. Cloth folds, fitted silhouettes and
    // skin contours exist in the geometry and continue to cast real shadows.
    internal static class HeroNaturalMeshLibrary
    {
        private static readonly Dictionary<string,Mesh> cache=new Dictionary<string,Mesh>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){foreach(var mesh in cache.Values)if(mesh!=null)Object.Destroy(mesh);cache.Clear();}
        private static Vector4 R(float y,float x,float z,float shift=0){return new Vector4(y,x,z,shift);}
        internal static Mesh Get(string kind)
        {
            Mesh result;if(cache.TryGetValue(kind,out result))return result;
            Vector4[] profile;float folds=0;
            switch(kind)
            {
                case "face":profile=new[]{R(.50f,.018f,.02f),R(.44f,.28f,.30f),R(.29f,.43f,.43f),R(.12f,.47f,.46f),R(-.06f,.43f,.42f,.025f),R(-.23f,.36f,.36f,.04f),R(-.38f,.24f,.27f,.035f),R(-.48f,.08f,.13f,.025f),R(-.50f,.015f,.025f)};break;
                case "shoulder":profile=new[]{R(.52f,.025f,.03f),R(.44f,.22f,.25f),R(.24f,.40f,.43f),R(-.02f,.50f,.49f),R(-.24f,.49f,.46f),R(-.43f,.38f,.36f),R(-.48f,.32f,.30f)};break;
                case "bracer":profile=new[]{R(.50f,.43f,.43f),R(.43f,.49f,.49f),R(.23f,.47f,.48f),R(-.15f,.42f,.43f),R(-.43f,.35f,.36f),R(-.50f,.38f,.40f)};break;
                case "boot":profile=new[]{R(.50f,.28f,.25f,-.15f),R(.35f,.31f,.27f,-.14f),R(.12f,.33f,.31f,-.10f),R(-.07f,.43f,.42f,.015f),R(-.24f,.49f,.50f,.015f),R(-.39f,.49f,.50f,.015f),R(-.50f,.44f,.47f,.015f)};break;
                case "sole":profile=new[]{R(.50f,.43f,.46f,.015f),R(.28f,.49f,.50f,.015f),R(-.20f,.49f,.50f,.015f),R(-.50f,.44f,.47f,.015f)};break;
                case "plate":profile=new[]{R(.50f,.29f,.20f),R(.42f,.42f,.35f),R(.23f,.50f,.47f),R(-.08f,.47f,.50f),R(-.31f,.40f,.42f),R(-.50f,.31f,.30f)};break;
                case "knee":profile=new[]{R(.50f,.035f,.025f),R(.38f,.30f,.25f),R(.13f,.47f,.44f),R(-.13f,.44f,.45f),R(-.38f,.26f,.25f),R(-.50f,.035f,.035f)};break;
                case "panel":profile=new[]{R(.50f,.37f,.25f),R(.44f,.44f,.43f),R(.23f,.48f,.46f,.08f),R(-.08f,.46f,.43f,-.03f),R(-.31f,.47f,.45f,.04f),R(-.45f,.50f,.44f),R(-.50f,.46f,.32f)};folds=.034f;break;
                case "strap":profile=new[]{R(.50f,.43f,.32f),R(.44f,.48f,.43f),R(.15f,.47f,.47f,.11f),R(-.18f,.48f,.46f,.13f),R(-.44f,.47f,.43f),R(-.50f,.43f,.32f)};folds=.012f;break;
                case "hat":profile=new[]{R(0,.48f,.46f),R(.10f,.44f,.43f,-.01f),R(.27f,.35f,.36f,-.05f),R(.46f,.27f,.28f,-.08f),R(.65f,.18f,.20f,-.12f),R(.84f,.11f,.12f,-.17f),R(1,.01f,.02f,-.22f)};folds=.018f;break;
                case "skirt":profile=new[]{R(0,.48f,.31f),R(.06f,.47f,.30f),R(.20f,.43f,.29f),R(.37f,.38f,.27f),R(.53f,.33f,.235f),R(.62f,.305f,.22f)};folds=.010f;break;
                case "helmet":profile=new[]{R(-.50f,.45f,.45f),R(-.24f,.48f,.48f),R(.03f,.47f,.47f),R(.26f,.39f,.40f),R(.43f,.23f,.25f),R(.50f,.02f,.03f)};break;
                case "neck":profile=new[]{R(.50f,.47f,.43f,-.025f),R(.28f,.43f,.40f,-.01f),R(.02f,.39f,.375f,.022f),R(-.23f,.42f,.40f),R(-.50f,.50f,.46f,-.025f)};break;
                case "gorget":profile=new[]{R(.50f,.43f,.39f),R(.40f,.46f,.42f),R(.17f,.435f,.405f),R(-.18f,.47f,.45f),R(-.40f,.50f,.48f),R(-.50f,.48f,.46f)};break;
                case "hair-lock":profile=new[]{R(.50f,.21f,.18f),R(.39f,.43f,.40f,-.04f),R(.15f,.49f,.48f,-.05f),R(-.13f,.43f,.44f,.025f),R(-.36f,.25f,.26f,.08f),R(-.50f,.02f,.03f,.11f)};folds=.016f;break;
                case "nose":profile=new[]{R(.50f,.08f,.10f,-.12f),R(.29f,.23f,.20f,-.05f),R(.02f,.37f,.47f,.05f),R(-.19f,.48f,.42f,.025f),R(-.38f,.36f,.28f,-.03f),R(-.50f,.10f,.11f,-.10f)};break;
                default:profile=new[]{R(.50f,.025f,.025f),R(.37f,.33f,.32f),R(.10f,.48f,.48f),R(-.16f,.45f,.46f),R(-.40f,.26f,.28f),R(-.50f,.025f,.025f)};break;
            }
            result=Loft("Natural hero / "+kind,Smooth(profile),28,folds,kind=="face");cache.Add(kind,result);return result;
        }
        internal static Mesh Torso(HeroClass hero)
        {
            string key="body/"+hero;Mesh mesh;if(cache.TryGetValue(key,out mesh))return mesh;
            float broad=hero==HeroClass.Vanguard?1.08f:hero==HeroClass.Ranger?.94f:1;
            var profile=new[]{R(.79f,.27f*broad,.18f),R(.89f,.29f*broad,.20f),R(1.03f,.30f*broad,.195f),R(1.20f,.33f*broad,.215f),R(1.40f,.365f*broad,.235f),R(1.54f,.39f*broad,.21f),R(1.63f,.33f*broad,.165f),R(1.70f,.145f,.11f),R(1.75f,.12f,.095f)};
            mesh=Loft("Continuous fitted hero torso / "+hero,Smooth(profile),32,.006f,false);
            var weights=new BoneWeight[mesh.vertexCount];var vertices=mesh.vertices;
            for(int i=0;i<weights.Length;i++){float upper=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.88f,1.34f,vertices[i].y));weights[i]=new BoneWeight{boneIndex0=0,boneIndex1=1,weight0=1-upper,weight1=upper};}
            mesh.boneWeights=weights;cache.Add(key,mesh);return mesh;
        }
        internal static Mesh EnemyTorso(bool guardian)
        {
            string key=guardian?"enemy-body/guardian":"enemy-body/goblin";
            Mesh mesh;if(cache.TryGetValue(key,out mesh))return mesh;
            float broad=guardian?1.45f:1;
            var profile=new[]{R(.79f,.28f*broad,.195f),R(.90f,.30f*broad,.215f),R(1.06f,.32f*broad,.22f),R(1.23f,.35f*broad,.24f),R(1.41f,.39f*broad,.25f),R(1.54f,.41f*broad,.23f),R(1.65f,.34f*broad,.17f),R(1.73f,.15f,.115f),R(1.78f,.12f,.095f)};
            mesh=Loft("Continuous anatomical enemy torso / "+(guardian?"Guardian":"Goblin"),Smooth(profile),32,.008f,false);
            var weights=new BoneWeight[mesh.vertexCount];var vertices=mesh.vertices;
            for(int i=0;i<weights.Length;i++){float upper=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.88f,1.34f,vertices[i].y));weights[i]=new BoneWeight{boneIndex0=0,boneIndex1=1,weight0=1-upper,weight1=upper};}
            mesh.boneWeights=weights;cache.Add(key,mesh);return mesh;
        }
        internal static Mesh Limb(bool leg)
        {
            string key=leg?"continuous-trousers":"continuous-sleeve";Mesh mesh;if(cache.TryGetValue(key,out mesh))return mesh;
            float length=leg?.68f:.56f,joint=leg?.37f:.30f;
            var profile=leg?new[]{R(.025f,.128f,.137f),R(-.06f,.134f,.146f),R(-.19f,.122f,.137f),R(-.32f,.104f,.113f),R(-.40f,.102f,.110f),R(-.51f,.105f,.116f),R(-.63f,.078f,.087f),R(-length,.072f,.079f)}:
                new[]{R(.08f,.025f,.028f),R(.025f,.103f,.118f),R(-.065f,.133f,.137f),R(-.19f,.118f,.119f),R(-.28f,.104f,.109f),R(-.36f,.108f,.105f),R(-.45f,.092f,.090f),R(-length,.070f,.074f)};
            mesh=Loft("Natural "+key,Smooth(profile),24,leg?.004f:.0035f,false);
            var weights=new BoneWeight[mesh.vertexCount];var vertices=mesh.vertices;
            for(int i=0;i<weights.Length;i++){float lower=Mathf.SmoothStep(0,1,Mathf.InverseLerp(joint-.08f,joint+.08f,-vertices[i].y));weights[i]=new BoneWeight{boneIndex0=0,boneIndex1=1,weight0=1-lower,weight1=lower};}
            mesh.boneWeights=weights;mesh.bindposes=new[]{Matrix4x4.identity,Matrix4x4.Translate(Vector3.up*joint)};cache.Add(key,mesh);return mesh;
        }
        internal static Mesh Hair()
        {
            const string key="continuous-swept-hair";Mesh mesh;if(cache.TryGetValue(key,out mesh))return mesh;
            // One connected scalp surface. The comb direction is shallow relief
            // in that surface, never separate tubular locks sitting on the face.
            const int rows=25,sides=64,width=sides+1;
            var vertices=new Vector3[rows*width];var uv=new Vector2[vertices.Length];var indices=new List<int>();
            for(int r=0;r<rows;r++)for(int s=0;s<=sides;s++)
            {
                float t=r/(float)(rows-1),angle=s==sides?0:s*Mathf.PI*2/sides;
                float sin=Mathf.Sin(angle),cos=Mathf.Cos(angle),front=Mathf.Max(0,sin),back=Mathf.Max(0,-sin);
                // Side-swept brow line flows into the temples and the lower nape.
                float hairline=-.115f-.105f*back+front*(.32f+.065f*cos)+.014f*Mathf.Pow(front,6)*Mathf.Sin(angle*5);
                float polar=Mathf.Lerp(.008f,Mathf.Acos(hairline/.55f),t),radial=Mathf.Sin(polar);
                float flow=angle*14+polar*5.5f+cos*2.3f;
                float relief=.008f*radial*Mathf.Sin(t*Mathf.PI*.90f)*(Mathf.Sin(flow)+.32f*Mathf.Sin(flow*2));
                int k=r*width+s;
                vertices[k]=new Vector3(cos*(.505f*radial+relief)+.020f*radial*radial, .55f*Mathf.Cos(polar)+.006f*radial*Mathf.Sin(flow), sin*(.485f*radial+relief)-.005f);
                uv[k]=new Vector2(s/(float)sides,t);
                if(r<rows-1&&s<sides){int next=k+width;indices.AddRange(new[]{k,k+1,next,k+1,next+1,next});}
            }
            // The sealed underside lies inside the head, below the hairline.
            Cap(indices,rows,width,sides,false);
            mesh=Finish("Continuous sculpted side-swept hair shell",vertices,uv,indices,rows,width,sides);cache.Add(key,mesh);return mesh;
        }
        internal static Mesh Hand()
        {
            const string key="hand-fingers";Mesh mesh;if(cache.TryGetValue(key,out mesh))return mesh;
            var parts=new List<CombineInstance>();var temporary=new List<Mesh>();
            var palm=Loft("palm",Smooth(new[]{R(.50f,.25f,.28f),R(.32f,.36f,.34f),R(.04f,.42f,.32f),R(-.24f,.40f,.29f),R(-.36f,.33f,.22f)}),20,0,false);temporary.Add(palm);parts.Add(new CombineInstance{mesh=palm,transform=Matrix4x4.identity});
            for(int i=0;i<4;i++)
            {
                float x=-.27f+i*.18f,bend=i==0||i==3?.06f:0;
                var finger=Tube("curved finger",new[]{new Vector3(x,-.27f,.035f),new Vector3(x,-.43f+bend,.06f),new Vector3(x,-.56f+bend,.14f),new Vector3(x,-.62f+bend,.22f)},new[]{.075f,.073f,.059f,.026f},10);temporary.Add(finger);parts.Add(new CombineInstance{mesh=finger,transform=Matrix4x4.identity});
            }
            var thumb=Tube("opposed thumb",new[]{new Vector3(.32f,.06f,.16f),new Vector3(.43f,-.08f,.23f),new Vector3(.38f,-.25f,.30f),new Vector3(.27f,-.33f,.30f)},new[]{.105f,.096f,.076f,.033f},12);temporary.Add(thumb);parts.Add(new CombineInstance{mesh=thumb,transform=Matrix4x4.identity});
            mesh=new Mesh{name="Natural hand with palm, knuckles and opposed curved fingers"};mesh.CombineMeshes(parts.ToArray(),true,true);mesh.RecalculateBounds();foreach(var item in temporary)Object.Destroy(item);cache.Add(key,mesh);return mesh;
        }
        internal static Mesh Tube(string name,Vector3[] centres,float[] radii,int sides)
        {
            int rows=(centres.Length-1)*5+1,width=sides+1;var vertices=new Vector3[rows*width];var uv=new Vector2[vertices.Length];var indices=new List<int>();
            for(int r=0;r<rows;r++)
            {
                float at=r/(float)(rows-1)*(centres.Length-1);int p=Mathf.Min(centres.Length-2,(int)at);float t=at-p;
                Vector3 a=centres[Mathf.Max(0,p-1)],b=centres[p],c=centres[p+1],d=centres[Mathf.Min(centres.Length-1,p+2)];
                Vector3 centre=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                Vector3 tangent=(c-b).normalized,right=Vector3.Cross(tangent,Vector3.forward).normalized;if(right.sqrMagnitude<.001f)right=Vector3.right;
                Vector3 front=Vector3.Cross(right,tangent).normalized;float radius=Mathf.Lerp(radii[p],radii[p+1],t);
                for(int s=0;s<=sides;s++){float angle=s==sides?0:s*Mathf.PI*2/sides;int k=r*width+s;vertices[k]=centre+(right*Mathf.Cos(angle)+front*Mathf.Sin(angle))*radius;uv[k]=new Vector2(s/(float)sides,r/(float)(rows-1));if(r<rows-1&&s<sides){int next=k+width;indices.AddRange(new[]{k,next,k+1,k+1,next,next+1});}}
            }
            Cap(indices,rows,width,sides,true);return Finish(name,vertices,uv,indices,rows,width,sides);
        }
        private static Vector4[] Smooth(Vector4[] source)
        {
            var rows=new List<Vector4>();
            for(int p=0;p<source.Length-1;p++)for(int i=0;i<4;i++)
            {float t=i/4f;Vector4 a=source[Mathf.Max(0,p-1)],b=source[p],c=source[p+1],d=source[Mathf.Min(source.Length-1,p+2)];Vector4 value=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);value.x=Mathf.Lerp(b.x,c.x,t);value.y=Mathf.Max(.001f,value.y);value.z=Mathf.Max(.001f,value.z);rows.Add(value);}
            rows.Add(source[source.Length-1]);return rows.ToArray();
        }
        private static Mesh Loft(string name,Vector4[] rings,int sides,float folds,bool face)
        {
            int width=sides+1;bool ascending=rings[rings.Length-1].x>rings[0].x;var vertices=new Vector3[rings.Length*width];var uv=new Vector2[vertices.Length];var indices=new List<int>();
            for(int r=0;r<rings.Length;r++)for(int s=0;s<=sides;s++)
            {
                Vector4 ring=rings[r];float angle=s==sides?0:s*Mathf.PI*2/sides,sin=Mathf.Sin(angle),cos=Mathf.Cos(angle);
                float relief=folds*(Mathf.Sin(angle*7+ring.x*5.3f)+.36f*Mathf.Sin(angle*13-ring.x*9));
                float x=cos*(ring.y+relief),z=sin*(ring.z+relief)+ring.w;
                if(face&&sin>0)
                {
                    float cheek=Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-.25f)*7,2)-Mathf.Pow((ring.x+.07f)*7,2));
                    float brow=Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-.20f)*8,2)-Mathf.Pow((ring.x-.12f)*12,2));
                    float socket=Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-.21f)*10,2)-Mathf.Pow((ring.x-.045f)*14,2));
                    z+=(cheek*.034f+brow*.045f-socket*.025f)*sin;
                }
                int k=r*width+s;vertices[k]=new Vector3(x,ring.x,z);uv[k]=new Vector2(s/(float)sides,r/(float)(rings.Length-1));
                if(r<rings.Length-1&&s<sides){int next=k+width;indices.AddRange(ascending?new[]{k,next,k+1,k+1,next,next+1}:new[]{k,k+1,next,k+1,next+1,next});}
            }
            Cap(indices,rings.Length,width,sides,ascending);return Finish(name,vertices,uv,indices,rings.Length,width,sides);
        }
        private static void Cap(List<int> indices,int rows,int width,int sides,bool reverse)
        {for(int s=1;s<sides-1;s++){indices.AddRange(reverse?new[]{0,s,s+1}:new[]{0,s+1,s});int start=(rows-1)*width;indices.AddRange(reverse?new[]{start,start+s+1,start+s}:new[]{start,start+s,start+s+1});}}
        private static Mesh Finish(string name,Vector3[] vertices,Vector2[] uv,List<int> indices,int rows,int width,int sides)
        {
            var mesh=new Mesh{name=name,vertices=vertices,uv=uv,triangles=indices.ToArray()};mesh.RecalculateNormals();
            var normals=mesh.normals;for(int r=0;r<rows;r++){int start=r*width,end=start+sides;normals[start]=normals[end]=(normals[start]+normals[end]).normalized;}mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }
}
