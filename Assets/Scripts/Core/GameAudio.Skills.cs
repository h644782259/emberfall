using System.Collections;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameAudio
    {
        private sealed class SkillBed
        {
            internal AudioSource Source;
            internal PlayerController Owner;
            internal Transform Anchor;
            internal System.Func<bool> Active;
            internal int Epoch, Family;
            internal float Age, Duration, Gain;
            internal bool Paused;
        }
        private static readonly string[] BedNames={"Wind","Void","Fire","Arrows","Roots","Spirit","Ice","Earth","Lightning"};
        private readonly AudioClip[] skillBeds=new AudioClip[9],chants=new AudioClip[40];
        private readonly SkillBed[] sustained=new SkillBed[4];
        private AudioSource chantSource;
        private PlayerController chantOwner;
        private int chantEpoch;
        private bool chantPaused;
        private float nextChant;
        private static readonly string[] ChantFolders = { "", "Cantonese/", "English/", "Mandarin/" };
        private static readonly string[] ChantLabels = { "原版（虚构咒语）", "粤语", "English", "普通话" };
        public static int SkillVoiceVersion
        {
            get { int value=PlayerPrefs.GetInt("Emberfall.SkillVoiceVersion",0);return value>=0&&value<4?value:0; }
            set
            {
                if(value<0||value>=4||value==SkillVoiceVersion)return;
                PlayerPrefs.SetInt("Emberfall.SkillVoiceVersion",value);PlayerPrefs.Save();
                if(instance==null)return;
                if(instance.chantSource!=null)instance.chantSource.Stop();
                instance.chantOwner=null;instance.chantPaused=false;instance.nextChant=0;
                for(int hero=0;hero<4;hero++)for(int skill=0;skill<10;skill++)
                    instance.chants[hero*10+skill]=skill==3||skill==8?null:LoadChant(hero,skill);
            }
        }
        public static string SkillVoiceVersionName { get { return ChantLabels[SkillVoiceVersion]; } }
        private static AudioClip LoadChant(int hero,int skill)
        {
            string name=hero+"-"+skill;
            var clip=Resources.Load<AudioClip>("Audio/Chants/"+ChantFolders[SkillVoiceVersion]+name);
            return clip!=null?clip:Resources.Load<AudioClip>("Audio/Chants/"+name);
        }

        public static IEnumerator PrewarmSkillAudio()
        {
            EnsureInstance();
            for(int i=0;i<BedNames.Length;i++)
            {if(instance.skillBeds[i]!=null)continue;instance.skillBeds[i]=SynthesizeSkillAccent(i);yield return null;}
            var game=GameSession.Instance;
            if(game==null||game.Player==null)yield break;
            for(int hero=0;hero<4;hero++)
            for(int skill=0;skill<10;skill++)
            {
                if(skill==3||skill==8)continue;
                int index=hero*10+skill;
                if(instance.chants[index]!=null&&instance.skillClips[index]!=null)continue;
                if(instance.chants[index]==null)instance.chants[index]=LoadChant(hero,skill);
                if(instance.skillClips[index]==null)instance.skillClips[index]=SynthesizeSkill((HeroClass)hero,skill);
                yield return null;
            }
        }
        private static AudioClip SynthesizeSkillAccent(int family)
        {
            // A brief material accent followed by silence, never a looping noise bed.
            var samples=new float[(int)(SampleRate*1.6)];
            double frequency=family==2?170:family==6?1450:family==8?620:280+family*83;
            for(int i=0;i<SampleRate*.24;i++)
            {
                double t=i/(double)SampleRate;
                double envelope=System.Math.Min(1,t/.008)*System.Math.Exp(-t*28)*System.Math.Max(0,1-t/.24);
                double phase=Tau*frequency*(t-.65*t*t);
                samples[i]=(float)((System.Math.Sin(phase)+.22*System.Math.Sin(phase*2.37))*envelope*.35);
            }
            var clip=AudioClip.Create("Short skill accent / "+BedNames[family],samples.Length,1,SampleRate,false);
            clip.hideFlags=HideFlags.DontSave;clip.SetData(samples,0);return clip;
        }
        internal static void SustainSkill(PlayerController owner,Transform anchor,SkillPerformanceBody kind,float duration,System.Func<bool> active=null)
        {
            if(owner==null||muted||quitting||lifecycle.BackgroundPaused)return;
            EnsureInstance();
            int family=kind==SkillPerformanceBody.Tornado?0:kind==SkillPerformanceBody.Gravity||kind==SkillPerformanceBody.GravityCollapse||kind==SkillPerformanceBody.Pressure?1:
                kind==SkillPerformanceBody.Fire||kind==SkillPerformanceBody.Meteor?2:kind==SkillPerformanceBody.ArrowVolley?3:kind==SkillPerformanceBody.Poison?4:
                kind==SkillPerformanceBody.Spirit?5:kind==SkillPerformanceBody.Frost?6:kind==SkillPerformanceBody.Fault?7:8;
            instance.BeginSkillBed(owner,anchor!=null?anchor:owner.transform,family,Mathf.Max(.8f,duration),active);
        }
        private void BeginSkillBed(PlayerController owner,Transform anchor,int family,float duration,System.Func<bool> active)
        {
            if(skillBeds[family]==null)return; // Assets load during scene preparation, never during a hit.
            SkillBed chosen=null;
            foreach(var bed in sustained)
                if(bed!=null&&bed.Owner==owner&&(bed.Anchor==anchor||family==6)&&bed.Family==family)
                {bed.Duration=Mathf.Max(bed.Duration,bed.Age+duration);bed.Anchor=anchor;bed.Active=active;bed.Epoch=owner.CombatEpoch;return;}
            for(int i=0;i<sustained.Length;i++)
            {
                if(sustained[i]==null)sustained[i]=new SkillBed{Source=NewSkillSource(true,120)};
                if(sustained[i].Owner==null){chosen=sustained[i];break;}
            }
            if(chosen==null)return; // Bounded layers: dense effects cannot overwhelm impacts or speech.
            chosen.Owner=owner;chosen.Anchor=anchor;chosen.Epoch=owner.CombatEpoch;chosen.Family=family;chosen.Active=active;
            chosen.Age=0;chosen.Duration=duration;chosen.Gain=0;chosen.Paused=false;
            chosen.Source.clip=skillBeds[family];chosen.Source.volume=0;chosen.Source.Play();
        }
        private AudioSource NewSkillSource(bool loop,int priority)
        {
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;
            source.spatialBlend=0;source.dopplerLevel=0;source.ignoreListenerPause=true;source.priority=priority;return source;
        }
        private void PlayChant(HeroClass hero,int skill)
        {
            var game=GameSession.Instance;
            if(game==null||game.Player==null||Time.unscaledTime<nextChant)return;
            var clip=chants[(int)hero*10+skill];if(clip==null)return;
            if(chantSource==null)chantSource=NewSkillSource(false,56);
            if(chantSource.isPlaying||chantPaused)return;
            chantOwner=game.Player;chantEpoch=chantOwner.CombatEpoch;chantPaused=false;
            chantSource.clip=clip;chantSource.pitch=1;chantSource.volume=.54f*masterVolume;chantSource.Play();
            nextChant=Time.unscaledTime+Mathf.Max(2.8f,clip.length+.6f);
        }
        private void TickSkillAudio()
        {
            var game=GameSession.Instance;
            bool ended=game==null||!game.HasStarted||game.CombatEffectsEnded||muted||quitting;
            bool paused=lifecycle.BackgroundPaused||game!=null&&(game.InputBlocked||Time.timeScale<=0);
            int layers=0;foreach(var bed in sustained)if(bed!=null&&bed.Owner!=null)layers++;
            foreach(var bed in sustained)
            {
                if(bed==null||bed.Source.clip==null)continue;
                bool valid=!ended&&bed.Owner!=null&&bed.Owner==game.Player&&!bed.Owner.IsDead&&bed.Owner.CombatEpoch==bed.Epoch;
                if(!valid){StopBed(bed);continue;}
                if(paused){if(!bed.Paused){bed.Source.Pause();bed.Paused=true;}continue;}
                if(bed.Paused){bed.Source.UnPause();bed.Paused=false;}
                if(bed.Anchor==null||!bed.Anchor.gameObject.activeInHierarchy||bed.Active!=null&&!bed.Active())bed.Duration=Mathf.Min(bed.Duration,bed.Age+.35f);
                bed.Age+=Time.deltaTime;
                if(bed.Age>=bed.Duration){StopBed(bed);continue;}
                float envelope=Mathf.Min(Mathf.Clamp01(bed.Age/.015f),Mathf.Clamp01((bed.Duration-bed.Age)/.2f));
                bed.Gain=.2f*envelope/Mathf.Sqrt(Mathf.Max(1,layers));
                bed.Source.volume=bed.Gain*masterVolume*(chantSource!=null&&chantSource.isPlaying?.65f:1);
            }
            if(chantSource==null)return;
            if(ended||chantOwner==null||chantOwner!=game.Player||chantOwner.IsDead||chantOwner.CombatEpoch!=chantEpoch)
            {chantSource.Stop();chantPaused=false;chantOwner=null;return;}
            chantSource.volume=.54f*masterVolume;
            if(paused&&!chantPaused){chantSource.Pause();chantPaused=true;}
            else if(!paused&&chantPaused){chantSource.UnPause();chantPaused=false;}
        }
        private static void StopBed(SkillBed bed)
        {bed.Source.Stop();bed.Source.clip=null;bed.Owner=null;bed.Anchor=null;bed.Active=null;bed.Paused=false;}
        internal static void InterruptSkillVoice(PlayerController owner)
        {
            if(instance==null)return;
            if(instance.chantOwner==owner)
            {if(instance.chantSource!=null)instance.chantSource.Stop();instance.chantOwner=null;instance.chantPaused=false;}
            foreach(var bed in instance.sustained)
                if(bed!=null&&bed.Owner==owner&&bed.Anchor==owner.transform)bed.Duration=Mathf.Min(bed.Duration,bed.Age+.12f);
        }
        private void StopSkillAudio()
        {
            foreach(var bed in sustained)if(bed!=null)StopBed(bed);
            if(chantSource!=null)chantSource.Stop();chantOwner=null;chantPaused=false;
        }
    }
}
