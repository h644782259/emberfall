using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Emberfall;
using UnityEngine;
public static class SideEventRewardTests
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    public static string Run(string root)
    {
        checks=0;var p=new ProgressionService(Path.Combine(root,"side-reward-"+Guid.NewGuid().ToString("N")));
        Check(p.CreateNewSlot(HeroClass.Vanguard),"create character");int events=0;p.Changed+=()=>events++;
        string a=Guid.NewGuid().ToString("N"),b=Guid.NewGuid().ToString("N"),before=JsonUtility.ToJson(p.Profile,true),disk=File.ReadAllText(p.SaveFilePath);bool fresh;
        Directory.CreateDirectory(p.SaveFilePath+".tmp");
        Check(!p.TryGrantSideEventReward(a,out fresh)&&!fresh&&events==0&&JsonUtility.ToJson(p.Profile,true)==before,"failed save publishes neither receipt, material nor event/supply gate");
        Check(File.ReadAllText(p.SaveFilePath)==disk,"failed save leaves durable state");Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.TryGrantSideEventReward(a,out fresh)&&fresh&&p.Profile.mechanicMaterials==1&&events==1,"first commit adds exactly one shard and opens supply gate");
        disk=File.ReadAllText(p.SaveFilePath);string backup=File.ReadAllText(p.SaveFilePath+".bak");
        Check(p.TryGrantSideEventReward(a.ToUpperInvariant(),out fresh)&&!fresh&&events==1&&p.Profile.mechanicMaterials==1,"case-normalized duplicate pays nothing and closes supply gate");
        Check(File.ReadAllText(p.SaveFilePath)==disk&&File.ReadAllText(p.SaveFilePath+".bak")==backup,"duplicate rotates no durable files");
        Check(p.TryGrantSideEventReward(b,out fresh)&&fresh&&p.TryGrantSideEventReward(a,out fresh)&&!fresh&&p.Profile.mechanicMaterials==2,"A B A interleaving deduplicates");
        var reload=new ProgressionService(p.SaveDirectory);Check(reload.LoadSlot(p.CurrentSlotId),"reload character");
        Check(reload.TryGrantSideEventReward(a,out fresh)&&!fresh&&reload.Profile.mechanicMaterials==2,"restart cannot repay retained receipt");
        string mode=Guid.NewGuid().ToString("N");Check(reload.TryGrantModeReward(mode,10,10,3,1),"main reward interleaves independently");int materials=reload.Profile.mechanicMaterials;
        Check(reload.TryGrantSideEventReward(b,out fresh)&&!fresh&&reload.Profile.mechanicMaterials==materials,"main reward never erases side receipt");
        foreach(string invalid in new[]{null,"","arbitrary",Guid.NewGuid().ToString("D")})
            Check(!reload.TryGrantSideEventReward(invalid,out fresh)&&!fresh&&reload.Profile.mechanicMaterials==materials,"invalid receipt rejected without supply gate");
        var latest=new List<string>();for(int i=0;i<40;i++){string id=Guid.NewGuid().ToString("N");latest.Add(id);Check(reload.TryGrantSideEventReward(id,out fresh)&&fresh,"commit bounded journal entry");}
        Check(reload.Profile.sideEventRewardReceipts.Count==32&&reload.Profile.sideEventRewardReceipts[0]==latest[8],"fixed most-recent32 bound");
        Check(reload.TryGrantSideEventReward(latest[8],out fresh)&&!fresh,"oldest retained entry still rejects replay");
        var json=JsonNode.Parse(File.ReadAllText(reload.SaveFilePath));var raw=new JsonArray();foreach(string id in latest)raw.Add(id.ToUpperInvariant());raw.Add("invalid");raw.Add(latest[39]);json["profile"]["sideEventRewardReceipts"]=raw;File.WriteAllText(reload.SaveFilePath,json.ToJsonString());disk=File.ReadAllText(reload.SaveFilePath);
        Check(reload.LoadSlot(reload.CurrentSlotId)&&reload.Profile.sideEventRewardReceipts.Count==32&&reload.Profile.sideEventRewardReceipts[31]==latest[39],"load normalizes duplicate case and caps journal");
        Check(File.ReadAllText(reload.SaveFilePath)==disk,"load normalization does not write migration");
        json["profile"]["sideEventRewardReceipts"]=null;File.WriteAllText(reload.SaveFilePath,json.ToJsonString());Check(reload.LoadSlot(reload.CurrentSlotId)&&reload.Profile.sideEventRewardReceipts.Count==0,"legacy/null journal becomes empty");
        return "PASS: "+checks+" side reward transaction/restart/bounded receipt checks (recent32 only; host must reject stale epochs)";
    }
}
