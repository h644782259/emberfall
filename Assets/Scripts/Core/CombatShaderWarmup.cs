using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall
{
    // Warm the combat/environment shaders at a guarded scene boundary so a
    // first hit does not ask the driver to prepare its common shader variants.
    internal static class CombatShaderWarmup
    {
        private static bool ready;
        private static readonly string[] paths = { "ThreatBoundary", "FilledSpell", "HeroSkillBody", "SpellEnergySurface",
            "SpellSolidSurface", "PhysicalContactSurface", "GroundSkillSurface", "BlackHoleLens", "LightningDischarge",
            "BlackHoleLensMobile", "WorldArt/ActorSurface", "WorldArt/WeatheredWorld", "WorldArt/PortalVolume" };
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { ready = false; }
        internal static IEnumerator Prewarm(Action<float> progress = null)
        {
            if (ready) { progress?.Invoke(1); yield break; }
            for (int i = 0; i < paths.Length; i++)
            {
                Shader shader = Resources.Load<Shader>(paths[i]);
                if (shader != null && shader.isSupported && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    var variants = new ShaderVariantCollection();
                    try
                    {
                        Add(variants, shader, PassType.Normal);
                        Add(variants, shader, PassType.ForwardBase);
                        Add(variants, shader, PassType.ForwardBase, "SHADOWS_SCREEN");
                        Add(variants, shader, PassType.ForwardBase, "VERTEXLIGHT_ON");
                        Add(variants, shader, PassType.ForwardBase, "SHADOWS_SCREEN", "VERTEXLIGHT_ON");
                        Add(variants, shader, PassType.ShadowCaster, "SHADOWS_DEPTH");
                        variants.WarmUp();
                    }
                    finally { UnityEngine.Object.Destroy(variants); }
                }
                progress?.Invoke((i + 1f) / paths.Length);
                yield return null;
            }
            ready = true;
        }
        private static void Add(ShaderVariantCollection collection, Shader shader, PassType pass, params string[] keywords)
        {
            // Some effects have only an unlit pass. Unity rejects variants that
            // the particular shader does not declare; those do not need warming.
            try { collection.Add(new ShaderVariantCollection.ShaderVariant(shader, pass, keywords)); }
            catch (ArgumentException) { }
        }
    }
}
