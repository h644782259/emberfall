using System;
using Emberfall;
public static class CostumeLayersTests
{
    public static string Run()
    {
        int n=0;Action<bool,string> check=(ok,msg)=>{n++;if(!ok)throw new Exception(msg);};
        int[] ranks={int.MinValue,-1,0,2,3,6,7,9,10,int.MaxValue};
        int[] stages={0,0,0,0,1,1,2,2,3,3};
        for(int i=0;i<ranks.Length;i++)check(CostumeLayers.UpgradeStage(ranks[i])==stages[i],"exact +3/+7/+10 silhouette milestones");
        foreach(string name in new[]{"Cuirass","Pauldrons","Layered Robe","Long robe front panel","Leather Vest","Single leather shoulder","Leaf ritual mantle","Totem moonstone"})check(CostumeLayers.IsBaseOuter(name),"competing outer piece hidden when equipped");
        foreach(string name in new[]{null,"","Breastplate","Spine","Neck","Head","Cape","Staff","Arrow rest","Equipped class costume","Forest Hood"})check(!CostumeLayers.IsBaseOuter(name),"identity, underlying body, rig and equipped pieces retained");
        return "PASS: "+n+" costume layer/milestone policy checks (no rendered-frame validation)";
    }
}
