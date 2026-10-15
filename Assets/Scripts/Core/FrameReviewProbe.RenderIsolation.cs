using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    internal sealed partial class FrameReviewProbe
    {
        private readonly List<Renderer> isolatedRenderers = new List<Renderer>();
        private ShadowQuality? savedShadows;
        private string RenderIsolationMode => comparisonWindow == 1 ? "decoration-hidden" :
            comparisonWindow == 2 ? "shadows-disabled" : comparisonWindow == 3 ? "actors-hidden" : "baseline";

        private void PrepareRenderIsolationWindow()
        {
            RestoreRenderIsolation();
            if(comparisonWindow == 2)
            {savedShadows=QualitySettings.shadows;QualitySettings.shadows=ShadowQuality.Disable;return;}
            if(comparisonWindow != 1 && comparisonWindow != 3)return;
            foreach(var renderer in FindObjectsByType<Renderer>())
            {
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
                bool selected = comparisonWindow == 1 ? renderer.name.StartsWith("Prepared decoration ") :
                    renderer.GetComponentInParent<CombatModel>() != null;
                if(selected)isolatedRenderers.Add(renderer);
            }
        }
        private void EnforceRenderIsolation()
        {foreach(var renderer in isolatedRenderers)if(renderer!=null)renderer.enabled=false;}
        private void RestoreRenderIsolation()
        {
            foreach(var renderer in isolatedRenderers)if(renderer!=null)renderer.enabled=true;
            isolatedRenderers.Clear();
            if(savedShadows.HasValue){QualitySettings.shadows=savedShadows.Value;savedShadows=null;}
        }
    }
}
