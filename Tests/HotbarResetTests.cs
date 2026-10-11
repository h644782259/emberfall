using System;
using System.IO;
using System.Linq;
using Emberfall;
using UnityEngine;

public static class HotbarResetTests
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    public static string Run(string directory)
    {
        checks=0;
        foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        {
            var p=new ProgressionService(Path.Combine(directory,"binding-reset-"+Guid.NewGuid().ToString("N")));
            Check(p.CreateNewSlot(hero),"isolated character created");
            p.Profile.hotbarKeys=GameBalance.DefaultHotbarKeys.Reverse().ToArray();
            p.Profile.hotbarPage=2;p.Save();
            Check(p.SaveBuildPreset(0,true),"custom mapping captured in preset");
            string before=JsonUtility.ToJson(p.Profile,true),disk=File.ReadAllText(p.SaveFilePath),backup=File.ReadAllText(p.SaveFilePath+".bak");
            var original=p.Profile;int events=0;p.Changed+=()=>events++;
            Directory.CreateDirectory(p.SaveFilePath+".tmp");
            Check(!p.ResetHotbarKeys(),"blocked write rejects complete reset");
            Check(ReferenceEquals(original,p.Profile)&&JsonUtility.ToJson(p.Profile,true)==before&&events==0,"failed reset publishes no partial mapping");
            Check(File.ReadAllText(p.SaveFilePath)==disk&&File.ReadAllText(p.SaveFilePath+".bak")==backup,"failed reset preserves primary and backup");
            Directory.Delete(p.SaveFilePath+".tmp");
            // Simulate storage becoming unavailable immediately after the first
            // publication: the former UI loop failed on its second slot write.
            Action blockAfterPublish=()=>Directory.CreateDirectory(p.SaveFilePath+".tmp");
            p.Changed+=blockAfterPublish;
            Check(p.ResetHotbarKeys()&&events==1&&p.Profile.hotbarKeys.SequenceEqual(GameBalance.DefaultHotbarKeys),"one publication commits all ten keys before later storage failure");
            p.Changed-=blockAfterPublish;Directory.Delete(p.SaveFilePath+".tmp");
            Check(p.Profile.hotbarPage==2&&p.Profile.equippedSkills.SequenceEqual(original.equippedSkills)&&
                JsonUtility.ToJson(p.Profile.buildPresets[0],true)==JsonUtility.ToJson(original.buildPresets[0],true),"reset preserves skill pages and stored custom preset");
            Check(!ReferenceEquals(p.Profile.hotbarKeys,GameBalance.DefaultHotbarKeys),"live keys never alias global defaults");
            var loaded=new ProgressionService(p.SaveDirectory);
            Check(loaded.LoadSlot(p.CurrentSlotId)&&loaded.Profile.hotbarKeys.SequenceEqual(GameBalance.DefaultHotbarKeys),"restart loads complete mapping");
            disk=File.ReadAllText(p.SaveFilePath);backup=File.ReadAllText(p.SaveFilePath+".bak");
            Check(p.ResetHotbarKeys()&&File.ReadAllText(p.SaveFilePath)==disk&&File.ReadAllText(p.SaveFilePath+".bak")==backup,"repeated reset is disk-idempotent");
        }
        return "PASS: "+checks+" atomic ten-key reset assertions (production persistence, managed JSON transport)";
    }
}
