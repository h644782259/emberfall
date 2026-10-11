using UnityEngine;
using System.Collections.Generic;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        private static void BuildWaterSurface(Transform parent,WorldResources r,string name,Vector3[] path,float width,float height,WaterEnvironment environment)
        {
            if(path==null||path.Length<2)return;
            var profile=new WaterPresentation(environment,width);
            // One connected surface with continuous shallow/deep color, rather
            // than stacked straight strips. Every vertex stays in the authored
            // ribbon; small bank irregularities move only toward its interior.
            Vector3[] samples=NaturalWaterSamples(path);
            Material water=r.Material(new Color(.20f,.36f,.36f)*profile.CurrentBrightness,false,VisualSurface.Water);
            if(water.HasProperty("_VertexTint")){water.SetFloat("_VertexTint",1);water.SetFloat("_GrainStrength",.10f);water.SetFloat("_Relief",.001f);water.SetFloat("_Glossiness",.70f);}
            const int across=11;var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();var uv=new List<Vector2>();
            for(int i=0;i<samples.Length;i++)
            {
                Vector3 normal;float miter;RibbonSection(samples,i,out normal,out miter);
                for(int j=0;j<across;j++)
                {
                    float s=j/(float)(across-1)*2-1;
                    float inward=.024f+.048f*(.5f+.5f*Mathf.Sin(i*.63f+s*3.4f));
                    Vector3 at=samples[i]+normal*(s*(profile.Width*.5f*miter-inward));at.y=height;
                    vertices.Add(at);uv.Add(new Vector2(j/(float)(across-1),i*.16f));
                    float bank=Mathf.SmoothStep(0,1,Mathf.Pow(Mathf.Abs(s),1.8f));
                    Color c=Color.Lerp(new Color(.52f,.72f,.84f),new Color(1.05f,1.23f,1.10f),bank);
                    c*=.95f+.05f*Mathf.Sin(i*.47f+j*.61f);c.a=1;colors.Add(c);
                    if(i==0||j==0)continue;
                    int a=(i-1)*across+j-1,b=a+1,cidx=i*across+j-1,d=cidx+1;
                    if(Vector3.Cross(vertices[cidx]-vertices[a],vertices[b]-vertices[a]).y>0){triangles.Add(a);triangles.Add(cidx);triangles.Add(b);triangles.Add(b);triangles.Add(cidx);triangles.Add(d);}
                    else{triangles.Add(a);triangles.Add(b);triangles.Add(cidx);triangles.Add(b);triangles.Add(d);triangles.Add(cidx);}
                }
            }
            var go=Geometry(parent,r,name+" continuous rippling water",vertices,triangles,water);
            var mesh=go.GetComponent<MeshFilter>().sharedMesh;mesh.SetColors(colors);mesh.SetUVs(0,uv);
            go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            BuildShoreWaterContact(parent,r,name,samples,profile.Width,height);
        }
        private static Vector3[] NaturalWaterSamples(Vector3[] path)
        {
            var result=new List<Vector3>{path[0]};
            for(int segment=1;segment<path.Length;segment++)
            {
                int steps=Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(path[segment-1],path[segment])/.42f),1,128);
                for(int step=1;step<=steps;step++)result.Add(Vector3.Lerp(path[segment-1],path[segment],step/(float)steps));
            }
            return result.ToArray();
        }
        private static void BuildNaturalStreamBed(Transform parent,WorldResources r,Vector3[] path,float width,float height)
        {
            Vector3[] samples=NaturalWaterSamples(path);var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();var colors=new List<Color>();
            const int across=7;
            for(int i=0;i<samples.Length;i++)
            {
                Vector3 normal;float miter;RibbonSection(samples,i,out normal,out miter);
                for(int j=0;j<across;j++)
                {
                    float s=j/(float)(across-1)*2-1;
                    float erosion=.04f+.08f*(.5f+.5f*Mathf.Sin(i*.51f+s*4.7f));
                    Vector3 at=samples[i]+normal*(s*(width*.5f*miter-erosion));at.y=height;vertices.Add(at);uv.Add(new Vector2(j/(float)(across-1),i*.18f));
                    float shade=.88f+.12f*Mathf.Sin(i*.91f+j*1.37f);colors.Add(new Color(shade,shade,shade,1));
                    if(i==0||j==0)continue;
                    int a=(i-1)*across+j-1,b=a+1,c=i*across+j-1,d=c+1;
                    if(Vector3.Cross(vertices[c]-vertices[a],vertices[b]-vertices[a]).y>0)triangles.AddRange(new[]{a,c,b,b,c,d});else triangles.AddRange(new[]{a,b,c,b,d,c});
                }
            }
            var material=r.Material(new Color(.35f,.34f,.25f),false,VisualSurface.Stone);NaturalWorldMaterial.Trail(material);
            if(material.HasProperty("_VertexTint"))material.SetFloat("_VertexTint",1);
            var go=Geometry(parent,r,"Eroded gravel stream shoulders",vertices,triangles,material);var mesh=go.GetComponent<MeshFilter>().sharedMesh;mesh.SetUVs(0,uv);mesh.SetColors(colors);
        }
        // Same miter as the rendered ribbon: the waterline must meet the real surface edge.
        private static void RibbonSection(Vector3[] path,int i,out Vector3 normal,out float miter)
        {
            Vector3 before=i==0?path[1]-path[0]:path[i]-path[i-1];
            Vector3 after=i==path.Length-1?before:path[i+1]-path[i];
            Vector3 direction=(before.normalized+after.normalized).normalized;
            normal=new Vector3(-direction.z,0,direction.x);
            Vector3 sectionNormal=new Vector3(-before.z,0,before.x).normalized;
            miter=1f/Mathf.Max(.72f,Vector3.Dot(normal,sectionNormal));
        }
        private static void BuildShoreWaterContact(Transform parent,WorldResources r,string name,Vector3[] path,float width,float height)
        {
            // Static, merged bank details; cap malformed/very long authored paths without runtime updates.
            int sections=path.Length;
            Material damp=r.Material(new Color(.23f,.28f,.19f),false,VisualSurface.Stone);
            var wet=new List<Vector3>();var line=new List<Vector3>();
            var topTriangles=new List<int>();var lineTriangles=new List<int>();
            for(int side=-1;side<=1;side+=2)
            {
                int start=wet.Count;
                for(int i=0;i<sections;i++)
                {
                    Vector3 normal;float miter;RibbonSection(path,i,out normal,out miter);
                    float inward=.024f+.048f*(.5f+.5f*Mathf.Sin(i*.63f+side*3.4f));
                    Vector3 edge=path[i]+normal*(side*(width*.5f*miter-inward));edge.y=height;
                    Vector3 inner=edge-normal*(side*.022f),outer=edge+normal*(side*(.05f+.025f*Mathf.Sin(i*.81f)));
                    inner.y=outer.y=height+.002f;wet.Add(inner);wet.Add(outer);
                    line.Add(edge+Vector3.up*.004f);line.Add(edge-Vector3.up*.012f);
                    if(i==sections-1)continue;
                    int a=start+i*2,b=a+2;
                    if(side==1){topTriangles.Add(a);topTriangles.Add(a+1);topTriangles.Add(b);topTriangles.Add(a+1);topTriangles.Add(b+1);topTriangles.Add(b);}
                    else{topTriangles.Add(a);topTriangles.Add(b);topTriangles.Add(a+1);topTriangles.Add(a+1);topTriangles.Add(b);topTriangles.Add(b+1);}
                    // Back faces are added with independent vertices below to retain lit normals.
                    int[] face={a,b,a+1,a+1,b,b+1};lineTriangles.AddRange(face);
                }
            }
            int frontVertices=line.Count,frontIndices=lineTriangles.Count;
            for(int i=0;i<frontVertices;i++)line.Add(line[i]);
            for(int i=0;i<frontIndices;i+=3)
            {lineTriangles.Add(lineTriangles[i+2]+frontVertices);lineTriangles.Add(lineTriangles[i+1]+frontVertices);lineTriangles.Add(lineTriangles[i]+frontVertices);}
            Geometry(parent,r,name+" shore damp seam",wet,topTriangles,damp);
            Geometry(parent,r,name+" shore vertical waterline",line,lineTriangles,damp);
        }
        private static void BuildBridgeWaterContact(Transform parent,WorldResources r,Rect footprint,float deckTop)
        {
            // Wet contact/wear seams sit inside the authored bridge deck, never new obstacles.
            Material damp=r.Material(new Color(.14f,.20f,.20f),false,VisualSurface.Wood);
            float x=footprint.center.x,z=footprint.center.y;
            for(int side=-1;side<=1;side+=2)
            {
                Primitive(parent,"Bridge damp bank contact",PrimitiveType.Cube,new Vector3(x,deckTop+.002f,z+side*(footprint.height*.5f-.11f)),new Vector3(footprint.width-.08f,.004f,.16f),damp);
                Primitive(parent,"Bridge deck edge wear",PrimitiveType.Cube,new Vector3(x+side*(footprint.width*.5f-.08f),deckTop+.002f,z),new Vector3(.08f,.004f,footprint.height-.12f),damp);
            }
        }
    }
}
