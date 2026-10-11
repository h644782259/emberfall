using System;
using System.Collections.Generic;
using Emberfall;

public static class RunChoicesTests
{
    public static string Run()
    {
        int assertions=0;
        Action<bool,string> check=(condition,message)=>{assertions++;if(!condition)throw new Exception(message);};
        for(int seed=0;seed<150;seed++)for(int hero=0;hero<4;hero++)
        {
            var state=new RunChoices();var ranks=new int[10];
            if(seed%2==0){ranks[0]=1;ranks[1]=1;ranks[4]=1;ranks[7]=1;}
            for(int wave=1;wave<=2;wave++)
            {
                state.Prepare(wave,(HeroClass)hero,ranks,seed);
                check(state.AwaitingChoice,"wave waits for confirmation");
                RunBlessing[] offer=state.Offer;
                check(offer.Length==3,"three options");
                check(new HashSet<RunBlessing>(offer).Count==3,"no duplicate cards");
                int compatible=0;foreach(var item in offer){if(RunChoices.IsCompatible(item,(HeroClass)hero,ranks))compatible++;check(!state.Has(item),"already selected blessings not repeated");}
                check(compatible>=2,"at least two immediately compatible options");
                check(!state.Choose(-1)&&state.AwaitingChoice,"invalid choice doesn't advance");
                check(!state.Choose(3)&&state.AwaitingChoice,"out-of-range cannot spend choice");
                state.Prepare(wave,(HeroClass)hero,ranks,seed+19);
                check(state.Offer[0]==offer[0],"re-entry cannot reroll offer");
                check(state.Choose(seed%3),"explicit choice consumed");
                check(state.Has(offer[seed%3]),"selected choice applied");
                check(!state.Choose(0),"double click cannot select second blessing");
                state.Prepare(wave,(HeroClass)hero,ranks,seed+21);
                check(!state.AwaitingChoice,"completed wave cannot grant again");
            }
            state.Reset();check(!state.AwaitingChoice,"exit clears pending modal");
            foreach(RunBlessing b in Enum.GetValues(typeof(RunBlessing)))check(!state.Has(b),"exit clears temporary buffs");
            state.Prepare(3,(HeroClass)hero,ranks,seed);check(!state.AwaitingChoice,"no choice after final wave");
        }
        // Same inputs and selections must yield byte-for-byte identical card order.
        for(int seed=0;seed<500;seed++)
        {
            var a=new RunChoices();var b=new RunChoices();var ranks=new int[10];
            for(int i=0;i<ranks.Length;i++)ranks[i]=3;
            for(int wave=1;wave<=2;wave++)
            {
                a.Prepare(wave,HeroClass.Arcanist,ranks,seed);b.Prepare(wave,HeroClass.Arcanist,ranks,seed);
                check(string.Join(",",a.Offer)==string.Join(",",b.Offer),"seeded offer order is deterministic");
                a.Choose(seed%3);b.Choose(seed%3);
            }
        }
        check(Enum.GetValues(typeof(RunBlessing)).Length==14,"six original plus eight distinct additions");
        foreach(RunBlessing blessing in Enum.GetValues(typeof(RunBlessing)))
        {
            check(RunChoices.StackLimit(blessing)==1,"every blessing has explicit one-stack cap");
            check(RunChoices.Description(blessing).Contains("最多1层")&&RunChoices.Description(blessing).Contains("离开本局"),"card explains stack limit and run-only lifetime");
            check(!string.IsNullOrEmpty(RunChoices.Name(blessing)),"every addition has a named card");
        }
        check(!RunChoices.IsCompatible((RunBlessing)99,HeroClass.Ranger,null)&&RunChoices.StackLimit((RunBlessing)99)==0,"unknown blessing is ineligible");
        return "PASS: "+assertions+" run-choice assertions";
    }
}
