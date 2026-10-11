using UnityEngine;
namespace Emberfall
{
    // Generated natural detail is carried by solid meshes, never a facing sprite.
    internal static class SkillBodyMaterialLibrary
    {
        internal enum Matter { Rock=0,Steel=1,Ice=2,Vine=3,Fur=4,Mud=5,Wood=6,Leather=7 }
        private static Texture2D atlas,flame;
        internal static Texture2D Flame => flame!=null?flame:(flame=Resources.Load<Texture2D>("SkillMatter/NaturalFlame"));
        internal static void Bind(Material material)
        {
            if(material==null)return;
            if(atlas==null)atlas=Resources.Load<Texture2D>("SkillMatter/NaturalMatterAtlas");
            if(atlas!=null)material.SetTexture("_MaterialAtlas",atlas);
            if(Flame!=null)material.SetTexture("_FlameTex",Flame);
        }
        internal static void Apply(Material material,Matter matter,float heat=0)
        {
            if(material==null)return;
            Color tint=material.HasProperty("_Tint")?material.GetColor("_Tint"):material.color;
            material.shader=Resources.Load<Shader>("HeroSkillBody");Bind(material);
            material.SetColor("_Tint",tint);material.SetFloat("_Surface",(float)matter);material.SetFloat("_Heat",heat);material.SetFloat("_Opacity",1);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){atlas=flame=null;}
    }
}
