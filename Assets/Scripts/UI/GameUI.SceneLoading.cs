using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Texture2D sceneLoadingBackground, sceneLoadingShade, sceneLoadingFill, sceneLoadingSheen;
        private string[] sceneLoadingPercentLabels;

        // Prepare the loading presentation before a scene transition can begin.
        // Drawing the overlay never loads an asset or creates a texture.
        private void PrewarmSceneLoadingArt()
        {
            if (sceneLoadingPercentLabels != null) return;
            sceneLoadingBackground = Resources.Load<Texture2D>("LoadingArt/JourneyBackground");
            sceneLoadingPercentLabels = new string[101];
            for (int i = 0; i < sceneLoadingPercentLabels.Length; i++) sceneLoadingPercentLabels[i] = i + "%";
            sceneLoadingShade = LoadingTexture(1, 128, "Journey lower shade");
            for (int y = 0; y < 128; y++)
            {
                float depth = 1f - y / 127f;
                sceneLoadingShade.SetPixel(0, y, new Color(.015f, .023f, .031f, Mathf.SmoothStep(0, .96f, depth)));
            }
            sceneLoadingShade.Apply(false, true);
            sceneLoadingFill = LoadingTexture(256, 8, "Journey progress metal");
            for (int y = 0; y < 8; y++) for (int x = 0; x < 256; x++)
            {
                Color tint = Color.Lerp(new Color(.34f, .71f, .69f), new Color(.96f, .77f, .42f), x / 255f);
                float highlight = .78f + .22f * Mathf.Sin(y / 7f * Mathf.PI);
                sceneLoadingFill.SetPixel(x, y, new Color(tint.r * highlight, tint.g * highlight, tint.b * highlight, 1));
            }
            sceneLoadingFill.Apply(false, true);
            sceneLoadingSheen = LoadingTexture(64, 8, "Journey warm sheen");
            for (int y = 0; y < 8; y++) for (int x = 0; x < 64; x++)
            {
                float softness = Mathf.Pow(Mathf.Sin(x / 63f * Mathf.PI), 3) * Mathf.Sin(y / 7f * Mathf.PI);
                sceneLoadingSheen.SetPixel(x, y, new Color(1, .91f, .65f, softness * .46f));
            }
            sceneLoadingSheen.Apply(false, true);
        }

        private static Texture2D LoadingTexture(int textureWidth, int textureHeight, string textureName)
        {
            return new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                name = textureName, hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
        }

        private void ReleaseSceneLoadingArt()
        {
            if (sceneLoadingBackground != null) Resources.UnloadAsset(sceneLoadingBackground);
            if (sceneLoadingShade != null) Destroy(sceneLoadingShade);
            if (sceneLoadingFill != null) Destroy(sceneLoadingFill);
            if (sceneLoadingSheen != null) Destroy(sceneLoadingSheen);
            sceneLoadingBackground = sceneLoadingShade = sceneLoadingFill = sceneLoadingSheen = null;
            sceneLoadingPercentLabels = null;
        }

        private void DrawSceneLoading()
        {
            bool mobile = MobileControls.Active;
            bool failed = !string.IsNullOrEmpty(session.SceneLoadingError);
            float u = mobile ? TouchRatio : 1f;
            int CaptionFont(float size) => mobile ? TouchFont(size) : Mathf.RoundToInt(size);
            int titleFont = CaptionFont(mobile ? 23 : 27);
            // Include scaled font metrics and breathing room for the Chinese glyphs.
            float titleHeight = Mathf.Max(40 * u, Style(titleFont, true).CalcSize(new GUIContent("旅途整备")).y + 4 * u);
            float titleExtra = titleHeight - 32 * u;
            float contentHeight = (failed ? 228 : 150) * u + titleExtra;
            float w = Mathf.Min(width - 48 * u, (mobile ? 560 : 760) * u);
            float x = (width - w) * .5f, y = Mathf.Max(12 * u, height - contentHeight - 24 * u);
            blockedRects.Add(new Rect(-guiOffset.x / scale, -guiOffset.y / scale, Screen.width / scale, Screen.height / scale));

            Matrix4x4 safeMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            Rect physicalScreen = new Rect(0, 0, Screen.width, Screen.height);
            Fill(physicalScreen, new Color(.015f, .023f, .031f, 1));
            if (Event.current.type == EventType.Repaint)
            {
                if (sceneLoadingBackground != null) GUI.DrawTexture(physicalScreen, sceneLoadingBackground, ScaleMode.ScaleAndCrop);
                if (sceneLoadingShade != null)
                {
                    float shadeHeight = Mathf.Min(Screen.height, (contentHeight + 110 * u) * scale + Screen.height - guiOffset.y - height * scale);
                    GUI.DrawTexture(new Rect(0, Screen.height - shadeHeight, Screen.width, shadeHeight), sceneLoadingShade);
                }
            }
            GUI.matrix = safeMatrix;
            GUI.color = previousColor;

            Color lettering = new Color(.96f, .94f, .87f);
            Color softGold = new Color(.84f, .73f, .51f);
            Text(new Rect(x, y, w, 18 * u), "EMBERFALL  ·  星烬纪元", CaptionFont(11), softGold, false);
            Text(new Rect(x, y + 22 * u, w, titleHeight), "旅途整备", titleFont, lettering, true);
            y += titleExtra;
            Text(new Rect(x, y + 60 * u, w - 64 * u, 40 * u), session.SceneLoadingStage, CaptionFont(14), lettering, false, true);
            int percentage = Mathf.Clamp(Mathf.FloorToInt(session.SceneLoadingProgress * 100), 0, 100);
            Text(new Rect(x + w - 64 * u, y + 60 * u, 64 * u, 24 * u),
                sceneLoadingPercentLabels == null ? "" : sceneLoadingPercentLabels[percentage], CaptionFont(14), softGold, true, false, TextAnchor.UpperRight);
            DrawSceneLoadingProgress(new Rect(x, y + 105 * u, w, 11 * u), session.SceneLoadingProgress, u);
            if (!failed)
                Text(new Rect(x, y + 126 * u, w, 24 * u), "准备完成后即可行动", CaptionFont(12), new Color(.75f, .78f, .78f), false, true);
            else
            {
                Text(new Rect(x, y + 126 * u, w, 42 * u), session.SceneLoadingError, CaptionFont(12), lettering, false, true);
                if (PrimaryButton(new Rect(x, y + 176 * u, Mathf.Min(w, 240 * u), 44 * u), "返回首页", softGold))
                { session.ReturnFromSceneLoadingFailure(); panel = Panel.None; BlockUITransition(); }
            }
        }

        private void DrawSceneLoadingProgress(Rect frame, float progress, float u)
        {
            Fill(frame, new Color(.12f, .10f, .075f));
            Border(frame, new Color(.57f, .44f, .24f), u);
            Fill(new Rect(frame.x + u, frame.y, frame.width - 2 * u, u), new Color(.91f, .77f, .47f, .85f));
            Fill(new Rect(frame.x + u, frame.yMax - u, frame.width - 2 * u, u), new Color(.30f, .24f, .15f));
            Rect inset = new Rect(frame.x + 2 * u, frame.y + 2 * u, Mathf.Max(0, frame.width - 4 * u), frame.height - 4 * u);
            Fill(inset, new Color(.035f, .062f, .065f));
            float completed = Mathf.Clamp01(progress);
            if (Event.current.type != EventType.Repaint || completed <= 0 || sceneLoadingFill == null) return;
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(new Rect(inset.x, inset.y, inset.width * completed, inset.height),
                sceneLoadingFill, new Rect(0, 0, completed, 1));
            if (sceneLoadingSheen != null && !EffectPreferences.ReducedEffects)
            {
                float sheenWidth = Mathf.Min(inset.width * .19f, 92 * u);
                float travel = Mathf.Repeat(Time.realtimeSinceStartup * .18f, 1) * (inset.width + 2 * sheenWidth) - sheenWidth;
                // Only the sheen travels; its clip ends at the real completed work.
                GUI.BeginGroup(new Rect(inset.x, inset.y, inset.width * completed, inset.height));
                GUI.DrawTexture(new Rect(travel, 0, sheenWidth, inset.height), sceneLoadingSheen);
                GUI.EndGroup();
            }
            GUI.color = previous;
        }
    }
}
