#!/usr/bin/env python3
"""Exercise actual GameAudio PCM and playback policy through a small Unity facade.

Exports uncompressed review WAVs and spectral/temporal measurements. This is not
native DSP, simulator playback, or physical speaker/listening acceptance.
"""
from pathlib import Path
import argparse, hashlib, importlib.util, json, os, shutil, subprocess, tempfile

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('cv', ROOT/'Tools/cloud-validation.py')
cv = importlib.util.module_from_spec(spec); spec.loader.exec_module(cv)
SDK = '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
PROGRAM = r'''
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Numerics;using System.Text.Json;
using UnityEngine;using Emberfall;
namespace UnityEngine {
public enum RuntimeInitializeLoadType{SubsystemRegistration,AfterSceneLoad}public class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
public class Object {public static void Destroy(Object x){if(x!=null)x.destroyed=true;}public bool destroyed;public static void DontDestroyOnLoad(Object x){}}
public class GameObject:Object {public bool activeInHierarchy=true;public GameObject(string name=""){}public T AddComponent<T>()where T:new(){var x=new T();if(x is MonoBehaviour m){m.gameObject=this;typeof(T).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(m,null);}return x;}}
public class MonoBehaviour:Object {public bool enabled=true;public bool isActiveAndEnabled=>enabled;public GameObject gameObject;public static T FindObjectOfType<T>()=>default;public static T[] FindObjectsOfType<T>()=>Array.Empty<T>();}
public enum HideFlags{DontSave}public class AudioClip:Object {public static int Created;public string name;public float[] Data;public int samples,channels,frequency;public HideFlags hideFlags;public static AudioClip Create(string n,int length,int c,int rate,bool stream){Created++;return new AudioClip{name=n,samples=length,channels=c,frequency=rate};}public bool SetData(float[] x,int offset){Data=x.ToArray();return true;}public bool GetData(float[] x,int offset){Array.Copy(Data,offset,x,0,x.Length);return true;}}
public class AudioSource:Object {public bool playOnAwake,loop,ignoreListenerPause,mute,enabled=true,isPlaying;public float spatialBlend,dopplerLevel,volume;public int priority,timeSamples,PlayCount,StopCount;public AudioClip clip;public void Play(){isPlaying=true;PlayCount++;}public void Stop(){isPlaying=false;StopCount++;}public void Pause(){isPlaying=false;}public void UnPause(){isPlaying=true;}}
public class AudioListener:Object {public static bool pause;public static float volume=1;public bool enabled=true;public bool isActiveAndEnabled=>enabled;}
public class Camera {public static Camera main=new Camera();public GameObject gameObject=new GameObject();public T GetComponent<T>()=>default;}
public static class AudioSettings {public static int outputSampleRate=48000;public static event Action<bool> OnAudioConfigurationChanged;}
public static class Time {public static float unscaledTime,unscaledDeltaTime=.016f;}public static class Application {public static bool isPlaying=true;}
public static class Debug {public static void LogError(string x){throw new Exception(x);}}
public static class Mathf {public static float Clamp01(float x)=>Math.Max(0,Math.Min(1,x));public static float MoveTowards(float a,float b,float max)=>Math.Abs(a-b)<=max?b:a+Math.Sign(b-a)*max;}
}
namespace Emberfall {public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}public enum ChapterNode{ForestCourt,Redrock,StarPlatform}public enum ExpeditionModeKind{HoldPoint,TimedBreakthrough,BossGauntlet}public class Enemy{public bool IsDead,IsBoss;public GameObject gameObject=new GameObject();}public class ModeState{public ExpeditionModeKind Mode;}public class GameSession{public static GameSession Instance;public bool HasStarted,InDungeon,InCombat,IsInCamp,ChapterActive;public int CurrentHub;public ChapterNode ActiveChapterNode;public object RoomChainRun;public ModeState ModeRun;public List<Enemy> Enemies=new List<Enemy>();}}
class Program {
static int assertions;static BindingFlags SI=BindingFlags.Static|BindingFlags.NonPublic,PI=BindingFlags.Instance|BindingFlags.NonPublic;
static void C(bool condition,string message){assertions++;if(!condition)throw new Exception(message);}
static T Read<T>(object owner,string name)=>(T)typeof(GameAudio).GetField(name,owner==null?SI:PI).GetValue(owner);
static void Invoke(object owner,string name)=>typeof(GameAudio).GetMethod(name,owner==null?SI:PI).Invoke(owner,null);
static string Hash(float[] x)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(x.AsSpan())));
static Dictionary<string,object> Measure(AudioClip clip) {
float[] x=clip.Data;double energy=x.Sum(v=>(double)v*v);int n=1;while(n<x.Length)n<<=1;Complex[] fft=new Complex[n];for(int i=0;i<x.Length;i++)fft[i]=new Complex(x[i],0);
for(int i=1,j=0;i<n;i++){int bit=n>>1;for(; (j&bit)!=0;bit>>=1)j^=bit;j^=bit;if(i<j){Complex t=fft[i];fft[i]=fft[j];fft[j]=t;}}
for(int length=2;length<=n;length<<=1){Complex step=Complex.FromPolarCoordinates(1,-2*Math.PI/length);for(int i=0;i<n;i+=length){Complex phase=Complex.One;for(int j=0;j<length/2;j++){Complex u=fft[i+j],v=fft[i+j+length/2]*phase;fft[i+j]=u+v;fft[i+j+length/2]=u-v;phase*=step;}}}
double spectral=0,weighted=0,low=0,high=0,dominantEnergy=0,dominantHz=0;for(int i=1;i<n/2;i++){double freq=i*clip.frequency/(double)n,p=fft[i].Magnitude*fft[i].Magnitude;spectral+=p;weighted+=freq*p;if(freq<400)low+=p;if(freq>1500)high+=p;if(p>dominantEnergy){dominantEnergy=p;dominantHz=freq;}}
int first=(int)(.045*clip.frequency),tail=(int)(.12*clip.frequency);double early=x.Take(first).Sum(v=>(double)v*v)/first,late=x.Skip(tail).Sum(v=>(double)v*v)/(x.Length-tail);double dc=x.Average(v=>(double)v),peak=x.Max(v=>Math.Abs(v));
return new Dictionary<string,object>{{"clip",clip.name},{"seconds",x.Length/(double)clip.frequency},{"sampleRate",clip.frequency},{"peak",peak},{"rms",Math.Sqrt(energy/x.Length)},{"dc",dc},{"firstSample",x[0]},{"lastSample",x[x.Length-1]},{"spectralCentroidHz",weighted/spectral},{"dominantFrequencyHz",dominantHz},{"energyBelow400Hz",low/spectral},{"energyAbove1500Hz",high/spectral},{"first45msMeanEnergy",early},{"after120msMeanEnergy",late},{"sha256",Hash(x)}};
}
static double M(Dictionary<string,object> m,string key)=>Convert.ToDouble(m[key]);
static void Wav(string path,AudioClip clip){using var file=new BinaryWriter(File.Create(path));file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));file.Write(36+clip.samples*2);file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));file.Write(16);file.Write((short)1);file.Write((short)1);file.Write(clip.frequency);file.Write(clip.frequency*2);file.Write((short)2);file.Write((short)16);file.Write(System.Text.Encoding.ASCII.GetBytes("data"));file.Write(clip.samples*2);foreach(float sample in clip.Data)file.Write((short)Math.Round(sample*32767));}
static void Main(string[] args) {
string output=args[0];Directory.CreateDirectory(output);Invoke(null,"ResetStatics");
GameAudio.PlayBasicAttack((HeroClass)(-1));GameAudio.PlayBasicAttack((HeroClass)4);C(Read<GameAudio>(null,"instance")==null,"invalid class cannot instantiate audio");
Application.isPlaying=false;GameAudio.PlayBasicAttack(HeroClass.Arcanist);C(Read<GameAudio>(null,"instance")==null,"outside PlayMode cannot instantiate audio");Application.isPlaying=true;
GameAudio.Muted=true;GameAudio.PlayBasicAttack(HeroClass.Arcanist);C(Read<GameAudio>(null,"instance")==null,"mute blocks first basic attack");GameAudio.Muted=false;
GameAudio owner=Read<GameAudio>(null,"instance");var voices=Read<AudioSource[]>(owner,"voices");C(voices.Length==8,"bounded eight-voice pool");GameAudio.MasterVolume=.35f;
var metrics=new List<Dictionary<string,object>>();var hashes=new HashSet<string>();double[] durations={.20,.24,.15,.28};
for(int h=0;h<4;h++) {
GameAudio.Muted=true;GameAudio.Muted=false;Time.unscaledTime+=1;GameAudio.PlayBasicAttack((HeroClass)h);var clip=Read<AudioClip[]>(owner,"basicAttackClips")[h];C(clip!=null,"class cache owns PCM");C(clip.channels==1&&clip.frequency==22050,"mono native-rate PCM");C(Math.Abs(clip.samples/22050.0-durations[h])<.0001,"class-specific short duration");C(clip.Data.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)),"finite PCM");C(hashes.Add(Hash(clip.Data)),"each class PCM differs");var m=Measure(clip);metrics.Add(m);C(M(m,"peak")>.69&&M(m,"peak")<.701,"normalized finite headroom");C(M(m,"rms")>.075&&M(m,"rms")<.35,"audible RMS without excess gain");C(Math.Abs(M(m,"dc"))<.005,"no significant DC offset");C(Math.Abs(M(m,"firstSample"))<.0001&&Math.Abs(M(m,"lastSample"))<.002,"ramped click-free boundaries");C(M(m,"after120msMeanEnergy")<M(m,"first45msMeanEnergy"),"short one-shot decays");C(voices.Take(6).Count(v=>v.isPlaying&&v.clip==clip)==1,"ordinary attack voice carries class PCM");C(voices.Skip(6).All(v=>!v.isPlaying),"basic attack preserves critical reserved voices");C(voices.Where(v=>v.isPlaying).All(v=>Math.Abs(v.volume-.175)<.00001&&!v.loop&&v.spatialBlend==0&&!v.mute),"original gain and 2D one-shot policy");Wav(Path.Combine(output,((HeroClass)h)+"-basic-attack.wav"),clip);
int created=AudioClip.Created,plays=voices.Sum(v=>v.PlayCount);Time.unscaledTime+=.03f;GameAudio.PlayBasicAttack((HeroClass)h);C(voices.Sum(v=>v.PlayCount)==plays,"attack minimum interval rejects duplicate");Time.unscaledTime+=.1f;GameAudio.PlayBasicAttack((HeroClass)h);C(voices.Sum(v=>v.PlayCount)==plays+1,"attack accepts after throttle interval");C(AudioClip.Created==created,"repeated attack uses cached clip");
}
C(Read<AudioClip[]>(owner,"clips")[(int)SoundCue.Attack]==null,"custom attacks do not generate redundant generic PCM");
C(M(metrics[0],"energyAbove1500Hz")>.18,"steel edge has bright blade scrape energy");C(M(metrics[1],"energyBelow400Hz")>.25&&M(metrics[1],"energyAbove1500Hz")>.01,"frost core has body and brittle crystal detail");C(M(metrics[2],"dominantFrequencyHz")>650&&M(metrics[2],"dominantFrequencyHz")<790,"bowstring has short twang resonance");C(M(metrics[2],"first45msMeanEnergy")>M(metrics[2],"after120msMeanEnergy")*25,"bowstring snap decays before arrow tail ends");C(M(metrics[3],"energyBelow400Hz")>.80,"spirit pulse emphasizes soft low body");C(M(metrics[0],"spectralCentroidHz")>M(metrics[3],"spectralCentroidHz")*3,"steel and spirit have clear spectral separation");
GameAudio.Muted=true;GameAudio.Muted=false;for(int i=0;i<6;i++){Time.unscaledTime+=.1f;GameAudio.PlayBasicAttack((HeroClass)(i%4));}C(voices.Take(6).All(v=>v.isPlaying),"ordinary attack pool can fill six voices");int before=voices.Sum(v=>v.PlayCount);Time.unscaledTime+=.1f;GameAudio.PlayBasicAttack(HeroClass.Vanguard);C(voices.Sum(v=>v.PlayCount)==before,"saturated basic attack cannot steal another ordinary voice");GameAudio.PlaySkill(HeroClass.Arcanist,0);C(voices.Skip(6).Any(v=>v.isPlaying),"skills retain critical voice budget");
GameAudio.SetBackgroundPaused(true);C(voices.All(v=>!v.isPlaying),"background suspension stops basic attacks");before=voices.Sum(v=>v.PlayCount);Time.unscaledTime+=1;GameAudio.PlayBasicAttack(HeroClass.Summoner);C(voices.Sum(v=>v.PlayCount)==before,"background state blocks basic attack");GameAudio.SetBackgroundPaused(false);GameAudio.PlayBasicAttack(HeroClass.Summoner);C(voices.Sum(v=>v.PlayCount)==before+1,"resume resets attack throttle");GameAudio.Muted=true;C(voices.All(v=>!v.isPlaying),"mute stops basic attack");before=voices.Sum(v=>v.PlayCount);GameAudio.PlayBasicAttack(HeroClass.Vanguard);C(voices.Sum(v=>v.PlayCount)==before,"mute blocks cached attack");GameAudio.Muted=false;GameAudio.PlayBasicAttack(HeroClass.Vanguard);C(voices.Sum(v=>v.PlayCount)==before+1,"unmute restores basic attack immediately");
Invoke(owner,"OnDisable");C(Read<AudioClip[]>(owner,"basicAttackClips").All(v=>v==null),"class PCM released with audio owner");C(voices.All(v=>v.clip==null&&!v.isPlaying),"released voice retains no clip reference");
var result=new{status="PASS",assertions,scope="Actual production GameAudio code through recording Unity facade; generated PCM, spectral and envelope measurements, cache, gain, voice budget and lifecycle. No native DSP or listening acceptance.",metrics};File.WriteAllText(Path.Combine(output,"pcm-and-policy-report.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("PASS "+assertions+" production PCM and playback assertions");
}
}
'''

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,default=ROOT/'ArtSource/Review/NaturalSkills-20261011/Validation/BasicAttackAudio')
    parser.add_argument('--negative-controls',action='store_true')
    args=parser.parse_args();args.output.mkdir(parents=True,exist_ok=True)
    source=ROOT/'Assets/Scripts/Core/GameAudio.cs'
    production=source.read_text()
    cases=[('production',production,True)]
    if args.negative_controls:
        cases.extend([
            ('class-alias',production.replace('SynthesizeBasicAttack(hero);','SynthesizeBasicAttack(HeroClass.Vanguard);'),False),
            ('silent-pcm',production.replace('samples[i] *= .7f / peak;','samples[i] *= 0f / peak;'),False),
            ('reserved-voice-theft',production.replace('instance.PlayInternal((int)SoundCue.Attack, instance.basicAttackClips[index]);','instance.PlayInternal((int)SoundCue.Cast, instance.basicAttackClips[index]);'),False),
        ])
    results=[]
    with tempfile.TemporaryDirectory(prefix='emberfall-basic-attack-audio-') as temp:
        folder=Path(temp);env=dict(os.environ,DOTNET_CLI_HOME=str(folder/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
        for name,code,expected in cases:
            fixture=folder/(name+'.cs');fixture.write_text(code)
            output=args.output if expected else folder/name
            project=cv.write_project(folder/(name+'-project'),[fixture,ROOT/'Assets/Scripts/Core/AudioVoicePolicy.cs',ROOT/'Assets/Scripts/Core/AudioLifecycleGate.cs'],PROGRAM)
            run=subprocess.run([SDK,'run','--project',str(project),'--',str(output)],text=True,capture_output=True,env=env)
            (args.output/(name+'.log')).write_text(run.stdout+run.stderr)
            succeeded=run.returncode==0
            if succeeded!=expected:raise SystemExit(name+' unexpected result\n'+run.stdout+run.stderr)
            results.append({'case':name,'expectedSuccess':expected,'actualExitCode':run.returncode,'status':'PASS'})
            print(name, 'PASS',run.stdout.strip().split('\n')[-1] if succeeded else 'regression rejected')
    sources={str(path.relative_to(ROOT)):hashlib.sha256(path.read_bytes()).hexdigest() for path in [source,ROOT/'Assets/Scripts/Core/AudioVoicePolicy.cs',ROOT/'Assets/Scripts/Core/AudioLifecycleGate.cs',Path(__file__)]}
    (args.output/'result.json').write_text(json.dumps({'status':'PASS','project':str(ROOT),'cases':results,'sourceSha256':sources,'nativePlayback':'not run'},indent=2)+'\n')

if __name__=='__main__':main()
