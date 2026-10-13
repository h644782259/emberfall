"""Offline, deterministic 4-second skill loops and processed fantasy incantations."""
from pathlib import Path
import wave
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
RATE=22050

def save(path,samples):
    path.parent.mkdir(parents=True,exist_ok=True)
    peak=max(.001,float(np.max(np.abs(samples))))
    samples=np.tanh(samples/peak*1.2)*.82
    with wave.open(str(path),'wb') as out:
        out.setparams((1,2,RATE,len(samples),'NONE','not compressed'))
        out.writeframes((samples*32767).astype('<i2').tobytes())

def filtered_noise(rng,n,low,high):
    frequencies=np.fft.rfftfreq(n,1/RATE)
    spectrum=np.fft.rfft(rng.normal(size=n))
    spectrum*=np.exp(-(frequencies/high)**4)*(1-np.exp(-(frequencies/low)**4))
    data=np.fft.irfft(spectrum,n=n)
    return data/max(.01,np.std(data))

names=['Wind','Void','Fire','Arrows','Roots','Spirit','Ice','Earth','Lightning']
t=np.arange(RATE*4)/RATE
for index,name in enumerate(names):
    rng=np.random.default_rng(741+index)
    low=filtered_noise(rng,len(t),25,240)
    air=filtered_noise(rng,len(t),500,3500)
    pulse=.65+.25*np.sin(2*np.pi*(1+index*.25)*t)
    if name=='Wind': data=.24*low+.20*air*pulse+.12*np.sin(2*np.pi*110*t)
    elif name=='Void': data=.36*np.sin(2*np.pi*55*t)+.16*np.sin(2*np.pi*82.5*t)*pulse+.14*low+.05*air
    elif name=='Fire': data=.27*low+.15*air*(.3+np.maximum(0,np.sin(2*np.pi*7*t))**5)
    elif name=='Arrows': data=.20*air*(.2+np.maximum(0,np.sin(2*np.pi*4*t))**8)+.10*low
    elif name=='Roots': data=.20*low+.14*air*np.maximum(0,np.sin(2*np.pi*3.25*t))**6+.09*np.sin(2*np.pi*73.5*t)
    elif name=='Spirit': data=.16*np.sin(2*np.pi*165*t)+.12*np.sin(2*np.pi*247.5*t)*pulse+.06*air
    elif name=='Ice': data=.14*air+.11*np.sin(2*np.pi*880*t)*np.maximum(0,np.sin(2*np.pi*2*t))**12+.08*np.sin(2*np.pi*1320*t)*pulse
    elif name=='Earth': data=.35*low+.20*np.sin(2*np.pi*48*t)*pulse+.06*air
    else: data=.20*air*(.25+np.maximum(0,np.sin(2*np.pi*9*t))**10)+.12*np.sin(2*np.pi*110*t)+.07*low
    # Cyclic crossfade: matching endpoints prevent clicks on any number of repeats.
    fade=2205
    blend=np.linspace(0,1,fade)
    data[:fade]=data[-fade:]*(1-blend)+data[:fade]*blend
    data=data[:-fade]
    save(ROOT/'Assets/Resources/Audio/Skills'/f'{name}.wav',data)

for source in sorted((ROOT/'Temp/SkillChants').glob('*.wav')):
    with wave.open(str(source),'rb') as sound:
        raw=np.frombuffer(sound.readframes(sound.getnframes()),dtype='<i2').astype(float)/32768
    nonzero=np.flatnonzero(np.abs(raw)>.005)
    if not len(nonzero): raise ValueError(f'Silent chant: {source}')
    raw=raw[max(0,nonzero[0]-220):min(len(raw),nonzero[-1]+440)]
    hero=int(source.stem[0]);ratio=[.76,.91,1.04,.81][hero]
    voice=np.interp(np.arange(0,len(raw)-1,ratio),np.arange(len(raw)),raw)
    voice=np.tanh(voice*2.2)*.65
    # Dark doubles and short, decaying reflections keep the voice forceful and intelligible as syllables.
    out=np.zeros(len(voice)+int(RATE*.65));out[:len(voice)]=voice
    for delay,gain in [(.021,.14),(.093,.20),(.173,.13),(.287,.08),(.431,.04)]:
        start=int(RATE*delay);out[start:start+len(voice)]+=voice*gain
    edge=min(220,len(out)//8);out[:edge]*=np.linspace(0,1,edge);out[-edge:]*=np.linspace(1,0,edge)
    save(ROOT/'Assets/Resources/Audio/Chants'/source.name,out)
print('Generated 9 sustained beds and 32 processed fictional chants.')
