using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private static string StyledSkillNumbers(string value)
        {
            var lines=value.Split('\n');
            for(int i=0;i<lines.Length;i++)
            {
                bool range=lines[i].Contains("米")&&!lines[i].Contains("攻击");
                lines[i]="<color="+(range?"#8BCDDD":"#FFC777")+">"+lines[i]+"</color>";
            }
            return string.Join("\n",lines);
        }
        private string SkillNumbers(GameProfile profile,int skill,int rank)
        {
            rank=Mathf.Clamp(rank,1,3);var hero=profile.heroClass;float power=CombatBalance.RankPower(rank);
            System.Func<float,string> damage=c=>(c*100).ToString("0.#")+"%攻击 + "+SkillDamageBudgets.BaseDamage(profile.level,c).ToString("0.#");
            string text="";
            if(skill==3)return "";
            if(skill==8)
            {
                if(hero==HeroClass.Arcanist||hero==HeroClass.Summoner)return "反击 "+damage(.6f+.3f*rank)+"\n半径 "+(3f*GameBalance.SkillRangeMultiplier(rank)).ToString("0.#")+" 米";
                return "";
            }
            if(hero==HeroClass.Summoner)
            {
                if(skill==0)text="冲击 "+damage(SummonerDamageRules.ImpulseCoefficient*power);
                else if(skill==1)text="每 "+SummonerDamageRules.ThornInterval+" 秒 "+damage(SummonerDamageRules.ThornTickCoefficient*power);
                else if(skill==5)text="穿透灵矢 "+damage(2.6f*power);
                else if(skill==7)text="每 "+SummonerDamageRules.MarkInterval+" 秒 "+damage(SummonerDamageRules.MarkTickCoefficient*power)+"\n终结 "+damage(SummonerDamageRules.MarkFinisherCoefficient*power);
                else
                {
                    int form=skill==2?0:skill==4?1:skill==6?3:2;
                    text="召唤物每击 "+damage(CompanionRules.AttackCoefficient(form)*power*(profile.summonerRoute==SummonerRoute.Bonded?1.2f:.8f))+"\n攻击间隔 "+CompanionRules.AttackInterval(form).ToString("0.##")+" 秒";
                }
            }
            else if(skill<=1)
            {
                float initial=SkillDamageBudgets.OpeningImpact(hero,skill,rank);
                if(hero==HeroClass.Ranger&&skill==0&&BuildCatalog.ConcentratedVenomEquipped(profile))initial=BuildCatalog.ConcentratedVenomCoefficient(rank);
                if(hero==HeroClass.Arcanist&&skill==0)
                {
                    var spikes=SkillDamageBudgets.FrostSpikes(rank);
                    text="首击 "+damage(initial*.6f)+"\n冰刺每 "+spikes.Interval+" 秒 "+damage(spikes.TickCoefficient);
                }
                else text=(hero==HeroClass.Ranger&&skill==0?"每箭 ":"首击 ")+damage(initial);
                if(rank>=2&&!(hero==HeroClass.Ranger&&skill==0))text+="\n追加 "+damage(SkillDamageBudgets.OpeningImpact(hero,skill,rank,1));
            }
            else if(skill==2)
            {
                var field=SkillDamageBudgets.EarlyField(hero,rank);
                text="每 "+field.Interval+" 秒 "+damage(field.TickCoefficient)+"\n全部命中合计 "+damage(field.TotalCoefficient);
            }
            else if(hero==HeroClass.Vanguard&&skill==4)text="格挡反击 "+damage(1.2f*power);
            else if(hero==HeroClass.Vanguard&&skill==6)text="治疗自身最大生命 "+(rank==1?30:rank==2?42:55)+"% · 不造成伤害";
            else if(hero==HeroClass.Arcanist&&skill==5)text="霜环 "+damage(SkillDamageBudgets.FrostWalkerPulse(rank))+"\n火路每 0.5 秒 "+damage(SkillDamageBudgets.FlameRideTick);
            else if(hero==HeroClass.Ranger&&skill==4)text="跃射 "+damage(SkillDamageBudgets.RangerVault(rank));
            else if(skill==6)text=hero==HeroClass.Arcanist?"冰霜 "+damage(2*power)+"\n雷击 "+damage(2.5f*power):"每箭 "+damage(1.4f*power)+" · 3 箭";
            else
            {
                float scale=power*SkillDamageBudgets.AdvancedScale(hero,skill);
                if(hero==HeroClass.Ranger&&skill==5)
                {var field=SkillDamageBudgets.ToxicField(rank);text="每 "+field.Interval+" 秒 "+damage(field.TickCoefficient*scale)+"\n全部命中合计 "+damage(field.TotalCoefficient*scale);}
                else
                {
                    int steps=SkillDamageBudgets.AdvancedSteps(hero,skill,rank);
                    text=(steps>1?"首段 ":"命中 ")+damage(scale*SkillDamageBudgets.AdvancedImpact(hero,skill,rank,0));
                    if(steps>1)text+="\n末段 "+damage(scale*SkillDamageBudgets.AdvancedImpact(hero,skill,rank,steps-1))+" · 共 "+steps+" 段";
                }
            }
            var preview=SkillTargetingController.Describe(hero,skill,rank);
            string range=preview.shape==SkillTargetingController.Shape.Cone?"前方 "+preview.distance.ToString("0.#")+" 米 · "+preview.angle.ToString("0")+"°":
                preview.shape==SkillTargetingController.Shape.Lane?"长 "+preview.distance.ToString("0.#")+" 米 · 宽 "+(preview.radius*2).ToString("0.#")+" 米":
                "半径 "+preview.radius.ToString("0.#")+" 米"+(preview.distance>0?" · 最远 "+preview.distance.ToString("0.#")+" 米":"");
            return text+"\n"+range;
        }
    }
}
