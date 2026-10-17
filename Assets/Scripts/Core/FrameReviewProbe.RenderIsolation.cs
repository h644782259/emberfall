using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    internal sealed partial class FrameReviewProbe
    {
        private readonly List<Renderer> isolatedRenderers = new List<Renderer>();
        private ShadowQuality? savedShadows;
        private bool disableReviewShadows;
        private int? savedReviewAntialiasing;
        private bool disableReviewAntialiasing;
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
        {
            // Hold explicit pixel-budget experiments through thermal policy changes.
            // Neither override is active during ordinary play.
            if (sustainedReview && resolutionReviewMaximum > 0) ApplyReviewResolution();
            if (disableReviewAntialiasing && QualitySettings.antiAliasing != 0)
            {
                if (!savedReviewAntialiasing.HasValue) savedReviewAntialiasing = QualitySettings.antiAliasing;
                QualitySettings.antiAliasing = 0;
            }
            // Explicit sustained A/B only; normal gameplay retains its shadow policy.
            if (disableReviewShadows && QualitySettings.shadows != ShadowQuality.Disable)
            {
                if (!savedShadows.HasValue) savedShadows = QualitySettings.shadows;
                QualitySettings.shadows = ShadowQuality.Disable;
            }
            foreach(var renderer in isolatedRenderers)if(renderer!=null)renderer.enabled=false;
        }
        private void RestoreRenderIsolation()
        {
            foreach(var renderer in isolatedRenderers)if(renderer!=null)renderer.enabled=true;
            isolatedRenderers.Clear();
            if(savedShadows.HasValue){QualitySettings.shadows=savedShadows.Value;savedShadows=null;}
            if(savedReviewAntialiasing.HasValue){QualitySettings.antiAliasing=savedReviewAntialiasing.Value;savedReviewAntialiasing=null;}
        }
    }
}
