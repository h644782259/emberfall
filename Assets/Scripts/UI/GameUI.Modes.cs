using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  private string entryRewardSelection,entryRewardContext,entryRewardHoverKey;
  private Rect entryRewardViewport,entryRewardPopupRect,entryRewardAnchor;
  private EntryRewardPreview entryRewardPopup;
  private Vector2 entryRewardScreenAnchor,entryRewardScreenEnd,entryRewardPopupScroll;
  private bool entryRewardPopupVisible,entryRewardAnchorIsRoot;
  private Vector2 entryRewardContentOrigin;
  private void BeginEntryRewardPopup()
  {
   if(panel==Panel.Inventory&&entryRewardPopupVisible&&Event.current.type==EventType.MouseDown&&Event.current.button==0&&
      !entryRewardPopupRect.Contains(Event.current.mousePosition))
   {DismissEquippedItemDetails();entryRewardPopup=null;Event.current.Use();return;}
   entryRewardPopup=null;entryRewardAnchorIsRoot=false;
   if(suppressRewardHover&&!dismissedRewardAnchor.Contains(Mouse))suppressRewardHover=false;
   if((!MobileControls.Active&&!(entryRewardSelection??" ").StartsWith("merchant:"))||!entryRewardPopupVisible||Event.current.type!=EventType.MouseDown)return;
   Vector2 point=Event.current.mousePosition;
   if(entryRewardPopupRect.Contains(point))
   {
    float closeUnit=MobileControls.Active?TouchRatio:1;bool compactClose=entryRewardPopup!=null&&CompactRewardItem(entryRewardPopup);
    if(new Rect(entryRewardPopupRect.xMax-(compactClose?48:44)*closeUnit,entryRewardPopupRect.y+(compactClose?4:0)*closeUnit,44*closeUnit,44*closeUnit).Contains(point))entryRewardSelection=null;
    if(entryRewardSelection==null)Event.current.Use();
   }
   else if(!entryRewardAnchor.Contains(point))entryRewardSelection=null;
  }
  private void DrawEntryRewardPopup()
  {
   entryRewardPopupVisible=entryRewardPopup!=null;
   if(entryRewardPopup==null)return;
   float u=MobileControls.Active?TouchRatio:1f;
   if(MobileControls.Active&&width<height)u=Mathf.Max(u,1f/scale);
   if(!entryRewardAnchorIsRoot){Vector2 at=GUIUtility.ScreenToGUIPoint(entryRewardScreenAnchor),end=GUIUtility.ScreenToGUIPoint(entryRewardScreenEnd);entryRewardAnchor=new Rect(at.x,at.y,end.x-at.x,end.y-at.y);}
   if(!entryRewardViewport.Overlaps(entryRewardAnchor)){entryRewardPopupVisible=false;return;}
   bool comparing=entryRewardPopup.GearSlot.HasValue||entryRewardPopup.AppearanceSlot.HasValue;
   bool compact=CompactRewardItem(entryRewardPopup);
   float w=Mathf.Min((comparing?(IsWornReward(entryRewardPopup)?340:620):compact?InventoryGridGeometry.ItemPopupWidth:320)*u,width-24*u),textWidth=w-40*u;
   float contentHeight=DrawRewardDetailRows(entryRewardPopup,textWidth/u,u,false)*u;
   float headerHeight=comparing?0:92;
   float bagAction=entryRewardPopup.BagSlot>=0?52:0;
   float h=Mathf.Min(contentHeight+(headerHeight+10+bagAction)*u,height-24*u);
   float x=entryRewardAnchor.xMax+10*u;
   if(x+w>width-12*u)x=entryRewardAnchor.x-w-10*u;
   x=Mathf.Clamp(x,12*u,Mathf.Max(12*u,width-w-12*u));
   float y=Mathf.Clamp(entryRewardAnchor.y,12*u,Mathf.Max(12*u,height-h-12*u));
   Rect box=new Rect(x,y,w,h);entryRewardPopupRect=box;blockedRects.Add(box);
   Fill(box,ink);Border(box,entryRewardPopup.QualityColor,2*u);
   if(!comparing)
   {
    float closeInset=(MobileControls.Active||(entryRewardSelection??"").StartsWith("merchant:"))?40*u:0;
    DrawEntryRewardIcon(new Rect(x+w-56*u-closeInset,y+8*u,40*u,40*u),entryRewardPopup,u);
    Text(new Rect(x+8*u,y+4*u,w-76*u-closeInset,28*u),(entryRewardPopup.Name??"").Split('\n')[0],Mathf.RoundToInt(14*u),pale,true,false);
    if(compact)
    {
     var quantity=System.Text.RegularExpressions.Regex.Match(entryRewardPopup.Description??"",@"数量[ ：]*(.+)");
     var lines=(entryRewardPopup.Name??"").Split('\n');
     string amount=quantity.Success?quantity.Groups[1].Value:lines.Length>1?lines[1]:entryRewardPopup.Quantity.ToString();
     Text(new Rect(x+8*u,y+32*u,w-76*u-closeInset,22*u),"数量："+amount,Mathf.RoundToInt(11*u),muted,false);
    }
    DrawItemDetailTags(new Rect(x+8*u,y+60*u,w-24*u,22*u),entryRewardPopup,u);
   }
   entryRewardPopupScroll=BeginTouchScroll("entry-reward-popup",new Rect(x+12*u,y+headerHeight*u,w-24*u,h-(headerHeight+10+bagAction)*u),entryRewardPopupScroll,new Rect(0,0,textWidth,contentHeight),showScrollbar:false);
   DrawRewardDetailRows(entryRewardPopup,textWidth/u,u,true);
   EndTouchScroll();
   if(entryRewardPopup.BagSlot>=0)
   {
    int slot=entryRewardPopup.BagSlot;
    if(Button(new Rect(x+12*u,y+h-50*u,w-24*u,40*u),"开启",gold,session.Progression.Profile.gemBags[slot]>0))
    {string outcome;bool saved=session.Progression.OpenGemBag((ItemSlot)slot,out outcome);Feedback(saved,outcome);entryRewardSelection=null;entryRewardPopup=null;return;}
   }
   if((MobileControls.Active||(entryRewardSelection??" ").StartsWith("merchant:"))&&PopupCloseButton(new Rect(box.xMax-(compact?48:44)*u,y+(compact?4:0)*u,44*u,44*u)))entryRewardSelection=null;
  }
  private string GemRewardDescription(EquipmentMechanic gem,Rarity rarity,int rank=0)
  {
   string attribute=BuildCatalog.IsAttributeGem(gem)?BuildCatalog.GemAttributeSummary(gem,rarity,rank):BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(gem))+" +"+(BuildCatalog.MechanicAttributeValue(gem,rank)*100).ToString("0.#")+"%";
   return BuildCatalog.GemName(gem)+"\n"+GameBalance.SlotName(BuildCatalog.MechanicSlot(gem))+"槽\n"+attribute+(BuildCatalog.IsAttributeGem(gem)?"":"\n"+BuildCatalog.MechanicDescription(gem));
  }
  private string GemDropDescription(EquipmentMechanic gem,bool preview)
  {return BuildCatalog.GemName(gem)+"\n"+GameBalance.ClassName(BuildCatalog.MechanicClass(gem))+"专用 · "+GameBalance.SlotName(BuildCatalog.MechanicSlot(gem))+"槽\n"+(preview?"通关宝箱必得整件；已拥有则转为3枚星烬碎片。\n":"")+"机制\n"+BuildCatalog.MechanicDescription(gem);}
  private void InspectRewardItem(Rect hit,EntryRewardPreview item,bool clickToInspect=false,string inspectionKey=null)
  {
   string key=inspectionKey??item.Key;
   if(!MobileControls.Active&&suppressRewardHover)return;
   if((MobileControls.Active||clickToInspect)&&GUI.Button(hit,GUIContent.none,invisibleButton)){entryRewardSelection=entryRewardSelection==key?null:key;entryRewardPopupScroll=Vector2.zero;}
   bool hover=hit.Contains(Event.current.mousePosition)&&GUI.enabled;
   if(!MobileControls.Active&&hover)entryRewardHoverKey=key;
   if(MobileControls.Active||clickToInspect?entryRewardSelection==key:hover||entryRewardPopupVisible&&entryRewardHoverKey==key&&entryRewardPopupRect.Contains(Mouse))
   {
    tooltip=null;entryRewardAnchorIsRoot=false;entryRewardViewport=new Rect(0,0,width,height);entryRewardPopup=item;
    entryRewardScreenAnchor=GUIUtility.GUIToScreenPoint(hit.position);entryRewardScreenEnd=GUIUtility.GUIToScreenPoint(new Vector2(hit.xMax,hit.yMax));
   }
  }
  private EntryRewardPreview ActualEquipmentPreview(ItemData item)
  {
   item=session.Progression.PreviewUpgrade(item,item.upgradeLevel);
   string description=ItemTitle(item)+"\n"+GameBalance.RarityName(item.rarity)+" · Lv"+item.level+"\n基础词条\n攻击  "+item.baseAttack+"\n防御  "+item.baseDefense+"\n生命  "+item.baseHealth;
   if(item.criticalChance>0)description+="\n暴击率  "+(item.criticalChance*100).ToString("0.##")+"%";
   if(item.criticalDamageBonus>0)description+="\n暴击伤害  "+(item.criticalDamageBonus*100).ToString("0.##")+"%";
   if(item.attackPercent>0)description+="\n攻击加成  "+(item.attackPercent*100).ToString("0.##")+"%";
   return new EntryRewardPreview{Key="obtained:"+item.id,Equipment=item,GearSlot=item.slot,Name=ItemTitle(item),Description=description,Rarity=item.rarity,Tint=GameBalance.RarityColor(item.rarity),Icon=UIIconAtlas.EquipmentCardIcon(item,session.Progression.Profile.heroClass)};
  }
  private string EntryRewardKind(EntryRewardPreview item)
  {return (item.Key??"").Contains("gem-bag:")?"宝石袋":item.Key=="revival"?"道具":item.AppearanceSlot.HasValue?"时装":(item.Key??"").Contains("gem:")?"宝石":item.Key=="shard"||item.Key=="thread"||((item.Key??"").Contains("refinement")||(item.Key??"").Contains("affix-reforge"))?"材料":item.Key=="experience"?"经验":item.Key=="gold"?"货币":(item.Key??"").Contains("potion")?"药剂":"装备";}
  private void DrawEntryRewardIcon(Rect icon,EntryRewardPreview item,float u,bool compact=false)
  {
   Fill(icon,card);
   DrawIcon(new Rect(icon.x+3*u,icon.y+3*u,icon.width-6*u,icon.height-6*u),item.Icon,Color.white);
   Border(icon,item.QualityColor,2*u);
   if(item.Rarity==Rarity.Legendary)DrawFashionSlotFlow(icon,item.QualityColor,u);
  }
  private bool CompactRewardItem(EntryRewardPreview item)
  {return item.Key=="revival"||((item.Key??"").Contains("refinement")||(item.Key??"").Contains("affix-reforge"))||item.Key=="shard"||item.Key=="thread"||item.Key=="gold"||item.Key=="experience"||(item.Key??"").Contains("potion");}
  private float DrawEntryRewardRows(EntryRewardPreview item,float available,float u,bool draw)
  {
   float y=0;string[] lines=item.Description.Split('\n');
   for(int i=1;i<lines.Length;i++)
   foreach(string raw in (item.Key.StartsWith("fashion:")?lines[i].Split(new[]{" · "},System.StringSplitOptions.RemoveEmptyEntries):new[]{lines[i]}))
   {
    string line=raw.Trim();string gemSlot=GemSlotTag(item);
    if(gemSlot.Length>0){if(line==gemSlot)continue;line=line.Replace(" · "+gemSlot,"");}
    if(line.Length==0||line.StartsWith("Lv")||line==GameBalance.RarityName(item.Rarity)||line=="兵装"||line=="羽翼")continue;
    if(CompactRewardItem(item)&&line.StartsWith("数量"))continue;
    bool section=line.StartsWith("实际属性")||line.StartsWith("基础属性")||line.StartsWith("随机附加")||line.StartsWith("机制");
    var stat=System.Text.RegularExpressions.Regex.Match(line,@"^(攻击加成|攻击|防御|生命|暴击率|暴击几率|暴击伤害|移动速度|受到伤害减免|技能冷却缩减|能量回复|护甲|吸血|数量)[ ：]*(.+)$");
    if(stat.Success)
    {
     if(draw){Rect row=new Rect(0,y*u,available*u,36*u);Fill(row,new Color(.06f,.10f,.14f));Border(row,new Color(.23f,.43f,.48f),u);
      string name=stat.Groups[1].Value;float valueWidth=Mathf.Min(72,available*.3f),labelWidth=available-40-valueWidth;
      DrawIcon(new Rect(7*u,(y+8)*u,20*u,20*u),UIIconAtlas.Utility(name=="攻击"?"attack":name=="防御"||name=="受到伤害减免"?"defense":name=="生命"||name=="吸血"?"health":name=="数量"?"shard":"core"),jade);
      DrawGemEffectText(new Rect(34*u,y*u,labelWidth*u,36*u),name,Mathf.RoundToInt(12*u),pale,true,TextAnchor.MiddleLeft);
      Text(new Rect((available-valueWidth-6)*u,y*u,valueWidth*u,36*u),stat.Groups[2].Value,Mathf.RoundToInt(14*u),jade,true,false,TextAnchor.MiddleRight);}
     y+=40;continue;
    }
    if(CompactRewardItem(item))continue;
    float h=Mathf.Max(24*u,Style(Mathf.RoundToInt(section?12*u:11*u),section,true).CalcHeight(new GUIContent(line),(available-16)*u)+8*u);
    if(draw){if(section)Fill(new Rect(0,(y+5)*u,3*u,h-10*u),item.QualityColor);Text(new Rect(8*u,y*u,(available-16)*u,h),line,Mathf.RoundToInt(section?12*u:11*u),section?item.QualityColor:(item.Key??"").Contains("gem-bag:")?pale:muted,section,true);}
    y+=h/u+4;
   }
   return y;
  }
  // Retain range/affix notes in previews while using the same stat rows for actual rewards.
  private bool IsWornReward(EntryRewardPreview item)
  {
   if(item.Equipment!=null)return IsEquipped(item.Equipment);
   if(item.AppearanceSlot.HasValue&&item.Key!=null){var worn=session.Progression.EquippedFashion(item.AppearanceSlot.Value);return worn!=null&&item.Key=="fashion:"+worn.id;}
   return false;
  }
  private float DrawRewardDetailRows(EntryRewardPreview item,float available,float u,bool draw)
  {
   if(item.PreviewBagSlot>=0)return DrawGemBagContents(item,available,u,draw);
   if(CompactRewardItem(item)){if(draw)Text(new Rect(0,0,available*u,44*u),ResourceDescription(item.Key),Mathf.RoundToInt(12*u),new Color(.78f,.84f,.9f),false,true);return 44;}
   if(!item.GearSlot.HasValue&&!item.AppearanceSlot.HasValue)return DrawEntryRewardRows(item,available,u,draw);
   string current="";ItemData currentEquipment=null;
   if(item.GearSlot.HasValue){var equipped=session.Progression.Equipped(item.GearSlot.Value);if(equipped!=null){var preview=ActualEquipmentPreview(equipped);current=preview.Description;currentEquipment=preview.Equipment;}}
   else {var equipped=session.Progression.EquippedFashion(item.AppearanceSlot.Value);if(equipped!=null)current=ProgressionService.FashionBonus(equipped);}
   var baseline=RewardStatValues(current);var selected=RewardStatValues(item.Description);
   foreach(var pair in baseline)if(!selected.ContainsKey(pair.Key))selected[pair.Key]=pair.Value.Contains("×")?"×100%":pair.Value.Contains("%")?"0%":"0";
   bool selectedWorn=IsWornReward(item);int firstColumn=selectedWorn?1:0;
   float gap=2,cardWidth=selectedWorn?available:(available-gap)*.5f,y=92;
   EntryRewardPreview worn=null;
   if(currentEquipment!=null)worn=ActualEquipmentPreview(currentEquipment);
   if(item.AppearanceSlot.HasValue){var f=session.Progression.EquippedFashion(item.AppearanceSlot.Value);if(f!=null)worn=new EntryRewardPreview{Name=f.name,Rarity=f.rarity,AppearanceSlot=f.slot,Icon=UIIconAtlas.FashionCardIcon(f,session.Progression.Profile.heroClass)};}
   if(draw){Fill(new Rect(0,0,available*u,DrawRewardDetailRows(item,available,u,false)*u),ink);if(!selectedWorn)Fill(new Rect(cardWidth*u,0,gap*u,DrawRewardDetailRows(item,available,u,false)*u),new Color(.15f,.29f,.31f));}
   if(draw)for(int col=firstColumn;col<2;col++){float x=(col-firstColumn)*(cardWidth+gap);Rect cardRect=new Rect(x*u,0,cardWidth*u,DrawRewardDetailRows(item,available,u,false)*u);Fill(cardRect,ink);Border(cardRect,col==0?muted:jade);}
   var progression=session.Progression;
   ItemData owned=item.Equipment==null?null:progression.Profile.inventory.Find(v=>v!=null&&v.id==item.Equipment.id);
   if(draw)for(int col=firstColumn;col<2;col++)
   {
    var value=col==0?worn:item;float x=(col-firstColumn)*(cardWidth+gap);Color accent=value==null?muted:GameBalance.RarityColor(value.Rarity);
    float closeInset=MobileControls.Active&&col==1?44:0;
    float headingWidth=cardWidth-20-closeInset-(value==null?0:48);
    Fill(new Rect(x*u,0,cardWidth*u,86*u),new Color(.02f,.035f,.055f));
    Text(new Rect((x+10)*u,2*u,headingWidth*u,20*u),col==0||selectedWorn?"已穿戴":"所选物品",Mathf.RoundToInt(12*u),col==0?pale:jade,true);
    string name=value==null?"未穿戴":string.IsNullOrEmpty(value.Name)?"所选时装":value.Name;
    string level=value==null?"":System.Text.RegularExpressions.Regex.Match(value.Description??"",@"Lv\.?\d+").Value;
    if(level.Length>0)name=level+"  "+name;
    if(value!=null)
    {
     Rect equipmentIcon=new Rect((x+cardWidth-48-closeInset)*u,8*u,40*u,40*u);
     Fill(equipmentIcon,card);
     DrawIcon(new Rect(equipmentIcon.x+2*u,equipmentIcon.y+2*u,36*u,36*u),value.Icon,Color.white);
     Border(equipmentIcon,accent,2*u);
     if(value.Rarity==Rarity.Legendary)DrawFashionSlotFlow(equipmentIcon,accent,u);
    }
    Text(new Rect((x+10)*u,24*u,headingWidth*u,32*u),name,Mathf.RoundToInt(15*u),pale,true,true);
    if(value!=null)DrawItemDetailTags(new Rect((x+8)*u,60*u,(cardWidth-16)*u,22*u),value,u);
   }
   FashionData ownedFashion=item.AppearanceSlot.HasValue&&item.Key!=null&&item.Key.StartsWith("fashion:")?progression.Profile.fashions.Find(v=>v!=null&&"fashion:"+v.id==item.Key):null;
   FashionData currentFashion=item.AppearanceSlot.HasValue?progression.EquippedFashion(item.AppearanceSlot.Value):null;
   FashionData selectedFashion=ownedFashion??(item.AppearanceSlot.HasValue?new FashionData{slot=item.AppearanceSlot.Value,rarity=item.Rarity}:null);
   for(int section=0;section<(item.GearSlot.HasValue?2:1);section++)
   {
    bool any=false;foreach(var stat in selected)if(!item.GearSlot.HasValue||(IsRandomEquipmentStat(stat.Key)?1:0)==section)any=true;
    if(!any)continue;
    if(draw)for(int col=firstColumn;col<2;col++){
     float x=(col-firstColumn)*(cardWidth+gap);
     Fill(new Rect(x*u,y*u,cardWidth*u,28*u),new Color(.07f,.13f,.16f));
     Fill(new Rect((x+8)*u,(y+8)*u,3*u,12*u),jade);
     Text(new Rect((x+18)*u,y*u,(cardWidth-24)*u,28*u),item.GearSlot.HasValue?(section==0?"基础词条":col==1&&item.Clear?"随机词条 · 最多"+ProgressionService.EquipmentAffixLimit(item.Rarity)+"项":"随机词条"):"基础属性 / 升阶加成",Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleLeft);
    }
    y+=32;
    foreach(var pair in selected)
    {
     if(item.GearSlot.HasValue&&(IsRandomEquipmentStat(pair.Key)?1:0)!=section)continue;
     string before;if(!baseline.TryGetValue(pair.Key,out before))before=pair.Value.Contains("×")?"×100%":pair.Value.Contains("%")?"0%":"0";
     if(draw)for(int col=firstColumn;col<2;col++){
      if(item.GearSlot.HasValue&&!EquipmentComparisonPresentation.HasNonzeroStat(col==0?before:pair.Value))continue;
      float x=(col-firstColumn)*(cardWidth+gap);Fill(new Rect(x*u,y*u,cardWidth*u,34*u),card);
      DrawIcon(new Rect((x+8)*u,(y+9)*u,16*u,16*u),UIIconAtlas.Utility(pair.Key=="攻击"?"attack":pair.Key=="防御"?"defense":pair.Key=="生命"?"health":"core"),jade);
      Text(new Rect((x+30)*u,y*u,Mathf.Min(72,cardWidth*.28f)*u,34*u),pair.Key,Mathf.RoundToInt(11*u),muted,true,false,TextAnchor.MiddleLeft);
      if(item.AppearanceSlot.HasValue)
      {
       var f=col==0?currentFashion:selectedFashion;
       int basis=ProgressionService.FashionBaseStat(f,pair.Key),rank=ProgressionService.FashionRankStat(f,pair.Key);
       float start=34+Mathf.Min(72,cardWidth*.28f),space=cardWidth-start-6;
       string prefix=pair.Key=="暴击几率"?"×":"";
       DrawComparedValue(new Rect((x+start)*u,y*u,space*.55f*u,34*u),prefix+basis+"%",col==0||selectedWorn?0:basis.CompareTo(ProgressionService.FashionBaseStat(currentFashion,pair.Key)),u,false);
       DrawComparedValue(new Rect((x+start+space*.55f)*u,y*u,space*.45f*u,34*u),"+"+rank+"%",item.Clear||item.UnenhancedReward||col==0||selectedWorn?0:rank.CompareTo(ProgressionService.FashionRankStat(currentFashion,pair.Key)),u,true);
       continue;
      }
      int bonus=EquipmentStatBonus(col==0?currentEquipment:item.Equipment,pair.Key),oldBonus=EquipmentStatBonus(currentEquipment,pair.Key);
      bool bonusVisible=bonus>0&&!(item.Clear&&col==1);
      float valueX=34+Mathf.Min(72,cardWidth*.28f),room=cardWidth-valueX-6;
      float valueWidth=bonusVisible?room*.56f:room;
      DrawComparedValue(new Rect((x+valueX)*u,y*u,valueWidth*u,34*u),col==0?before:pair.Value,col==0||selectedWorn?0:EquipmentComparisonPresentation.StatDirection(pair.Value,before),u,false);
      if(bonusVisible)DrawComparedValue(new Rect((x+valueX+valueWidth)*u,y*u,(room-valueWidth)*u,34*u),"+"+bonus,item.Clear||item.UnenhancedReward||col==0||selectedWorn?0:bonus.CompareTo(oldBonus),u,true);
     }
     y+=38;
    }
   }
   if(item.AllowActions&&(owned!=null||ownedFashion!=null))
   {
    if(draw)
    {
     float actionX=selectedWorn?0:cardWidth+gap;
     if(InventoryPictogramAction(new Rect((actionX+cardWidth*.15f)*u,y*u,cardWidth*.7f*u,40*u),selectedWorn?"卸下":"穿戴",UIIconAtlas.Utility("confirm"),selectedWorn||owned==null||owned.level<=progression.Profile.level,selectedWorn,true))
     {
      bool saved=owned!=null?(selectedWorn?progression.Unequip(owned.slot):progression.Equip(owned.id)):(selectedWorn?progression.UnequipFashion(ownedFashion.slot):progression.EquipFashion(ownedFashion.id));
      Feedback(saved,selectedWorn?"已卸下":"已穿戴");if(saved){RebuildBagItems();if(!selectedWorn)DismissEquippedItemDetails();}
     }
    }
    y+=48;
   }
   return y;
  }
  private void DrawItemDetailTags(Rect row,EntryRewardPreview item,float u)
  {
   string slot=item.GearSlot.HasValue?GameBalance.SlotName(item.GearSlot.Value):item.AppearanceSlot.HasValue?(item.AppearanceSlot==FashionSlot.Wings?"羽翼":"兵装"):GemSlotTag(item);
   float gap=6*u,first=Mathf.Min(60*u,(row.width-gap*2)/3),remaining=row.width-first-gap;
   DrawDetailTag(new Rect(row.x,row.y,first,row.height),GameBalance.RarityName(item.Rarity),item.QualityColor,u);
   int count=slot.Length>0?2:1;float cell=Mathf.Min(80*u,(remaining-gap*(count-1))/count);
   DrawDetailTag(new Rect(row.x+first+gap,row.y,cell,row.height),EntryRewardKind(item),jade,u);
   if(count==2)DrawDetailTag(new Rect(row.x+first+cell+gap*2,row.y,cell,row.height),slot,jade,u);
  }
  private string GemSlotTag(EntryRewardPreview item)
  {return EntryRewardKind(item)=="宝石"?System.Text.RegularExpressions.Regex.Match(item.Description??"",@"(?:武器|护甲|饰品)槽").Value:"";}
  private void DrawDetailTag(Rect r,string label,Color tint,float u)
  {Fill(r,new Color(.02f,.035f,.055f,1));Border(r,new Color(tint.r,tint.g,tint.b,.85f));Text(r,label,Mathf.RoundToInt(12*u),Color.Lerp(tint,Color.white,.78f),true,false,TextAnchor.MiddleCenter);}
  private void DrawComparedValue(Rect r,string value,int direction,float u,bool enhancement)
  {
   Color tint=direction>0?new Color(.3f,.94f,.55f):direction<0?new Color(1f,.38f,.36f):enhancement?gold:pale;
   if(direction!=0)r.width-=18*u;
   int size=Mathf.RoundToInt(15*u);while(size>Mathf.RoundToInt(9*u)&&Style(size,true,false).CalcSize(new GUIContent(value)).x>r.width)size--;
   Text(r,value,size,tint,true,false,TextAnchor.MiddleLeft);
   if(direction!=0)DrawIcon(new Rect(r.x+Mathf.Min(r.width,Style(size,true,false).CalcSize(new GUIContent(value)).x)+2*u,r.center.y-7*u,14*u,14*u),UIIconAtlas.StatTrendArrow(direction>0),tint);
  }
  private static bool IsRandomEquipmentStat(string name)
  {return name=="攻击加成"||name=="暴击率"||name=="暴击伤害";}
  private static int EquipmentStatBonus(ItemData item,string name)
  {return item==null?0:name=="攻击"?item.attack-item.baseAttack:name=="防御"?item.defense-item.baseDefense:name=="生命"?item.health-item.baseHealth:0;}
  private void DrawEquipmentStatParts(Rect area,string basis,int bonus,float u,Color tint)
  {
   string extra=bonus>0?" +"+bonus:"";int size=Mathf.RoundToInt(11*u);
   while(size>Mathf.RoundToInt(8*u)&&Style(size,true,false).CalcSize(new GUIContent(basis+extra)).x>area.width)size--;
   float extraWidth=Style(size,true,false).CalcSize(new GUIContent(extra)).x;
   Text(new Rect(area.x,area.y,Mathf.Max(0,area.width-extraWidth),area.height),basis,size,tint,true,false,TextAnchor.MiddleRight);
   if(bonus>0)Text(new Rect(area.xMax-extraWidth,area.y,extraWidth,area.height),extra,size,jade,true,false,TextAnchor.MiddleRight);
  }
  private static System.Collections.Generic.Dictionary<string,string> RewardStatValues(string description)
  {
   var values=new System.Collections.Generic.Dictionary<string,string>();
   foreach(string raw in description.Split(new[]{"\n"," · "},System.StringSplitOptions.RemoveEmptyEntries)){
    var match=System.Text.RegularExpressions.Regex.Match(raw.Trim(),@"^(攻击加成|攻击|防御|生命|暴击率|暴击几率|暴击伤害|移动速度|受到伤害减免)[ ：]*(.+)$");
    if(match.Success&&EquipmentComparisonPresentation.HasNonzeroStat(match.Groups[2].Value))values[match.Groups[1].Value]=match.Groups[2].Value;
   }
   return values;
  }
  private sealed class EntryRewardPreview
  {
   public int BagSlot=-1,PreviewBagSlot=-1;public ItemData Equipment;public string Key,Name,Description;public Texture2D Icon;public Color Tint;public Color QualityColor {get{return GameBalance.RarityColor(Rarity);}}public Rarity Rarity;public bool Clear;public bool UnenhancedReward;public bool AllowActions;public int Quantity=1;public ItemSlot? GearSlot;public FashionSlot? AppearanceSlot;
  }
  private readonly System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<EntryRewardPreview>> entryPreviewCache = new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<EntryRewardPreview>>();
  private System.Collections.Generic.List<EntryRewardPreview> EntryRewardPreviews(int mode,int tier,bool chapter)
  {
   string cacheKey=session.Progression.CurrentSlotId+":"+session.Progression.Profile.heroClass+":"+session.Progression.Profile.level+":"+mode+":"+tier+":"+chapter+":"+session.SelectedChapterNode+":"+session.SelectedChapterDifficulty+":"+session.SelectedChapterTier;
   System.Collections.Generic.List<EntryRewardPreview> cached;
   if(entryPreviewCache.TryGetValue(cacheKey,out cached))return cached;
   var result=new System.Collections.Generic.List<EntryRewardPreview>();int level=chapter?ProgressionService.EquipmentGenerationLevel(session.Progression.Profile.level):AdventureRewardRules.DungeonLevel(tier);
   int enemyTier=chapter?session.SelectedChapterTier:tier;
   bool hasBoss=chapter?session.SelectedChapterNode==ChapterNode.StarPlatform:mode==-1||mode==2||mode==3;
   // Each reachable quality has its own identity and graphic. Slot count comes from the completed-tier table.
   for(int slotIndex=0;slotIndex<3&&!(chapter&&session.SelectedChapterNode==ChapterNode.StarPlatform);slotIndex++)
   {
    var slot=(ItemSlot)slotIndex;int count=DropPreviewRules.SlotCount(mode,tier,slot);if(count==0)continue;
    foreach(var rarity in DropPreviewRules.ClearRarities(mode,tier))
     result.Add(new EntryRewardPreview{Key="clear:"+slot+":"+rarity,GearSlot=slot,Clear=true,Rarity=rarity,Name=GameBalance.SlotName(slot),Icon=UIIconAtlas.EquipmentCardIcon(slot,level,rarity,session.Progression.Profile.heroClass),Tint=GameBalance.RarityColor(rarity),Description=ProgressionService.EquipmentDropPreview(slot,rarity,level)});
   }
   if(chapter&&session.SelectedChapterNode==ChapterNode.StarPlatform)
   foreach(var gem in BuildCatalog.DroppableGemsFor(session.Progression.Profile.heroClass))foreach(var rarity in new[]{Rarity.Epic,Rarity.Legendary})
    result.Add(new EntryRewardPreview{Key="gem:"+gem+":"+rarity,Clear=true,Rarity=rarity,Name=BuildCatalog.GemName(gem),Icon=UIIconAtlas.Utility("gem"),Tint=GameBalance.RarityColor(rarity),Description=GemRewardDescription(gem,rarity)});
   if(!(chapter&&session.SelectedChapterNode==ChapterNode.StarPlatform))result.Add(new EntryRewardPreview{Key="refinement",Clear=true,Rarity=Rarity.Epic,Name="装备洗练石\n"+ProgressionService.ChestStackMinimum(true,tier)+"～"+ProgressionService.ChestStackMaximum(true,tier),Icon=UIIconAtlas.Utility("gem"),Tint=GameBalance.RarityColor(Rarity.Epic),Description="装备洗练石\n数量  "+ProgressionService.ChestStackMinimum(true,tier)+"～"+ProgressionService.ChestStackMaximum(true,tier)+"\n提高装备属性数值，不会降低。"});
   if(!(chapter&&session.SelectedChapterNode==ChapterNode.StarPlatform))result.Add(new EntryRewardPreview{Key="shard",Clear=true,Rarity=Rarity.Rare,Name="星烬碎片\n"+ProgressionService.ChestStackMinimum(false,tier)+"～"+ProgressionService.ChestStackMaximum(false,tier),Icon=UIIconAtlas.Utility("shard"),Tint=jade,Description="星烬碎片\n数量："+ProgressionService.ChestStackMinimum(false,tier)+"～"+ProgressionService.ChestStackMaximum(false,tier)+"\n用于机制宝石兑换、升阶与升华。"});
   if(AdventureRewardRules.ChestFashion(mode,chapter))foreach(var rarity in new[]{Rarity.Legendary})foreach(var slot in new[]{FashionSlot.Weapon,FashionSlot.Wings})
    result.Add(new EntryRewardPreview{Key="fashion:"+slot+":"+rarity,AppearanceSlot=slot,Clear=true,Rarity=rarity,Name=ProgressionService.FashionName(slot,rarity,session.Progression.Profile.heroClass),Icon=UIIconAtlas.FashionCardIcon(slot,(int)rarity,session.Progression.Profile.heroClass),Tint=GameBalance.RarityColor(rarity),Description=ProgressionService.FashionName(slot,rarity,session.Progression.Profile.heroClass)+"\n"+(slot==FashionSlot.Weapon?"兵装":"羽翼")+" · "+GameBalance.RarityName(rarity)+"\n"+ProgressionService.FashionBonus(slot,rarity)});
   if(AdventureRewardRules.ChestAffixReforge(chapter?(int)session.SelectedChapterNode:mode,chapter)){var reforge=ResourceItemPreview("affix-reforge",1);reforge.Clear=true;result.Add(reforge);}
   if(entryPreviewCache.Count>=64)entryPreviewCache.Clear();
   entryPreviewCache[cacheKey]=result;return result;
  }
  private sealed class EntryRewardGroup
  {
   public readonly System.Collections.Generic.List<EntryRewardPreview> Variants=new System.Collections.Generic.List<EntryRewardPreview>();
   public EntryRewardPreview First {get{return Variants[0];}}
  }
  private readonly System.Collections.Generic.Dictionary<System.Collections.Generic.List<EntryRewardPreview>,System.Collections.Generic.List<EntryRewardGroup>> entryRewardGroups=new System.Collections.Generic.Dictionary<System.Collections.Generic.List<EntryRewardPreview>,System.Collections.Generic.List<EntryRewardGroup>>();
  private System.Collections.Generic.List<EntryRewardGroup> GroupEntryRewards(System.Collections.Generic.List<EntryRewardPreview> items)
  {
   System.Collections.Generic.List<EntryRewardGroup> result;
   if(entryRewardGroups.TryGetValue(items,out result))return result;
   result=new System.Collections.Generic.List<EntryRewardGroup>();var byKey=new System.Collections.Generic.Dictionary<string,EntryRewardGroup>();
   foreach(var item in items)
   {
    string identity=item.GearSlot.HasValue?"gear:"+item.GearSlot.Value:item.Key.StartsWith("gem:")?item.Key.Substring(0,item.Key.LastIndexOf(':')):item.Key;
    // Each rarity owns its artwork and frame; never show a legendary preview through an epic icon.
    identity=item.Clear+":"+identity+":"+item.Rarity;EntryRewardGroup group;
    if(!byKey.TryGetValue(identity,out group)){group=new EntryRewardGroup();byKey[identity]=group;result.Add(group);}
    group.Variants.Add(item);
   }
   if(entryRewardGroups.Count>=64)entryRewardGroups.Clear();entryRewardGroups[items]=result;return result;
  }
  private float DrawEntryRewardPreviews(float available,float u,int mode,int tier,bool chapter,bool draw,bool compact=false)
  {
   string context=chapter?"chapter"+session.SelectedChapterNode+":"+session.SelectedChapterDifficulty+":"+session.SelectedChapterTier:mode+":"+tier;
   if(entryRewardContext!=context){entryRewardContext=context;entryRewardSelection=null;entryRewardHoverKey=null;entryRewardPopupScroll=Vector2.zero;}
   bool gemChest=chapter&&session.SelectedChapterNode==ChapterNode.StarPlatform;
   var groups=GroupEntryRewards(EntryRewardPreviews(mode,tier,chapter));int columns=Mathf.Max(1,Mathf.FloorToInt(available/(compact?68:88)));float cell=available/columns,end=0;
   float iconSize=compact?28:40,labelY=iconSize+(compact?2:4);int labelFont=compact?10:11;
   for(int section=0;section<2;section++)
   {
    bool clear=section==1;var cards=groups.FindAll(group=>group.First.Clear==clear);if(cards.Count==0)continue;
    if(draw)
    {
     Text(new Rect(8*u,end*u,(available-16)*u,(compact?16:22)*u),clear?"通关宝箱":"通关固定掉落",Mathf.RoundToInt((compact?11:13)*u),gold,true);
     if(clear)
     {
      Rect odds=new Rect((available-90)*u,end*u,82*u,(compact?16:22)*u);
      Text(odds,"概率说明 ⓘ",Mathf.RoundToInt((compact?9:10)*u),muted,false,false,TextAnchor.MiddleRight);
      var info=new EntryRewardPreview{Key="chest-odds",Name="通关宝箱概率",Rarity=Rarity.Legendary,Icon=UIIconAtlas.Utility("inventory"),Description=gemChest?"每箱1颗宝石\n史诗80% · 传说20%\n重复宝石转为碎片。":"通关宝箱概率\n传说装备概率  "+AdventureRewardRules.LegendaryChance(tier)+"%\n每箱获得一件物品或一组材料。\n连续未获得传说装备时，第30箱必出；获得后重新计数。"};
      InspectRewardItem(MobileControls.Active?new Rect(odds.x,odds.y,odds.width,44*u):odds,info);
      int remaining=Mathf.Max(1,AdventureRewardRules.LegendaryPityChests-session.Progression.Profile.legendaryEquipmentMisses);
      Rect pity=new Rect(8*u,(end+(compact?18:26))*u,(available-16)*u,(compact?26:34)*u);
      Fill(pity,new Color(.22f,.12f,.025f));Border(pity,gold,u);
      Fill(new Rect(pity.x,pity.y,3*u,pity.height),gold);
      Text(new Rect(pity.x+8*u,pity.y,pity.width-16*u,pity.height),gemChest?"每箱1颗宝石 · 史诗80% · 传说20%":remaining==1?"下一箱必出传说装备":"再开 "+remaining+" 箱内必出传说装备",Mathf.RoundToInt((compact?11:14)*u),new Color(1f,.82f,.35f),true,true,TextAnchor.MiddleCenter);
     }
    }
    end+=clear?(compact?50:68):(compact?22:24);
    for(int row=0;row*columns<cards.Count;row++)
    {
     float labelHeight=compact?14:16;
     for(int col=0;col<columns&&row*columns+col<cards.Count;col++)
      labelHeight=Mathf.Max(labelHeight,Style(Mathf.RoundToInt(labelFont*u),false,true).CalcHeight(new GUIContent(cards[row*columns+col].First.Name),(cell-8)*u)/u);
     float rowHeight=Mathf.Max(compact?44:0,labelY+labelHeight)+(compact?4:6);
     for(int col=0;col<columns&&row*columns+col<cards.Count;col++)
     {
      if(!draw)continue;var group=cards[row*columns+col];var item=group.First;
      foreach(var variant in group.Variants)if(variant.Key==entryRewardSelection||variant.Key==entryRewardHoverKey)item=variant;
      Rect hit=new Rect((col*cell+4)*u,end*u,(cell-8)*u,Mathf.Max(compact?44:0,rowHeight-(compact?4:6))*u),icon=new Rect((col*cell+(cell-iconSize)*.5f)*u,end*u,iconSize*u,iconSize*u);
      DrawEntryRewardIcon(icon,item,u,compact);
      Text(new Rect(hit.x,hit.y+labelY*u,hit.width,labelHeight*u),item.Name,Mathf.RoundToInt(labelFont*u),pale,false,true,TextAnchor.MiddleCenter);
      if(MobileControls.Active&&GUI.Button(hit,GUIContent.none,invisibleButton)){entryRewardSelection=entryRewardSelection==item.Key?null:item.Key;entryRewardPopupScroll=Vector2.zero;}
      Rect rootHit=new Rect(entryRewardContentOrigin.x+hit.x,entryRewardContentOrigin.y+hit.y,hit.width,hit.height);
      bool hover=entryRewardViewport.Contains(Mouse)&&rootHit.Contains(Mouse)&&GUI.enabled;
      if(!MobileControls.Active&&hover)entryRewardHoverKey=item.Key;
      bool selected=false;foreach(var variant in group.Variants)if(variant.Key==(MobileControls.Active?entryRewardSelection:entryRewardHoverKey))selected=true;
      bool show=!suppressRewardHover&&(MobileControls.Active?selected&&entryRewardSelection!=null:hover||entryRewardPopupVisible&&selected&&entryRewardPopupRect.Contains(Mouse));
      if(show){entryRewardPopup=item;entryRewardAnchorIsRoot=true;entryRewardAnchor=compact?rootHit:new Rect(entryRewardContentOrigin.x+icon.x,entryRewardContentOrigin.y+icon.y,icon.width,icon.height);}
     }
     end+=rowHeight;
    }
    end+=4;
   }
   return end+4;
  }
  private Texture2D dungeonEntryAtlas;
  private void DrawDungeonEntryArtwork(Rect area,int index)
  {
   if(dungeonEntryAtlas==null)dungeonEntryAtlas=Resources.Load<Texture2D>("UI/DungeonEntryAtlas");
   Fill(area,card);
   if(dungeonEntryAtlas!=null)
   {
    index=Mathf.Clamp(index,0,7);Rect uv=new Rect((index%2)*.5f,1-(index/2+1)*.25f,.5f,.25f);
    float sourceAspect=dungeonEntryAtlas.width*.5f/(dungeonEntryAtlas.height*.25f),aspect=area.width/Mathf.Max(1,area.height);
    if(aspect>sourceAspect){float crop=uv.height*sourceAspect/aspect;uv.y+=(uv.height-crop)*.5f;uv.height=crop;}
    else {float crop=uv.width*aspect/sourceAspect;uv.x+=(uv.width-crop)*.5f;uv.width=crop;}
    GUI.DrawTextureWithTexCoords(area,dungeonEntryAtlas,uv);
   }
   Border(area,new Color(gold.r,gold.g,gold.b,.4f));
  }
  private Vector2 adventureListScroll,adventureDetailScroll;
  private bool adventureChapterSelected;
  private Rect AdventureRect(MobilePanelLayout.Area a,float u){return new Rect(a.X*u,a.Y*u,a.Width*u,a.Height*u);}
  private void DrawArenaSelection()
  {
   float u=MobileControls.Active?TouchRatio:1f,inset=MobileControls.Active?12:24;var l=new AdventureSelectionLayout(width/u,height/u,inset);
   blockedRects.Add(new Rect(0,0,width,height));
   Box(AdventureRect(l.Frame,u),jade,false);
   Text(new Rect((l.X+inset)*u,(l.Y+inset)*u,(l.Frame.Width-2*inset-48)*u,36*u),"选择冒险",Mathf.RoundToInt(20*u),pale,true);
   if(PopupCloseButton(new Rect((l.X+l.Frame.Width-44-inset)*u,(l.Y+inset)*u,44*u,36*u))){adventureChapterSelected=false;session.CancelDungeonSelection();BlockUITransition();return;}
   adventureChapterSelected=false;
   string[] names={"沉星遗迹","守望林庭","烬河突围","蚀星斗场","回廊远征"};
   adventureListScroll=BeginTouchScroll("adventure-list",AdventureRect(l.List,u),adventureListScroll,new Rect(0,0,(l.List.Width-18)*u,300*u));
   for(int i=0;i<5;i++)
   {
    bool chapter=i>=5;var node=(ChapterNode)(chapter?i-5:0);
    bool unlocked=!chapter||ChapterProgression.IsUnlocked(session.Progression.Profile,node);
    bool chosen=chapter?adventureChapterSelected&&session.SelectedChapterNode==node:!adventureChapterSelected&&session.SelectedArenaMode==i-1;
    string caption=chapter?ChapterDefinition.Get(node).Name:names[i];
    if(!unlocked)caption+=" · "+ChapterProgression.UnlockLevel(node)+"级";
    if(TabButton(AdventureRect(l.Entry(i),u),caption,chosen,unlocked,pointerFeedback:false))
    {
     adventureChapterSelected=chapter;
     if(chapter){PrepareInlineChapterSelection();SelectChapterNode(node);}
     else session.SelectedArenaMode=i-1;
     adventureDetailScroll=Vector2.zero;adventureRewardHint=null;CancelMobileScroll();
    }
   }
   EndTouchScroll();
   if(adventureChapterSelected)
   {
    DrawInlineChapterEntry(AdventureRect(l.Details,u),u);
    DrawChapterEntryControls(new Rect(l.List.X*u,l.FooterY*u,l.List.Width*u,48*u),u);
    if(PrimaryButton(new Rect(l.Details.X*u,l.FooterY*u,l.Details.Width*u,48*u),"进入 "+ChapterDefinition.Get(session.SelectedChapterNode).Name,gold,ChapterProgression.IsUnlocked(session.Progression.Profile,session.SelectedChapterNode)))ConfirmSelectedChapter();
    return;
   }
   int mode=session.SelectedArenaMode,tier=session.SelectedDungeonTier;float contentWidth=l.Details.Width-18;
   string detail=names[mode+1]+" · Lv"+AdventureRewardRules.DungeonLevel(tier);
   float rewardHeight=DrawEntryRewardPreviews(contentWidth,u,mode,tier,false,false),rewardY=52;
   Rect detailBody=AdventureRect(l.Details,u);bool overflow=(rewardY+rewardHeight)*u>detailBody.height;
   if(overflow)adventureDetailScroll=BeginTouchScroll("adventure-detail",detailBody,adventureDetailScroll,new Rect(0,0,contentWidth*u,(rewardY+rewardHeight)*u));
   else {adventureDetailScroll=Vector2.zero;GUI.BeginGroup(detailBody);}
   DrawDungeonEntryArtwork(new Rect(8*u,4*u,64*u,40*u),mode+1);
   Text(new Rect(82*u,4*u,(contentWidth-90)*u,40*u),detail,Mathf.RoundToInt(18*u),pale,true,false,TextAnchor.MiddleLeft);
   entryRewardViewport=detailBody;entryRewardContentOrigin=new Vector2(detailBody.x-adventureDetailScroll.x,detailBody.y+rewardY*u-adventureDetailScroll.y);
   GUI.BeginGroup(new Rect(0,rewardY*u,contentWidth*u,rewardHeight*u));DrawEntryRewardPreviews(contentWidth,u,mode,tier,false,true);GUI.EndGroup();
   if(overflow)EndTouchScroll();else GUI.EndGroup();
   float x=l.List.X,y=l.FooterY,selectorButton=Mathf.Min(44,l.List.Width*.24f),selectorLabel=l.List.Width-selectorButton*2;

   bool normal=!adventureChapterSelected;
   if(Button(new Rect(x*u,y*u,selectorButton*u,48*u),"−",jade,normal&&tier>1))session.SelectedDungeonTier--;
   Text(new Rect((x+selectorButton)*u,y*u,selectorLabel*u,48*u),"Lv"+AdventureRewardRules.DungeonLevel(tier),Mathf.RoundToInt(13*u),gold,true,false,TextAnchor.MiddleCenter);
   if(Button(new Rect((x+selectorButton+selectorLabel)*u,y*u,selectorButton*u,48*u),"+",jade,normal&&tier<session.MaximumDungeonTier))session.SelectedDungeonTier++;
   if(PrimaryButton(new Rect(l.Details.X*u,y*u,l.Details.Width*u,48*u),"进入挑战",gold))
   {session.ConfirmDungeonSelection();}
  }
  private void DrawMobileModeStatus(Rect r)
  {
   float y=r.y;
   if(session.ChapterActive)
   {
    var first=session.ChapterSealView(0);var second=session.ChapterSealView(1);
    DrawMobileObjectiveText(r,ref y,first!=null&&session.ChapterRun.DoorUnlocked?"双印完成 · 前往出口":AdventureMapCaption,11,gold,true,true);
    if(first!=null){DrawMobileSealText(r,ref y,first);DrawMobileSealText(r,ref y,second);}
    else DrawMobileObjectiveText(r,ref y,session.ChapterObjectiveCompact,10,pale);
    return;
   }
   if(session.RoomChainRun!=null)
   {
    var objective=session.RoomObjectiveView;var first=session.RoomSealView(0);
    DrawMobileObjectiveText(r,ref y,first!=null&&session.RoomChainRun.DoorUnlocked?"双印完成 · 前往北门":objective.Title,11,gold,true,true);
    if(first!=null){DrawMobileSealText(r,ref y,first);DrawMobileSealText(r,ref y,session.RoomSealView(1));}
    else {DrawMobileObjectiveText(r,ref y,objective.ProgressText,10,pale);DrawMobileObjectiveText(r,ref y,objective.Hint,10,session.RoomCaptureContested?gold:jade);}
    DrawMobileObjectiveText(r,ref y,objective.SupportHint,10,muted);
    return;
   }
   DrawMobileObjectiveText(r,ref y,AdventureMapCaption,11,gold,true,true);
   DrawMobileObjectiveText(r,ref y,"阶段 "+session.DungeonWave+" / 3 · "+Mathf.CeilToInt(session.ModeRun.RemainingSeconds)+"秒",10,pale);
   DrawMobileObjectiveText(r,ref y,(session.ModeRun.Mode==ExpeditionModeKind.HoldPoint?session.ModeRun.HoldStateLabel+" ":"")+Mathf.RoundToInt(session.ModeRun.ObjectiveProgress*100)+"%",10,jade);
  }
  private void DrawMobileSealText(Rect r,ref float y,ChapterSealPresentation seal)
  {
   if(seal==null)return;
   DrawMobileObjectiveText(r,ref y,seal.Label,10,seal.Complete?jade:seal.Contested?gold:pale,seal.Occupied);
  }
 }
}
