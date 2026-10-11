using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Builds a separate screenshot-only player; never changes global scripting defines.</summary>
    public static class VisualValidationBuild
    {
        public static void Build()
        {
            ProjectTools.EnsureSettings();
            string originalProduct = PlayerSettings.productName;
            bool originalBackground = PlayerSettings.runInBackground;
            int originalWidth = PlayerSettings.defaultScreenWidth;
            int originalHeight = PlayerSettings.defaultScreenHeight;
            try
            {
                // A separate product also keeps Unity's window preferences separate from the release.
                PlayerSettings.productName = "Emberfall Visual Validation";
                PlayerSettings.runInBackground = true;
                PlayerSettings.defaultScreenWidth = 1280;
                PlayerSettings.defaultScreenHeight = 720;
                AssetDatabase.SaveAssets();
                string workspace = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string output = Path.Combine(workspace, "Builds", "VisualValidation", "Emberfall.exe");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                BuildReport result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/Main.unity" },
                    target = BuildTarget.StandaloneWindows64,
                    locationPathName = output,
                    options = BuildOptions.Development,
                    extraScriptingDefines = new[] { "EMBERFALL_VISUAL_VALIDATION" }
                });
                if (result.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Visual validation player build failed: " + result.summary.result + ", errors=" + result.summary.totalErrors);
                Debug.Log("Visual validation player ready: " + output + " --visual-validation-root <absolute isolated output directory>");
            }
            finally
            {
                PlayerSettings.productName = originalProduct;
                PlayerSettings.runInBackground = originalBackground;
                PlayerSettings.defaultScreenWidth = originalWidth;
                PlayerSettings.defaultScreenHeight = originalHeight;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
