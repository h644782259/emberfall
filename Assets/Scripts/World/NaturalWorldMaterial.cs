using UnityEngine;
namespace Emberfall
{
    internal static class NaturalWorldMaterial
    {
        internal static bool Enabled=true;
        static Texture2D atlas,leaf;static bool loaded;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){atlas=null;leaf=null;loaded=false;Enabled=true;}
        internal static void LivingLeaf(Material material)
        {
            if(!Enabled||!material.HasProperty("_LeafSurface"))return;
            if(leaf==null)leaf=Resources.Load<Texture2D>("WorldArt/LivingLeafSurface");
            if(leaf==null)return;
            material.mainTexture=leaf;material.SetVector("_AtlasRegion",new Vector4(1,1,0,0));
            material.SetFloat("_LeafSurface",1);material.SetFloat("_Relief",.005f);
            material.SetFloat("_Glossiness",.28f);material.SetFloat("_GrainStrength",.6f);
        }
        internal static void Apply(Material material,VisualSurface surface,bool emissive)
        {
            if(!Enabled||emissive||!material.HasProperty("_AtlasRegion")||
               (surface!=VisualSurface.Stone&&surface!=VisualSurface.Wood&&surface!=VisualSurface.Foliage))return;
            if(!loaded){atlas=Resources.Load<Texture2D>("WorldArt/NaturalSurfaceAtlas");loaded=true;}
            if(atlas==null)return;
            material.mainTexture=atlas;
            material.SetVector("_AtlasRegion",surface==VisualSurface.Wood?new Vector4(.488f,.488f,.506f,.506f):surface==VisualSurface.Foliage?new Vector4(.488f,.488f,.006f,.006f):new Vector4(.488f,.488f,.006f,.506f));
            material.SetFloat("_Relief",surface==VisualSurface.Stone?.035f:surface==VisualSurface.Wood?.014f:.010f);
            material.SetFloat("_GrainScale",surface==VisualSurface.Stone?.55f:surface==VisualSurface.Wood?.7f:.42f);
            material.SetFloat("_GrainStrength",surface==VisualSurface.Stone?.65f:.48f);
            material.SetFloat("_Weather",surface==VisualSurface.Stone?1:0);
        }
        internal static void Trail(Material material)
        {
            Apply(material,VisualSurface.Stone,false);
            if(atlas==null||!material.HasProperty("_AtlasRegion"))return;
            material.SetVector("_AtlasRegion",new Vector4(.488f,.488f,.506f,.006f));
            material.SetFloat("_GrainScale",.46f);material.SetFloat("_Weather",0);
            material.SetFloat("_Relief",.020f);
            material.SetTexture("_GroundTex",Resources.Load<Texture2D>("WorldArt/GroundMeadow"));
            material.SetFloat("_TrailEdge",1);
        }
    }
}
