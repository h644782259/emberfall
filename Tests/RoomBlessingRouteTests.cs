using System;
using System.Collections.Generic;
using Emberfall;
public static class RoomBlessingRouteTests
{
    public static string Run()
    {
        int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
        for(int seed=0;seed<150;seed++)for(int hero=0;hero<4;hero++)foreach(bool mobile in new[]{false,true})
        {
            var p=new GameProfile{heroClass=(HeroClass)hero,specialization=(ElementalistSpecialization)(seed%3),summonerRoute=(SummonerRoute)(seed%2),equippedSkills=new[]{0,2,5}};
            for(int i=0;i<10;i++)p.skillRanks[i]=(seed+i)%4;
            int[] usable=RunChoices.UsableRanks(p,mobile);
            for(int i=0;i<10;i++)check((usable[i]>0)==(p.skillRanks[i]>0&&!GameBalance.IsPassive(i)&&(mobile||Array.IndexOf(p.equippedSkills,i)>=0)),"only actually reachable skills recommend mechanics");
            var a=new RunChoices();var b=new RunChoices();
            a.PrepareRoomChoice(2,p,mobile,seed);check(!a.AwaitingChoice,"rest cannot skip first checkpoint");
            for(int stage=1;stage<=2;stage++)
            {
                a.PrepareRoomChoice(stage,p,mobile,seed);b.PrepareRoomChoice(stage,p,mobile,seed);
                var cards=a.Offer;check(cards.Length==3&&new HashSet<RunBlessing>(cards).Count==3,"three distinct offers");
                check(string.Join(",",cards)==string.Join(",",b.Offer),"seed/config/path deterministic");
                int usableCount=0;bool bossEffective=false;
                foreach(var card in cards)
                {
                    check(!a.Has(card)&&RunChoices.IsCompatible(card,p.heroClass,usable),"eligible and unowned");
                    check(!string.IsNullOrEmpty(RunChoices.Association(card,p,mobile)),"short actionable association");
                    if(RunChoices.IsCompatible(card,p.heroClass,usable))usableCount++;
                    bossEffective|=RunChoices.RoomBossEffective(card);
                }
                check(usableCount>=2,"at least two immediately usable offers");
                check(RunChoices.RoomResource(cards[1])||RunChoices.RoomSurvival(cards[1]),"second slot patches resources or survival");
                if(stage==2)check(bossEffective,"rest offers at least one option effective on boss itself");
                a.PrepareRoomChoice(stage,p,!mobile,seed+77);check(string.Join(",",cards)==string.Join(",",a.Offer),"pending choice cannot reroll");
                check(!a.Choose(-1)&&a.AwaitingChoice,"invalid confirm preserves modal");
                check(a.Choose(seed%3)&&b.Choose(seed%3)&&!a.Choose(0),"choice exactly once");
                a.PrepareRoomChoice(stage,p,mobile,seed);check(!a.AwaitingChoice,"completed checkpoint cannot repeat");
            }
            int count=0;foreach(var item in a.Active)count++;check(count==2,"exactly two run blessings");
            a.PrepareRoomChoice(3,p,mobile,seed);check(!a.AwaitingChoice,"no third choice");
            a.Reset();a.PrepareRoomChoice(1,p,mobile,seed);check(a.AwaitingChoice,"fresh run grants first checkpoint");
            for(int route=0;route<2;route++)
            {var info=CampRouteCards.Describe(p,mobile,route);check(info.Name.Length>0&&info.Loop.Length>0&&info.Requirements.Length>0,"all four classes have two concrete route cards");}
        }
        var ranger=new GameProfile{heroClass=HeroClass.Ranger,equippedSkills=new[]{0}};ranger.skillRanks[7]=1;ranger.skillRanks[4]=1;
        check(CampRouteCards.Describe(ranger,false,1).Requirements.Contains("装入"),"desktop learned but unequipped requires equip");
        check(CampRouteCards.Describe(ranger,true,1).Ready,"mobile learned fixed buttons need no desktop equip");
        var vanguard=new GameProfile{heroClass=HeroClass.Vanguard};
        check(CampRouteCards.Describe(vanguard,true,0).Ready&&CampRouteCards.Describe(vanguard,true,0).Stage==CampRouteStage.BasicReady&&CampRouteCards.Describe(vanguard,true,0).Enhancement.Contains("回刃长剑"),"base route works while optional enhancement remains visible");
        vanguard.inventory.Add(new ItemData{id="sword",slot=ItemSlot.Weapon,mechanic=EquipmentMechanic.ReturningBlade});vanguard.weaponId="sword";
        check(CampRouteCards.Describe(vanguard,true,0).Ready,"equipped route mechanism recognized");
        var seen=new HashSet<RunBlessing>();
        var secondSeen=new Dictionary<RunBlessing,HashSet<RunBlessing>>();
        var firstSeen=new HashSet<RunBlessing>();
        var witnesses=new Dictionary<RunBlessing,string>();
        for(int hero=0;hero<4;hero++)foreach(bool mobile in new[]{false,true})for(int seed=0;seed<512;seed++)
        {
            // Legal level-50 all-first-rank build: eight active buttons equipped,
            // ordinary class choices only; no impossible mechanic prerequisites.
            var p=new GameProfile{heroClass=(HeroClass)hero,level=50,equippedSkills=new[]{0,1,2,4,5,6,7,9,-1,-1}};
            for(int skill=0;skill<10;skill++)p.skillRanks[skill]=1;
            var opening=new RunChoices();opening.PrepareRoomChoice(1,p,mobile,seed);
            var cards=opening.Offer;
            check(RunChoices.RoomStyle(cards[0]),"opening first slot offers a play-style option");
            foreach(var card in cards)seen.Add(card);
            for(int selected=0;selected<3;selected++)
            {
                var run=new RunChoices();run.PrepareRoomChoice(1,p,mobile,seed);
                var first=run.Offer[selected];check(run.Choose(selected),"first blessing obtained through normal offer path");firstSeen.Add(first);
                if(!witnesses.ContainsKey(first))witnesses.Add(first,"hero="+p.heroClass+", mobile="+mobile+", seed="+seed+", slot="+selected);
                run.PrepareRoomChoice(2,p,mobile,seed);var rest=run.Offer;
                int related=0;foreach(var item in rest)if(RunChoices.RoomAssociated(first,item))related++;
                check(related==1&&RunChoices.RoomAssociated(first,rest[0]),"exactly one rest slot strongly reinforces selected first blessing");
                check(!Array.Exists(rest,item=>item==first),"selected blessing cannot appear again");
                check(Array.Exists(rest,RunChoices.RoomBossEffective),"rest retains boss-body value independently of guard kills");
                if(!secondSeen.ContainsKey(first))secondSeen.Add(first,new HashSet<RunBlessing>());
                secondSeen[first].Add(rest[2]);
            }
        }
        foreach(RunBlessing item in Enum.GetValues(typeof(RunBlessing)))
        {
            check(seen.Contains(item)&&firstSeen.Contains(item),"every public blessing has a normal legal first-choice witness: "+item);
            check(secondSeen[item].Count>=4,"open rest slot retains distribution breadth after "+item);
            Console.WriteLine("WITNESS "+item+": "+witnesses[item]+"; rest-open distinct="+secondSeen[item].Count);
        }
        check(RunChoices.Association(RunBlessing.MarkedPursuit,ranger,true).Contains("护卫")&&
            RunChoices.Association(RunBlessing.ExecutionMend,ranger,true).Contains("护卫"),"kill cards explain boss-guard use");
        check(RunChoices.Description(RunBlessing.KeenSight).Contains("持续伤害与伙伴不继承")&&
            RunChoices.Description(RunBlessing.DeadlyEdge).Contains("持续伤害与伙伴不继承"),"crit descriptions exclude DOT and companions");
        return "PASS: "+checks+" room blessing/route assertions (no Unity execution)";
    }
}
