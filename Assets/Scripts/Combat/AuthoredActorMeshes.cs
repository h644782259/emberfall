using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Blender-authored rigid pieces; all animation, materials and attachment transforms
    // remain owned by the original builders. Missing/corrupt data keeps original geometry.
    internal static class AuthoredActorMeshes
    {
        internal static bool Enabled = true;
        private static readonly Dictionary<string,Mesh> cache = new Dictionary<string,Mesh>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach(var mesh in cache.Values) if(mesh!=null) UnityEngine.Object.Destroy(mesh);
            cache.Clear(); Enabled=true;
        }
        internal static string Key(string name, PrimitiveType shape)
        {
            // Exact construction names intentionally exclude scenery and dynamic meshes.
            if(shape==PrimitiveType.Capsule)
            {
                switch(name)
                {
                    case "Breastplate": return "Cuirass";
                    case "Tailored cloth torso": return "ClothTorso";
                    case "Treant bark torso": return "BarkTorso";
                    case "Spirit wolf torso": return "WolfTorso";
                    case "Wolf foreleg": return "Paw";
                    case "Wolf hindleg": return "WolfHindLeg";
                    case "Wolf tail": return "WolfTail";
                    case "Bow Limb": case "Layered bow limb": return "BowLimb";
                    case "Curved shell plate": return "BossPlate";
                }
            }
            if(shape==PrimitiveType.Sphere)
            {
                switch(name)
                {
                    case "Tailored shoulder mantle": return "ClothShoulder";
                    case "Pauldrons": case "Shoulder shell": case "Single leather shoulder": return "Pauldron";
                    case "Helmet": case "Leather Cap": return "Helmet";
                    case "Star spirit": case "Spirit Core": case "Exposed star heart":
                    case "Arcane Crystal": case "Focus crystal": return "SpiritCore";
                    case "Slime Body": return "Slime";
                }
            }
            if(shape==PrimitiveType.Cube)
            {
                switch(name)
                {
                    case "Raised breastplate": return "CuirassPlate";
                    case "Boot": case "Grounded claw": return "Boot";
                    case "Wolf head": return "WolfHead";
                    case "Wolf muzzle": return "WolfMuzzle";
                    case "Wolf ear": return "WolfEar";
                    case "Great Hammer": return "Hammer";
                    case "Bound spirit totem": return "Totem";
                    case "Crossguard": case "Swept guard": return "SwordGuard";
                    case "Staff Crystal Crown": case "Crystal prong": return "StaffCrown";
                }
            }
            if(shape==PrimitiveType.Cylinder && name=="Faceted engine housing")return "BossHousing";
            return null;
        }
        internal static void Apply(GameObject obj,string name,PrimitiveType shape)
        {
            if(!Enabled)return;
            string key=Key(name,shape);if(key==null)return;
            Mesh mesh=Prewarm(key);
            var filter=obj.GetComponent<MeshFilter>();
            if(mesh!=null && filter!=null)filter.sharedMesh=mesh;
        }
        internal static readonly string[] PrewarmKeys={"Cuirass","ClothTorso","BarkTorso","WolfTorso","Paw","WolfHindLeg","WolfTail","BowLimb","BossPlate","ClothShoulder","Pauldron","Helmet","SpiritCore","Slime","CuirassPlate","Boot","WolfHead","WolfMuzzle","WolfEar","Hammer","Totem","SwordGuard","StaffCrown","BossHousing"};
        internal static Mesh Prewarm(string key)
        {
            Mesh mesh;if(cache.TryGetValue(key,out mesh))return mesh;
            var source=Resources.Load<TextAsset>("ActorModules/"+key);
            mesh=source==null?null:Decode(source.bytes,"Blender actor "+key);
            cache[key]=mesh;return mesh;
        }
        internal static Mesh Decode(byte[] bytes,string name)
        {
            if(bytes==null || bytes.Length<12 || bytes.Length>196620)return null;
            try
            {
                using(var stream=new MemoryStream(bytes,false))using(var reader=new BinaryReader(stream))
                {
                    if(reader.ReadUInt32()!=0x45464D31)return null;
                    int count=reader.ReadInt32(),indices=reader.ReadInt32();
                    if(count<3||count>4096||indices<3||indices>12288||indices%3!=0||12L+count*32L+indices*4L!=bytes.Length)return null;
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
