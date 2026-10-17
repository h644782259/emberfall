using System;
using UnityEngine;

namespace Emberfall
{
    internal sealed partial class FrameReviewProbe
    {
        private bool resolutionComparison;
        private int resolutionReviewMaximum, savedReviewWidth, savedReviewHeight;
        private FullScreenMode savedReviewScreenMode;
        private int capturedResolutionWindow=-1;

        private void ConfigureResolutionReview()
        {
            if(tag=="performance-resolution-ab")
            {resolutionComparison=true;freezeSimulation=true;reviewSeconds=30;}
            // Explicit review only, never a saved user preference or a default.
            if((sustainedReview||groundComparison)&&int.TryParse(Environment.GetEnvironmentVariable("EMBERFALL_REVIEW_MAX_DIMENSION"),out int value)&&
                (value==960||value==1120||value==1280||value==1600||value==1920))resolutionReviewMaximum=value;
        }

        private void ApplyReviewResolution()
        {
            if(!resolutionComparison&&resolutionReviewMaximum==0)return;
            if(savedReviewWidth==0)
            {savedReviewWidth=Screen.width;savedReviewHeight=Screen.height;savedReviewScreenMode=Screen.fullScreenMode;}
            int maximum=resolutionComparison?(comparisonWindow%2==0?1920:1600):resolutionReviewMaximum;
            int sourceWidth=Display.main.systemWidth,sourceHeight=Display.main.systemHeight;
            if(sourceWidth<=0||sourceHeight<=0){sourceWidth=savedReviewWidth;sourceHeight=savedReviewHeight;}
            if((sourceWidth>sourceHeight)!=(savedReviewWidth>savedReviewHeight))
            {int swap=sourceWidth;sourceWidth=sourceHeight;sourceHeight=swap;}
            GraphicsQualityPolicy.RenderSize(sourceWidth,sourceHeight,maximum,out int width,out int height);
            if(Screen.width!=width||Screen.height!=height)Screen.SetResolution(width,height,savedReviewScreenMode);
            if(resolutionComparison&&comparisonWindow<2&&capturedResolutionWindow!=comparisonWindow&&
                Environment.GetEnvironmentVariable("EMBERFALL_REVIEW_SCREENSHOTS")=="1")
            {capturedResolutionWindow=comparisonWindow;StartCoroutine(CaptureResolutionFrame(comparisonWindow));}
        }

        // Explicit visual review during warmup, never within a timing window.
        private System.Collections.IEnumerator CaptureResolutionFrame(int window)
        {
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                string directory=System.IO.Path.Combine(Application.persistentDataPath,"Review");
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,"resolution-review-"+window+".png"),texture.EncodeToPNG());
            }
            finally {if(texture!=null)Destroy(texture);}
        }

        private void RestoreReviewResolution()
        {
            if(savedReviewWidth>0)Screen.SetResolution(savedReviewWidth,savedReviewHeight,savedReviewScreenMode);
            savedReviewWidth=savedReviewHeight=0;
        }
    }
}
