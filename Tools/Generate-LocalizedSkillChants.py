#!/usr/bin/env python3
"""Bake offline macOS system speech for the 32 active skills in three languages.
Requires installed Sinji (Cantonese), Samantha (English), Tingting (Mandarin).
Original fictional chants are never modified. No speech service at game runtime.
"""
from pathlib import Path
import re
import sys
import math
import array
import subprocess
import tempfile
import uuid
import wave

ROOT = Path(__file__).resolve().parents[1]
ENGLISH = [
    ['Whirlwind Slash', 'Earth Shatter', 'Blade Storm', '', 'Sky Cleaver', 'Vanguard Charge', 'Banner of Life', 'Earth Rupture', '', 'Final Judgment'],
    ['Frost Nova', 'Meteor', 'Void Vortex', '', 'Chain Lightning', 'Frostfire Walker', 'Rising Gravity', 'Gravity Switch', '', 'Meteor Shower', 'Astral Pillar'],
    ['Arrow Fan', 'Shock Trap', 'Arrow Rain', '', 'Wind Leap', 'Venom Cage', 'Piercing Gale', 'Phantom Volley', '', 'Starfall Arrows'],
    ['Spirit Blast', 'Thorn Cage', 'Spirit Wolf', '', 'Astral Turret', 'Soul Spear', 'Ember Wings', 'Gravity Mark', '', 'Ancient Treant'],
]

def process_chant(path, hero):
    # Same trim, class pitch, saturation, reflections and normalization as
    # Generate-SkillSoundscapes.py; standard library keeps generation portable.
    with wave.open(str(path)) as sound:
        raw = array.array('h', sound.readframes(sound.getnframes()))
    raw = [v / 32768 for v in raw]
    audible = [i for i, v in enumerate(raw) if abs(v) > .005]
    if not audible:
        raise ValueError(f'Silent chant: {path}')
    raw = raw[max(0, audible[0]-220):min(len(raw), audible[-1]+440)]
    ratio = [.76, .91, 1.04, .81][hero]
    voice = []
    for i in range(math.ceil((len(raw)-1)/ratio)):
        x = i*ratio
        k = int(x)
        v = raw[k] + (raw[k+1]-raw[k])*(x-k)
        voice.append(math.tanh(v*2.2)*.65)
    out = voice + [0.] * int(22050*.65)
    for delay, gain in [(.021,.14),(.093,.20),(.173,.13),(.287,.08),(.431,.04)]:
        start = int(22050*delay)
        for i, v in enumerate(voice):
            out[start+i] += v*gain
    edge = min(220,len(out)//8)
    for i in range(edge):
        out[i] *= i/(edge-1)
        out[-edge+i] *= 1-i/(edge-1)
    peak = max(.001, max(abs(v) for v in out))
    samples = array.array('h', (int(math.tanh(v/peak*1.2)*.82*32767) for v in out))
    with wave.open(str(path), 'wb') as sound:
        sound.setparams((1,2,22050,len(samples),'NONE','not compressed'))
        sound.writeframes(samples.tobytes())

def main():
    source = (ROOT / 'Assets/Scripts/Core/GameTypes.cs').read_text(encoding='utf-8-sig')
    block = source.split('string[,] SkillNames = {', 1)[1].split('};', 1)[0]
    names = re.findall(r'"([^"]+)"', block)
    assert len(names) == 40
    base = ROOT / 'Assets/Resources/Audio/Chants'
    template = (base / '0-0.wav.meta').read_text()
    with tempfile.TemporaryDirectory(prefix='emberfall-chants-') as temp:
        languages = [('Cantonese', 'Sinji'), ('English', 'Samantha'), ('Mandarin', 'Tingting')]
        if '--elementalist-rework' in sys.argv:
            languages.append(('', 'Samantha'))
        for language, voice in languages:
            folder = base / language
            folder.mkdir(exist_ok=True)
            folder_meta = Path(str(folder) + '.meta')
            if not folder_meta.exists():
                folder_meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
            for hero in range(4):
                for skill in range(11 if hero == 1 else 10):
                    if '--elementalist-rework' in sys.argv and (hero != 1 or skill not in (0,2,6,7,9,10)):
                        continue
                    if skill in (3, 8):
                        continue
                    phrase = ENGLISH[hero][skill] if language == 'English' else ('巡星天柱' if skill == 10 else names[hero * 10 + skill])
                    if language == '':
                        phrase = {0:'Iss ah, vey rah!',2:'Voh rah, neh kha thoom!',6:'Ohm rah, vey thah!',7:'Kah roh, ah veh!',9:'Ohr dah, seh rah, thoom!',10:'Zeh lah, koh rai!'}[skill]
                    aiff = Path(temp) / 'voice.aiff'
                    destination = folder / f'{hero}-{skill}.wav'
                    output = Path(temp) / 'processed.wav'
                    subprocess.run(['say', '-v', voice, '-r', str([190,175,205,175][hero]), '-o', str(aiff), phrase], check=True)
                    subprocess.run(['afconvert', '-f', 'WAVE', '-d', 'LEI16@22050', '-c', '1', str(aiff), str(output)], check=True)
                    with wave.open(str(output)) as audio:
                        assert audio.getnframes() > 4000, f'Empty speech: {language} {phrase}'
                    process_chant(output, hero)
                    destination.write_bytes(output.read_bytes())
                    output=destination
                    meta = Path(str(output) + '.meta')
                    if not meta.exists():
                        meta.write_text(re.sub(r'guid: \w+', f'guid: {uuid.uuid4().hex}', template))
            print(f'{language}: generated selected active skill clips', flush=True)

if __name__ == '__main__':
    main()
