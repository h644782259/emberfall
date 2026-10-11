using System;
using Emberfall;
using UnityEngine;

// The actual production mesh recipes run against a small managed math shim.
// These are geometry/resource-budget checks, not GPU or visual acceptance tests.
public static class ProceduralVisualTests
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if(!value)throw new Exception(message); }
    private static bool Finite(float x) { return !float.IsNaN(x) && !float.IsInfinity(x); }
    public static string Run()
    {
        checks=0;
        Validate(VisualMeshRecipes.BevelBox(),.501f,.501f,"bevel box",216,900);
        Validate(VisualMeshRecipes.RoundBody(false,16,12),.501f,.501f,"sphere",221,1152);
        Validate(VisualMeshRecipes.RoundBody(true,16,12),.501f,1.001f,"capsule",238,1248);
        Validate(VisualMeshRecipes.Cylinder(24),.501f,1.001f,"cylinder",102,288);
        Validate(VisualMeshRecipes.Rock(),.58f,.51f,"boulder",117,576);
        foreach(int segments in new[]{int.MinValue,0,7,16,99,int.MaxValue})
        foreach(int rings in new[]{int.MinValue,0,7,12,99,int.MaxValue})
        {
            var sphere=VisualMeshRecipes.RoundBody(false,segments,rings);
            var capsule=VisualMeshRecipes.RoundBody(true,segments,rings);
            Check(sphere.Vertices.Length<=825 && capsule.Vertices.Length<=858,"bounded round-body allocation");
            Validate(sphere,.501f,.501f,"bounded sphere",825,4608);
            Validate(capsule,.501f,1.001f,"bounded capsule",858,4800);
        }
        var a=VisualMeshRecipes.BevelBox();var b=VisualMeshRecipes.BevelBox();
        for(int i=0;i<a.Vertices.Length;i++)Check((a.Vertices[i]-b.Vertices[i]).sqrMagnitude==0,"deterministic recipe");
        Check(a.Vertices[0].sqrMagnitude<.75f,"box corners rounded inside original collider envelope");
        for(int frame=0;frame<90;frame++)for(int sample=0;sample<7;sample++)
        {
            float time=frame/30f, across=sample/3f-1f;
            var top=VisualMeshRecipes.DrapePoint(across,0,time,1,0);
            Check(Math.Abs(top.y)<.00001f && Math.Abs(top.z+.035f)<.00001f,"cloth pinned at shoulder seam");
            var bottom=VisualMeshRecipes.DrapePoint(across,1,time,1,0);
            Check(Finite(bottom.x)&&Finite(bottom.y)&&Finite(bottom.z),"cloth remains finite");
            Check(Math.Abs(bottom.x)<.53f && bottom.y>=-1.211f && bottom.y<=-1.1f && bottom.z>-.63f && bottom.z<-.33f,"cloth bounded drape envelope");
            var next=VisualMeshRecipes.DrapePoint(across,1,time+.001f,1,0);
            Check((next-bottom).sqrMagnitude<.000001f,"cloth continuous between nearby frames");
        }
        return "PASS: "+checks+" procedural geometry assertions (no rendered-frame validation)";
    }
    private static void Validate(VisualMeshData data,float xz,float y,string name,int maxVertices,int maxIndices)
    {
        Check(data.Vertices.Length<=maxVertices && data.Triangles.Length<=maxIndices,name+" fixed resource budget");
        Check(data.Vertices.Length==data.Normals.Length && data.Vertices.Length==data.Uv.Length,name+" attributes align");
        Check(data.Triangles.Length%3==0,name+" complete triangles");
        for(int i=0;i<data.Vertices.Length;i++)
        {
            var p=data.Vertices[i];var n=data.Normals[i];
            Check(Finite(p.x)&&Finite(p.y)&&Finite(p.z)&&Finite(n.x)&&Finite(n.y)&&Finite(n.z),name+" finite geometry");
            Check(Math.Abs(p.x)<=xz && Math.Abs(p.z)<=xz && Math.Abs(p.y)<=y,name+" preserves primitive envelope");
            Check(n.sqrMagnitude>.98f && n.sqrMagnitude<1.02f,name+" unit normals");
        }
        for(int i=0;i<data.Triangles.Length;i+=3)
        {
            int a=data.Triangles[i],b=data.Triangles[i+1],c=data.Triangles[i+2];
            Check(a>=0 && b>=0 && c>=0 && a<data.Vertices.Length && b<data.Vertices.Length && c<data.Vertices.Length,name+" valid indices");
            Vector3 normal=Vector3.Cross(data.Vertices[b]-data.Vertices[a],data.Vertices[c]-data.Vertices[a]);
            // Longitude-grid poles have intentional zero-area triangles; real faces must wind outward.
            if(normal.sqrMagnitude>1e-10f)Check(Vector3.Dot(normal,data.Normals[a]+data.Normals[b]+data.Normals[c])>0,name+" outward face winding");
        }
    }
}

namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
    }
    public struct Vector3
    {
        public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 right=>new Vector3(1,0,0); public static Vector3 left=>new Vector3(-1,0,0);
        public static Vector3 up=>new Vector3(0,1,0); public static Vector3 down=>new Vector3(0,-1,0);
        public static Vector3 forward=>new Vector3(0,0,1); public static Vector3 back=>new Vector3(0,0,-1);
        public float sqrMagnitude=>x*x+y*y+z*z;
        public Vector3 normalized=>sqrMagnitude>1e-12f?this*(1f/(float)Math.Sqrt(sqrMagnitude)):new Vector3();
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    }
    public static class Mathf
    {
        public const float PI=(float)Math.PI;
        public static float Sin(float x)=>(float)Math.Sin(x); public static float Cos(float x)=>(float)Math.Cos(x);
        public static float Clamp(float x,float min,float max)=>Math.Min(max,Math.Max(min,x));
    }
}
