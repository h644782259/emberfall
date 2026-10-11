using System;
using System.Globalization;
using Emberfall;

public static class MobileSaveLocationTests
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    private static float Units(string value) { return StringInfo.ParseCombiningCharacters(value).Length; }

    public static string Run()
    {
        checks = 0;
        string[] paths = {
            "/var/mobile/Containers/Data/Application/01234567-89AB-CDEF-0123-456789ABCDEF/Documents",
            "C:\\Users\\A Name\\AppData\\LocalLow\\Emberfall\\emberfall-save-0123456789abcdef0123456789abcdef.json",
            "/保存路径/玩家一/角色存档/",
            "/home/player/" + new string('x', 240),
            "/e\u0301/\U0001f525/中文/space name/"
        };
        foreach (string original in paths)
            foreach (int limit in new[] { 1, 5, 17, 40, 500 })
            {
                string display = MobileSavePathText.Wrap(original, limit, Units);
                Check(display.Replace("\n", "") == original, "display adds line breaks without truncating or changing the original path");
                Check(display == MobileSavePathText.Wrap(original, limit, Units), "path wrapping is deterministic");
                foreach (string line in display.Split('\n'))
                {
                    Check(Units(line) <= limit, "each display line fits its measured width");
                    Check(line.Length == 0 || CharUnicodeInfo.GetUnicodeCategory(line, 0) != UnicodeCategory.NonSpacingMark, "combining mark never starts a new display line");
                    Check(line.Length == 0 || !char.IsLowSurrogate(line[0]), "surrogate pair never splits across lines");
                    Check(line.Length == 0 || !char.IsHighSurrogate(line[line.Length - 1]), "line never ends with a split surrogate pair");
                }
            }
        Check(MobileSavePathText.Wrap("", 1, Units) == "" && MobileSavePathText.Wrap(null, 1, Units) == "", "empty path displays safely");
        Check(MobileSavePathText.Wrap("ab\r\ncd", 2, Units) == "ab\r\ncd", "existing explicit line breaks are preserved");
        Check(MobileSavePathText.Wrap("a\U0001f525b", 1, text => Units(text) * 2) == "a\n\U0001f525\nb", "oversized single grapheme remains intact");
        bool badWidth = false; try { MobileSavePathText.Wrap("path", float.NaN, Units); } catch (ArgumentOutOfRangeException) { badWidth = true; }
        Check(badWidth, "invalid measurement width fails explicitly");
        bool noMeasure = false; try { MobileSavePathText.Wrap("path", 1, null); } catch (ArgumentNullException) { noMeasure = true; }
        Check(noMeasure, "missing font measure fails explicitly");
        foreach (var size in new[] { new[] { 568f, 320f }, new[] { 667f, 375f }, new[] { 1024f, 768f } })
        {
            var layout = new MobilePanelLayout(size[0], size[1]);
            Check(layout.Body.Height >= 188, "compact save-location body remains readable and scrollable");
            foreach (int count in new[] { 2, 3 })
                for (int i = 0; i < count; i++)
                {
                    var action = layout.FooterButton(i, count);
                    Check(action.Height == 48 && action.Width >= 170, "copy/open/back retain 48-unit touch targets");
                    Check(!action.Overlaps(layout.Body), "fixed save-location action never overlaps scroll content");
                }
        }
        return checks + " mobile save path Unicode/wrapping and compact action geometry assertions passed";
    }
}
