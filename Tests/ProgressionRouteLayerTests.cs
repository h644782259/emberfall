using System;
using System.IO;
using Emberfall;
public static class ProgressionRouteLayerTests
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    public static string Run(string root)
    {
        checks=0;
        foreach(bool mobile in new[]{false,true})
        {
            var warrior=new GameProfile{heroClass=HeroClass.Vanguard};var basic=CampRouteCards.Describe(warrior,mobile,0);
            Check(basic.Ready&&basic.Stage==CampRouteStage.BasicReady&&basic.Enhancement.Contains("回刃"),"counter loop works without optional ReturningBlade");
            var blade=new ItemData{id="blade",slot=ItemSlot.Weapon,mechanic=EquipmentMechanic.ReturningBlade,level=1};warrior.inventory.Add(blade);warrior.weaponId=blade.id;
            Check(CampRouteCards.Describe(warrior,mobile,0).Stage==CampRouteStage.EnhancedReady,"equipped valid mechanism enhances base loop");
            blade.slot=ItemSlot.Relic;Check(CampRouteCards.Describe(warrior,mobile,0).Stage==CampRouteStage.BasicReady,"wrong-slot item cannot grant enhanced status");blade.slot=ItemSlot.Weapon;blade.level=20;
            Check(CampRouteCards.Describe(warrior,mobile,0).Stage==CampRouteStage.BasicReady,"overlevel item cannot grant enhanced status");
            var arcanist=new GameProfile{heroClass=HeroClass.Arcanist,equippedSkills=new[]{0,1}};arcanist.skillRanks[0]=arcanist.skillRanks[1]=1;
            Check(CampRouteCards.Describe(arcanist,mobile,0).Stage==CampRouteStage.BasicReady,"balanced specialization already supports shatter without FrostEcho");
            arcanist.specialization=ElementalistSpecialization.Burn;
            Check(!CampRouteCards.Describe(arcanist,mobile,0).Ready,"burn specialization is a real basic shatter blocker");
            arcanist.skillRanks[5]=1;arcanist.equippedSkills=new[]{1,5};
            Check(CampRouteCards.Describe(arcanist,mobile,1).Stage==CampRouteStage.BasicReady,"burn base loop doesn't require CinderTrail ground trail");
            var ranger=new GameProfile{heroClass=HeroClass.Ranger,equippedSkills=new[]{0}};ranger.skillRanks[0]=1;
            Check(CampRouteCards.Describe(ranger,mobile,0).Stage==CampRouteStage.BasicReady,"three-poison detonation doesn't require VenomSpread");
            var summoner=new GameProfile{heroClass=HeroClass.Summoner,equippedSkills=new[]{2,4},summonerRoute=SummonerRoute.Bonded};summoner.skillRanks[2]=summoner.skillRanks[4]=1;
            Check(CampRouteCards.Describe(summoner,mobile,0).Stage==CampRouteStage.BasicReady,"bonded loop doesn't require TwinSummonResonance");
            summoner.skillRanks[4]=0;var missing=CampRouteCards.Describe(summoner,mobile,0);
            Check(missing.Stage==CampRouteStage.BasicMissing&&missing.NextStep=="学习"+GameBalance.SkillName(HeroClass.Summoner,4)&&missing.NextAction==CampRouteAction.Skill&&missing.NextSkill==4,"one truthful next step names the missing base skill");
        }
        foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(bool mobile in new[]{false,true})for(int route=0;route<2;route++)
        {
            var legal=new GameProfile{heroClass=hero,level=100,equippedSkills=new[]{0,1,2,4,5,6,7,9},specialization=route==0?ElementalistSpecialization.Shatter:ElementalistSpecialization.Burn,summonerRoute=(SummonerRoute)route};
            for(int skill=0;skill<GameBalance.SkillCount;skill++)legal.skillRanks[skill]=1;
            var card=CampRouteCards.Describe(legal,mobile,route);
            Check(card.Ready&&card.Stage!=CampRouteStage.BasicMissing,"every class/route/platform has a fully unlocked base-ready witness without mechanisms");
        }
        var desktop=new GameProfile{heroClass=HeroClass.Arcanist,equippedSkills=new[]{0}};desktop.skillRanks[0]=desktop.skillRanks[1]=1;
        Check(CampRouteCards.Describe(desktop,false,0).Stage==CampRouteStage.BasicMissing&&CampRouteCards.Describe(desktop,true,0).Stage==CampRouteStage.BasicReady,"desktop equipped pages versus mobile learned buttons retain separate usability");
        Check(!ProgressionHudHint.TutorialUsable(desktop,false,false)&&ProgressionHudHint.TutorialUsable(desktop,true,false),"tutorial only advertises usable learned skills on this platform");
        Check(ProgressionHudHint.TutorialUsable(new GameProfile{heroClass=HeroClass.Summoner},false,true),"starter companion supports command tutorial without learning summon or buying a mechanism");
        var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Arcanist),"create HUD fixture");
        string title,step;var attention=new ProgressionAttention{FirstClearClaimable=true};
        Check(!ProgressionHudHint.TryGet(p,attention,true,true,true,out title,out step),"room win blocker overrides camp actions and tutorials");
        Check(ProgressionHudHint.TryGet(p,attention,false,true,true,out title,out step)&&title=="领取首通核心","camp operation outranks fallback tutorial");
        Check(p.SelectCoreGoal(EquipmentMechanic.CinderTrail),"select HUD identity");
        Check(ProgressionHudHint.TryGet(p,null,false,false,true,out title,out step)&&title.Contains("余烬"),"self-selected goal outranks tutorial");
        Check(p.SelectProgressionGoal(ProgressionGoalKind.ClassTutorial)&&ProgressionHudHint.TryGet(p,null,false,false,false,out title,out step)&&step.Contains("先学习"),"unusable explicitly selected tutorial explains its prerequisite");
        Check(p.SelectProgressionGoal(ProgressionGoalKind.None)&&ProgressionHudHint.TryGet(p,null,false,false,true,out title,out step)&&title=="职业练习","automatic tutorial only appears without selected goal");
        p.Profile.classTutorialCompleted=true;Check(!ProgressionHudHint.TryGet(p,null,false,false,true,out title,out step),"completed tutorial doesn't replace exploration");
        return "PASS: "+checks+" route layer and shared HUD priority checks";
    }
}
