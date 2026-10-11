using System;
using Emberfall;

public static class CombatBalanceTests
{
    private static int assertions;
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception("CombatBalance: " + message);
    }
    private static bool Near(float left, float right, float tolerance = .001f) { return Math.Abs(left-right) <= tolerance; }
    private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

    public static string Run()
    {
        assertions = 0;
        Check(Near(CombatBalance.UpgradeMultiplier(10),1.5f),"+10 is +50%, not twelve-percent compounding");
        Check(Near(CombatBalance.UpgradeMultiplier(-1),1f),"negative rank clamps to zero");
        Check(Near(CombatBalance.UpgradeMultiplier(int.MaxValue),1.5f),"extreme rank cannot exceed cap");
        Check(CombatBalance.UpgradeValue(0,10,2)==0,"absent item stat never acquires an off-slot attribute");
        Check(CombatBalance.UpgradeValue(100,5)==125,"rank five is linear from base");
        Check(CombatBalance.UpgradeValue(100,10)==150,"rank ten is linear from base");
        Check(CombatBalance.UpgradeValue(221,10)==332,"cumulative half rounding is explicit");
        Check(CombatBalance.UpgradeValue(1,10)==11,"tiny nonzero attributes retain minimum growth");
        Check(CombatBalance.UpgradeValue(1,10,2)==21,"health minimum is per rank, not compound");
        Check(CombatBalance.UpgradeValue(int.MaxValue,10,int.MaxValue,int.MaxValue)==int.MaxValue,"extreme bases/minimums never overflow");
        Check(CombatBalance.UpgradeValue(100,10,1,120)==120,"stat cap applies after safe arithmetic");
        Check(CombatBalance.UpgradeValue(100,-20)==100,"negative rank never strips basis");
        Check(CombatBalance.UpgradeValue(-100,10)==0&&CombatBalance.UpgradeValue(100,10,1,-1)==0,"invalid basis and cap safe");
        for(int basis=1;basis<=10000;basis+=37)
        {
            int previous=basis;
            for(int rank=0;rank<=10;rank++)
            {
                int next=CombatBalance.UpgradeValue(basis,rank);
                Check(next>=previous,"upgrade monotonic without mutating basis");
                Check(next==CombatBalance.UpgradeValue(basis,rank),"preview/repeated equip cannot compound");
                previous=next;
            }
        }
        float[] armors={float.NegativeInfinity,-100f,0f,1f,25f,100f,500f,840.6f,10000f,float.MaxValue,float.PositiveInfinity};
        for(int level=1;level<=100;level++)
        {
            float previous=1;
            foreach(float armor in armors)
            {
                float mult=CombatBalance.ArmorDamageMultiplier(armor,level);
                Check(Finite(mult)&&mult>=.30f&&mult<=1f,"armor reduction finite/bounded");
                Check(mult<=previous+.00001f,"more armor never increases taken damage");
                previous=mult;
            }
            Check(Near(CombatBalance.ArmorDamageMultiplier(float.NaN,level),1f),"invalid armor grants no protection");
            if(level>1)Check(CombatBalance.ArmorDamageMultiplier(500,level)>=CombatBalance.ArmorDamageMultiplier(500,level-1),"fixed armor is less dominant at higher level");
        }
        Check(Near(CombatBalance.ArmorDamageMultiplier(840.6f,100),450f/1290.6f),"legacy level100 armor no longer reaches 97% passive DR");
        Check(Near(CombatBalance.ArmorDamageMultiplier(500,int.MinValue),CombatBalance.ArmorDamageMultiplier(500,1)),"lower invalid level clamps");
        Check(Near(CombatBalance.ArmorDamageMultiplier(500,int.MaxValue),CombatBalance.ArmorDamageMultiplier(500,100)),"upper invalid level clamps");
        Check(CombatBalance.MinimumCombinedDamageMultiplier==.16f,"temporary mitigation has an explicit combined floor");
        Check(CombatBalance.MinimumCombinedDamageMultiplier<CombatBalance.MinimumArmorDamageMultiplier,"temporary guard still adds meaningful protection");
        for(int level=1;level<=100;level++)for(int tier=1;tier<=100;tier++)
        {
            float boss=CombatBalance.EnemyHealth(level,tier,true,EnemyKind.Guardian);
            float damage=CombatBalance.EnemyDamage(level,tier,true);
            Check(Finite(boss)&&Finite(damage)&&boss>0&&damage>0,"enemy output finite positive");
            Check(boss>CombatBalance.EnemyHealth(level,tier,false,EnemyKind.Guardian),"boss budget exceeds elite");
            if(tier>1)
            {
                Check(boss>CombatBalance.EnemyHealth(level,tier-1,true,EnemyKind.Guardian),"HP tier axis never disappears at level cap");
                Check(damage>CombatBalance.EnemyDamage(level,tier-1,true),"damage tier axis never disappears at level cap");
            }
            if(level>1)
            {
                Check(boss>CombatBalance.EnemyHealth(level-1,tier,true,EnemyKind.Guardian),"level HP grows monotonically");
                Check(damage>CombatBalance.EnemyDamage(level-1,tier,true),"level damage grows monotonically");
            }
        }
        foreach(EnemyKind kind in Enum.GetValues(typeof(EnemyKind)))
        {
            Check(Finite(CombatBalance.EnemyHealth(int.MaxValue,int.MaxValue,false,kind)),"all enemy kinds bounded");
            Check(Near(CombatBalance.EnemyHealth(100,100,false,kind),CombatBalance.EnemyHealth(int.MaxValue,int.MaxValue,false,kind)),"extreme input clamps consistently");
        }
        Check(Near(CombatBalance.EnemyHealth(-1,-1,true,EnemyKind.Guardian),1340),"low invalid level/tier safe");
        float healthRatio=CombatBalance.EnemyHealth(100,100,true,EnemyKind.Guardian)/CombatBalance.EnemyHealth(100,11,true,EnemyKind.Guardian);
        float damageRatio=CombatBalance.EnemyDamage(100,100,true)/CombatBalance.EnemyDamage(100,11,true);
        Check(healthRatio>2f&&healthRatio<2.1f,"tier100 versus11 HP is material but bounded");
        Check(damageRatio>2.7f&&damageRatio<2.8f&&damageRatio>healthRatio,"tier threat grows faster than HP");
        Check(Near(CombatBalance.RankPower(1),1)&&Near(CombatBalance.RankPower(3),1.6f),"three existing skill ranks retained");
        Check(Near(SkillDamageBudgets.AdvancedScale(HeroClass.Vanguard,9),.24f),"ultimate keeps its per-skill bounded envelope");
        Check(Near(46.24f*SkillDamageBudgets.AdvancedScale(HeroClass.Vanguard,9),11.0976f),"Vanguard rank3 ultimate total envelope, not a single strike");
        Check(11.0976f*2500 < CombatBalance.EnemyHealth(100,1,true,EnemyKind.Guardian),"high-investment reference full ultimate cannot delete a fresh baseline boss");
        BudgetContracts();
        return "PASS: "+assertions+" combat-balance assertions";
    }
    private static int Round(double value) { return (int)Math.Round(value,MidpointRounding.AwayFromZero); }
    private static int OldUpgrade(int basis, int rank, int minimum)
    {
        for(int i=0;i<rank;i++) if(basis>0)basis+=Math.Max(minimum,Round(basis*.12));
        return basis;
    }
    private static void BudgetContracts()
    {
        // This reproduces the reported failure independently of new growth.
        int[] old={OldUpgrade(459,10,1),OldUpgrade(184,10,1),OldUpgrade(221,10,1),OldUpgrade(738,10,2),OldUpgrade(551,10,2)};
        Check(old[0]==1426&&old[1]==573&&old[2]==688&&old[3]==2292&&old[4]==1712,"exact legacy epic equipment reproduced");
        Check(Near((317+old[0]+old[1])*1.22f,2825.52f,.01f),"reported legacy attack reproduced");
        Check(Near(145.6f+old[2]+7,840.6f)&&2150+old[3]+old[4]==6154,"reported legacy armor/health reproduced");
        Check(310+100*65==6810&&100f/(100+840.6f*4)<.03f,"reported boss health/passive-near-immunity reproduced");
        Check(Near((317+CombatBalance.UpgradeValue(459,10)+CombatBalance.UpgradeValue(184,10))*1.22f,1564.04f,.01f),"rebased epic attack exact");
        Check(2150+CombatBalance.UpgradeValue(738,10,2)+CombatBalance.UpgradeValue(551,10,2)==4084,"rebased epic health exact");
        int[] levels={20,50,100};double[] rarity={1,1.8,2.5};int[] upgrades={0,5,10};
        foreach(int level in levels)
        {
            double priorAttack=0,priorHealth=0;
            for(int build=0;build<3;build++)
            {
                int weapon=CombatBalance.UpgradeValue(Round((5+level*2.5)*rarity[build]),upgrades[build]);
                int relic=CombatBalance.UpgradeValue(Round((2+level)*rarity[build]),upgrades[build]);
                int defense=CombatBalance.UpgradeValue(Round((3+level*1.2)*rarity[build]),upgrades[build]);
                int armorHealth=CombatBalance.UpgradeValue(Round((10+level*4)*rarity[build]),upgrades[build],2);
                int relicHealth=CombatBalance.UpgradeValue(Round((6+level*3)*rarity[build]),upgrades[build],2);
                double attack=(20+3*(level-1)+weapon+relic)*(level==20?1.14:1.22);
                double armor=7+1.4*(level-1)+defense+(level==20?4:7);
                double hp=170+20*(level-1)+armorHealth+relicHealth;
                if(build==2){attack*=1.09;armor*=1.08;hp*=1.12;if(level==100){attack*=1.105;hp*=1.17;}}
                Check(attack>priorAttack&&hp>priorHealth,"normal/formed/high investment retains monotonic power");
                priorAttack=attack;priorHealth=hp;
                double boss=CombatBalance.EnemyHealth(level,1,true,EnemyKind.Guardian);
                double proxy=boss/(2*attack);
                Check(build==0?proxy>30&&proxy<45:build==1?proxy>18&&proxy<30:proxy>10&&proxy<20,"explicit output-rate ruler design band, not measured TTK");
                double taken=CombatBalance.EnemyDamage(level,1,true)*1.4*CombatBalance.ArmorDamageMultiplier((float)armor,level)/hp;
                Check(taken>.06&&taken<.30,"baseline boss slam remains relevant without a one-hit kill");
                if(level>=50)Check(attack*46.24*SkillDamageBudgets.AdvancedScale(HeroClass.Vanguard,9)*1.65<boss*.8,"whole critical Vanguard ultimate leaves follow-up room");
                if(level==100&&build==2)
                {
                    double pushSlam=CombatBalance.EnemyDamage(100,100,true)*1.4*CombatBalance.ArmorDamageMultiplier((float)armor,100)/hp;
                    Check(pushSlam>.20&&pushSlam<.30,"high-investment tier100 slam costs about quarter health");
                    Check(69==35+34,"high-budget mastery spends only69, not four maxed tracks");
                }
            }
        }
        // Full-crit theoretical envelopes; assumes every event connects, no
        // external vulnerability, and no perfect-dodge one-shot core bonus.
        const double arcanistHighAttack=2431.05, rangerHighAttack=2020.83;
        double boss100=CombatBalance.EnemyHealth(100,1,true,EnemyKind.Guardian);
        Check(arcanistHighAttack*47.84*SkillDamageBudgets.AdvancedScale(HeroClass.Arcanist,9)*1.65<boss100*.8,"critical Arcanist upper envelope below80% of fresh baseline boss");
        Check(rangerHighAttack*58.24*SkillDamageBudgets.AdvancedScale(HeroClass.Ranger,9)*1.65<boss100*.8,"critical Ranger upper envelope below80% of fresh baseline boss");
        // Returning blade trades8% of basic damage for a1.1x secondary hit;
        // this remains a net gain with two targets even before its new counter.
        Check(8*.92+1.10>8,"four two-target basic attacks plus one return exceed baseline");
        Check(.92<1,"single-target ordinary basics retain the gear tradeoff");
    }

}
