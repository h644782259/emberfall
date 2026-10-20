using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool MerchantPriceButton(Rect r,string action,int price,bool shards,float u,bool enabled)
        {
            bool selling=merchantMode==2;
            bool confirming=selling&&action=="确认出售";
            bool clicked=DrawButton(r,"",selling?ButtonRole.Danger:ButtonRole.Primary,enabled,enabled?null:action);
            Color textColor=enabled?pale:muted;
            string amount=(selling?"+":"")+Money(price);int font=Mathf.RoundToInt(14*u);
            float amountWidth=Style(font,true,false).CalcSize(new GUIContent(amount)).x;
            float iconSize=18*u,gap=5*u,markerWidth=selling?18*u:0;
            float start=r.center.x-(markerWidth+iconSize+gap+amountWidth)*.5f;
            if(selling)
            {
                if(confirming)DrawIcon(new Rect(start,r.center.y-9*u,18*u,18*u),UIIconAtlas.Utility("confirm"),gold);
                else Text(new Rect(start,r.y,markerWidth,r.height),"↑",font,enabled?new Color(1f,.76f,.72f):muted,true,false,TextAnchor.MiddleCenter);
                start+=markerWidth;
            }
            DrawIcon(new Rect(start,r.center.y-iconSize*.5f,iconSize,iconSize),UIIconAtlas.Utility(shards?"shard":"coin"),Color.white);
            Text(new Rect(start+iconSize+gap,r.y,amountWidth,r.height),amount,font,textColor,true,false,TextAnchor.MiddleLeft);
            return clicked;
        }
        private void DrawMerchantItemIcon(Rect slot,Texture2D icon,Color tint,bool legendary,float u)
        {
            if(legendary)
            {
                // Same closed rarity frame and travelling edge light as bag supply slots.
                Fill(slot,card);Border(slot,tint);DrawFashionSlotFlow(slot,tint,u);
                slot=new Rect(slot.x+4*u,slot.y+3*u,slot.width-8*u,slot.height-6*u);
            }
            DrawIcon(slot,icon,tint);
        }
        private bool MerchantRarityTab(Rect r,Rarity rarity,float u)
        {
            bool selected=merchantGemRarity==rarity;Color tint=GameBalance.RarityColor(rarity);
            Fill(r,selected?new Color(.10f,.18f,.22f):new Color(.04f,.075f,.10f));
            if(selected)Fill(new Rect(r.x+8*u,r.yMax-2*u,r.width-16*u,2*u),tint);
            Text(r,GameBalance.RarityName(rarity),Mathf.RoundToInt(12*u),selected?tint:muted,true,false,TextAnchor.MiddleCenter);
            return GUI.Button(r,GUIContent.none,invisibleButton);
        }
        private int merchantMode,merchantSelection=-1;
        private EquipmentMechanic merchantGemSaleConfirmation;
        private float merchantActionUntil=-1;
        private Vector2 merchantGridScroll;
        private void SelectMerchantMode(int mode,bool force=false)
        {if(!force&&merchantMode==mode)return;merchantQuantityKind=-1;entryRewardSelection=null;merchantGemSaleConfirmation=EquipmentMechanic.None;merchantMode=mode;merchantSelection=mode==2?-1:0;merchantGridScroll=Vector2.zero;}
        private bool StartMerchantAction()
        {if(Time.unscaledTime<merchantActionUntil)return false;merchantActionUntil=Time.unscaledTime+.35f;return true;}
        private Rarity merchantGemRarity=Rarity.Common;
        private void DrawMerchantService()
        {
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            HandleMerchantQuantityPointer(u);
            bool prior=GUI.enabled;GUI.enabled=prior&&merchantQuantityKind<0;
            var l=new MerchantServiceLayout(width/u,height/u,MobileControls.IsIPad);
            blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(l.Frame,u),jade,false);
            Text(BuildPlanRect(l.Header,u),"商人",Mathf.RoundToInt(22*u),pale,true);
            DrawServiceBalances(BuildPlanRect(l.Balance,u),u);
            if(PopupCloseButton(BuildPlanRect(l.Close,u))){ClosePanel();return;}
            string[] tabs={"购买","兑换","出售"};
            for(int mode=0;mode<tabs.Length;mode++)if(TabButton(BuildPlanRect(l.Tab(mode),u),tabs[mode],merchantMode==mode))SelectMerchantMode(mode);
            if(!MerchantServiceActive){Text(BuildPlanRect(l.Body,u),"返回营地后可使用商店。",Mathf.RoundToInt(16*u),muted);GUI.enabled=prior;return;}
            if(merchantMode==2)
            {
                var bulk=p.PrepareMerchantBulkSale(MerchantServiceActive);
                Text(BuildPlanRect(l.Info,u),bulk==null?"暂无可一键出售的装备":"未锁定且评分更低 · "+bulk.Count+"件 · +"+bulk.Gold+"金币",Mathf.RoundToInt(12*u),muted,false,true);
                if(DrawButton(BuildPlanRect(l.Action,u),"一键出售",ButtonRole.Primary,bulk!=null&&Time.unscaledTime>=merchantActionUntil,fontSize:Mathf.RoundToInt(15*u))&&StartMerchantAction())
                {bool sold=p.SellAtMerchant(bulk,MerchantServiceActive);MerchantFeedback(sold,"已出售 "+bulk.Count+" 件 · +"+bulk.Gold+" 金币");if(sold){RebuildBagItems();ResolveSelectedItem();}}
            }
            var mechanics=BuildCatalog.DroppableGemsFor(p.Profile.heroClass);
            var saleItems=new List<ItemData>();
            if(merchantMode==2)foreach(var gear in p.Profile.inventory)if(gear!=null)saleItems.Add(gear);
            var saleGems=new List<MechanicAttachment>();
            if(merchantMode==2)foreach(var gem in p.Profile.attachments)if(gem!=null)saleGems.Add(gem);
            var saleOrder=new List<int>();
            if(merchantMode==2)for(int group=0;group<2;group++)
            {
                bool available=group==0;
                for(int i=0;i<saleItems.Count;i++)if(CanSellMerchantEquipment(saleItems[i])==available)saleOrder.Add(i);
                for(int i=0;i<saleGems.Count;i++)if((p.GemSaleLock(saleGems[i].mechanic,MerchantServiceActive).Length==0)==available)saleOrder.Add(saleItems.Count+i);
            }
            int count=merchantMode==0?4:merchantMode==1?3:saleItems.Count+saleGems.Count;
            if(merchantMode==2)merchantSelection=-1;
            else merchantSelection=Mathf.Clamp(merchantSelection,0,Mathf.Max(0,count-1));
            float contentHeight=l.GridHeight(count,true)+(merchantMode==1?((count+l.Columns-1)/l.Columns)*28:0);
            merchantGridScroll=BeginTouchScroll("merchant-"+merchantMode,BuildPlanRect(l.Body,u),merchantGridScroll,new Rect(0,0,l.Body.Width*u,Mathf.Max(l.Body.Height,contentHeight)*u));
            for(int index=0;index<count;index++)
            {
                int itemIndex=merchantMode==2?saleOrder[index]:index;
                var area=l.Tile(index,true);Rect tile=new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);
                if(merchantMode==1){tile.y+=(index/l.Columns)*28*u;tile.height+=28*u;}
                bool selected=false;
                Fill(tile,selected?new Color(.12f,.22f,.24f):card);Border(tile,selected?gold:muted*.4f);
                if(selected){Border(new Rect(tile.x+2*u,tile.y+2*u,tile.width-4*u,tile.height-4*u),gold);DrawIcon(new Rect(tile.xMax-25*u,tile.y+4*u,20*u,20*u),UIIconAtlas.Utility("confirm"),gold);}
                if(merchantMode==1)
                {
                    var slot=(ItemSlot)index;var bag=GemBagPreview(slot,false);string reason=p.GemBagPurchaseLock(slot,MerchantServiceActive);
                    DrawEntryRewardIcon(new Rect(tile.center.x-30*u,tile.y+12*u,60*u,60*u),bag,u);
                    Text(new Rect(tile.x+6*u,tile.y+78*u,tile.width-12*u,26*u),bag.Name,Mathf.RoundToInt(12*u),gold,true,false,TextAnchor.MiddleCenter);
                    InspectRewardItem(new Rect(tile.x,tile.y,tile.width,102*u),bag,true);
                    if(MerchantPriceButton(new Rect(tile.x+6*u,tile.y+105*u,tile.width-12*u,44*u),reason.Length==0?"兑换":reason,ProgressionService.GemBagPrice,true,u,reason.Length==0&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())MerchantFeedback(p.BuyGemBag(slot,MerchantServiceActive),"兑换成功 · "+GameBalance.SlotName(slot)+"宝石袋 ×1，已放入背包");
                    continue;
                }
                if(merchantMode==0&&index==3)
                {
                    var purchase=p.PrepareRevivalPurchase(MerchantServiceActive,captureState:false);var preview=ResourceItemPreview("revival",1);
                    DrawEntryRewardIcon(new Rect(tile.center.x-23*u,tile.y+6*u,46*u,46*u),preview,u);
                    Text(new Rect(tile.x+6*u,tile.y+52*u,tile.width-12*u,24*u),"复活石",Mathf.RoundToInt(11*u),pale,false,true,TextAnchor.MiddleCenter);
                    InspectRewardItem(new Rect(tile.x,tile.y,tile.width,76*u),preview,true);
                    if(MerchantPriceButton(new Rect(tile.x+6*u,tile.y+77*u,tile.width-12*u,44*u),purchase==null?"无法购买":"购买",ProgressionService.RevivalStonePrice,false,u,purchase!=null&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())OpenMerchantQuantity(4,EquipmentMechanic.None,Rarity.Epic,"复活石");
                    continue;
                }
                if(merchantMode==0&&index==2)
                {
                    var purchase=p.PrepareAffixReforgePurchase(MerchantServiceActive,captureState:false);var preview=ResourceItemPreview("affix-reforge",1);
                    DrawMerchantItemIcon(new Rect(tile.center.x-23*u,tile.y+6*u,46*u,46*u),preview.Icon,preview.QualityColor,true,u);
                    Text(new Rect(tile.x+6*u,tile.y+52*u,tile.width-12*u,24*u),"词条重铸石",Mathf.RoundToInt(11*u),gold,false,true,TextAnchor.MiddleCenter);
                    InspectRewardItem(new Rect(tile.x,tile.y,tile.width,76*u),preview,true);
                    if(MerchantPriceButton(new Rect(tile.x+6*u,tile.y+77*u,tile.width-12*u,44*u),p.Profile.affixReforgeStones>=999999?"数量已满":purchase==null?"金币不足":"购买",ProgressionService.AffixReforgeStonePrice,false,u,purchase!=null&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())OpenMerchantQuantity(2,EquipmentMechanic.None,Rarity.Legendary,"词条重铸石");
                    continue;
                }
                if(merchantMode==0&&index>0)
                {
                    int quantity=1;
                    var purchase=p.PrepareRefinementPurchase(MerchantServiceActive,quantity,captureState:false);
                    Color tint=GameBalance.RarityColor(Rarity.Epic);
                    DrawIcon(new Rect(tile.center.x-23*u,tile.y+6*u,46*u,46*u),UIIconAtlas.Utility("refinement"),tint);
                    Text(new Rect(tile.x+6*u,tile.y+52*u,tile.width-12*u,24*u),"装备洗练石",Mathf.RoundToInt(11*u),pale,false,true,TextAnchor.MiddleCenter);
                    InspectRewardItem(new Rect(tile.x,tile.y,tile.width,76*u),new EntryRewardPreview{Key="merchant:refinement:"+quantity,Name="装备洗练石",Rarity=Rarity.Epic,Tint=tint,Icon=UIIconAtlas.Utility("refinement"),Description="装备洗练石\n数量  "+quantity+"\n用于铁匠洗练，提高装备属性数值。"},true);
                    string reason=p.Profile.refinementStones>999999-quantity?"洗练石已满":purchase==null?"金币不足":"购买";
                    if(MerchantPriceButton(new Rect(tile.x+6*u,tile.y+77*u,tile.width-12*u,44*u),reason,quantity*ProgressionService.RefinementStonePrice,false,u,purchase!=null&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())
                        OpenMerchantQuantity(1,EquipmentMechanic.None,Rarity.Epic,"装备洗练石");
                    continue;
                }
                if(merchantMode==2&&itemIndex>=saleItems.Count)
                {
                    var gem=saleGems[itemIndex-saleItems.Count];Color tint=GameBalance.RarityColor(gem.rarity);
                    Rect gemIcon=new Rect(tile.center.x-23*u,tile.y+6*u,46*u,46*u);
                    DrawMerchantItemIcon(gemIcon,UIIconAtlas.Utility("gem"),tint,gem.rarity==Rarity.Legendary,u);
                    if(gem.mounted)DrawWornIconBadge(gemIcon,u,"已镶嵌");
                    Text(new Rect(tile.x+6*u,tile.y+52*u,tile.width-12*u,24*u),BuildCatalog.GemName(gem.mechanic),Mathf.RoundToInt(11*u),pale,false,true,TextAnchor.MiddleCenter);
                    InspectRewardItem(new Rect(tile.x,tile.y,tile.width,76*u),new EntryRewardPreview{Key="merchant:gem:sale:"+gem.mechanic,Name=BuildCatalog.GemName(gem.mechanic),Rarity=gem.rarity,Tint=tint,Icon=UIIconAtlas.Utility("gem"),Description=GemRewardDescription(gem.mechanic,gem.rarity,gem.upgradeRank)},true);
                    string reason=p.GemSaleLock(gem.mechanic,MerchantServiceActive);
                    bool confirming=merchantGemSaleConfirmation==gem.mechanic;
                    if(MerchantPriceButton(new Rect(tile.x+6*u,tile.y+77*u,tile.width-12*u,44*u),reason.Length>0?(gem.mounted?"已镶嵌":reason):confirming?"确认出售":"出售",p.GemSellValue(gem.mechanic),true,u,reason.Length==0)&&StartMerchantAction())
                    {if(confirming){MerchantFeedback(p.SellGem(gem.mechanic,MerchantServiceActive),"宝石已出售");merchantGemSaleConfirmation=EquipmentMechanic.None;}else merchantGemSaleConfirmation=gem.mechanic;}
                    continue;
                }
                ItemData item=merchantMode==2?saleItems[itemIndex]:null;
                EquipmentMechanic mechanic=merchantMode==1?mechanics[index]:EquipmentMechanic.None;
                bool owned=mechanic!=EquipmentMechanic.None&&p.Attachment(mechanic)!=null&&p.Attachment(mechanic).rarity>=merchantGemRarity;
                var quote=item==null?p.PrepareMerchantPurchase(mechanic,MerchantServiceActive,merchantGemRarity,captureState:false):null;
                bool first=p.Profile.pendingFirstClearReward&&!p.Profile.firstClearRewardClaimed&&mechanic!=EquipmentMechanic.None&&merchantGemRarity==Rarity.Epic;
                int price=item!=null?p.SellValue(item):mechanic==EquipmentMechanic.None?ProgressionService.PotionPrice:first?0:BuildCatalog.GemPrice(merchantGemRarity);
                string caption=item!=null?item.name:mechanic==EquipmentMechanic.None?"生命药剂":BuildCatalog.GemName(mechanic);
                DrawMerchantItemIcon(new Rect(tile.center.x-23*u,tile.y+(merchantMode==1?26:6)*u,46*u,46*u),item!=null?UIIconAtlas.EquipmentCardIcon(item,session.Progression.Profile.heroClass):UIIconAtlas.Utility(mechanic==EquipmentMechanic.None?"potion":"gem"),item!=null?GameBalance.RarityColor(item.rarity):merchantMode==1?GameBalance.RarityColor(merchantGemRarity):Color.white,item!=null?item.rarity==Rarity.Legendary:mechanic!=EquipmentMechanic.None&&merchantGemRarity==Rarity.Legendary,u);
                if(item!=null&&IsEquipped(item))DrawWornIconBadge(new Rect(tile.center.x-23*u,tile.y+6*u,46*u,46*u),u,"已穿戴");
                Text(new Rect(tile.x+6*u,tile.y+(merchantMode==1?74:52)*u,tile.width-12*u,24*u),caption,Mathf.RoundToInt(11*u),pale,false,true,TextAnchor.MiddleCenter);
                if(merchantMode==1)
                {
                    string stats=BuildCatalog.IsAttributeGem(mechanic)?BuildCatalog.GemAttributeSummary(mechanic,merchantGemRarity,0):BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(mechanic))+" +"+(BuildCatalog.MechanicAttributeValue(mechanic,0)*100).ToString("0.#")+"%";
                    DrawDetailTag(new Rect(tile.x+4*u,tile.y+4*u,38*u,18*u),GameBalance.SlotName(BuildCatalog.MechanicSlot(mechanic)),jade,u);
                    DrawDetailTag(new Rect(tile.xMax-42*u,tile.y+4*u,38*u,18*u),GameBalance.RarityName(merchantGemRarity),GameBalance.RarityColor(merchantGemRarity),u);
                    DrawGemEffectText(new Rect(tile.x+6*u,tile.y+100*u,tile.width-12*u,30*u),"基础 "+stats+"\n"+(BuildCatalog.IsAttributeGem(mechanic)?"属性宝石":"机制宝石"),Mathf.RoundToInt(10*u),jade,true,TextAnchor.MiddleCenter);
                }
                EntryRewardPreview detail;
                if(item!=null){detail=ActualEquipmentPreview(item);detail.AllowActions=true;detail.Key="merchant:equipment:"+item.id;}
                else if(mechanic!=EquipmentMechanic.None)detail=new EntryRewardPreview{Key="merchant:gem:buy:"+mechanic+":"+merchantGemRarity,Name=caption,Rarity=merchantGemRarity,Tint=GameBalance.RarityColor(merchantGemRarity),Icon=UIIconAtlas.Utility("gem"),Description=GemRewardDescription(mechanic,merchantGemRarity)};
                else detail=new EntryRewardPreview{Key="merchant:potion",Name="生命药剂",Rarity=Rarity.Common,Tint=jade,Icon=UIIconAtlas.Utility("potion"),Description="生命药剂\n恢复50%生命"};
                InspectRewardItem(new Rect(tile.x,tile.y,tile.width,(merchantMode==1?130:76)*u),detail,true);
                if(item!=null)
                {
                    Rect sell=new Rect(tile.x+6*u,tile.y+77*u,tile.width-12*u,44*u);
                    bool canSell=CanSellMerchantEquipment(item);string sellCaption=canSell?"出售":item.locked?"已锁定":IsEquipped(item)?"已穿戴":"无法出售";
                    if(MerchantPriceButton(sell,sellCaption,price,false,u,canSell&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())
                    {string id=item.id;RebuildBagItems();SellInventoryItem(id);}
                }
                else
                {
                    string captionAction=first?(owned?"领取 · +3碎片":"领取首通宝石"):owned?"已拥有":quote!=null?merchantMode==0?"购买":p.Attachment(mechanic)!=null?"兑换 · 提升品质":"兑换":merchantMode==0?p.Profile.potions>=99?"药剂已满":"金币不足":"碎片不足";
                    Rect action=new Rect(tile.x+6*u,tile.y+(merchantMode==1?131:77)*u,tile.width-12*u,44*u);
                    if(MerchantPriceButton(action,captionAction,price,merchantMode==1,u,quote!=null&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())
                    {
                        if(mechanic==EquipmentMechanic.None)OpenMerchantQuantity(0,mechanic,merchantGemRarity,caption);
                        else MerchantFeedback(p.BuyAtMerchant(p.PrepareMerchantPurchase(mechanic,MerchantServiceActive,merchantGemRarity),MerchantServiceActive),(first?"领取成功 · ":"兑换成功 · ")+caption);
                    }

                }
            }
            EndTouchScroll();
            Rect info=BuildPlanRect(l.Info,u);
            if(merchantMode==2&&count==0)Text(BuildPlanRect(l.Body,u),"没有可出售物品。",Mathf.RoundToInt(16*u),jade);
            GUI.enabled=prior;DrawMerchantQuantity(u);DrawMerchantFeedback(u);
        }

        private string merchantFeedbackText,merchantFeedbackSlot;
        private bool merchantFeedbackSuccess;
        private float merchantFeedbackUntil;
        private ProgressionService merchantFeedbackOwner;
        private void MerchantFeedback(bool success,string message)
        {
            Feedback(success,message);
            merchantFeedbackText=session.Notification;
            merchantFeedbackSuccess=success;merchantFeedbackUntil=Time.unscaledTime+4.5f;
            merchantFeedbackOwner=session.Progression;merchantFeedbackSlot=session.Progression.CurrentSlotId;
            if(success)GameAudio.Play(SoundCue.Loot);
        }
        private void DrawMerchantFeedback(float u)
        {
            if(string.IsNullOrEmpty(merchantFeedbackText)||Time.unscaledTime>=merchantFeedbackUntil||
                merchantFeedbackOwner!=session.Progression||merchantFeedbackSlot!=session.Progression.CurrentSlotId)return;
            float w=Mathf.Min(480*u,width-32*u);
            float h=Mathf.Max(54*u,Style(Mathf.RoundToInt(14*u),true,true).CalcHeight(new GUIContent(merchantFeedbackText),w-64*u)+20*u);
            Rect box=new Rect((width-w)*.5f,height-h-20*u,w,h);
            Color accent=merchantFeedbackSuccess?jade:new Color(1f,.42f,.36f);
            Fill(box,merchantFeedbackSuccess?new Color(.035f,.16f,.13f,.98f):new Color(.22f,.06f,.06f,.98f));Border(box,accent,2*u);
            DrawIcon(new Rect(box.x+12*u,box.center.y-12*u,24*u,24*u),UIIconAtlas.Utility(merchantFeedbackSuccess?"confirm":"cancel"),accent);
            Text(new Rect(box.x+46*u,box.y+10*u,w-58*u,h-20*u),merchantFeedbackText,Mathf.RoundToInt(14*u),pale,true,true,TextAnchor.MiddleLeft);
        }

        private bool CanSellMerchantEquipment(ItemData item)
        {return item!=null&&MerchantServiceActive&&!session.Progression.IsPracticeOnly&&!item.locked&&!IsEquipped(item);}
        private int merchantQuantityKind=-1,merchantQuantity=1;
        private string merchantQuantityName,merchantQuantityText="1",merchantQuantityFingerprint,merchantQuantitySlot;
        private ProgressionService merchantQuantityOwner;
        private EquipmentMechanic merchantQuantityMechanic;
        private Rarity merchantQuantityRarity;
        private void OpenMerchantQuantity(int kind,EquipmentMechanic mechanic,Rarity rarity,string name)
        {
            merchantQuantityKind=kind;merchantQuantityMechanic=mechanic;merchantQuantityRarity=rarity;merchantQuantityName=name;
            merchantQuantity=1;merchantQuantityText="1";merchantQuantitySlot=session.Progression.CurrentSlotId;merchantQuantityOwner=session.Progression;merchantQuantityFingerprint=merchantQuantityOwner.BuildStateFingerprint();
            entryRewardSelection=entryRewardHoverKey=null;entryRewardPopupVisible=false;
        }
        private int MerchantQuantityMaximum()
        {
            var p=session.Progression;
            return Mathf.Max(1,merchantQuantityKind==0?Mathf.Min(99-p.Profile.potions,p.Profile.gold/ProgressionService.PotionPrice):
                merchantQuantityKind==1?Mathf.Min(999999-p.Profile.refinementStones,p.Profile.gold/ProgressionService.RefinementStonePrice):
                merchantQuantityKind==4?Mathf.Min(999999-p.Profile.revivalStones,p.Profile.gold/ProgressionService.RevivalStonePrice):
                merchantQuantityKind==2?Mathf.Min(999999-p.Profile.affixReforgeStones,p.Profile.gold/ProgressionService.AffixReforgeStonePrice):1);
        }
        private Rect MerchantQuantityBox(float u)
        {float w=Mathf.Min(420*u,width-24*u),h=260*u;return new Rect((width-w)*.5f,(height-h)*.5f,w,h);}
        private void SetMerchantQuantity(int value)
        {
            merchantQuantity=Mathf.Clamp(value,1,MerchantQuantityMaximum());merchantQuantityText=merchantQuantity.ToString();
            GUI.FocusControl(null);GUIUtility.keyboardControl=0;
        }
        private void HandleMerchantQuantityPointer(float u)
        {
            // Claim the overlay press before the underlying shop scroll view sees it.
            var e=Event.current;
            if(merchantQuantityKind<0||!GUI.enabled||e.type!=EventType.MouseDown||e.button!=0)return;
            Rect box=MerchantQuantityBox(u);float y=box.y+66*u;Vector2 pointer=e.mousePosition;
            if(new Rect(box.x+16*u,y,44*u,44*u).Contains(pointer))SetMerchantQuantity(merchantQuantity-1);
            else if(new Rect(box.xMax-60*u,y,44*u,44*u).Contains(pointer))SetMerchantQuantity(merchantQuantity+1);
            else if(new Rect(box.x+16*u,y+52*u,80*u,36*u).Contains(pointer))SetMerchantQuantity(MerchantQuantityMaximum());
            else return;
            GUIUtility.hotControl=0;e.Use();
        }
        private void DrawMerchantQuantity(float u)
        {
            if(merchantQuantityKind<0)return;var p=session.Progression;
            bool fresh=p==merchantQuantityOwner&&p.CurrentSlotId==merchantQuantitySlot&&p.BuildStateFingerprint()==merchantQuantityFingerprint;
            int maximum=MerchantQuantityMaximum();
            Rect box=MerchantQuantityBox(u);float w=box.width;
            blockedRects.Add(box);Fill(box,ink);Border(box,gold);
            Text(new Rect(box.x+16*u,box.y+12*u,w-76*u,32*u),merchantQuantityName,Mathf.RoundToInt(17*u),gold,true);
            if(PopupCloseButton(new Rect(box.xMax-48*u,box.y+8*u,44*u,36*u))){merchantQuantityKind=-1;return;}
            float rowY=box.y+66*u;
            if(Button(new Rect(box.x+16*u,rowY,44*u,44*u),"−",jade,merchantQuantity>1))SetMerchantQuantity(merchantQuantity-1);
            Rect number=new Rect(box.x+68*u,rowY,w-136*u,44*u);
            Surface(number,new Color(.008f,.018f,.028f));SurfaceFrame(number,jade*.4f);
            var numberStyle=new GUIStyle(Style(Mathf.RoundToInt(24*u),true,false));
            numberStyle.alignment=TextAnchor.MiddleCenter;numberStyle.normal.textColor=pale;
            numberStyle.padding=new RectOffset(4,4,0,0);
            merchantQuantityText=GUI.TextField(number,merchantQuantityText,7,numberStyle);
            int parsed;bool validInput=int.TryParse(merchantQuantityText,out parsed)&&parsed>=1&&parsed<=maximum;if(validInput)merchantQuantity=parsed;
            if(Button(new Rect(box.xMax-60*u,rowY,44*u,44*u),"+",jade,merchantQuantity<maximum))SetMerchantQuantity(merchantQuantity+1);
            if(Button(new Rect(box.x+16*u,rowY+52*u,80*u,36*u),"最大",jade))SetMerchantQuantity(maximum);
            Text(new Rect(box.x+104*u,rowY+52*u,w-120*u,36*u),"数量 "+merchantQuantity+" / "+maximum,Mathf.RoundToInt(13*u),pale,false,false,TextAnchor.MiddleRight);
            validInput=int.TryParse(merchantQuantityText,out parsed)&&parsed>=1&&parsed<=maximum;
            var quote=!fresh||!validInput?null:merchantQuantityKind==4?p.PrepareRevivalPurchase(MerchantServiceActive,merchantQuantity):merchantQuantityKind==2?p.PrepareAffixReforgePurchase(MerchantServiceActive,merchantQuantity):merchantQuantityKind==1?p.PrepareRefinementPurchase(MerchantServiceActive,merchantQuantity):p.PrepareMerchantPurchase(merchantQuantityMechanic,MerchantServiceActive,merchantQuantityRarity,merchantQuantity);
            bool first=p.Profile.pendingFirstClearReward&&!p.Profile.firstClearRewardClaimed&&merchantQuantityRarity==Rarity.Epic;
            int unitPrice=merchantQuantityKind==4?ProgressionService.RevivalStonePrice:merchantQuantityKind==0?ProgressionService.PotionPrice:merchantQuantityKind==1?ProgressionService.RefinementStonePrice:merchantQuantityKind==2?ProgressionService.AffixReforgeStonePrice:first?0:BuildCatalog.GemPrice(merchantQuantityRarity);
            DrawPrice(new Rect(box.x+16*u,box.yMax-92*u,w-32*u,28*u),unitPrice*merchantQuantity,merchantQuantityKind==3,u);
            if(Button(new Rect(box.x+16*u,box.yMax-54*u,(w-40*u)*.5f,44*u),"取消",muted)){merchantQuantityKind=-1;return;}
            if(Button(new Rect(box.center.x+4*u,box.yMax-54*u,(w-40*u)*.5f,44*u),fresh?"确认交易":"请重新选择",gold,quote!=null))
            {bool saved=p.BuyAtMerchant(quote,MerchantServiceActive);MerchantFeedback(saved,"购买成功 · "+merchantQuantityName+" ×"+merchantQuantity);if(saved)merchantQuantityKind=-1;}
        }
        private bool ServiceCostAction(Rect r,string caption,int cost,bool material,float u,bool enabled,string reason=null)
        {
            bool clicked=Button(r,"",jade,enabled,reason);
            Text(new Rect(r.x+10*u,r.y,r.width-104*u,r.height),caption,Mathf.RoundToInt(13*u),enabled?pale:muted,true,false,TextAnchor.MiddleLeft);
            DrawPriceTint(new Rect(r.xMax-92*u,r.y+6*u,82*u,r.height-12*u),cost,material,u,enabled?gold:muted);
            return clicked;
        }
        private void DrawServiceBalances(Rect r,float unit)
        {
            float half=r.width*.5f;
            DrawPrice(new Rect(r.x,r.y,half-4*unit,r.height),session.Progression.Profile.gold,false,unit);
            DrawPrice(new Rect(r.x+half,r.y,half,r.height),session.Progression.Profile.mechanicMaterials,true,unit);
            InspectRewardItem(new Rect(r.x,r.y,half-4*unit,r.height),ResourceItemPreview("gold",session.Progression.Profile.gold));
            InspectRewardItem(new Rect(r.x+half,r.y,half,r.height),ResourceItemPreview("shard",session.Progression.Profile.mechanicMaterials));
        }
        private void DrawPrice(Rect r,int amount,bool material,float unit)
        {DrawPriceTint(r,amount,material,unit,gold);}
        private void DrawPriceTint(Rect r,int amount,bool material,float unit,Color tint)
        {
            Fill(r,new Color(.14f,.115f,.055f,.55f));
            Border(r,new Color(tint.r,tint.g,tint.b,.25f));
            DrawIcon(new Rect(r.x+4*unit,r.center.y-9*unit,18*unit,18*unit),UIIconAtlas.Utility(material?"shard":"coin"),material?GameBalance.RarityColor(Rarity.Rare):gold);
            string value=Money(amount);
            Rect label=new Rect(r.x+27*unit,r.y,Mathf.Max(1,r.width-31*unit),r.height);
            int fontSize=Mathf.Max(1,Mathf.RoundToInt(13*unit));
            while(fontSize>1&&Style(fontSize,true).CalcSize(new GUIContent(value)).x>label.width)fontSize--;
            Text(label,value,fontSize,tint,true,false,TextAnchor.MiddleLeft);
        }
    }
}
