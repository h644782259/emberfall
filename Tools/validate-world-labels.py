#!/usr/bin/env python3
"""Check authored Chinese labels and bundled glyph outlines without running Unity.

Requires fonttools. Optional --preview path.png also requires Pillow and produces
only a font specimen, not a Unity screenshot or rendering validation.
"""
import argparse
from pathlib import Path
import re
from fontTools.ttLib import TTFont
from fontTools.pens.boundsPen import BoundsPen

ROOT = Path(__file__).resolve().parent.parent
FONT = ROOT / "Assets/Resources/Fonts/EmberfallWorldLabels.otf"
EXPECTED = {
    "CAMP": "营地", "FALLEN STAR": "沉星遗迹",
    "STAR CORE": "星核", "APPRENTICE": "观星学徒",
    "CODEX": "装备图鉴", "CLASS TRIAL": "职业试炼",
    "THE FALLEN SANCTUM": "沉星遗迹",
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--preview", type=Path)
    args = parser.parse_args()
    source = (ROOT / "Assets/Scripts/World/WorldBuilder.cs").read_text(encoding="utf-8")
    direct = dict(re.findall(r'Label\(parent, "([^"]+)", "([^"]+)"', source))
    facility_ids = re.findall(r'"([^"]+)"', re.search(r'string\[\] names = \{([^}]+)\}', source).group(1))
    facility_labels = re.findall(r'"([^"]+)"', re.search(r'string\[\] labels = \{([^}]+)\}', source).group(1))
    assert len(facility_ids) == len(facility_labels) == 4
    assert direct | dict(zip(facility_ids, facility_labels)) == EXPECTED
    assert 'Label(facilities, names[i], labels[i],' in source
    assert 'new GameObject(objectName)' in source
    assert 'Font font=GameFont.WorldLabels;' in source
    resolver = (ROOT / "Assets/Scripts/UI/GameFont.cs").read_text(encoding="utf-8")
    assert 'WorldLabelPath="Fonts/EmberfallWorldLabels"' in resolver
    world_labels = re.search(r'public static Font WorldLabels\s*\{(.*?)\n        \}', resolver, re.S)
    assert world_labels is not None, 'World-label resolver is missing'
    assert 'worldLabels=Resources.Load<Font>(WorldLabelPath)' in world_labels.group(1)
    assert 'return worldLabels!=null?worldLabels:Shared;' in world_labels.group(1)
    assert 'BundledUiPath="Fonts/NotoSansSC-Regular"' in resolver
    assert 'shared=Resources.Load<Font>(BundledUiPath)' in resolver
    assert 'text.font=font' in source and 'sharedMaterial=font.material' in source
    metadata = FONT.with_suffix('.otf.meta').read_text()
    assert 'includeFontData: 1' in metadata and 'forceTextureCase: -2' in metadata
    license_text = FONT.with_name('EmberfallWorldLabels-LICENSE.txt').read_text()
    assert 'SIL OPEN FONT LICENSE Version 1.1' in license_text and 'Adobe' in license_text
    with TTFont(FONT) as font:
        assert font['name'].getDebugName(1) == 'Emberfall World Labels'
        assert 'SIL OPEN FONT LICENSE Version 1.1' in font['name'].getDebugName(13)
        cmap = font.getBestCmap()
        glyphs = font.getGlyphSet()
        characters = set(''.join(EXPECTED.values()))
        assert len(characters) == 18, 'Authored label glyph coverage changed'
        for character in sorted(characters):
            assert ord(character) in cmap, 'Missing character: ' + character
            name = cmap[ord(character)]
            assert name != '.notdef', 'Tofu glyph: ' + character
            pen = BoundsPen(glyphs)
            glyphs[name].draw(pen)
            assert pen.bounds is not None, 'Empty glyph: ' + character
            x0, y0, x1, y1 = pen.bounds
            assert x1 > x0 and y1 > y0 and glyphs[name].width > 0
        print(f'PASS: 7 labels, {len(characters)} Chinese glyphs with nonempty outlines, stable object IDs, bundled font/material wiring and license')
    if args.preview:
        from PIL import Image, ImageDraw, ImageFont
        specimen = Image.new('RGB', (760, 410), '#233530')
        draw = ImageDraw.Draw(specimen)
        specimen_font = ImageFont.truetype(str(FONT), 48)
        for index, value in enumerate(dict.fromkeys(EXPECTED.values())):
            draw.text((35 + (index % 2) * 370, 20 + (index // 2) * 120), value,
                      font=specimen_font, fill=('#75d9fa', '#d29ff5', '#ffcb7e')[index // 2])
        args.preview.parent.mkdir(parents=True, exist_ok=True)
        specimen.save(args.preview)
        print('Font specimen only (not Unity): ' + str(args.preview))


if __name__ == '__main__':
    main()
