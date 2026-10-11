using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // F2 rigid geometry only: existing rigs, palettes, control, collision and anchor state retain ownership.
    internal static class EnemySilhouetteArt
    {
        internal static bool Enabled=true;
        private static readonly Dictionary<string,Mesh> cache=new Dictionary<string,Mesh>();
        internal static string Key(string name,bool anchor)
        {
            if(anchor)
            {
                switch(name){case "Power anchor plinth":return "AnchorPlinth";case "Breakable crystal":return "AnchorCrystal";case "Anchor claw":return "AnchorClaw";}
                return null;
            }
            switch(name)
            {
                case "Long Ear":return "GoblinEar";
                case "Leather Cap":return "GoblinCap";
                case "Goblin Knife":return "GoblinKnife";
                case "Guardian chest plate":return "GuardianChest";
                case "Guardian layered pauldron":return "GuardianShoulder";
                case "Iron Crown":return "GuardianCrown";
            }
            return null;
        }
        internal static void ApplyEnemy(CombatModel model){Apply(model.transform,false);}
        internal static void ApplyAnchors(Transform root){Apply(root,true);}
        private static void Apply(Transform root,bool anchor)
        {
            if(!Enabled||root==null)return;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                string key=Key(filter.gameObject.name,anchor);if(key==null)continue;
                Mesh mesh=Prewarm(key);
                if(mesh!=null)filter.sharedMesh=mesh;
            }
        }
        internal static readonly string[] PrewarmKeys={"GoblinEar","GoblinCap","GoblinKnife","GuardianChest","GuardianShoulder","GuardianCrown","AnchorPlinth","AnchorCrystal","AnchorClaw"};
        internal static Mesh Prewarm(string key)
        {
            Mesh mesh;if(cache.TryGetValue(key,out mesh))return mesh;
            var resource=Resources.Load<TextAsset>("EnemySilhouettes/"+key);
            mesh=resource==null?null:AuthoredActorMeshes.Decode(resource.bytes,"Enemy silhouette / "+key);
            if(mesh!=null)
            {
                float halfHeight=key=="GoblinEar"||key=="AnchorPlinth"||key=="AnchorClaw"?1f:.5f;
                bool valid=mesh.triangles.Length<=1536;
                foreach(var v in mesh.vertices)valid &= Mathf.Abs(v.x)<=.5001f&&Mathf.Abs(v.z)<=.5001f&&Mathf.Abs(v.y)<=halfHeight+.0001f;
                if(!valid){Object.Destroy(mesh);mesh=null;}
            }
            cache[key]=mesh;return mesh;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){foreach(var mesh in cache.Values)if(mesh!=null)Object.Destroy(mesh);cache.Clear();Enabled=true;}
    }
}
