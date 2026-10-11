using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Emberfall;

// Test-only deterministic ideal-hit combat host. Damage/cost/cooldown coefficients
// come from production rules; this file adapts live skill events to a clock/ledger.
public static class ComboBudgetSimulation
{
    public const float Step = .01f, InputInterval = .2f;
    public sealed class Recipe
    {
        public readonly string Name; public readonly int[] Priority; public readonly bool WaitForPoison;
        public Recipe(string name,bool poison,params int[] priority){Name=name;Priority=priority;WaitForPoison=poison;}
    }
    public sealed class Cast
    {
        public int Skill,Rank,Id; public float Started,Committed,Cost;
    }
    public sealed class Hit
    {
        public float Time,Coefficient; public string Category; public int Skill,CastId;
    }
    public sealed class ScheduledImpact
    {
        public float Time,Coefficient; public int Skill,Rank,CastId; public string Effect;
    }
    public sealed class Result
    {
        public HeroClass Hero; public int Level; public string Recipe; public float Seconds;
        public int[] Ranks; public int ChargesStarted,ChargesCancelled,PendingCharge,MaximumPets,PetCommands;
        public float EnergySpent,EnergyRestored,EnergyRemaining,MinimumEnergy=100,MaximumEnergy=100,PendingSkillCoefficient;
        public readonly List<Cast> Casts=new List<Cast>(); public readonly List<Hit> Hits=new List<Hit>();
        public readonly List<float> EnergySamples=new List<float>();
        public int BasicHits {get{return Hits.Count(h=>h.Category=="basic");}}
        public float Damage {get{return Hits.Sum(h=>h.Coefficient);}}
        public float Category(string category){return Hits.Where(h=>h.Category==category).Sum(h=>h.Coefficient);}
        public string Signature {get{return string.Join(";",Casts.Select(c=>$"{c.Skill}:{c.Rank}@{c.Committed:F2}"))+"|"+string.Join(";",Hits.Select(h=>$"{h.Time:F2}:{h.Category}:{h.Coefficient:F6}"))+"|"+EnergyRemaining.ToString("F6",CultureInfo.InvariantCulture);}}
    }
    private sealed class Pet
    {
        public int Form,Rank;public float Expires,NextAttack,CommandUntil,CommandMultiplier;
    }
    public static Recipe[] Recipes(HeroClass hero)
    {
        if(hero==HeroClass.Summoner)return new[]{new Recipe("bonded_commands",false,2,4,1,0),new Recipe("treant_mark",false,9,7,2,4,1,0)};
        if(hero==HeroClass.Ranger)return new[]{new Recipe("poison_fan",true,2,1,0),new Recipe("rain_poison",true,9,2,1,0)};
        return new[]{new Recipe(hero==HeroClass.Arcanist?"frost_meteor":"cleave_field",false,0,1,2),new Recipe("ultimate_field",false,9,2,0,1)};
    }
    public static int[] LegalRanks(int level,Recipe recipe)
    {
        var ranks=new int[GameBalance.SkillCount];int points=GameBalance.SkillPointBudget(Math.Min(100,level));
        for(int skill=0;skill<ranks.Length;skill++)
            if(points>0&&level>=GameBalance.SkillRankRequiredLevel(skill,1)&&GameBalance.SkillPrerequisites[skill].All(p=>ranks[p]>0)){ranks[skill]=1;points--;}
        foreach(int skill in recipe.Priority.Concat(Enumerable.Range(0,ranks.Length)).Distinct())
            if(skill>=0&&skill<ranks.Length&&ranks[skill]>0)
                while(points>0&&ranks[skill]<3&&level>=GameBalance.SkillRankRequiredLevel(skill,ranks[skill]+1)){ranks[skill]++;points--;}
        return ranks;
    }
    public static Result Run(HeroClass hero,int level,Recipe recipe,float seconds,bool basics=true,float cancelChargeAt=-1)
    {return new Host(hero,level,recipe,seconds,basics,cancelChargeAt).Run();}
    private sealed class Host
    {
        private readonly HeroClass hero;private readonly Recipe recipe;private readonly Result result;private readonly SkillRuntime runtime;
        private readonly bool basics;private readonly float cancelAt;
        private SkillBasicRecoveryClock basicRecovery;
        private readonly List<ScheduledImpact> pending=new List<ScheduledImpact>(); private readonly List<Pet> pets=new List<Pet>();
        private readonly RecentCastGate meteorGate=new RecentCastGate(),poisonGate=new RecentCastGate();
        private readonly OpeningFrostCounter frostCounter=new OpeningFrostCounter();private readonly CombatProcCooldown frostCooldown=new CombatProcCooldown();
        private ScheduledTickWindow poison;private int poisonStacks;private float poisonStrength,frostUntil;
        private float time,nextInput,nextBasic,chargeEnd,chargeStart;private int charging=-1,nextCast;private bool cancelled;
        public Host(HeroClass hero,int level,Recipe recipe,float seconds,bool basics,float cancelAt)
        {
            if(seconds<=0||seconds>120||float.IsNaN(seconds)||float.IsInfinity(seconds))throw new ArgumentOutOfRangeException(nameof(seconds));
            this.hero=hero;this.recipe=recipe;this.basics=basics;this.cancelAt=cancelAt;runtime=new SkillRuntime(hero);
            result=new Result{Hero=hero,Level=level,Recipe=recipe.Name,Seconds=seconds,Ranks=LegalRanks(level,recipe)};
            if(hero==HeroClass.Summoner)pets.Add(new Pet{Form=0,Rank=result.Ranks[2],Expires=float.PositiveInfinity,NextAttack=0});
        }
        public Result Run()
        {
            int steps=(int)Math.Round(result.Seconds/Step);
            for(int step=0;step<=steps;step++)
            {
                time=step/100f;
                if(step>0)
                {
                    basicRecovery.Advance(Step);
                    float prior=runtime.Energy;runtime.Advance(Step);result.EnergyRestored+=runtime.Energy-prior;frostCooldown.Advance(Step);
                    if(poison!=null&&!poison.Complete)
                    {
                        int ticks=poison.Advance(Step,true);
                        for(int tick=0;tick<ticks;tick++)Hit(poisonStrength*poisonStacks,"poison",-1,0);
                        if(poison.Complete){poisonStacks=0;poisonStrength=0;}
                    }
                }
                Drain();
                if(!cancelled&&cancelAt>=0&&time+1e-5f>=cancelAt&&charging>=0){charging=-1;cancelled=true;result.ChargesCancelled++;}
                bool committedCharge=false;
                if(charging>=0&&time+1e-5f>=chargeEnd)
                {int skill=charging;charging=-1;Commit(skill,chargeStart);committedCharge=true;}
                if(charging<0&&!committedCharge&&time+1e-5f>=nextInput)
                {
                    nextInput=time+InputInterval;
                    foreach(int skill in recipe.Priority)
                    {
                        if(skill<0||skill>=result.Ranks.Length||GameBalance.IsPassive(skill)||result.Ranks[skill]==0||runtime.Remaining(skill)>0||runtime.Energy<GameBalance.SkillEnergyCost(hero,skill))continue;
                        if(recipe.WaitForPoison&&hero==HeroClass.Ranger&&skill==0&&poisonStacks<3)continue;
                        float charge=SkillDamageBudgets.ChargeSeconds(hero,skill);
                        if(charge>0){charging=skill;chargeStart=time;chargeEnd=time+charge;result.ChargesStarted++;}
                        else Commit(skill,time);
                        break;
                    }
                }
                if(basics&&charging<0&&!committedCharge&&time+1e-5f>=nextBasic&&!basicRecovery.Blocked)
                {
                    nextBasic=time+SkillDamageBudgets.BasicInterval(hero);
                    Hit(SkillDamageBudgets.BasicCoefficient(hero),"basic",-1,0);Restore(SkillDamageBudgets.BasicEnergyOnHit);
                    if(hero==HeroClass.Ranger)Poison(PlayerUpgradeRules.BasicPoisonDuration,PlayerUpgradeRules.BasicPoisonCoefficient);
                    if(hero==HeroClass.Arcanist&&frostCounter.RecordHit(1,true,result.Ranks[0]>0)&&frostCooldown.TryTrigger(PlayerUpgradeRules.BasicFrostProcCooldown))
                        frostUntil=Math.Max(frostUntil,time+PlayerUpgradeRules.BasicFrostMarkDuration);
                }
                TickPets();Drain();
                result.EnergySamples.Add(runtime.Energy);result.MinimumEnergy=Math.Min(result.MinimumEnergy,runtime.Energy);result.MaximumEnergy=Math.Max(result.MaximumEnergy,runtime.Energy);
            }
            result.PendingCharge=charging>=0?1:0;result.EnergyRemaining=runtime.Energy;
            result.PendingSkillCoefficient=pending.Sum(e=>e.Coefficient);return result;
        }
        private void Commit(int skill,float started)
        {
            int rank=result.Ranks[skill];float before=runtime.Energy;
            if(!runtime.TryConsume(skill,rank))return;
            basicRecovery.Begin(hero,skill,SkillDamageBudgets.ChargeSeconds(hero,skill)>0);
            float spent=before-runtime.Energy;result.EnergySpent+=spent;int cast=++nextCast;
            result.Casts.Add(new Cast{Skill=skill,Rank=rank,Id=cast,Started=started,Committed=time,Cost=spent});
            if(hero==HeroClass.Summoner&&(skill==2||skill==4||skill==9)){Contract(skill,rank,cast);return;}
            foreach(ScheduledImpact impact in SkillTimeline(hero,skill,rank))
                pending.Add(new ScheduledImpact{Time=time+impact.Time,Coefficient=impact.Coefficient,Skill=skill,Rank=rank,CastId=cast,Effect=impact.Effect});
        }
        private void Drain()
        {
            // Stable insertion order resolves same-time ticks exactly once.
            while(true)
            {
                int next=-1;for(int i=0;i<pending.Count;i++)if(pending[i].Time<=time+1e-5f&&(next<0||pending[i].Time<pending[next].Time))next=i;
                if(next<0)break;ScheduledImpact impact=pending[next];pending.RemoveAt(next);
                Hit(impact.Coefficient,"skill",impact.Skill,impact.CastId);
                if(impact.Effect=="nova")frostUntil=Math.Max(frostUntil,time+PlayerUpgradeRules.NovaFreezeDuration(impact.Rank));
                if(impact.Effect=="meteor"&&meteorGate.TryEnterEligible(impact.CastId,frostUntil>time))
                {frostUntil=0;Hit(impact.Coefficient*PlayerUpgradeRules.ShatterMultiplier(ElementalistSpecialization.None),"reaction",impact.Skill,impact.CastId);}
                if(impact.Effect=="fan"&&poisonStacks>=3&&poisonGate.TryEnter(impact.CastId))
                {Hit(poisonStrength*poisonStacks*PlayerUpgradeRules.PoisonDetonationTicks,"reaction",impact.Skill,impact.CastId);poisonStacks=0;poisonStrength=0;if(poison!=null)poison.Clear();}
                if(impact.Effect=="thorn")Poison(SummonerDamageRules.ThornPoisonDuration(impact.Rank),impact.Coefficient*SummonerDamageRules.ThornPoisonFraction);
            }
        }
        private void Poison(float duration,float coefficient)
        {
            if(poison==null||poison.Complete){poison=new ScheduledTickWindow(duration,StatusTickRates.Poison,StatusTickRates.Poison);poisonStacks=0;poisonStrength=0;}
            else poison.Refresh(duration);
            poisonStacks=Math.Min(3,poisonStacks+1);poisonStrength=Math.Max(poisonStrength,coefficient);
        }
        private void Restore(float energy){float before=runtime.Energy;runtime.RestoreEnergy(energy);result.EnergyRestored+=runtime.Energy-before;}
        private void Hit(float coefficient,string category,int skill,int cast)
        {if(coefficient>0)result.Hits.Add(new Hit{Time=time,Coefficient=coefficient,Category=category,Skill=skill,CastId=cast});}
        private void Contract(int skill,int rank,int cast)
        {
            int form=skill==2?0:skill==4?1:2;Pet pet=pets.Find(p=>p.Form==form&&p.Expires>time);
            if(pet==null){pet=new Pet{Form=form,Rank=rank,NextAttack=time};pets.Add(pet);}
            pet.Rank=rank;bool permanent=CompanionRules.PermanentPartner(form==0,form,false);
            pet.Expires=permanent?float.PositiveInfinity:Math.Max(pet.Expires,time+CompanionRules.ContractLifetime(form,rank,false));
            pet.CommandUntil=time+CompanionRules.CommandDuration(false);pet.CommandMultiplier=CompanionRules.CommandMultiplier(rank);result.PetCommands++;
            if(form==0){Hit(CompanionRules.RankPower(rank)*pet.CommandMultiplier*CompanionRules.WolfCommandCoefficient,"pet_command",skill,cast);pet.NextAttack=time+CompanionRules.WolfCommandRecovery;}
            else pet.NextAttack=Math.Min(pet.NextAttack,time+CompanionRules.CommandReadyDelay);
        }
        private void TickPets()
        {
            pets.RemoveAll(p=>p.Expires<=time);result.MaximumPets=Math.Max(result.MaximumPets,pets.Count);
            foreach(Pet pet in pets)
                if(time+1e-5f>=pet.NextAttack)
                {
                    pet.NextAttack=time+CompanionRules.AttackInterval(pet.Form);
                    float command=pet.CommandUntil>time?pet.CommandMultiplier:1;
                    Hit(CompanionRules.RankPower(pet.Rank)*CompanionRules.AttackCoefficient(pet.Form)*command,"pet_"+pet.Form,-1,0);
                }
        }
    }
    private static void Add(List<ScheduledImpact> hits,float at,float coefficient,string effect="")
    {if(coefficient>0)hits.Add(new ScheduledImpact{Time=at,Coefficient=coefficient,Effect=effect});}
    private static void Field(List<ScheduledImpact> hits,PeriodicSkillBudget budget,float start=0,float multiplier=1,string effect="")
    {
        for(int i=0;i<budget.Ticks;i++)Add(hits,start+budget.Startup+i*budget.Interval,budget.TickCoefficient*multiplier,effect);
        Add(hits,start+budget.Startup+budget.Duration,budget.FinisherCoefficient*multiplier);
    }
    public static List<ScheduledImpact> SkillTimeline(HeroClass hero,int skill,int rank)
    {
        if(rank<1||rank>3)throw new ArgumentOutOfRangeException(nameof(rank));
        var hits=new List<ScheduledImpact>();float power=CombatBalance.RankPower(rank);
        if(hero==HeroClass.Summoner)
        {
            if(skill==0)Add(hits,0,power*SummonerDamageRules.ImpulseCoefficient);
            else if(skill==1)for(int i=0;i<SummonerDamageRules.ThornTicks(rank);i++)Add(hits,SummonerDamageRules.ThornStartup+i*SummonerDamageRules.ThornInterval,power*SummonerDamageRules.ThornTickCoefficient,"thorn");
            else if(skill==7){for(int i=0;i<SummonerDamageRules.MarkTicks;i++)Add(hits,SummonerDamageRules.MarkFirstTick+i*SummonerDamageRules.MarkInterval,power*SummonerDamageRules.MarkTickCoefficient);Add(hits,SummonerDamageRules.MarkFinisherTime,power*SummonerDamageRules.MarkFinisherCoefficient);}
            else if(skill!=2&&skill!=4&&skill!=9)throw new ArgumentException("Unsupported summoner recipe skill");
            return hits;
        }
        if(skill==2){Field(hits,SkillDamageBudgets.EarlyField(hero,rank));return hits;}
        if(skill==9)
        {
            int steps=SkillDamageBudgets.AdvancedSteps(hero,skill,rank);float scale=power*SkillDamageBudgets.AdvancedScale(hero,skill),last=0;
            for(int step=0;step<steps;step++){last=SkillDamageBudgets.AdvancedFirstEvent(hero,skill)+step*SkillDamageBudgets.AdvancedInterval(hero,skill);Add(hits,last,scale*SkillDamageBudgets.AdvancedImpact(hero,skill,rank,step));}
            Field(hits,SkillDamageBudgets.AdvancedTail(hero,skill,rank),last,scale);
            for(int arrow=0;arrow<SkillDamageBudgets.RadialArrowCount(hero,skill,rank);arrow++)Add(hits,last,scale*SkillDamageBudgets.RadialArrowCoefficient);
            return hits;
        }
        if(skill!=0&&skill!=1)throw new ArgumentException("Unsupported recipe skill; do not silently invent its damage");
        if(hero==HeroClass.Vanguard)
        {
            Add(hits,0,SkillDamageBudgets.OpeningImpact(hero,skill,rank));
            if(rank>=2)
            {
                float start=skill==0?.18f:.25f,interval=skill==0?.22f:1,duration=skill==0&&rank==3?.22f:0;
                for(int i=0;i<SkillDamageBudgets.TickCount(duration,interval);i++)Add(hits,start+i*interval,SkillDamageBudgets.OpeningImpact(hero,skill,rank,1));
                if(rank==3)Add(hits,start+duration,SkillDamageBudgets.OpeningImpact(hero,skill,rank,2));
            }
        }
        else if(hero==HeroClass.Arcanist)
        {
            Add(hits,skill==0?0:.7f,SkillDamageBudgets.OpeningImpact(hero,skill,rank),skill==0?"nova":"meteor");
            if(rank>=2)Add(hits,skill==0?.5f:1.1f,SkillDamageBudgets.OpeningImpact(hero,skill,rank,1),skill==0?"nova":"meteor");
            if(rank==3&&skill==1)Field(hits,SkillDamageBudgets.MeteorAftermath(rank));
            if(rank==3&&skill==0)
            {
                var volley=new ProjectileVolleyBudget<object>(1,1.8f);object target=new object();
                for(int i=0;i<8;i++)Add(hits,0,volley.Apply(target,new CombatDamage(SkillDamageBudgets.OpeningImpact(hero,skill,rank,2),false),false).Amount);
            }
        }
        else if(skill==0)
        {
            var volley=new ProjectileVolleyBudget<object>(1,SkillDamageBudgets.FanTargetCap(rank));object target=new object();
            for(int arrow=0;arrow<5+(rank-1)*2;arrow++)
            {
                Add(hits,0,volley.Apply(target,new CombatDamage(SkillDamageBudgets.OpeningImpact(hero,0,rank),false),false).Amount,"fan");
                if(rank==3)Add(hits,0,volley.Apply(target,new CombatDamage(SkillDamageBudgets.OpeningImpact(hero,0,rank,1),false),true).Amount);
            }
        }
        else
        {
            Add(hits,.4f,SkillDamageBudgets.OpeningImpact(hero,1,rank));if(rank>=2)Add(hits,.8f,SkillDamageBudgets.OpeningImpact(hero,1,rank,1));
        }
        return hits;
    }
    public static float SummonerFullLifetime(int skill)
    {
        if(skill==1)return SummonerDamageRules.ThornStartup+(SummonerDamageRules.ThornTicks(3)-1)*SummonerDamageRules.ThornInterval+SummonerDamageRules.ThornPoisonDuration(3);
        if(skill==7)return SummonerDamageRules.MarkFinisherTime;
        if(skill==9)return SkillDamageBudgets.ChargeSeconds(HeroClass.Summoner,9)+CompanionRules.ContractLifetime(2,3,false);
        throw new ArgumentOutOfRangeException(nameof(skill));
    }
    public static Result StandaloneSummoner(int skill,float seconds)
    {return Run(HeroClass.Summoner,100,new Recipe("single_"+skill,false,skill),seconds,false);}
    public static string Csv(IEnumerable<Result> rows)
    {
        var lines=new List<string>{"class,level,recipe,window_seconds,learned_ranks,committed_skills,cast_timeline,charges_started,pending_charge,energy_spent,energy_restored_actual,energy_remaining,basic_hits,basic_A,skill_A,reaction_A,poison_A,pet_A,total_A,max_pets,pet_commands,pending_skill_A"};
        Func<float,string> f=v=>v.ToString("0.000",CultureInfo.InvariantCulture);
        foreach(Result r in rows)
            lines.Add(string.Join(",",r.Hero,r.Level,r.Recipe,f(r.Seconds),string.Join("/",r.Ranks),r.Casts.Count,string.Join(";",r.Casts.Select(c=>c.Skill+"@"+f(c.Committed))),r.ChargesStarted,r.PendingCharge,f(r.EnergySpent),f(r.EnergyRestored),f(r.EnergyRemaining),r.BasicHits,f(r.Category("basic")),f(r.Category("skill")),f(r.Category("reaction")),f(r.Category("poison")),f(r.Hits.Where(h=>h.Category.StartsWith("pet_")).Sum(h=>h.Coefficient)),f(r.Damage),r.MaximumPets,r.PetCommands,f(r.PendingSkillCoefficient)));
        return string.Join("\n",lines)+"\n";
    }
    public static List<Result> StandardRows()
    {
        var rows=new List<Result>();foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(int level in new[]{20,50,100})foreach(Recipe recipe in Recipes(hero))foreach(float seconds in new[]{5f,10f})rows.Add(Run(hero,level,recipe,seconds));return rows;
    }
}
