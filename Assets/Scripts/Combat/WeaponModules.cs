using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Shared offline weapon geometry; no animation, material, combat or socket ownership.
    internal static class WeaponModules
    {
        internal static bool Enabled=true;
        private static readonly Dictionary<string,Mesh> cache=new Dictionary<string,Mesh>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){foreach(var mesh in cache.Values)if(mesh!=null)Object.Destroy(mesh);cache.Clear();Enabled=true;}
        internal static Mesh Load(string name)
        {
            if(!Enabled)return null;
            Mesh mesh;if(cache.TryGetValue(name,out mesh))return mesh;
            var source=Resources.Load<TextAsset>("WeaponModules/"+name);
            mesh=source==null?null:AuthoredActorMeshes.Decode(source.bytes,"Blender weapon "+name);
            if(mesh!=null&&mesh.triangles.Length>512*3){Object.Destroy(mesh);mesh=null;}
            if(mesh!=null)
                foreach(var v in mesh.vertices)
                    if(Mathf.Abs(v.x)>1.001f||Mathf.Abs(v.z)>1.001f||v.y>1.001f||v.y<(name=="StarBlade"?-.001f:-1.001f))
                    {Object.Destroy(mesh);mesh=null;break;}
            cache[name]=mesh;return mesh;
        }
        internal static void Set(MeshFilter filter,Mesh mesh,Vector3? scale=null)
        {
            if(filter==null||mesh==null)return;
            var binding=filter.GetComponent<WeaponModuleBinding>();
            if(binding==null){binding=filter.gameObject.AddComponent<WeaponModuleBinding>();binding.Original=filter.sharedMesh;binding.Scale=filter.transform.localScale;}
            filter.sharedMesh=mesh;filter.transform.localScale=scale??binding.Scale;
        }
        internal static void Restore(MeshFilter filter)
        {
            var binding=filter.GetComponent<WeaponModuleBinding>();if(binding==null)return;
            filter.sharedMesh=binding.Original;filter.transform.localScale=binding.Scale;
        }
    }
    internal sealed class WeaponModuleBinding:MonoBehaviour {internal Mesh Original;internal Vector3 Scale;}
}
