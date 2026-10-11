using System;
using Emberfall;
public static class UiRenderCacheTests
{
    static int checks;
    static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    public static string Run()
    {
        checks=0;
        var state=new CollectionPreviewState();
        var weapon=new ItemData{id="weapon",level=12,rarity=Rarity.Epic,upgradeLevel=3,mechanic=EquipmentMechanic.FrostEcho};
        var wings=new FashionData{id="wing",slot=FashionSlot.Wings,rarity=Rarity.Rare};
        Func<CollectionPreviewAppearance> current=()=>new CollectionPreviewAppearance(HeroClass.Arcanist,weapon,null,null,wings,null);
        state.Observe(current());Check(state.NeedsModel&&!state.ShouldRender(false,0)&&state.ShouldRender(true,0),"first appearance waits for repaint");
        state.ModelReady();state.Rendered(0);
        for(int frame=1;frame<600;frame++)
        {state.Observe(current());Check(!state.NeedsModel&&!state.ShouldRender(true,frame),"static preview has no periodic render");}
        state.SetYaw(380);Check(!state.ShouldRender(true,600),"equivalent yaw does not invalidate");
        state.SetYaw(float.NaN);Check(!state.ShouldRender(true,600),"invalid yaw is ignored");
        state.SetYaw(65);Check(state.ShouldRender(true,600)&&!state.NeedsModel,"yaw only rerenders current model");state.Rendered(600);
        state.Invalidate();Check(!state.ShouldRender(true,600)&&state.ShouldRender(true,601)&&!state.NeedsModel,"resume refreshes texture at next frame without rebuilding unchanged model");state.Rendered(601);
        state.InvalidateTexture();Check(state.ShouldRender(true,601)&&!state.ShouldRender(false,601)&&!state.NeedsModel,"lost native pixels may repaint again in the same frame but never on layout");state.Rendered(601);
        state.SetYaw(110);Check(!state.ShouldRender(true,601)&&state.ShouldRender(true,602),"ordinary yaw changes still coalesce after texture recovery");state.Rendered(602);
        int changeFrame=700;
        foreach(Action mutate in new Action[]{()=>weapon.level++,()=>weapon.rarity=Rarity.Legendary,()=>weapon.upgradeLevel++,()=>weapon.mechanicVariant++,()=>weapon.mechanic=EquipmentMechanic.CinderTrail,()=>weapon.id="new-weapon",()=>wings.rarity=Rarity.Legendary,()=>wings.slot=FashionSlot.Weapon,()=>wings.id="new-fashion"})
        {mutate();state.Observe(current());Check(state.NeedsModel&&state.ShouldRender(true,changeFrame),"in-place visual change rebuilds and redraws");state.ModelReady();state.Rendered(changeFrame++);}
        var before=current();weapon.name="renamed";weapon.attack+=200;
        Check(before.Equals(current()),"nonvisual item state is not an invalidation key");
        Check(!current().Equals(new CollectionPreviewAppearance(HeroClass.Ranger,weapon,null,null,wings,null)),"hero change detected");
        Check(!new CollectionPreviewAppearance(HeroClass.Ranger,null,null,null,null,null).Equals(new CollectionPreviewAppearance(HeroClass.Ranger,new ItemData(),null,null,null,null)),"null equipment distinct from an object with empty identity");
        // Actual production snapshot/equality hot path; no model/render/native Unity calls in this probe.
        var armor=new ItemData{id="armor",level=12,rarity=Rarity.Epic,upgradeLevel=6};
        var relic=new ItemData{id="relic",rarity=Rarity.Epic,upgradeLevel=6,mechanic=EquipmentMechanic.FrostEcho};
        var fashionWeapon=new FashionData{id="fashion-weapon",slot=FashionSlot.Weapon,rarity=Rarity.Legendary};
        var reference=new CollectionPreviewAppearance(HeroClass.Arcanist,weapon,armor,relic,wings,fashionWeapon);int matching=0;
        for(int i=0;i<2000;i++)if(new CollectionPreviewAppearance(HeroClass.Arcanist,weapon,armor,relic,wings,fashionWeapon).Equals(reference))matching++;
        long start=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<20000;i++)if(new CollectionPreviewAppearance(HeroClass.Arcanist,weapon,armor,relic,wings,fashionWeapon).Equals(reference))matching++;
        long bytes=GC.GetAllocatedBytesForCurrentThread()-start;Check(bytes==0&&matching==22000,"snapshot/equality steady-state allocates no managed memory");
        CombatTextMetrics metric=default(CombatTextMetrics);float aspect;
        Check(!metric.TryGet(0,out aspect),"unmeasured text misses cache");metric=new CombatTextMetrics(.75f,3);
        Check(metric.TryGet(3,out aspect)&&aspect==.75f&&!metric.TryGet(4,out aspect),"font revision change invalidates cached metrics");
        foreach(float scale in new[]{1f,1.25f,1.8f})foreach(float dpi in new[]{.5f,1f,2f,3f})foreach(bool critical in new[]{false,true})
        {
            Check(metric.TryGet(3,out aspect),"changing scale/density retains normalized glyph aspect");
            float h=CombatTextLayout.PixelHeight(scale,dpi,critical);
            Check(Math.Abs((aspect*h)/h-.75f)<.00001,"scaled glyph box preserves original font shape");
        }
        var metrics=new CombatTextMetrics[24];int measured=0;
        for(int frame=0;frame<48;frame++)for(int i=0;i<metrics.Length;i++)
        {if(!metrics[i].TryGet(7,out aspect)){metrics[i]=new CombatTextMetrics(.5f,7);measured++;}}
        Check(measured==24,"24 stable labels over48 reflows measure24 times, not1152");
        for(int i=0;i<metrics.Length;i++)Check(!metrics[i].TryGet(8,out aspect),"atlas rebuild invalidates every live caption");
        return "PASS: "+checks+" UI cache correctness checks; 20,000 production appearance snapshots allocate "+bytes+" managed bytes; 24 stable captions/48 reflows require "+measured+" metric samples (not Unity profiling)";
    }
}
