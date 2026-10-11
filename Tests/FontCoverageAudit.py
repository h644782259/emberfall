#!/usr/bin/env python3
"""Inspect packaged cmap against runtime C# literals; this is not Unity font rendering."""
import argparse
import hashlib
import json
from pathlib import Path
import re
from fontTools.ttLib import TTFont

ROOT=Path(__file__).resolve().parents[1]
TOKENS=re.compile(r'//[^\n]*|/\*.*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'',re.S)
ESCAPES=re.compile(r'\\(?:u([0-9a-fA-F]{4})|U([0-9a-fA-F]{8})|x([0-9a-fA-F]{1,4})|(.))',re.S)
def literals(source):
    for match in TOKENS.finditer(source):
        token=match.group()
        if token.startswith('@"'):yield match.start(),token[2:-1].replace('""','"')
        elif token.startswith(('"',"'")):
            def decode(m):
                code=next((x for x in m.groups()[:3] if x),None)
                if code:return chr(int(code,16))
                return {'n':'\n','r':'\r','t':'\t','0':'\0','a':'\a','b':'\b','f':'\f','v':'\v'}.get(m[4],m[4])
            yield match.start(),ESCAPES.sub(decode,token[1:-1])
def category(c):
    value=ord(c)
    if 0x3400<=value<=0x9fff or 0x3000<=value<=0x303f or 0xff00<=value<=0xffef:return 'cjk'
    if value>=128 and c.isprintable():return 'symbol'
    return None

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--font',type=Path,action='append')
    parser.add_argument('--output',type=Path)
    parser.add_argument('--strict',action='store_true',help='fail if any inspected font lacks a required source glyph')
    args=parser.parse_args()
    occurrences={}
    for path in sorted((ROOT/'Assets/Scripts').rglob('*.cs')):
        source=path.read_text(encoding='utf-8')
        for offset,value in literals(source):
            for c in set(value):
                if category(c):occurrences.setdefault(c,[]).append(str(path.relative_to(ROOT))+':'+str(source.count('\n',0,offset)+1))
    report={'scope':'Runtime C# string/character literals, excluding comments. Cmap only; no Unity rasterization/device verification.',
            'locationSampleLimit':3,'requiredCjk':sum(category(c)=='cjk' for c in occurrences),'requiredSymbols':sum(category(c)=='symbol' for c in occurrences),'fonts':[]}
    for path in args.font or sorted((ROOT/'Assets/Resources/Fonts').glob('*.otf')):
        with TTFont(path) as font:
            cmap=font.getBestCmap() or {}
            missing=[{'character':c,'codepoint':f'U+{ord(c):04X}','category':category(c),'occurrences':len(occurrences[c]),'locations':list(dict.fromkeys(occurrences[c]))[:3]} for c in sorted(occurrences) if ord(c) not in cmap]
            entry={'path':str(path),'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'cmapGlyphs':len(cmap),'missingCjk':sum(x['category']=='cjk' for x in missing),'missingSymbols':sum(x['category']=='symbol' for x in missing),'missing':missing}
            report['fonts'].append(entry)
            print(f"{path.name}: {entry['bytes']} bytes, {entry['cmapGlyphs']} cmap entries, missing CJK={entry['missingCjk']}/{report['requiredCjk']}, symbols={entry['missingSymbols']}/{report['requiredSymbols']}")
            if entry['missingSymbols']:print('Missing symbols: '+''.join(x['character'] for x in missing if x['category']=='symbol'))
    if args.output:args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    return 1 if args.strict and any(x['missing'] for x in report['fonts']) else 0
if __name__=='__main__':raise SystemExit(main())
