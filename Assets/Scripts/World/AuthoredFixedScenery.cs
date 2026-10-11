using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // F4 fixed scenery; all animation, materials and attachment transforms
    // remain owned by the original builders. Missing/corrupt data keeps original geometry.
    internal static class AuthoredFixedScenery
    {
        internal static bool Enabled = true;
        private static readonly Dictionary<string,Mesh> cache = new Dictionary<string,Mesh>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach(var mesh in cache.Values) if(mesh!=null) UnityEngine.Object.Destroy(mesh);
            cache.Clear(); Enabled=true;
        }
        internal static string Key(string name,PrimitiveType shape)
        {
            switch(name)
            {
                case "Column base":return "ColumnBase";
                case "Column":return "FlutedColumn";
                case "Capital":return "Capital";
                case "Capital bevel collar":return "Collar";
                case "Column band":return "ColumnBand";
                case "Gate plinth":return "GatePlinth";
                case "Pitched workshop roof module":return "RoofTiles";
                case "Workshop projecting eave":return "Eave";
                case "Workshop ridge cap":return "Ridge";
                case "Kiln chimney cap":return "ChimneyCap";
                case "Merchant stocked shelf":return "Shelf";
                case "Forged anvil face":return "Anvil";
                case "Anvil stump":return "Stump";
                case "Exchange lectern":return "Lectern";
                case "Observatory circular dais":return "ObservatoryDais";
                case "NPC tunic":return "NpcTunic";
                case "NPC sleeve":return "NpcSleeve";
                case "NPC boots":return "NpcBoot";
                case "NPC head":return "NpcHead";
                case "Merchant cap":return "MerchantCap";
                case "Smith leather apron":return "Apron";
                case "STAR CORE plinth":case "APPRENTICE plinth":case "CODEX plinth":case "CLASS TRIAL plinth":return "FacilityBase";
            }
            return null;
        }
        private static Mesh Load(string key)
        {
            if(!Enabled)return null;Mesh mesh;
            if(!cache.TryGetValue(key,out mesh))
            {
                var source=Resources.Load<TextAsset>("FixedScenery/"+key);
                mesh=source==null?null:Decode(source.bytes,"Blender fixed scenery "+key);cache[key]=mesh;
            }
            return mesh;
        }
        internal static bool Frame(GameObject root,float radius,Material material)
        {
            Mesh mesh=Load("GateFrame");if(mesh==null)return false;
            root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;
            root.transform.localScale=Vector3.one*radius;return true;
        }
        internal static void Crest(Transform parent,string facility,Vector3 at,Material material)
        {
            string key=facility=="STAR CORE"?"CoreCrest":facility=="APPRENTICE"?"ApprenticeCrest":facility=="CODEX"?"CodexCrest":facility=="CLASS TRIAL"?"TrialCrest":null;
            if(key==null)return;Mesh mesh=Load(key);if(mesh==null)return;
            var root=new GameObject(facility+" fixed crest");root.transform.SetParent(parent,false);root.transform.localPosition=at;root.transform.localScale=new Vector3(.65f,.12f,.65f);
            root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        internal static void Apply(GameObject obj,string name,PrimitiveType shape)
        {
            if(!Enabled)return;
            string key=Key(name,shape);if(key==null)return;
            Mesh mesh=Load(key);
            var filter=obj.GetComponent<MeshFilter>();
            if(mesh!=null && filter!=null)filter.sharedMesh=mesh;
        }
        internal static Mesh Decode(byte[] bytes,string name)
        {
            if(bytes==null || bytes.Length<12 || bytes.Length>432012)return null;
            try
            {
                using(var stream=new MemoryStream(bytes,false))using(var reader=new BinaryReader(stream))
                {
                    if(reader.ReadUInt32()!=0x45465334)return null;
                    int count=reader.ReadInt32(),indices=reader.ReadInt32();
                    if(count<3||count>12000||indices<3||indices>6000||indices%3!=0||12L+count*32L+indices*4L!=bytes.Length)return null;
                    var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
                    for(int i=0;i<count;i++)
                    {
                        float x=reader.ReadSingle(),y=reader.ReadSingle(),z=reader.ReadSingle();
                        float nx=reader.ReadSingle(),ny=reader.ReadSingle(),nz=reader.ReadSingle();
                        float u=reader.ReadSingle(),v=reader.ReadSingle();
                        if(!Finite(x)||!Finite(y)||!Finite(z)||!Finite(nx)||!Finite(ny)||!Finite(nz)||!Finite(u)||!Finite(v))return null;
                        float n=nx*nx+ny*ny+nz*nz;if(n<.9f||n>1.1f)return null;
                        vertices[i]=new Vector3(x,y,z);normals[i]=new Vector3(nx,ny,nz);uv[i]=new Vector2(u,v);
                    }
                    var triangles=new int[indices];
                    for(int i=0;i<indices;i++){int index=reader.ReadInt32();if(index<0||index>=count)return null;triangles[i]=index;}
                    var mesh=new Mesh{name=name,vertices=vertices,normals=normals,uv=uv,triangles=triangles};mesh.RecalculateBounds();return mesh;
                }
            }
            catch(IOException){return null;}
        }
        private static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value)&&Math.Abs(value)<=16f;}
    }
}
