using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Reuses sculpted actor/projectile pieces to make recognisable physical spell
    // bodies. No rings, glyphs, cages, lattices or camera-facing geometry.
    internal static class HeroSkillBodyAssets
    {
        private static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {foreach(var mesh in meshes.Values)if(mesh!=null)Object.Destroy(mesh);meshes.Clear();}
        private static Mesh Source(string path)
        {
            Mesh mesh;if(meshes.TryGetValue(path,out mesh))return mesh;
            var data=Resources.Load<TextAsset>(path);
            mesh=data==null?null:AuthoredActorMeshes.Decode(data.bytes,"Physical spell source / "+path);
            meshes[path]=mesh;return mesh;
        }
        internal static Mesh Spell(string name) => Source("BlenderSpellBases/"+name);
        internal static Mesh Rock => Source("BlenderProjectiles/MeteorRock");
        internal static Mesh Spirit => Composite("Spirit animal",builder=>
        {
            builder.Add("ActorModules/WolfTorso",new Vector3(0,.48f,-.12f),new Vector3(.46f,.53f,.34f),Quaternion.Euler(90,0,0));
            builder.Add("ActorModules/WolfHead",new Vector3(0,.73f,.48f),new Vector3(.45f,.43f,.47f),Quaternion.Euler(-12,0,0));
            builder.Add("ActorModules/WolfMuzzle",new Vector3(0,.64f,.77f),new Vector3(.24f,.19f,.42f),Quaternion.identity);
            for(int side=-1;side<=1;side+=2)
            {
                builder.Add("ActorModules/WolfEar",new Vector3(side*.15f,1.0f,.45f),new Vector3(.17f,.34f,.2f),Quaternion.Euler(0,0,-side*12));
                builder.Add("ActorModules/Paw",new Vector3(side*.21f,.15f,.29f),new Vector3(.14f,.35f,.14f),Quaternion.Euler(-22,0,side*9));
                builder.Add("ActorModules/WolfHindLeg",new Vector3(side*.22f,.2f,-.45f),new Vector3(.18f,.3f,.18f),Quaternion.Euler(30,0,side*8));
            }
            builder.Add("ActorModules/WolfTail",new Vector3(.07f,.58f,-.73f),new Vector3(.19f,.47f,.21f),Quaternion.Euler(-52,0,22));
        });
        internal static Mesh Armor => Composite("Layered protective armor",builder=>
        {
            builder.Add("ActorModules/CuirassPlate",new Vector3(0,.74f,.29f),new Vector3(.82f,.88f,.24f),Quaternion.Euler(-8,0,0));
            builder.Add("ActorModules/CuirassPlate",new Vector3(0,.72f,-.28f),new Vector3(.72f,.8f,.22f),Quaternion.Euler(10,180,0));
            for(int side=-1;side<=1;side+=2)
            {
                builder.Add("ActorModules/Pauldron",new Vector3(side*.47f,1.13f,0),new Vector3(.4f,.3f,.45f),Quaternion.Euler(0,0,side*12));
                builder.Add("ActorModules/BossPlate",new Vector3(side*.36f,.53f,0),new Vector3(.23f,.57f,.43f),Quaternion.Euler(0,side*80,side*12));
            }
        });
        internal static Mesh GravityPlanet => Composite("Fractured gravity planet",builder=>
        {
            // Forty sculpted interlocking basalt plates, combined into one draw.
            for(int i=0;i<40;i++)
            {
                float y=1-2*(i+.5f)/40f,angle=i*2.399963f;
                Vector3 normal=new Vector3(Mathf.Cos(angle)*Mathf.Sqrt(1-y*y),y,Mathf.Sin(angle)*Mathf.Sqrt(1-y*y));
                builder.AddRock(normal*.34f,new Vector3(.39f,.30f,.43f),Quaternion.LookRotation(normal)*Quaternion.Euler(i*17,i*31,i*13));
            }
        });
        internal static Mesh GroundBreak => Composite("Broken ground slabs",builder=>
        {
            for(int i=0;i<9;i++)
            {
                float angle=i*2.399963f,r=.1f+(i%4)*.18f;
                builder.AddRock(new Vector3(Mathf.Cos(angle)*r,.025f+(i%3)*.025f,Mathf.Sin(angle)*r),
                    new Vector3(.29f+(i%2)*.12f,.1f+(i%3)*.06f,.34f),Quaternion.Euler(i*9,i*47,12-i*3));
            }
        });
        private static Mesh Composite(string name,System.Action<Builder> build)
        {
            Mesh result;if(meshes.TryGetValue(name,out result))return result;
            var builder=new Builder();build(builder);result=builder.Finish(name);meshes[name]=result;return result;
        }
        private sealed class Builder
        {
            private readonly List<CombineInstance> pieces=new List<CombineInstance>();
            internal void Add(string path,Vector3 at,Vector3 scale,Quaternion rotation)
            {AddMesh(Source(path),at,scale,rotation);}
            internal void AddRock(Vector3 at,Vector3 scale,Quaternion rotation)
            {AddMesh(Rock,at,scale,rotation);}
            private void AddMesh(Mesh mesh,Vector3 at,Vector3 scale,Quaternion rotation)
            {if(mesh!=null)pieces.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(at,rotation,scale)});}
            internal Mesh Finish(string name)
            {var mesh=new Mesh{name=name};mesh.CombineMeshes(pieces.ToArray(),true,true);mesh.RecalculateBounds();return mesh;}
        }
    }
}
