using System;
using UnityEngine;

namespace Emberfall
{
    internal sealed partial class FrameReviewProbe
    {
        private const string SingleGrainKeyword="EMBER_WORLD_SINGLE_GRAIN_REVIEW";
        private bool changedSingleGrain, savedSingleGrain;
        private void ConfigureSingleGrainReview()
        {
            // This component exists only after an explicit PERF_REVIEW launch.
            if(Environment.GetEnvironmentVariable("EMBERFALL_REVIEW_SINGLE_GRAIN")!="1")return;
            savedSingleGrain=Shader.IsKeywordEnabled(SingleGrainKeyword);
            changedSingleGrain=true;
            Shader.EnableKeyword(SingleGrainKeyword);
        }
        private void RestoreSingleGrainReview()
        {
            if(!changedSingleGrain)return;
            if(savedSingleGrain)Shader.EnableKeyword(SingleGrainKeyword);
            else Shader.DisableKeyword(SingleGrainKeyword);
            changedSingleGrain=false;
        }
    }
}
