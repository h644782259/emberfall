using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Original rigid identity pieces only. Existing joint, palette and renderer own every part.
    internal static class ActorSilhouetteF1
    {
        internal static bool Enabled=true;
        private static readonly Dictionary<string,Mesh> cache=new Dictionary<string,Mesh>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){foreach(var mesh in cache.Values)if(mesh!=null)Object.Destroy(mesh);cache.Clear();Enabled=true;}
        internal static string Key(string name,bool treant)
        {
            if(treant)switch(name)
            {
                case "Head":return "BarkFace";
                case "Sleeve":case "Trouser":return "BarkLimb";
                case "Glove":return "BarkHand";
                case "Boot":return "RootFoot";
            }
            switch(name)
            {
                case "Long robe front panel":case "Embroidered robe panel":return "RobePanel";
                case "Forest Hood":return "Hood";
                case "Quiver":return "Quiver";
                case "Spare Arrow":return "ArrowFeather";
                case "Spirit antler":case "Spirit antler branch":return "Antler";
                case "Star crown":return "SpiritCrown";
                case "Spirit wing":return "SpiritWing";
                case "Leaf crown":return "Canopy";
                default:return null;
            }
        }
        internal static readonly string[] PrewarmKeys={"BarkFace","BarkLimb","BarkHand","RootFoot","RobePanel","Hood","Quiver","ArrowFeather","Antler","SpiritCrown","SpiritWing","Canopy"};
        internal static Mesh Prewarm(string key)
        {
            Mesh mesh;if(cache.TryGetValue(key,out mesh))return mesh;
            var asset=Resources.Load<TextAsset>("ActorSilhouettes/F1/"+key);
            mesh=asset==null?null:AuthoredActorMeshes.Decode(asset.bytes,"F1 silhouette / "+key);
            cache[key]=mesh;return mesh;
        }
        internal static void Apply(GameObject obj,string name,bool treant)
        {
            if(!Enabled)return;string key=Key(name,treant);if(key==null)return;
            Mesh mesh=Prewarm(key);
            var filter=obj.GetComponent<MeshFilter>();if(mesh!=null&&filter!=null)filter.sharedMesh=mesh;
        }
    }
}
