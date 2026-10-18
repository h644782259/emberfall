"""Production audio playback and forge success / paid-failure regression."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
source=(root/'Assets/Scripts/Core/GameAudio.cs').read_text()
def member(key):
 a=source.index(key);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b].replace('private void PlayInternal','public void PlayInternal')
def declaration(key):
 a=source.index(key);return source[a:source.index(';',a)+1]
fields='\n'.join(declaration(k) for k in ['private const int CueCount','private const int VoiceCount','private const int SampleRate','private const double Tau','private const float DefaultVolume','private static readonly float[] Durations','private static readonly float[] MinimumIntervals'])
methods='\n'.join(member(k) for k in ['private void PlayInternal(','private static AudioClip Synthesize(','private static double ForgeStrike(','private static double Bell('])
program=r'''using System;using System.IO;using System.Linq;using System.Reflection;using Emberfall;
class AudioClip{public float[] Data;public object hideFlags;public static AudioClip Create(string n,int l,int c,int r,bool stream)=>new AudioClip();public bool SetData(float[] data,int offset){Data=data;return true;}}
static class HideFlags{public static object DontSave;}
static class Debug{public static void LogWarning(object s){}public static void LogError(string s){throw new Exception(s);}}
static class Time{public static float unscaledTime;}
class AudioSource{public bool isPlaying,mute,enabled;public float volume;public AudioClip clip;public static int Plays;public void Stop(){isPlaying=false;}public void Play(){isPlaying=true;Plays++;}}
class AudioProbe{
FIELDS
METHODS
float[] lastPlayed;AudioClip[] clips;AudioSource[] voices;float lastImpact=-999,masterVolume=1;int nextVoice;
void EnsureListener(){}void EnsurePool(){if(voices!=null)return;voices=Enumerable.Range(0,VoiceCount).Select(_=>new AudioSource()).ToArray();clips=new AudioClip[CueCount];lastPlayed=Enumerable.Repeat(float.NegativeInfinity,CueCount).ToArray();}
public static bool Complete()=>Durations.Length==CueCount&&MinimumIntervals.Length==CueCount&&Enum.GetValues(typeof(SoundCue)).Cast<SoundCue>().All(c=>(int)c>=0&&(int)c<CueCount);
public AudioClip Clip(SoundCue c)=>clips[(int)c];
}
class Program{
static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}
static void Main(){
C(AudioProbe.Complete(),"every cue has duration and throttle entry");var audio=new AudioProbe();
foreach(SoundCue cue in Enum.GetValues(typeof(SoundCue))){Time.unscaledTime+=2;audio.PlayInternal((int)cue);}
C(AudioSource.Plays>0,"real playback path reached");
var p=new ProgressionService(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Vanguard),"create");p.Profile.level=100;p.Profile.gold=100000;p.Save();
foreach(int roll in new[]{0,99}){
var item=p.Equipped(ItemSlot.Weapon);int rank=p.SlotUpgradeRank(ItemSlot.Weapon),gold=p.Profile.gold,price=p.UpgradeCost(item),attempts=p.Profile.equipmentUpgradeAttempts;var q=p.PrepareSmithUpgrade(ItemSlot.Weapon,true);
bool success=(bool)typeof(ProgressionService).GetMethod("ApplyUpgradeAttempt",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,new object[]{item.id,roll});bool paid=p.Profile.equipmentUpgradeAttempts>attempts;
C(success==(roll==0)&&paid,"success and paid failure");
SoundCue cue=success?SoundCue.ForgeSuccess:paid?SoundCue.ForgeFailure:SoundCue.UI;
Time.unscaledTime+=2;int plays=AudioSource.Plays;audio.PlayInternal((int)cue);C(AudioSource.Plays==plays+1,"feedback plays without bounds exception");
audio.PlayInternal((int)cue);C(AudioSource.Plays==plays+1,"duplicate sound throttled");
C(p.Profile.gold==gold-price&&p.Profile.equipmentUpgradeAttempts==attempts+1&&p.SlotUpgradeRank(ItemSlot.Weapon)==rank+(success?1:0),"transaction committed exactly once");
C(!p.UpgradeAtSmith(q,true)&&p.Profile.gold==gold-price,"stale retry cannot spend again");
C(p.LoadSlot(p.CurrentSlotId)&&p.Profile.gold==gold-price&&p.SlotUpgradeRank(ItemSlot.Weapon)==rank+(success?1:0),"result survives reload");}
var a=audio.Clip(SoundCue.ForgeSuccess).Data;var b=audio.Clip(SoundCue.ForgeFailure).Data;
C(a.All(float.IsFinite)&&b.All(float.IsFinite)&&a.Any(v=>Math.Abs(v)>.1)&&b.Any(v=>Math.Abs(v)>.1),"both synthesized clips audible finite PCM");C(!a.SequenceEqual(b),"success and failure sound different");
Directory.Delete(p.SaveDirectory,true);Console.WriteLine("PASS "+n+" forge audio and saved transaction assertions (stub audio device)");}}
'''.replace('FIELDS',fields).replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 sources += [root/'Assets/Scripts/Core/AudioVoicePolicy.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
