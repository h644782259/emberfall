using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall.Editor
{
    // Called by the live runtime smoke test after each world has been constructed.
    public static class WorldLabelValidation
    {
        public static void Validate(ZoneKind zone, Action<bool, string> check)
        {
            bool dungeon = zone == ZoneKind.Dungeon;
            GameObject world = GameObject.Find(dungeon ? "Fallen Star Sanctum" : "Windwhisper Fields");
            check(world != null, "World exists for Chinese label validation");
            Font bundledFont = Resources.Load<Font>(GameFont.WorldLabelPath);
            check(bundledFont != null && bundledFont.dynamic, "Chinese label font is bundled and dynamic");
            check(GameFont.WorldLabels == bundledFont, "World-label resolver selects the packaged subset when present");
            var expected = dungeon
                ? new Dictionary<string, string> { { "THE FALLEN SANCTUM", "沉星遗迹" } }
                : new Dictionary<string, string>
                {
                    { "CAMP", "营地" }, { "FALLEN STAR", "沉星遗迹" },
                    { "STAR CORE", "星核" }, { "APPRENTICE", "观星学徒" },
                    { "CODEX", "装备图鉴" }, { "CLASS TRIAL", "职业试炼" }
                };
            TextMesh[] labels = world.GetComponentsInChildren<TextMesh>();
            check(labels.Length == expected.Count, "All authored world labels are covered by localization checks");
            foreach (TextMesh label in labels)
            {
                string localized;
                check(expected.TryGetValue(label.name, out localized) && label.text == localized,
                    "World label keeps its stable object name and displays Chinese: " + label.name);
                check(label.font == bundledFont, "World label uses the bundled Chinese font: " + label.name);
                MeshRenderer renderer = label.GetComponent<MeshRenderer>();
                check(renderer.sharedMaterial == bundledFont.material && renderer.sharedMaterial.mainTexture != null,
                    "World label uses its font's glyph atlas: " + label.name);
                bundledFont.RequestCharactersInTexture(label.text, label.fontSize, label.fontStyle);
                foreach (char character in label.text)
                {
                    CharacterInfo glyph;
                    check(bundledFont.HasCharacter(character) &&
                        bundledFont.GetCharacterInfo(character, out glyph, label.fontSize, label.fontStyle) &&
                        glyph.glyphWidth > 0 && glyph.glyphHeight > 0 && glyph.advance > 0,
                        "Chinese glyph has visible geometry: " + character);
                }
                // Even the four-character floor/portal titles fit within their authored areas.
                check(renderer.bounds.size.x > 0 && renderer.bounds.size.x < 5,
                    "Chinese world label has a compact, nonempty width: " + label.name);
                expected.Remove(label.name);
            }
            check(expected.Count == 0, "Every expected Chinese world label was found once");
        }
    }
}
