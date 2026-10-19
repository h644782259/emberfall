using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class SkillVoiceValidation
    {
        public static void Run()
        {
            const string key="Emberfall.SkillVoiceVersion";
            bool existed=PlayerPrefs.HasKey(key);
            int saved=PlayerPrefs.GetInt(key,0);
            try
            {
                PlayerPrefs.DeleteKey(key);
                Require(GameAudio.SkillVoiceVersion==0,"Default must preserve original chants");
                var load=typeof(GameAudio).GetMethod("LoadChant",BindingFlags.Static|BindingFlags.NonPublic);
                string[] folders={"","Cantonese/","English/","Mandarin/"};
                for(int version=0;version<4;version++)
                {
                    GameAudio.SkillVoiceVersion=version;
                    Require(PlayerPrefs.GetInt(key,0)==version,"Selection persisted");
                    for(int hero=0;hero<4;hero++)for(int skill=0;skill<10;skill++)
                    {
                        if(skill==3||skill==8)continue;
                        string path="Audio/Chants/"+folders[version]+hero+"-"+skill;
                        var expected=Resources.Load<AudioClip>(path);
                        Require(expected!=null&&expected.samples>4000,"Missing or empty clip: "+path);
                        Require((AudioClip)load.Invoke(null,new object[]{hero,skill})==expected,"Wrong language: "+path);
                    }
                }
                GameAudio.SkillVoiceVersion=99;
                Require(GameAudio.SkillVoiceVersion==3,"Invalid setting must be ignored");
                PlayerPrefs.SetInt(key,-1);
                Require(GameAudio.SkillVoiceVersion==0,"Corrupt preference must use original");
                Debug.Log("PASS: 128 skill chants, four language selections, persistence and invalid preference fallback.");
            }
            finally
            {
                if(existed)PlayerPrefs.SetInt(key,saved);else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
        private static void Require(bool valid,string message)
        {if(!valid)throw new InvalidOperationException(message);}
    }
}
