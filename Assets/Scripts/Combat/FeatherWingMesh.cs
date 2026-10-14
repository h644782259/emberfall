using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Cached solid feather geometry: three shared material batches per wing.
    internal static class FeatherWingMesh
    {
        private static readonly Mesh[] painted=new Mesh[2];
        private static readonly Material[] paintedMaterials=new Material[4];
        public static Material PaintedMaterial(int tier)
        {
            if(paintedMaterials[tier]!=null)return paintedMaterials[tier];
            var mat=new Material(Resources.Load<Shader>("FeatherWingSurface"));
            mat.mainTexture=Resources.Load<Texture2D>("IconArt/feather-wings-v2/"+tier);
            mat.SetFloat("_Glow",tier==0?0:tier==1?.07f:tier==2?.15f:.28f);
            return paintedMaterials[tier]=mat;
        }
        public static Mesh PaintedSurface(int side)
        {
            int key=side<0?0:1;if(painted[key]!=null)return painted[key];
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            const int columns=18,rows=22;
            for(int face=0;face<2;face++)for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float u=x/(float)columns,v=y/(float)rows;
                // Curved, thick two-sided shell, attached at the shoulder, never camera-facing.
                float depth=-Mathf.Sin(u*Mathf.PI*.82f)*.18f+Mathf.Pow(v-.58f,2)*.12f+face*.045f;
                // Compress the transparent central artwork margin into the back mount.
                float across=Mathf.Max(0,u*1.7f-.16f);
                float height=(v-.50f)*2.16f;
                vertices.Add(new Vector3(side*across,height,depth));
                uv.Add(new Vector2(.5f+side*u*.5f,v));
            }
            int plane=(columns+1)*(rows+1);
            for(int face=0;face<2;face++)for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {int a=face*plane+y*(columns+1)+x,b=a+columns+1,c=a+1,d=b+1;bool reverse=(side<0)^(face==1);if(reverse){Add(tris,a,c,b);Add(tris,c,d,b);}else{Add(tris,a,b,c);Add(tris,c,b,d);}}
            var mesh=new Mesh{name="Paint matched curved wing shell "+side};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.UploadMeshData(true);return painted[key]=mesh;
        }
        private static void Add(List<int> list,int a,int b,int c){list.Add(a);list.Add(b);list.Add(c);}
    }
    internal sealed class FeatherWingSway:MonoBehaviour
    {
        private int side,tier;private Renderer visible;private Quaternion rest;
        public void Initialize(int side,int tier,Renderer renderer){this.side=side;this.tier=tier;visible=renderer;rest=transform.localRotation;}
        private void LateUpdate(){if(visible==null||!visible.isVisible)return;transform.localRotation=rest*Quaternion.Euler(0,side*Mathf.Sin(Time.time*1.45f)*(2+tier*.6f),side*Mathf.Sin(Time.time*1.45f+.4f)*1.2f);}
    }
}
