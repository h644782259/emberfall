using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private string GraphicsModeButton(GraphicsQualityMode mode)
        { return (session.GraphicsQualitySelection == mode ? "✓ " : "") + GraphicsQualityPolicy.Name(mode); }

        private string GraphicsModeExplanation()
        {
            switch (GraphicsQualityRuntime.Current.Mode)
            {
                case GraphicsQualityMode.Smooth: return GraphicsQualityRuntime.Current.Mobile
                    ? "降低画面精细度、关闭实时投影，减少持续运行负载，保留操作响应与战斗提示。"
                    : "优先流畅操作，减少远处草叶、碎石与装饰特效。";
                case GraphicsQualityMode.High: return "更清晰的材质边缘、柔和阴影与丰富场景细节。";
                default: return "兼顾清晰度与持续流畅，保留近处场景和打击细节。";
            }
        }
        private void DrawDesktopGraphicsSettings(Rect area)
        {
            Text(new Rect(area.x, area.y, area.width, 26), "画质与性能", 18, pale, true);
            float buttonWidth = (area.width - 24) / 4;
            for (int i = 0; i < 4; i++)
            {
                var mode = (GraphicsQualityMode)i;
                if (Button(new Rect(area.x + i * (buttonWidth + 8), area.y + 34, buttonWidth, 40), GraphicsModeButton(mode), session.GraphicsQualitySelection == mode ? gold : jade))
                    session.SetGraphicsQuality(mode);
            }
            Text(new Rect(area.x, area.y + 84, area.width, 30), session.GraphicsQualitySummary, 13, gold, false, true);
            Text(new Rect(area.x, area.y + 118, area.width, 38), GraphicsModeExplanation(), 13, muted, false, true);
            Text(new Rect(area.x, area.y + 158, area.width, 30), "各档保留技能范围、命中反馈与敌人预警。", 12, muted, false, true);
        }
        private float MobileGraphicsSettingsHeight(float width)
        {
            return 154 + MeasureMobileParagraph(session.GraphicsQualitySummary, width, 11, false) +
                MeasureMobileParagraph(GraphicsModeExplanation(), width, 11, false) +
                MeasureMobileParagraph("各档保留技能范围、命中反馈与敌人预警。", width, 11, false);
        }
        private void DrawMobileGraphicsSettings(float x, float y, float width)
        {
            Text(TouchRect(x, y, width, 28), "画质与性能", TouchFont(16), pale, true);
            float column = (width - 12) * .5f;
            for (int i = 0; i < 4; i++)
            {
                var mode = (GraphicsQualityMode)i;
                if (Button(TouchRect(x + (i % 2) * (column + 12), y + 36 + (i / 2) * 58, column, 48), GraphicsModeButton(mode), session.GraphicsQualitySelection == mode ? gold : jade))
                    session.SetGraphicsQuality(mode);
            }
            float at = y + 154;
            at += DrawMobileParagraph(x, at, width, session.GraphicsQualitySummary, 11, gold);
            at += DrawMobileParagraph(x, at, width, GraphicsModeExplanation(), 11, muted);
            DrawMobileParagraph(x, at, width, "各档保留技能范围、命中反馈与敌人预警。", 11, muted);
        }
    }
}
