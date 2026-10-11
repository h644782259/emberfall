using UnityEngine;
using UnityEngine.Rendering;
namespace Emberfall
{
    // One fixed-buffer mesh per water strip, at most ten updates per second.
    // The world owns its shared material; this component owns only its mesh.
    public sealed class WaterFlowBands:MonoBehaviour
    {
        private const int Bands=6;
        private WaterFlowPath path;
        private readonly WaterFlowClock clock=new WaterFlowClock();
        private Mesh mesh;
        private readonly Vector3[] vertices=new Vector3[Bands*4];
        private float speed,width,height;
        public static void Create(Transform parent,Vector3[] points,float width,float height,WaterEnvironment environment,Material material)
        {
            float[] x=new float[points.Length],z=new float[points.Length];for(int i=0;i<points.Length;i++){x[i]=points[i].x;z[i]=points[i].z;}
            var go=new GameObject("Low-frequency path water flow");go.transform.SetParent(parent,false);
            var flow=go.AddComponent<WaterFlowBands>();flow.path=new WaterFlowPath(x,z);flow.width=width;flow.height=height;
            flow.speed=WaterFlowClock.Speed(environment);
            flow.mesh=new Mesh{name="Six owned water flow bands"};flow.mesh.MarkDynamic();
            var triangles=new int[Bands*6];var normals=new Vector3[Bands*4];
            for(int i=0;i<Bands;i++){int v=i*4,t=i*6;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;for(int j=0;j<4;j++)normals[v+j]=Vector3.up;}
            flow.UpdateVertices();flow.mesh.triangles=triangles;flow.mesh.normals=normals;
            go.AddComponent<MeshFilter>().sharedMesh=flow.mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
        }
        private void Update(){if(path!=null&&clock.Advance(Time.deltaTime,speed,path.Length))UpdateVertices();}
        private void UpdateVertices()
        {
            for(int i=0;i<Bands;i++)
            {
                var sample=path.Sample(clock.Distance+path.Length*(i+.35f)/Bands);
                Vector3 at=new Vector3(sample.X,height,sample.Z),forward=new Vector3(sample.DirectionX,0,sample.DirectionZ)*.20f;
                Vector3 across=new Vector3(-sample.DirectionZ,0,sample.DirectionX)*width*.5f;
                int v=i*4;vertices[v]=at-across-forward;vertices[v+1]=at+across-forward;vertices[v+2]=at+across+forward;vertices[v+3]=at-across+forward;
            }
            mesh.vertices=vertices;mesh.RecalculateBounds();
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
