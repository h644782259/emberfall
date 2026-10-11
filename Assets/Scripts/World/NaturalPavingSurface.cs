using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // The thin surface keeps its real support height, while its perimeter is
    // broken, buried and blended into the same meadow used by the terrain.
    internal static class NaturalPavingSurface
    {
        internal static bool Applies(string name)
        {
            string n=name.ToLowerInvariant();
            return n=="courtyard stone foundations"||n=="forest floor"||n=="sunlit eastern clearing"||
                n=="caravan resting court"||n=="camp rug"||n=="buried north foundation"||n=="buried east foundation"||
                n.Contains("processional paving")||n=="courtyard mosaic"||n=="altar mosaic court"||
                n=="broken cathedral footprint";
        }

        internal static GameObject Create(Transform parent,WorldResources resources,string name,Vector2[] outline,float height,Material source)
        {
            Vector2 center=Vector2.zero;float area=0;
            for(int i=0;i<outline.Length;i++)
            {center+=outline[i];Vector2 b=outline[(i+1)%outline.Length];area+=outline[i].x*b.y-b.x*outline[i].y;}
            center/=outline.Length;
            var boundary=new List<Vector2>();
            for(int i=0;i<outline.Length;i++)
            {
                int first=area>=0?i:outline.Length-1-i,next=area>=0?(i+1)%outline.Length:(outline.Length-2-i+outline.Length)%outline.Length;
                Vector2 a=outline[first],b=outline[next];int steps=Mathf.Max(2,Mathf.CeilToInt(Vector2.Distance(a,b)/(name=="Camp rug"?.22f:.65f)));
                for(int k=0;k<steps;k++)boundary.Add(Vector2.Lerp(a,b,k/(float)steps));
            }
            int seed=23;unchecked{foreach(char c in name)seed=seed*31+c;}
            float phase=(seed&65535)*.013f,maxRadius=0;
            bool rug=name=="Camp rug",vegetation=name=="Forest floor"||name=="Sunlit eastern clearing";
            for(int i=0;i<boundary.Count;i++)
            {
                Vector2 delta=boundary[i]-center;float radius=delta.magnitude;
                float cut=(rug?.035f:.05f)+(.5f+.5f*Mathf.Sin(i*2.731f+phase))*(rug?.12f:Mathf.Min(.26f,radius*.075f));
                boundary[i]=center+delta*Mathf.Max(.7f,1-cut/Mathf.Max(radius,.001f));maxRadius=Mathf.Max(maxRadius,radius);
            }
            int count=boundary.Count,rings=Mathf.Clamp(Mathf.CeilToInt(maxRadius/.65f),rug?8:3,48);
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            vertices.Add(new Vector3(center.x,height,center.y));uv.Add(center);colors.Add(Color.white);
            for(int ring=1;ring<=rings;ring++)
            {
                float t=ring/(float)rings;
                for(int i=0;i<count;i++)
                {
                    Vector2 point=Vector2.Lerp(center,boundary[i],t);
                    float shoulder=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.79f)/.21f));
                    float y=Mathf.Lerp(height,rug?height-.006f:Mathf.Min(.004f,height*.15f),shoulder);
                    // Worn fabric gathers in small folds where its hem meets
                    // the stone. The standing area and its support stay flat.
                    if(rug)y+=shoulder*(1-shoulder)*.009f*Mathf.Sin(i*1.71f+phase);
                    vertices.Add(new Vector3(point.x,y,point.y));uv.Add(point);
                    colors.Add(new Color(1,1,1,1-shoulder));
                }
            }
            for(int i=0;i<count;i++){triangles.Add(0);triangles.Add(1+(i+1)%count);triangles.Add(1+i);}
            for(int ring=1;ring<rings;ring++)for(int i=0;i<count;i++)
            {
                int a=1+(ring-1)*count+i,b=1+(ring-1)*count+(i+1)%count,c=a+count,d=b+count;
                triangles.AddRange(new[]{a,b,c,b,d,c});
            }
            int outer=1+(rings-1)*count,bottom=vertices.Count;
            float baseHeight=rug?height-.012f:Mathf.Min(-.003f,height-.012f);
            for(int i=0;i<count;i++)
            {Vector2 p=boundary[i];vertices.Add(new Vector3(p.x,baseHeight,p.y));uv.Add(p);colors.Add(new Color(1,1,1,0));}
            int bottomCenter=vertices.Count;vertices.Add(new Vector3(center.x,baseHeight,center.y));uv.Add(center);colors.Add(Color.white);
            for(int i=0;i<count;i++)
            {
                int n=(i+1)%count;triangles.AddRange(new[]{outer+i,outer+n,bottom+i,outer+n,bottom+n,bottom+i,bottomCenter,bottom+i,bottom+n});
            }
            var mesh=new Mesh{name="Buried worn surface / "+name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();resources.Own(mesh);
            if(rug)
            {
                Vector2 minimum=outline[0],maximum=outline[0];
                foreach(Vector2 p in outline){minimum=Vector2.Min(minimum,p);maximum=Vector2.Max(maximum,p);}
                Vector2 extent=maximum-minimum;
                for(int i=0;i<uv.Count;i++)uv[i]=new Vector2((uv[i].x-minimum.x)/extent.x,(uv[i].y-minimum.y)/extent.y);
                mesh.SetUVs(0,uv);
            }
            Material material=new Material(source){name=source.name+" / ground transition"};resources.Own(material);
            if(rug){material.mainTextureScale=Vector2.one;material.mainTextureOffset=Vector2.zero;}
            if(!vegetation&&!rug)NaturalWorldMaterial.Apply(material,VisualSurface.Stone,false);
            if(material.HasProperty("_PavingBlend"))
            {
                material.SetFloat("_PavingBlend",rug?0:1);material.SetTexture("_GroundTex",Resources.Load<Texture2D>("WorldArt/GroundMeadow"));
                if(rug){material.SetFloat("_RugSurface",1);material.SetFloat("_GrainStrength",.78f);material.SetFloat("_GrainScale",2.1f);material.SetFloat("_Relief",.009f);}
                material.SetFloat("_Weather",vegetation||rug?0:.75f);material.SetFloat("_Glossiness",rug?.12f:.16f);
            }
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
    }
}
