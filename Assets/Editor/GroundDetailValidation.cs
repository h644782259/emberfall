using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class GroundDetailValidation
    {
        public static void RunAndExport() { Run(); IOSBuild.Export(); }
        public static void Run()
        {
            var type=typeof(WorldBuilder).GetNestedType("DetailBatch",BindingFlags.NonPublic);
            var meshes=new Mesh[2,3];
            var root=new GameObject("Ground detail validation");
            var material=new Material(Shader.Find("Standard"));material.color=new Color(.34f,.5f,.22f);
            var target=new RenderTexture(768,512,24);
            var pixels=new Texture2D(768,512,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            string directory=Path.Combine(Path.GetTempPath(),"emberfall-ground-detail");Directory.CreateDirectory(directory);
            var log=new System.Text.StringBuilder();
            try
            {
                for(int mode=0;mode<2;mode++)for(int shape=0;shape<3;shape++)
                {
                    object batch=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{mode==1},null);
                    string method=shape==0?"Grass":shape==1?"Leaf":"Pebble";
                    object[] args=shape==2?new object[]{Vector3.zero,new Vector3(.12f,.05f,.09f),.3f}:new object[]{Vector3.zero,.3f,shape==0?.28f:.16f,Color.white};
                    type.GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(batch,args);
                    var mesh=(Mesh)type.GetMethod("Mesh",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(batch,new object[]{method+mode});
                    meshes[mode,shape]=mesh;ValidateClosed(mesh);
                    log.AppendLine(method+" "+(mode==0?"original":"mobile")+": "+mesh.vertexCount+" vertices, "+mesh.triangles.Length/3+" triangles; closed manifold PASS");
                    if(mode==1)
                    {
                        if(mesh.triangles.Length>=meshes[0,shape].triangles.Length)throw new Exception("No triangle reduction");
                        var old=meshes[0,shape].bounds;var current=mesh.bounds;
                        if((old.center-current.center).magnitude>.01f||(old.size-current.size).magnitude>.02f)throw new Exception("Ground ornament envelope changed materially");
                    }
                }
                var camera=new GameObject("Detail camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);
                camera.transform.position=new Vector3(0,1.7f,-3.5f);camera.transform.LookAt(new Vector3(0,0,.4f));
                camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.15f,.18f);camera.targetTexture=target;
                var light=new GameObject("Detail light").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(35,-25,0);
                var filters=new List<MeshFilter>();
                for(int row=0;row<8;row++)for(int column=0;column<15;column++)
                {
                    var item=new GameObject("Detail sample");item.layer=30;item.transform.SetParent(root.transform);
                    item.transform.position=new Vector3((column-7)*.14f,0,row*.16f);
                    var filter=item.AddComponent<MeshFilter>();filter.sharedMesh=meshes[0,column%3];filters.Add(filter);
                    item.AddComponent<MeshRenderer>().sharedMaterial=material;
                }
                for(int mode=0;mode<2;mode++)
                {
                    for(int i=0;i<filters.Count;i++)filters[i].sharedMesh=meshes[mode,i%3];
                    camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,768,512),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(directory,mode==0?"original.png":"mobile.png"),pixels.EncodeToPNG());
                }
                File.WriteAllText(Path.Combine(directory,"result.txt"),log.ToString());Debug.Log(log.ToString());
            }
            finally
            {
                RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);
                foreach(var mesh in meshes)if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(material);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
        private static void ValidateClosed(Mesh mesh)
        {
            var positions=mesh.vertices;var indices=mesh.triangles;
            var weld=new Dictionary<Vector3,int>();var remap=new int[positions.Length];
            for(int i=0;i<positions.Length;i++)
            {
                var p=positions[i];if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))throw new Exception("Nonfinite vertex");
                if(!weld.TryGetValue(p,out int key)){key=weld.Count;weld.Add(p,key);}remap[i]=key;
            }
            var edges=new Dictionary<(int,int),(int count,int winding)>();
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=positions[indices[i]];var b=positions[indices[i+1]];var c=positions[indices[i+2]];
                if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-16f)throw new Exception("Degenerate triangle");
                for(int corner=0;corner<3;corner++)
                {
                    int x=remap[indices[i+corner]],y=remap[indices[i+(corner+1)%3]];
                    var key=x<y?(x,y):(y,x);edges.TryGetValue(key,out var edge);edges[key]=(edge.count+1,edge.winding+(x<y?1:-1));
                }
            }
            foreach(var edge in edges.Values)if(edge.count!=2||edge.winding!=0)throw new Exception("Open or inconsistently wound ornament");
        }
    }
}
