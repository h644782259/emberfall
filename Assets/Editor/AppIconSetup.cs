using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
namespace Emberfall.Editor
{
    /// <summary>One opaque 1024px master; Unity generates platform sizes at build time.</summary>
    public static class AppIconSetup
    {
        public static void Apply()
        {
            Texture2D icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/EmberfallIcon.png");
            if(icon==null)throw new BuildFailedException("Emberfall application icon is missing.");
            SetLegacy(NamedBuildTarget.Unknown,icon);
            SetLegacy(NamedBuildTarget.Standalone,icon);
            // Modern iOS application/settings/spotlight/notification/marketing slots.
            // The serialized iPhone slots remain populated even without this module.
            if(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,BuildTarget.iOS))
                foreach(PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
                {
                    PlatformIcon[] slots=PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS,kind);
                    foreach(PlatformIcon slot in slots)slot.SetTexture(icon);
                    PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS,kind,slots);
                }
        }
        private static void SetLegacy(NamedBuildTarget target,Texture2D icon)
        {
            int[] sizes=PlayerSettings.GetIconSizes(target,IconKind.Any);
            if(sizes.Length==0){if(target==NamedBuildTarget.Unknown)PlayerSettings.SetIcons(target,new[]{icon},IconKind.Any);return;}
            Texture2D[] textures=new Texture2D[sizes.Length];for(int i=0;i<textures.Length;i++)textures[i]=icon;
            PlayerSettings.SetIcons(target,textures,IconKind.Any);
        }
    }
}
