using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall
{
    // One connected, terrain-conforming surface per cast. Damage footprints
    // remain owned by FlameRide; no stamped flame models or per-point materials.
    internal sealed class GroundFlameTrail
    {
        private const int Columns=9, MaximumRows=384;
        private const float HalfWidth=1.05f, Step=.22f;
        private readonly GameObject root;
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly List<Vector3> vertices=new List<Vector3>(MaximumRows*Columns);
        private readonly List<Vector2> coordinates=new List<Vector2>(MaximumRows*Columns);
        private readonly List<Vector3> sides=new List<Vector3>(MaximumRows*Columns);
        private readonly List<int> triangles=new List<int>((MaximumRows-1)*(Columns-1)*6);
        private readonly Vector3[] path=new Vector3[24];
        private readonly float[] birth=new float[24];
        private readonly Vector3[] row=new Vector3[Columns],prior=new Vector3[Columns];
        private readonly bool[] dry=new bool[Columns],priorDry=new bool[Columns];
        private int revision=-1,oldCursor=-1,oldCount=-1;
        private float oldNewest=-1;
        private struct RowKey : System.IEquatable<RowKey>
        {
            internal Vector3 Center,Tangent,PreviousCenter,PreviousTangent;
            internal float Width,PreviousWidth;
            internal bool Connected;
            public bool Equals(RowKey b){return Center.Equals(b.Center)&&Tangent.Equals(b.Tangent)&&PreviousCenter.Equals(b.PreviousCenter)&&PreviousTangent.Equals(b.PreviousTangent)&&Width==b.Width&&PreviousWidth==b.PreviousWidth&&Connected==b.Connected;}
            public override bool Equals(object b){return b is RowKey key&&Equals(key);}
            public override int GetHashCode(){unchecked{return (((((Center.GetHashCode()*397^Tangent.GetHashCode())*397^PreviousCenter.GetHashCode())*397^PreviousTangent.GetHashCode())*397^Width.GetHashCode())*397^PreviousWidth.GetHashCode())*397^(Connected?1:0);}}
        }
        private sealed class CachedRow
        {
            internal readonly Vector3[] Points=new Vector3[Columns];
            internal readonly bool[] Dry=new bool[Columns];
            internal int Faces,Stamp;
        }
        private readonly Dictionary<RowKey,CachedRow> rowCache=new Dictionary<RowKey,CachedRow>();
        private readonly Stack<CachedRow> spareRows=new Stack<CachedRow>();
        private readonly List<RowKey> expiredRows=new List<RowKey>();
        private RowKey previousRow;
        private int buildStamp;

        internal GroundFlameTrail()
        {
            root=new GameObject("Continuous ground flame / connected flow surface");
            mesh=new Mesh{name="Continuous terrain flame ribbon"};mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh=mesh;
            material=new Material(Resources.Load<Shader>("GroundFlameTrail"));
            material.SetTexture("_FlowNoise",SkillFlowVolume.FlowNoise);
            material.SetFloat("_Steps",EffectPreferences.ReducedEffects||Application.isMobilePlatform?8:12);
            var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(Camera.main!=null)Camera.main.depthTextureMode|=DepthTextureMode.Depth;
        }

        internal void Draw(Vector3[] points,float[] ages,int count,int cursor,float now,float life)
        {using(PerformanceDiagnostics.Measure(PerformanceDiagnostics.Section.FlameTrailBuild))DrawTrail(points,ages,count,cursor,now,life);}
        private void DrawTrail(Vector3[] points,float[] ages,int count,int cursor,float now,float life)
        {
            material.SetFloat("_Clock",now);material.SetFloat("_Life",life);
            if(count==0)return;
            int newest=(cursor+points.Length-1)%points.Length;
            // Geometry is static between deposits. Shader ages all samples using
            // the producer's clock, including pauses and independent tail expiry.
            if(oldCursor==cursor&&oldCount==count&&oldNewest==ages[newest]&&revision==WorldTraversal.Revision)return;
            if(revision!=WorldTraversal.Revision)
            {foreach(var cached in rowCache.Values)spareRows.Push(cached);rowCache.Clear();}
            oldCursor=cursor;oldCount=count;oldNewest=ages[newest];revision=WorldTraversal.Revision;buildStamp++;
            int start=count==points.Length?cursor:0,n=0;
            for(int i=0;i<count;i++)
            {
                int slot=(start+i)%points.Length;
                if(now-ages[slot]>=life)continue;
                path[n]=points[slot];birth[n++]=ages[slot];
            }
            vertices.Clear();coordinates.Clear();sides.Clear();triangles.Clear();
            int rows=0;
            for(int i=0;i<n;i++)
            {
                Vector3 tangent=i+1<n?CombatFx.Flat(path[i+1]-path[i]):i>0?CombatFx.Flat(path[i]-path[i-1]):Vector3.forward;
                if(tangent.sqrMagnitude<.0001f)tangent=Vector3.forward;
                tangent.Normalize();
                // Do not bridge a teleport, an expired gap or a sharp reversal.
                bool connect=i>0&&(path[i]-path[i-1]).sqrMagnitude<6.25f&&birth[i]-birth[i-1]<.5f;
                if(connect)
                {
                    Vector3 travel=CombatFx.Flat(path[i]-path[i-1]);
                    Vector3 before=i>1?CombatFx.Flat(path[i-1]-path[i-2]).normalized:travel.normalized;
                    if(Vector3.Dot(before,travel.normalized)<-.65f)connect=false;
                }
                if(!connect)
                {
                    AddRow(path[i]-tangent*.35f,tangent,birth[i],false,ref rows,0);
                    AddRow(path[i],tangent,birth[i],true,ref rows,1);
                }
                else
                {
                    Vector3 travel=CombatFx.Flat(path[i]-path[i-1]);
                    int steps=Mathf.Max(1,Mathf.CeilToInt(travel.magnitude/Step));
                    for(int step=1;step<=steps;step++)
                    {
                        float t=step/(float)steps;
                        AddRow(Vector3.Lerp(path[i-1],path[i],t),travel.normalized,
                            Mathf.Lerp(birth[i-1],birth[i],t),true,ref rows,1);
                    }
                }
                bool end=i==n-1||(path[i+1]-path[i]).sqrMagnitude>=6.25f||birth[i+1]-birth[i]>=.5f;
                if(end)AddRow(path[i]+tangent*.35f,tangent,birth[i],true,ref rows,0);
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,coordinates);mesh.SetUVs(1,sides);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var bounds=mesh.bounds;bounds.Expand(Vector3.up*3.4f);mesh.bounds=bounds;
            expiredRows.Clear();
            foreach(var entry in rowCache)if(entry.Value.Stamp!=buildStamp){expiredRows.Add(entry.Key);spareRows.Push(entry.Value);}
            foreach(var key in expiredRows)rowCache.Remove(key);
        }

        private void AddRow(Vector3 center,Vector3 tangent,float born,bool connect,ref int rows,float width)
        {
            if(rows>=MaximumRows)return;
            Vector3 side=Vector3.Cross(Vector3.up,tangent);
            int first=vertices.Count;
            bool joined=connect&&rows>0;
            var key=new RowKey{Center=center,Tangent=tangent,Width=width,Connected=joined,
                PreviousCenter=joined?previousRow.Center:Vector3.zero,PreviousTangent=joined?previousRow.Tangent:Vector3.zero,PreviousWidth=joined?previousRow.Width:0};
            CachedRow cached;bool found=rowCache.TryGetValue(key,out cached);
            if(!found){cached=spareRows.Count>0?spareRows.Pop():new CachedRow();cached.Faces=0;rowCache.Add(key,cached);}
            cached.Stamp=buildStamp;
            for(int x=0;x<Columns;x++)
            {
                float across=x/(float)(Columns-1)*2-1;
                if(!found)
                {
                    Vector3 point=center+side*(across*HalfWidth*width);
                    point.y=WorldTraversal.SurfaceHeight(point,.025f);cached.Points[x]=point;
                    cached.Dry[x]=!WorldTraversal.IsOpenWater(point,.01f)&&CombatSight.Area(center,point);
                }
                row[x]=cached.Points[x];dry[x]=cached.Dry[x];
                vertices.Add(row[x]);coordinates.Add(new Vector2(across,born));
                sides.Add(new Vector3(side.x,width,side.z));
            }
            if(joined)for(int x=0;x<Columns-1;x++)
            {
                if(!found)
                {
                    if(FaceClear(prior[x],row[x],prior[x+1],priorDry[x]&&dry[x]&&priorDry[x+1],center))cached.Faces|=1<<(x*2);
                    if(FaceClear(prior[x+1],row[x],row[x+1],priorDry[x+1]&&dry[x]&&dry[x+1],center))cached.Faces|=1<<(x*2+1);
                }
                if((cached.Faces&(1<<(x*2)))!=0){triangles.Add(first-Columns+x);triangles.Add(first+x);triangles.Add(first-Columns+x+1);}
                if((cached.Faces&(1<<(x*2+1)))!=0){triangles.Add(first-Columns+x+1);triangles.Add(first+x);triangles.Add(first+x+1);}
            }
            for(int x=0;x<Columns;x++){prior[x]=row[x];priorDry[x]=dry[x];}
            previousRow=key;rows++;
        }

        private bool FaceClear(Vector3 pa,Vector3 pb,Vector3 pc,bool supported,Vector3 source)
        {
            if(!supported||Mathf.Max(pa.y,Mathf.Max(pb.y,pc.y))-Mathf.Min(pa.y,Mathf.Min(pb.y,pc.y))>.28f)return false;
            if(WorldTraversal.IsOpenWater((pa+pb)*.5f,.01f)||WorldTraversal.IsOpenWater((pb+pc)*.5f,.01f)||
                WorldTraversal.IsOpenWater((pc+pa)*.5f,.01f)||WorldTraversal.IsOpenWater((pa+pb+pc)/3,.01f))return false;
            return CombatSight.VisualTriangle(source,pa,pb,pc);
        }

        internal void Dispose()
        {
            if(root!=null){root.SetActive(false);Object.Destroy(root);}
            if(mesh!=null)Object.Destroy(mesh);if(material!=null)Object.Destroy(material);
        }
        internal void SetVisible(bool visible){if(root!=null)root.SetActive(visible);}
    }
}
