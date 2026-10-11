using UnityEngine;
namespace Emberfall
{
    // Each decoded mesh belongs to FilledSkillVfx's existing cache/reset lifecycle.
    // Missing or malformed individual files keep that silhouette's procedural fallback.
    internal static class AuthoredSpellBases
    {
        internal static Mesh Identity(string name)
        {var data=Resources.Load<TextAsset>("BlenderSkillIdentities/"+name);return data==null?null:AuthoredActorMeshes.Decode(data.bytes,"Skill identity / "+name);}
        internal static Mesh Load(string name)
        {
            var data=Resources.Load<TextAsset>("BlenderSpellBases/"+name);
            return data==null?null:AuthoredActorMeshes.Decode(data.bytes,"Authored spell / "+name);
        }
    }
}
