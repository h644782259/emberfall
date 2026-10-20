using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int smithCategory,smithSelectedSlot;
        private string smithUpgradeNotice,smithUpgradeNoticeItem,smithUpgradeNoticeSlot;
        private ProgressionService smithUpgradeNoticeOwner;
        private bool smithUpgradeNoticeSuccess,smithUpgradeResultOpen,smithReforgeResult;
        private string[] smithReforgeLines=System.Array.Empty<string>();
        private float smithReforgeStarted;
        private void ShowAffixReforgeResult(bool saved,string id)
        {
            var p=session.Progression;var item=p.Profile.inventory.Find(v=>v!=null&&v.id==id);
            var lines=new System.Collections.Generic.List<string>();
            if(saved&&item!=null)
            {
                if(item.criticalChance>0)lines.Add("暴击率  +"+(item.criticalChance*100).ToString("0.##")+"%");
                if(item.criticalDamageBonus>0)lines.Add("暴击伤害  +"+(item.criticalDamageBonus*100).ToString("0.##")+"%");
                if(item.attackPercent>0)lines.Add("攻击加成  +"+(item.attackPercent*100).ToString("0.##")+"%");
            }
            smithReforgeLines=lines.ToArray();smithReforgeResult=true;smithReforgeStarted=Time.unscaledTime;
            smithUpgradeNoticeOwner=p;smithUpgradeNoticeSlot=p.CurrentSlotId;
            smithUpgradeNoticeItem=item==null?"":ItemTitle(item);smithUpgradeNotice=p.LastError;
            smithUpgradeNoticeSuccess=saved;smithUpgradeResultOpen=true;
            GameAudio.Play(!saved?SoundCue.UI:lines.Count>=3?SoundCue.Victory:lines.Count==2?SoundCue.LevelUp:SoundCue.Loot);
            BlockUITransition();
        }
        private void DrawSmithReforgeResult(float u)
        {
            int count=smithReforgeLines.Length;bool success=smithUpgradeNoticeSuccess,full=success&&count>=3;
            Color accent=!success?new Color(1f,.43f,.35f):full?gold:count==2?new Color(.78f,.55f,1f):jade;
            Fill(new Rect(0,0,width,height),new Color(0,0,0,.76f));blockedRects.Add(new Rect(0,0,width,height));
            float w=Mathf.Min(460*u,width-24*u),h=Mathf.Min(350*u,height-24*u);
            Rect box=new Rect((width-w)*.5f,(height-h)*.5f,w,h);
            Fill(box,full?new Color(.14f,.09f,.025f):card);Border(box,accent,2*u);
            Fill(new Rect(box.x,box.y,w,6*u),accent);
            if(full&&!EffectPreferences.ReducedEffects)
                DrawRewardRadiance(new Rect(box.center.x-60*u,box.y,120*u,90*u),gold,Mathf.Clamp01((Time.unscaledTime-smithReforgeStarted)/1.4f));
            string title=!success?"重铸未完成":full?"满词条！":count==2?"双词条 · 精彩重铸":"重铸成功";
            Text(new Rect(box.x+20*u,box.y+20*u,w-40*u,36*u),title,Mathf.RoundToInt((full?28:23)*u),accent,true,false,TextAnchor.MiddleCenter);
            Text(new Rect(box.x+20*u,box.y+60*u,w-40*u,26*u),smithUpgradeNoticeItem,Mathf.RoundToInt(13*u),pale,true,false,TextAnchor.MiddleCenter);
            Text(new Rect(box.x+20*u,box.y+90*u,w-40*u,24*u),!success?"请检查条件后重试":full?"完美齐聚 · 所有随机词条已集齐":count==2?"获得 2 条随机词条":"获得 "+count+" 条随机词条",Mathf.RoundToInt(13*u),accent,true,false,TextAnchor.MiddleCenter);
            float rowHeight=Mathf.Min(42*u,(h-184*u)/Mathf.Max(1,count));
            for(int i=0;i<count;i++)
            {
                Rect row=new Rect(box.x+24*u,box.y+122*u+i*rowHeight,w-48*u,rowHeight-4*u);
                Fill(row,new Color(accent.r,accent.g,accent.b,.13f));
                DrawIcon(new Rect(row.x+8*u,row.center.y-9*u,18*u,18*u),UIIconAtlas.Utility("core"),accent);
                Text(new Rect(row.x+34*u,row.y,row.width-42*u,row.height),smithReforgeLines[i],Mathf.RoundToInt(14*u),pale,true,false,TextAnchor.MiddleLeft);
            }
            if(!success)Text(new Rect(box.x+24*u,box.y+122*u,w-48*u,h-184*u),smithUpgradeNotice,Mathf.RoundToInt(13*u),pale,false,true,TextAnchor.MiddleCenter);
            if(NavigationButton(new Rect(box.x+24*u,box.yMax-50*u,w-48*u,38*u),full?"收下好运":"知道了",accent))CloseSmithUpgradeResult();
            if(PopupCloseButton(new Rect(box.xMax-44*u,box.y+8*u,32*u,32*u)))CloseSmithUpgradeResult();
        }
        private void CloseSmithUpgradeResult(){smithUpgradeResultOpen=false;BlockUITransition();}
        private void DrawSmithUpgradeResult(float u)
        {
            if(!smithUpgradeResultOpen)return;
            if(smithReforgeResult){DrawSmithReforgeResult(u);return;}
            Color accent=smithUpgradeNoticeSuccess?jade:new Color(1f,.43f,.35f);
            Fill(new Rect(0,0,width,height),new Color(0,0,0,.72f));
            blockedRects.Add(new Rect(0,0,width,height));
            float w=Mathf.Min(360*u,width-24*u),h=Mathf.Min(190*u,height-24*u);
            Rect box=new Rect((width-w)*.5f,(height-h)*.5f,w,h);
            Fill(box,card);Border(box,accent,2*u);
            Fill(new Rect(box.x,box.y,w,7*u),accent);
            Text(new Rect(box.x+20*u,box.y+30*u,w-40*u,32*u),smithUpgradeNoticeSuccess?"强化成功":"强化失败",Mathf.RoundToInt(24*u),accent,true,false,TextAnchor.MiddleCenter);
            Text(new Rect(box.x+20*u,box.y+78*u,w-40*u,32*u),smithUpgradeNotice,Mathf.RoundToInt(18*u),pale,true,false,TextAnchor.MiddleCenter);
            if(NavigationButton(new Rect(box.x+24*u,box.yMax-54*u,w-48*u,38*u),"知道了",accent))CloseSmithUpgradeResult();
            if(PopupCloseButton(new Rect(box.xMax-44*u,box.y+10*u,32*u,32*u)))CloseSmithUpgradeResult();
        }

        private readonly System.Collections.Generic.Dictionary<string,Vector2> smithVariantScroll=new System.Collections.Generic.Dictionary<string,Vector2>();
        private bool smithSocketPicker;
        private EquipmentMechanic smithSocketPrevious;
        private Vector2 smithSocketScroll;
        private Vector2 smithDetailScroll,smithPreviewScroll;
        private ProgressionService.AffixReforgeQuote smithAffixQuote;
        private EquipmentMechanic smithPreviewMechanic;
        private bool smithPreviewAscend;
        private void OpenGemPreview(EquipmentMechanic mechanic,bool ascend)
        {smithPreviewMechanic=mechanic;smithPreviewAscend=ascend;smithPreviewScroll=Vector2.zero;}
        private void DrawGemPreview(float u)
        {
            if(smithPreviewMechanic==EquipmentMechanic.None)return;
            var p=session.Progression;var gem=p.Attachment(smithPreviewMechanic);
            if(gem==null){smithPreviewMechanic=EquipmentMechanic.None;return;}
            blockedRects.Add(new Rect(0,0,width,height));
            float w=Mathf.Min(420*u,width-24*u),h=Mathf.Min(410*u,height-24*u);
            Rect box=new Rect((width-w)*.5f,(height-h)*.5f,w,h);Fill(box,card);Border(box,jade);
            Text(new Rect(box.x+14*u,box.y+10*u,w-70*u,28*u),BuildCatalog.GemName(gem.mechanic),Mathf.RoundToInt(17*u),gold,true);
            if(PopupCloseButton(new Rect(box.xMax-48*u,box.y+4*u,44*u,36*u))){smithPreviewMechanic=EquipmentMechanic.None;return;}
            Rect body=new Rect(box.x+8*u,box.y+48*u,w-16*u,h-144*u);float contentWidth=body.width/u-18,y=0;
            DrawGemUpgradeComparison(ref y,contentWidth,u,gem,smithPreviewAscend,false);

            smithPreviewScroll=BeginTouchScroll("smith-gem-preview",body,smithPreviewScroll,new Rect(0,0,contentWidth*u,Mathf.Max(body.height,y*u)));
            y=0;DrawGemUpgradeComparison(ref y,contentWidth,u,gem,smithPreviewAscend,true);
            EndTouchScroll();
            int cost=smithPreviewAscend?ProgressionService.AscensionCost:ProgressionService.AttachmentUpgradeCost;
            string reason=smithPreviewAscend?p.AttachmentAscensionLock(gem.mechanic,SmithServiceActive):p.AttachmentUpgradeLock(gem.mechanic,SmithServiceActive);
            bool enough=p.Profile.mechanicMaterials>=cost;
            Text(new Rect(box.x+14*u,box.yMax-88*u,w-28*u,30*u),reason,Mathf.RoundToInt(12*u),reason.Length==0?muted:new Color(1,.35f,.3f),false,true);
            Rect confirm=new Rect(box.x+14*u,box.yMax-52*u,w-28*u,40*u);
            bool accepted=PrimaryButton(confirm,"",gold,reason.Length==0);
            Text(new Rect(confirm.x+10*u,confirm.y,confirm.width-115*u,confirm.height),smithPreviewAscend?(gem.ascensionRank>=3?"升华已满":"确认升华"):(gem.upgradeRank>=ProgressionService.MaximumAttachmentRank?"已满阶":"继续升阶"),Mathf.RoundToInt(14*u),reason.Length==0?pale:muted,true,false,TextAnchor.MiddleLeft);
            DrawPriceTint(new Rect(confirm.xMax-100*u,confirm.y,90*u,confirm.height),cost,true,u,enough?gold:new Color(1,.25f,.2f));
            if(accepted){bool saved=smithPreviewAscend?p.AscendAttachment(gem.mechanic,SmithServiceActive):p.UpgradeAttachment(gem.mechanic,SmithServiceActive);Feedback(saved,smithPreviewAscend?"宝石已升华":"宝石已升阶");if(saved)smithPreviewScroll=Vector2.zero;}
        }
        private void DrawSmithResource(Rect rect,int amount,Texture2D icon,Color tint,float u,string resourceKey)
        {
            Fill(rect,new Color(.14f,.115f,.055f,.55f));Border(rect,new Color(tint.r,tint.g,tint.b,.25f));
            DrawIcon(new Rect(rect.x+4*u,rect.center.y-9*u,18*u,18*u),icon,tint);
            Text(new Rect(rect.x+27*u,rect.y,rect.width-31*u,rect.height),amount.ToString(),Mathf.RoundToInt(13*u),pale,true,false,TextAnchor.MiddleLeft);
            InspectRewardItem(rect,ResourceItemPreview(resourceKey,amount));
        }
        private void DrawSmithMaxBadge(Rect rect,string label,float u)
        {
            Fill(rect,new Color(.24f,.14f,.035f));Border(rect,gold,2*u);
            Text(rect,label,Mathf.RoundToInt(15*u),new Color(1,.9f,.55f),true,false,TextAnchor.MiddleCenter);
        }
        private void DrawSmithService()
        {
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            if(MobileControls.Active&&width<height)u=Mathf.Max(u,1f/scale);
            if(smithUpgradeResultOpen&&(smithUpgradeNoticeOwner!=p||smithUpgradeNoticeSlot!=p.CurrentSlotId))smithUpgradeResultOpen=false;
            bool prior=GUI.enabled;GUI.enabled=prior&&!smithUpgradeResultOpen&&smithPreviewMechanic==EquipmentMechanic.None&&!smithSocketPicker&&smithAffixQuote==null;
            var l=new SmithServiceLayout(width/u,height/u,MobileControls.IsIPad);
            blockedRects.Add(new Rect(0,0,width,height));
            Box(new Rect(8*u,4*u,width-16*u,height-8*u),jade,false);
            float balanceWidth=Mathf.Min(100,(width/u-164)/5);
            float balanceStart=width-64*u-5*(balanceWidth+4)*u;
            Text(new Rect(16*u,10*u,Mathf.Max(48*u,balanceStart-24*u),36*u),"铁匠",Mathf.RoundToInt(22*u),gold,true);
            DrawPrice(new Rect(balanceStart,12*u,balanceWidth*u,28*u),p.Profile.gold,false,u);
            DrawPrice(new Rect(balanceStart+(balanceWidth+4)*u,12*u,balanceWidth*u,28*u),p.Profile.mechanicMaterials,true,u);
            InspectRewardItem(new Rect(balanceStart,12*u,balanceWidth*u,28*u),ResourceItemPreview("gold",p.Profile.gold));
            InspectRewardItem(new Rect(balanceStart+(balanceWidth+4)*u,12*u,balanceWidth*u,28*u),ResourceItemPreview("shard",p.Profile.mechanicMaterials));
            DrawSmithResource(new Rect(balanceStart+2*(balanceWidth+4)*u,12*u,balanceWidth*u,28*u),p.Profile.refinementStones,UIIconAtlas.Utility("refinement"),GameBalance.RarityColor(Rarity.Epic),u,"refinement");
            DrawSmithResource(new Rect(balanceStart+3*(balanceWidth+4)*u,12*u,balanceWidth*u,28*u),p.Profile.fashionThreads,UIIconAtlas.Reward(2),GameBalance.RarityColor(Rarity.Rare),u,"thread");
            DrawSmithResource(new Rect(balanceStart+4*(balanceWidth+4)*u,12*u,balanceWidth*u,28*u),p.Profile.affixReforgeStones,UIIconAtlas.Utility("affix-reforge"),gold,u,"affix-reforge");
            if(PopupCloseButton(new Rect(width-56*u,10*u,42*u,36*u))){smithPreviewMechanic=EquipmentMechanic.None;GUI.enabled=prior;ClosePanel();return;}
            smithCategory=Mathf.Clamp(smithCategory,0,3);
            string[] categories={"强化","镶嵌","洗练","时装"};
            float categoryStep=Mathf.Min(116,(width/u-32)/4);
            for(int i=0;i<categories.Length;i++)
                if(TabButton(new Rect((16+i*categoryStep)*u,58*u,(categoryStep-6)*u,40*u),categories[i],smithCategory==i)&&smithCategory!=i){smithCategory=i;smithDetailScroll=Vector2.zero;}
            Rect body=new Rect(16*u,110*u,width-32*u,height-122*u);
            if(smithCategory==3){DrawFashionSmith(body,u);GUI.enabled=prior;return;}
            if(smithCategory==1)
            {
                DrawPhoneSocketService(body,u);
                GUI.enabled=prior;DrawGemPreview(u);DrawSocketPicker(u);DrawAffixConfirmation(u);DrawSmithUpgradeResult(u);return;
            }
            int columns=width/u>=540?3:1;
            float cardWidth=(body.width/u-(columns-1)*12)/columns;
            float[] cardHeights=new float[3];float rowHeight=0;
            for(int i=0;i<3;i++)
            {
                var gear=p.Equipped((ItemSlot)i);
                cardHeights[i]=gear==null?180:DrawSmithDetail(gear,cardWidth-16,u,false)+(smithCategory==0?(MobileControls.Active?50:62):0)+16;
                rowHeight=Mathf.Max(rowHeight,cardHeights[i]);
            }
            float contentHeight=columns==3?rowHeight:cardHeights[0]+cardHeights[1]+cardHeights[2]+24;
            smithDetailScroll=BeginTouchScroll("smith-all-equipment",body,smithDetailScroll,new Rect(0,0,body.width,Mathf.Max(body.height,contentHeight*u)));
            float top=0;
            for(int i=0;i<3;i++)
            {
                var slot=(ItemSlot)i;var item=p.Equipped(slot);
                Rect tile=new Rect((columns==3?i*(cardWidth+12):0)*u,(columns==3?0:top)*u,cardWidth*u,(columns==3?rowHeight:cardHeights[i])*u);
                Fill(tile,card);Border(tile,item==null?muted:GameBalance.RarityColor(item.rarity));
                GUI.BeginGroup(new Rect(tile.x+8*u,tile.y+8*u,tile.width-16*u,tile.height-16*u));
                if(item==null)
                {
                    DrawIcon(new Rect((cardWidth-64)*.5f*u,12*u,48*u,48*u),UIIconAtlas.EquipmentCardIcon(slot,1,Rarity.Common,p.Profile.heroClass),muted);
                    Text(new Rect(0,72*u,(cardWidth-16)*u,28*u),GameBalance.SlotName(slot),Mathf.RoundToInt(15*u),pale,true,false,TextAnchor.MiddleCenter);
                    Text(new Rect(0,110*u,(cardWidth-16)*u,40*u),"未穿戴",Mathf.RoundToInt(12*u),muted,false,false,TextAnchor.MiddleCenter);
                }
                else
                {
                    float y=DrawSmithDetail(item,cardWidth-16,u,true);
                    if(smithCategory==2)
                    {
                        string rerollReason=p.AffixReforgeLockReason(item.id,SmithServiceActive);
                        string reason=p.RefinementLockReason(item.id,SmithServiceActive);
                        float aw=(cardWidth-28)*.5f,buttonHeight=44;
                        Rect action=new Rect(4*u,tile.height-68*u,aw*u,buttonHeight*u);
                        Rect reroll=new Rect(action.xMax+4*u,action.y,aw*u,buttonHeight*u);
                        if(SmithMaterialAction(action,"refinement",reason=="数值已满"?"已满":"洗练",GameBalance.RarityColor(Rarity.Epic),u,reason.Length==0,"提升基础词条和已有随机词条数值，只升不降，不新增词条。"+(reason.Length==0?"":"\n"+reason)))
                            Feedback(p.RefineEquipment(item.id,SmithServiceActive),"洗练已保存");
                        if(SmithMaterialAction(reroll,"affix-reforge",ProgressionService.HasFullEquipmentAffixes(item)?"满词条":"词条重铸",ProgressionService.HasFullEquipmentAffixes(item)?gold:GameBalance.RarityColor(Rarity.Rare),u,rerollReason.Length==0,"仅重铸随机词条；数值可能降低或词条消失，基础词条与强化等级不变。"+(rerollReason.Length==0?"":"\n"+rerollReason)))
                            smithAffixQuote=p.PrepareAffixReforge(item.id,SmithServiceActive);
                    }
                    if(smithCategory==0)
                    {
                        var quote=p.PrepareSmithUpgrade(slot,SmithServiceActive);
                        int cost=p.UpgradeCost(item);bool capped=p.SlotUpgradeRank(slot)>=p.CurrentUpgradeLimit;
                        float actionWidth=Mathf.Min(148,cardWidth-32);
                        float actionHeight=MobileControls.Active?36:44;
                        Rect action=new Rect((cardWidth-16-actionWidth)*.5f*u,tile.height-(28+actionHeight)*u,actionWidth*u,actionHeight*u);
                        Color accent=capped||quote!=null?gold:muted;
                        Fill(action,new Color(accent.r,accent.g,accent.b,.13f));Border(action,new Color(accent.r,accent.g,accent.b,.55f));
                        if(!capped&&QuietAction(action,"",quote!=null))
                        {
                            int attempts=p.Profile.equipmentUpgradeAttempts;int oldRank=p.SlotUpgradeRank(slot);
                            bool success=p.UpgradeAtSmith(quote,SmithServiceActive);
                            bool paid=p.Profile.equipmentUpgradeAttempts>attempts;
                            smithUpgradeNotice="等级变化  +"+oldRank+" → +"+p.SlotUpgradeRank(slot);
                            smithUpgradeNoticeOwner=p;smithUpgradeNoticeSlot=p.CurrentSlotId;smithUpgradeNoticeItem=ItemTitle(item);
                            smithReforgeResult=false;smithUpgradeNoticeSuccess=success;smithUpgradeResultOpen=true;
                            GameAudio.Play(success?SoundCue.ForgeSuccess:paid?SoundCue.ForgeFailure:SoundCue.UI);
                            BlockUITransition();
                        }
                        if(capped)DrawSmithMaxBadge(action,"已满级",u);
                        if(!capped)
                        {
                            string amount=cost.ToString();int priceFont=Mathf.RoundToInt(12*u);
                            float amountWidth=Style(priceFont,true,false).CalcSize(new GUIContent(amount)).x;
                            float iconSize=18*u,gap=6*u,start=action.center.x-(iconSize+gap+amountWidth)*.5f;
                            DrawIcon(new Rect(start,action.center.y-iconSize*.5f,iconSize,iconSize),UIIconAtlas.Utility("coin"),gold);
                            Text(new Rect(start+iconSize+gap,action.y,amountWidth,action.height),amount,priceFont,p.Profile.gold>=cost?accent:new Color(.98f,.28f,.24f),true,false,TextAnchor.MiddleLeft);
                        }
                    }
                }
                GUI.EndGroup();top+=cardHeights[i]+12;
            }
            EndTouchScroll();
            GUI.enabled=prior;DrawGemPreview(u);DrawSocketPicker(u);DrawAffixConfirmation(u);DrawSmithUpgradeResult(u);
        }
        private void DrawPhoneSocketService(Rect body,float u)
        {
            var p=session.Progression;smithSelectedSlot=Mathf.Clamp(smithSelectedSlot,0,2);
            float slotSize=Mathf.Min(64,(body.height/u-16)/3);
            for(int i=0;i<3;i++)
            {
                var item=p.Equipped((ItemSlot)i);Rect tab=new Rect(body.x,body.y+i*(slotSize+8)*u,slotSize*u,slotSize*u);
                Fill(tab,smithSelectedSlot==i?new Color(.08f,.25f,.24f):card);Border(tab,smithSelectedSlot==i?gold:muted,smithSelectedSlot==i?3:1);
                DrawIcon(new Rect(tab.x+7*u,tab.y+7*u,tab.width-14*u,tab.height-14*u),item!=null?UIIconAtlas.EquipmentCardIcon(item,p.Profile.heroClass):UIIconAtlas.EquipmentCardIcon((ItemSlot)i,1,Rarity.Common,p.Profile.heroClass),item==null?muted:GameBalance.RarityColor(item.rarity));
                if(QuietAction(tab,"")){smithSelectedSlot=i;smithDetailScroll=Vector2.zero;}
            }
            Rect detail=new Rect(body.x+(slotSize+12)*u,body.y,body.width-(slotSize+12)*u,body.height);Fill(detail,card);Border(detail,jade*.4f);
            var selected=p.Equipped((ItemSlot)smithSelectedSlot);
            if(selected==null){Text(detail,"未穿戴",Mathf.RoundToInt(14*u),muted,false,false,TextAnchor.MiddleCenter);return;}
            GUI.BeginGroup(detail);

            float w=detail.width/u,h=detail.height/u,left=Mathf.Min(290,w*.34f),right=w-left-16;
            Text(new Rect(8*u,2*u,(w-16)*u,22*u),selected.name,Mathf.RoundToInt(12*u),gold,true);
            var gems=new System.Collections.Generic.List<MechanicAttachment>();
            foreach(var mechanic in BuildCatalog.GemsFor(p.Profile.heroClass)){var value=p.Attachment(mechanic);if(value!=null&&value.mounted&&BuildCatalog.MechanicSlot(mechanic)==selected.slot)gems.Add(value);}
            bool stacked=w<620;
            float rowHeight=stacked?310:152,formWidth=(right-8)*.5f;
            smithDetailScroll=BeginTouchScroll("smith-gem-growth",new Rect(0,28*u,w*u,(h-28)*u),smithDetailScroll,new Rect(0,0,(w-16)*u,3*rowHeight*u));
            w-=16;if(stacked)left=w;right=stacked?w:w-left-16;formWidth=(right-8)*.5f;
            for(int index=0;index<ProgressionService.MaximumMountedGemsPerSlot;index++)
            {
                var gem=index<gems.Count?gems[index]:null;float top=index*rowHeight;
                float size=Mathf.Clamp(rowHeight-64,28,44);Rect socket=new Rect(8*u,(top+4)*u,size*u,size*u);
                Fill(socket,card);Border(socket,gem==null?jade:GameBalance.RarityColor(gem.rarity),2);
                if(gem!=null&&gem.rarity==Rarity.Legendary)DrawFashionSlotFlow(socket,GameBalance.RarityColor(gem.rarity),u);
                if(gem==null)Text(socket,"+",Mathf.RoundToInt(24*u),jade,false,false,TextAnchor.MiddleCenter);
                else {DrawIcon(socket,UIIconAtlas.Utility("gem"),GameBalance.RarityColor(gem.rarity));DrawWornIconBadge(socket,u,"已镶嵌");}
                if(QuietAction(socket,"",SmithServiceActive)){smithSocketPrevious=gem==null?EquipmentMechanic.None:gem.mechanic;smithSocketPicker=true;smithSocketScroll=Vector2.zero;}
                if(gem==null)
                {
                    Text(new Rect((size+14)*u,(top+4)*u,(left-size-18)*u,36*u),"宝石槽 "+(index+1),Mathf.RoundToInt(11*u),muted,false,true);
                    
                    continue;
                }
                Text(new Rect((size+14)*u,(top+2)*u,(left-size-18)*u,22*u),BuildCatalog.GemName(gem.mechanic),Mathf.RoundToInt(11*u),GameBalance.RarityColor(gem.rarity),true);
                float mechanismX=stacked?12:left+12,mechanismTop=stacked?top+138:top;
                float current=BuildCatalog.GemAttributeValue(gem.mechanic,gem.rarity,gem.upgradeRank)*100;
                DrawDetailTag(new Rect(10*u,(top+57)*u,44*u,22*u),gem.upgradeRank+"阶",jade,u);
                DrawGemEffectText(new Rect(62*u,(top+57)*u,(left-72)*u,22*u),BuildCatalog.GemAttributeLabel(gem.mechanic)+" +"+current.ToString("0.#")+"%",Mathf.RoundToInt(12*u),jade,true,TextAnchor.MiddleLeft);
                string reason=p.AttachmentUpgradeLock(gem.mechanic,SmithServiceActive);
                if(DrawButton(new Rect(10*u,(top+(MobileControls.Active?94:102))*u,Mathf.Min(104,(left-28)*.5f)*u,(MobileControls.Active?40:30)*u),gem.upgradeRank>=9?"升阶已满":"升阶",ButtonRole.Action,gem.upgradeRank<9,reason,Mathf.RoundToInt(12*u)))OpenGemPreview(gem.mechanic,false);
                DrawDetailTag(new Rect(mechanismX*u,(mechanismTop+4)*u,86*u,24*u),"升华 "+gem.ascensionRank+"级",gold,u);
                string ascendReason=p.AttachmentAscensionLock(gem.mechanic,SmithServiceActive);
                if(DrawButton(new Rect((MobileControls.Active?18+Mathf.Min(104,(left-28)*.5f):mechanismX)*u,(MobileControls.Active?top+94:mechanismTop+102)*u,Mathf.Min(104,(left-28)*.5f)*u,(MobileControls.Active?40:30)*u),gem.ascensionRank>=3?"升华已满":"升华",ButtonRole.Action,gem.ascensionRank<3,ascendReason,Mathf.RoundToInt(12*u)))OpenGemPreview(gem.mechanic,true);
                Fill(new Rect(8*u,(top+rowHeight-8)*u,(w-16)*u,1*u),muted*.18f);
                for(int variant=0;variant<2;variant++)
                {
                    // In the mobile two-column layout, extend the description to
                    // the same bottom edge as the upgrade/ascension buttons (94+40).
                    float formHeight=MobileControls.Active&&!stacked?94+40-36:60;
                    Rect option=new Rect((mechanismX+(stacked?0:variant*(formWidth+8)))*u,(mechanismTop+36+(stacked?variant*66:0))*u,(stacked?right-24:formWidth-8)*u,formHeight*u);
                    bool active=gem.variantUnlocked&&gem.variant==variant;Fill(option,active?new Color(.055f,.19f,.18f):card);Border(option,active?gold:muted*.4f);
                    DrawIcon(new Rect(option.x+5*u,option.y+5*u,16*u,16*u),UIIconAtlas.Utility(active?"confirm":variant==0?"core":"attack"),active?gold:jade);
                    Text(new Rect(option.x+25*u,option.y+2*u,option.width-30*u,22*u),SmithVariantName(gem.mechanic,variant),Mathf.RoundToInt(11*u),active?gold:pale,true,false,TextAnchor.MiddleLeft);
                    string formText=gem.ascensionRank>0?BuildCatalog.GemFormDescription(gem.mechanic,variant,gem.ascensionRank):"未解锁";

                    Rect copy=new Rect(option.x+6*u,option.y+27*u,option.width-12*u,option.height-31*u);
                    int font=Mathf.RoundToInt(12*u);while(font>Mathf.RoundToInt(9*u)&&Style(font,false,true).CalcHeight(new GUIContent(formText),copy.width)>copy.height)font--;
                    Text(copy,formText,font,gem.variantUnlocked?pale:muted,false,true);
                    if(QuietAction(option,"",!active&&gem.variantUnlocked&&SmithServiceActive))SelectSmithVariant(gem.mechanic,variant);
                }
            }
            EndTouchScroll();
            GUI.EndGroup();
        }
        private void DrawGemUpgradeComparison(ref float y,float width,float u,MechanicAttachment gem,bool ascend,bool draw)
        {
            int nextRank=Mathf.Min(9,gem.upgradeRank+1),nextAscension=Mathf.Min(3,Mathf.Max(0,gem.ascensionRank)+1);
            if(!ascend)GoalParagraph(ref y,width,u,"升阶预览",15,gold,true,draw);
            if(!ascend)
            {
                Rarity rarity=nextRank>=3&&gem.rarity<Rarity.Epic?Rarity.Epic:gem.rarity;
                float before=BuildCatalog.GemAttributeValue(gem.mechanic,gem.rarity,gem.upgradeRank)*100;
                float after=BuildCatalog.GemAttributeValue(gem.mechanic,rarity,nextRank)*100;
                GoalParagraph(ref y,width,u,gem.upgradeRank+"阶  →  "+nextRank+"阶",13,pale,true,draw);
                GoalParagraph(ref y,width,u,BuildCatalog.GemAttributeLabel(gem.mechanic)+"  +"+before.ToString("0.#")+"%  →  +"+after.ToString("0.#")+"%",14,jade,true,draw);
                return;
            }
            GoalParagraph(ref y,width,u,"升华 "+gem.ascensionRank+"级  →  "+nextAscension+"级",13,pale,true,draw);
            for(int variant=0;variant<2;variant++)
            {
                GoalParagraph(ref y,width,u,BuildCatalog.GemFormName(gem.mechanic,variant)+(gem.variant==variant&&gem.variantUnlocked?" · 当前形态":""),13,gold,true,draw);
                GoalParagraph(ref y,width,u,"当前："+(gem.ascensionRank>0?BuildCatalog.GemFormDescription(gem.mechanic,variant,gem.ascensionRank):"未解锁"),12,muted,false,draw);
                GoalParagraph(ref y,width,u,"升华后："+BuildCatalog.GemFormDescription(gem.mechanic,variant,nextAscension),12,jade,false,draw);
            }
        }
        private string SmithVariantName(EquipmentMechanic mechanic,int variant)
        {
            return BuildCatalog.GemFormName(mechanic,variant);
        }
        private void SelectSmithVariant(EquipmentMechanic mechanic,int variant)
        {
            var p=session.Progression;var gem=p.Attachment(mechanic);
            if(gem==null||variant<0||variant>1||gem.variant==variant)return;
            if(p.ToggleAttachmentVariant(mechanic,SmithServiceActive))Feedback(true,"已切换为"+SmithVariantName(mechanic,variant));
            else Feedback(false,p.LastError);
        }
        private string SmithVariantDescription(EquipmentMechanic mechanic,int variant)
        {
            var gem=session.Progression.Attachment(mechanic);
            return BuildCatalog.GemFormDescription(mechanic,variant,gem==null?0:gem.ascensionRank);
        }

        private void DrawSocketPicker(float u)
        {
            if(!smithSocketPicker)return;
            var p=session.Progression;var slot=(ItemSlot)smithSelectedSlot;
            blockedRects.Add(new Rect(0,0,width,height));
            float w=Mathf.Min(460*u,width-24*u),h=Mathf.Min(420*u,height-24*u);
            Rect box=new Rect((width-w)/2,(height-h)/2,w,h);Fill(box,card);Border(box,jade);
            Text(new Rect(box.x+16*u,box.y+12*u,w-76*u,30*u),GameBalance.SlotName(slot)+" · 选择宝石",Mathf.RoundToInt(18*u),gold,true);
            if(PopupCloseButton(new Rect(box.xMax-48*u,box.y+6*u,44*u,36*u))){smithSocketPicker=false;return;}
            var candidates=new System.Collections.Generic.List<MechanicAttachment>();
            foreach(var mechanic in BuildCatalog.GemsFor(p.Profile.heroClass))
            {
                var a=p.Attachment(mechanic);
                if(a!=null&&BuildCatalog.MechanicSlot(mechanic)==slot)candidates.Add(a);
            }
            Rect body=new Rect(box.x+12*u,box.y+56*u,w-24*u,h-68*u);
            if(candidates.Count==0){Text(body,"暂无可镶嵌的宝石",Mathf.RoundToInt(14*u),muted,false,true,TextAnchor.MiddleCenter);return;}
            float cw=(body.width/u-10)/2;
            smithSocketScroll=BeginTouchScroll("smith-socket-picker",body,smithSocketScroll,new Rect(0,0,body.width,Mathf.Max(body.height,((candidates.Count+1)/2)*178*u)));
            for(int i=0;i<candidates.Count;i++)
            {
                var a=candidates[i];Rect tile=new Rect((i%2)*(cw+10)*u,(i/2)*178*u,cw*u,168*u);
                Fill(tile,new Color(.06f,.09f,.13f));Border(tile,a.mounted?jade:GameBalance.RarityColor(a.rarity));
                Rect gemIcon=new Rect(tile.center.x-24*u,tile.y+12*u,48*u,48*u);
                DrawIcon(gemIcon,UIIconAtlas.Utility("gem"),GameBalance.RarityColor(a.rarity));
                if(a.rarity==Rarity.Legendary)DrawFashionSlotFlow(gemIcon,GameBalance.RarityColor(a.rarity),u);
                if(a.mounted)DrawWornIconBadge(gemIcon,u,"已镶嵌");
                Text(new Rect(tile.x+6*u,tile.y+65*u,tile.width-12*u,28*u),BuildCatalog.GemName(a.mechanic),Mathf.RoundToInt(13*u),pale,true,false,TextAnchor.MiddleCenter);
                DrawGemEffectText(new Rect(tile.x+6*u,tile.y+94*u,tile.width-12*u,30*u),BuildCatalog.IsAttributeGem(a.mechanic)?BuildCatalog.GemAttributeSummary(a.mechanic,a.rarity,a.upgradeRank):a.mounted?"已镶嵌":"阶数 "+a.upgradeRank,Mathf.RoundToInt(10*u),a.mounted?jade:muted,false,TextAnchor.MiddleCenter);
                string mountReason=p.AttachmentReplacementLock(a.mechanic,smithSocketPrevious,SmithServiceActive);
                if(Button(new Rect(tile.x+8*u,tile.y+124*u,tile.width-16*u,36*u),a.mechanic==smithSocketPrevious?"卸下":a.mounted?"已镶嵌":smithSocketPrevious!=EquipmentMechanic.None?"替换":"镶嵌",jade,SmithServiceActive&&mountReason.Length==0,mountReason))
                {if(a.mechanic==smithSocketPrevious?p.SetAttachmentMounted(a.mechanic,false,SmithServiceActive):p.ReplaceAttachment(a.mechanic,smithSocketPrevious,SmithServiceActive)){Feedback(true,"宝石已更新");smithSocketPicker=false;}else Feedback(false,p.LastError);}
            }
            EndTouchScroll();
        }
        private bool SmithMaterialAction(Rect r,string materialKey,string caption,Color materialTint,float u,bool enabled,string reason)
        {
            bool clicked=DrawButton(r,"",ButtonRole.Action,enabled,reason);
            Color textColor=enabled?pale:muted;
            float iconSize=14*u,costWidth=36*u;
            Text(new Rect(r.x+4*u,r.y,r.width-costWidth-8*u,r.height),caption,Mathf.RoundToInt(11*u),textColor,true,false,TextAnchor.MiddleCenter);
            DrawIcon(new Rect(r.xMax-costWidth,r.center.y-iconSize*.5f,iconSize,iconSize),UIIconAtlas.Utility(materialKey),materialTint);
            Text(new Rect(r.xMax-costWidth+iconSize,r.y,costWidth-iconSize-3*u,r.height),"×1",Mathf.RoundToInt(10*u),textColor,true,false,TextAnchor.MiddleCenter);
            return clicked;
        }
        private void DrawSmithStatHeading(ref float y,float width,float u,string title,bool draw)
        {
            if(draw)
            {
                Fill(new Rect(0,y*u,width*u,28*u),new Color(.07f,.13f,.16f));
                Fill(new Rect(8*u,(y+8)*u,3*u,12*u),jade);
                Text(new Rect(18*u,y*u,(width-24)*u,28*u),title,Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleLeft);
            }
            y+=32;
        }
        private float DrawSmithDetail(ItemData item,float width,float u,bool draw)
        {
            var p=session.Progression;float y=8;
            // Shared item-card presentation, without inventory ownership/actions.
            if(draw)
            {
                Color rarity=GameBalance.RarityColor(item.rarity);
                Fill(new Rect(0,0,width*u,88*u),new Color(.02f,.035f,.055f));
                Rect icon=new Rect((width-50)*u,8*u,42*u,42*u);
                Fill(icon,card);
                DrawIcon(new Rect(icon.x+2*u,icon.y+2*u,38*u,38*u),UIIconAtlas.EquipmentCardIcon(item,p.Profile.heroClass),Color.white);
                Border(icon,rarity,2*u);
                string title="Lv"+item.level+"  "+item.name+" +"+p.SlotUpgradeRank(item.slot);
                float titleWidth=width-68;int titleFont=Mathf.RoundToInt(15*u);
                while(titleFont>Mathf.RoundToInt(10*u)&&Style(titleFont,true).CalcSize(new GUIContent(title)).x>titleWidth*u)titleFont--;
                Text(new Rect(8*u,8*u,titleWidth*u,42*u),title,titleFont,rarity,true,false,TextAnchor.MiddleLeft);
                if(new Rect(8*u,8*u,titleWidth*u,42*u).Contains(Mouse))tooltip=title;
                string[] tags={GameBalance.RarityName(item.rarity),"装备",GameBalance.SlotName(item.slot)};
                float tagWidth=(width-32)/3;
                for(int i=0;i<tags.Length;i++)
                {
                    Rect tag=new Rect((8+i*(tagWidth+8))*u,58*u,tagWidth*u,22*u);
                    Color tint=i==0?rarity:jade;
                    Fill(tag,new Color(tint.r,tint.g,tint.b,.12f));Border(tag,tint,u);
                    Text(tag,tags[i],Mathf.RoundToInt(11*u),pale,true,false,TextAnchor.MiddleCenter);
                }
            }
            y=96;
            if(smithCategory==2)
            {
                var cap=p.PreviewRefinementLimit(item.id);string reason=p.RefinementLockReason(item.id,SmithServiceActive);
                if(cap==null)return y;

                string[] labels={"攻击","防御","生命","暴击率","暴击伤害","攻击加成"};
                string[] current={item.attack.ToString(),item.defense.ToString(),item.health.ToString(),(item.criticalChance*100).ToString("0.##")+"%",(item.criticalDamageBonus*100).ToString("0.##")+"%",(item.attackPercent*100).ToString("0.##")+"%"};
                string[] limits={cap.attack.ToString(),cap.defense.ToString(),cap.health.ToString(),(cap.criticalChance*100).ToString("0.##")+"%",(cap.criticalDamageBonus*100).ToString("0.##")+"%",(cap.attackPercent*100).ToString("0.##")+"%"};
                var basis=p.PreviewUpgrade(item,0);var upper=p.PreviewUpgrade(cap,0);
                float[] values={basis.attack,basis.defense,basis.health,item.criticalChance,item.criticalDamageBonus,item.attackPercent};
                float[] maxima={upper.attack,upper.defense,upper.health,cap.criticalChance,cap.criticalDamageBonus,cap.attackPercent};
                for(int stat=0;stat<6;stat++)
                {
                    if(stat==0||stat==3)
                    {
                        if(stat==3)y+=8;
                        float headingY=y;
                        DrawSmithStatHeading(ref y,width,u,stat==0?"基础词条":"随机词条",draw);
                        if(draw&&new Rect(0,headingY*u,width*u,(y-headingY)*u).Contains(Event.current.mousePosition))
                            tooltip=stat==0?"显示数值包含强化加成。洗练可提升基础属性，词条重铸不影响基础属性。":"洗练提升已有随机词条数值；词条重铸会重新随机这些词条，数值可能降低或词条消失。";
                        if(stat==3&&item.criticalChance<=0&&item.criticalDamageBonus<=0&&item.attackPercent<=0)
                            GoalParagraph(ref y,width,u,"暂无随机词条",11,muted,false,draw);
                    }
                    bool present=stat==0?item.attack>0:stat==1?item.defense>0:stat==2?item.health>0:stat==3?item.criticalChance>0:stat==4?item.criticalDamageBonus>0:item.attackPercent>0;if(!present)continue;
                    if(draw){
                        float progress=maxima[stat]>0?Mathf.Clamp01(values[stat]/maxima[stat]):1;
                        bool full=values[stat]>=maxima[stat]-.000001f;
                        Color tint=Color.Lerp(new Color(.24f,.62f,.82f),new Color(.35f,.88f,.64f),Mathf.Clamp01(progress/.8f));
                        if(progress>.8f)tint=Color.Lerp(tint,gold,(progress-.8f)/.2f);
                        Rect row=new Rect(8*u,y*u,(width-16)*u,28*u);
                        Fill(row,card);
                        DrawIcon(new Rect(row.x+2*u,row.center.y-8*u,16*u,16*u),UIIconAtlas.Utility(stat==0?"attack":stat==1?"defense":stat==2?"health":"core"),jade);
                        float labelWidth=96*u;
                        Text(new Rect(row.x+22*u,row.y,labelWidth-22*u,row.height),labels[stat],Mathf.RoundToInt(11*u),muted,true,false,TextAnchor.MiddleLeft);
                        Rect track=new Rect(row.x+labelWidth+4*u,row.y+2*u,row.width-labelWidth-4*u,24*u);
                        Fill(track,new Color(.025f,.045f,.06f));
                        Fill(new Rect(track.x,track.y,track.width*progress,track.height),tint);
                        Border(track,full?gold:jade*.4f,u);
                        // A dark inset keeps light values readable over every fill amount.
                        Rect value=new Rect(track.x+2*u,track.y+2*u,track.width-4*u,track.height-4*u);
                        Fill(value,new Color(.012f,.025f,.035f,.84f));
                        Text(value,current[stat]+" / "+limits[stat]+(full?" 满":""),Mathf.RoundToInt(11*u),pale,true,false,TextAnchor.MiddleCenter);
                    }y+=32;
                }
                y+=76;
            }
            else if(smithCategory==0)
            {
                DrawSmithStatHeading(ref y,width,u,"基础词条",draw);
                int rank=p.SlotUpgradeRank(item.slot);var next=p.PreviewUpgrade(item,Mathf.Max(rank,Mathf.Min(rank+1,p.CurrentUpgradeLimit)));
                int[] before={item.attack,item.defense,item.health},after={next.attack,next.defense,next.health};
                int visibleStats=0;for(int stat=0;stat<3;stat++)if(before[stat]>0||after[stat]>0)visibleStats++;
                float statStep=MobileControls.Active?32:38,statHeight=MobileControls.Active?28:32;
                if(draw)
                {
                    int visible=0;for(int stat=0;stat<3;stat++)
                    {
                        if(before[stat]<=0&&after[stat]<=0)continue;
                        Rect row=new Rect(8*u,(y+visible++*statStep)*u,(width-16)*u,statHeight*u);
                        Fill(row,card);
                        DrawIcon(new Rect(row.x+2*u,row.center.y-8*u,16*u,16*u),UIIconAtlas.Utility(stat==0?"attack":stat==1?"defense":"health"),jade);
                        Text(new Rect(row.x+22*u,row.y,74*u,row.height),stat==0?"攻击":stat==1?"防御":"生命",Mathf.RoundToInt(11*u),muted,true,false,TextAnchor.MiddleLeft);
                        float col=(row.width-96*u)/3;
                        Text(new Rect(row.x+96*u,row.y,col,row.height),before[stat].ToString(),Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleCenter);
                        Text(new Rect(row.x+96*u+col,row.y,col,row.height),"→",Mathf.RoundToInt(14*u),muted,false,false,TextAnchor.MiddleCenter);
                        Text(new Rect(row.x+96*u+col*2,row.y,col,row.height),after[stat].ToString(),Mathf.RoundToInt(12*u),after[stat]>before[stat]?jade:muted,true,false,TextAnchor.MiddleCenter);
                    }
                }
                y+=visibleStats*statStep+8;
                if(item.criticalChance>0||item.criticalDamageBonus>0||item.attackPercent>0)
                {
                    DrawSmithStatHeading(ref y,width,u,"随机词条",draw);
                    string[] names={"暴击率","暴击伤害","攻击加成"};
                    float[] stats={item.criticalChance,item.criticalDamageBonus,item.attackPercent};
                    for(int i=0;i<stats.Length;i++)if(stats[i]>0)
                    {
                        if(draw)
                        {
                            Rect row=new Rect(8*u,y*u,(width-16)*u,28*u);Fill(row,card);
                            DrawIcon(new Rect(row.x+2*u,row.center.y-8*u,16*u,16*u),UIIconAtlas.Utility("core"),jade);
                            Text(new Rect(row.x+22*u,row.y,74*u,row.height),names[i],Mathf.RoundToInt(11*u),muted,true,false,TextAnchor.MiddleLeft);
                            Text(new Rect(row.x+96*u,row.y,(row.width-96*u)/3,row.height),(stats[i]*100).ToString("0.##")+"%",Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleCenter);
                        }
                        y+=32;
                    }
                }

            }
            else
            {

                MechanicAttachment mounted=null;
                foreach(var mechanic in BuildCatalog.GemsFor(p.Profile.heroClass))
                {
                    var a=p.Attachment(mechanic);
                    if(BuildCatalog.MechanicSlot(mechanic)==item.slot&&a!=null&&a.mounted){mounted=a;break;}
                }
                if(draw)
                {
                    float size=64,socketX=(width-size)*.5f;
                    Rect socket=new Rect(socketX*u,(y)*u,size*u,size*u);
                    Fill(socket,card);Border(socket,mounted==null?jade:GameBalance.RarityColor(mounted.rarity),2);
                    if(mounted!=null&&mounted.rarity==Rarity.Legendary)DrawFashionSlotFlow(socket,GameBalance.RarityColor(mounted.rarity),u);
                    if(mounted==null)Text(socket,"+",Mathf.RoundToInt(32*u),jade,false,false,TextAnchor.MiddleCenter);
                    else {Rect gemIcon=new Rect(socket.x+8*u,socket.y+8*u,48*u,48*u);DrawIcon(gemIcon,UIIconAtlas.Utility("gem"),GameBalance.RarityColor(mounted.rarity));DrawWornIconBadge(gemIcon,u,"已镶嵌");}
                    if(QuietAction(socket,"",SmithServiceActive)){smithSelectedSlot=(int)item.slot;smithSocketPrevious=mounted==null?EquipmentMechanic.None:mounted.mechanic;smithSocketPicker=true;smithSocketScroll=Vector2.zero;}
                    if(mounted!=null)
                    {
                        Text(new Rect(socket.xMax+8*u,socket.y,Mathf.Max(64*u,(width*u-socket.xMax-12*u)),64*u),mounted.upgradeRank+"阶\n升华 "+mounted.ascensionRank,Mathf.RoundToInt(12*u),gold,true,false,TextAnchor.MiddleLeft);
                        float actionHeight=MobileControls.Active?44:32,actionWidth=Mathf.Min(112,(width-24)/2);
                        float actionX=(width-actionWidth*2-8)*.5f;
                        for(int action=0;action<2;action++)
                        {
                            bool ascend=action==0;
                            string reason=ascend?p.AttachmentAscensionLock(mounted.mechanic,SmithServiceActive):p.AttachmentUpgradeLock(mounted.mechanic,SmithServiceActive);
                            Rect button=new Rect((actionX+action*(actionWidth+8))*u,(y+72)*u,actionWidth*u,actionHeight*u);
                            string caption=ascend?(mounted.ascensionRank>=3?"升华已满":"升华"):(mounted.upgradeRank>=9?"已满阶级":"升阶");
                            if(Button(button,caption,ascend?gold:jade,reason.Length==0))OpenGemPreview(mounted.mechanic,ascend);
                        }
                        string ascendReason=p.AttachmentAscensionLock(mounted.mechanic,SmithServiceActive);
                        if(ascendReason.Length>0)Text(new Rect(8*u,(y+120)*u,(width-16)*u,40*u),ascendReason,Mathf.RoundToInt(11*u),muted,false,true,TextAnchor.MiddleCenter);
                    }
                }
                y+=mounted==null?76:170;
                if(mounted!=null)
                {
                    string effect=BuildCatalog.IsAttributeGem(mounted.mechanic)?BuildCatalog.GemAttributeSummary(mounted.mechanic,mounted.rarity,mounted.upgradeRank):BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(mounted.mechanic))+" +"+(BuildCatalog.MechanicAttributeValue(mounted.mechanic,mounted.upgradeRank)*100).ToString("0.#")+"%";
                    if(MobileControls.Active){if(draw)DrawGemEffectText(new Rect(8*u,y*u,(width-16)*u,26*u),effect,Mathf.RoundToInt(12*u),jade,true,TextAnchor.MiddleLeft);y+=26;}
                    else GoalParagraph(ref y,width,u,effect,12,jade,true,draw);
                }
                if(mounted!=null)
                {
                    if(BuildCatalog.HasMechanicVariant(mounted.mechanic))
                    {
                        GoalParagraph(ref y,width,u,"机制形态",13,muted,false,draw);
                        bool compact=MobileControls.Active&&!MobileControls.IsIPad;
                        bool stackedVariants=width<420&&!compact;
                        float variantWidth=stackedVariants?width-16:width/2-16;
                        float variantHeight=220,variantTextHeight=variantHeight-92;
                        if(draw)for(int variant=0;variant<2;variant++)
                        {
                            Rect option=new Rect((stackedVariants?8:8+variant*(width/2))*u,(y+(stackedVariants?variant*(variantHeight+8):0))*u,variantWidth*u,variantHeight*u);
                            bool selected=mounted.variantUnlocked&&mounted.variant==variant;
                            Fill(option,card);Border(option,selected?jade:muted,selected?2:1);
                            DrawIcon(new Rect(option.x+10*u,option.y+12*u,36*u,36*u),UIIconAtlas.Utility(variant==0?"core":"attack"),selected?jade:pale);
                            Text(new Rect(option.x+50*u,option.y,option.width-54*u,56*u),SmithVariantName(mounted.mechanic,variant),Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleLeft);
                            DrawIcon(new Rect(option.xMax-18*u,option.y,18*u,18*u),UIIconAtlas.Utility(selected?"confirm":mounted.variantUnlocked?"help":"lock"),selected?jade:gold);
                            string scrollKey="smith-form-"+mounted.mechanic+"-"+variant,description=SmithVariantDescription(mounted.mechanic,variant);
                            Vector2 offset;smithVariantScroll.TryGetValue(scrollKey,out offset);
                            Rect descriptionArea=new Rect(option.x+10*u,option.y+56*u,option.width-20*u,variantTextHeight*u);
                            float textWidth=descriptionArea.width-16*u,textHeight=Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(description),textWidth);
                            smithVariantScroll[scrollKey]=BeginTouchScroll(scrollKey,descriptionArea,offset,new Rect(0,0,textWidth,Mathf.Max(descriptionArea.height,textHeight)));
                            Text(new Rect(0,0,textWidth,textHeight),description,Mathf.RoundToInt(12*u),pale,false,true);
                            EndTouchScroll();
                            Text(new Rect(option.x+10*u,option.yMax-28*u,option.width-20*u,24*u),selected?"当前形态":mounted.variantUnlocked?"点击切换":"3阶首次升华解锁",Mathf.RoundToInt(12*u),selected?jade:gold,true);
                            if(QuietAction(option,"",!selected&&mounted.variantUnlocked&&SmithServiceActive)){SelectSmithVariant(mounted.mechanic,variant);}
                        }
                        y+=(variantHeight+8)*(stackedVariants?2:1);
                    }
                }

            }
            return y+8;
        }
        private void DrawAffixConfirmation(float u)
        {
            var quote=smithAffixQuote;if(quote==null)return;var p=session.Progression;
            float w=Mathf.Min(440*u,width-24*u),h=210*u;Rect box=new Rect((width-w)*.5f,(height-h)*.5f,w,h);
            blockedRects.Add(box);Fill(box,ink);Border(box,gold);
            Text(new Rect(box.x+16*u,box.y+12*u,w-76*u,32*u),"确认词条重铸",Mathf.RoundToInt(17*u),gold,true);
            if(PopupCloseButton(new Rect(box.xMax-48*u,box.y+8*u,44*u,36*u))){smithAffixQuote=null;return;}
            bool fresh=quote.Owner==p&&quote.Slot==p.CurrentSlotId&&quote.Fingerprint==p.BuildStateFingerprint();
            Text(new Rect(box.x+16*u,box.y+60*u,w-32*u,80*u),fresh?"消耗词条重铸石 ×1\n随机词条数值可能降低或者词条消失":"装备或材料已变化，请取消后重新确认。",Mathf.RoundToInt(15*u),pale,false,true);
            if(Button(new Rect(box.x+16*u,box.yMax-54*u,(w-40*u)*.5f,44*u),"取消",muted)){smithAffixQuote=null;return;}
            if(Button(new Rect(box.center.x+4*u,box.yMax-54*u,(w-40*u)*.5f,44*u),"确认重铸",gold,fresh&&p.AffixReforgeLockReason(quote.Id,SmithServiceActive).Length==0))
            {bool saved=p.ApplyAffixReforge(quote,SmithServiceActive);if(saved)smithAffixQuote=null;ShowAffixReforgeResult(saved,quote.Id);}
        }

    }
}
