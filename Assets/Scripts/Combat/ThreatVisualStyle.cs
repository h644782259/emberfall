using UnityEngine;
namespace Emberfall
{
    // Damage boundaries share one danger color; source identity is a separate glyph.
    internal static class ThreatVisualStyle
    {
        public static readonly Color Danger=new Color(1f,.22f,.1f,.95f);
        public static Material Material()
        {var shader=Resources.Load<Shader>("ThreatBoundary");var value=shader==null?CombatFx.NewGlow():new Material(shader);value.renderQueue=3900;return value;}
    }
}
