using UnityEngine;
namespace Emberfall
{
    public partial class GameUI
    {
        private Vector2 bagGemScroll;
        private void DrawBagGems(MobilePanelLayout.Area area)
        {
            float u=MobileControls.Active?TouchRatio:1;Rect bounds=MobilePanelRect(area);
            var gems=session.Progression.Profile.attachments;
            if(gems.Count==0){Text(bounds,"尚未获得宝石",Mathf.RoundToInt(14*u),muted,false,false,TextAnchor.MiddleCenter);return;}
            float cellSize=MobileControls.Active?InventoryGridGeometry.MobileCellSize:InventoryGridGeometry.DesktopCellSize;
            var grid=new InventoryGridGeometry(bounds.width/u-16,cellSize);
            float contentHeight=Mathf.Ceil((float)gems.Count/grid.Columns)*grid.Stride*u;
            bagGemScroll=BeginTouchScroll("inventory-gems",bounds,bagGemScroll,new Rect(0,0,bounds.width-16*u,Mathf.Max(bounds.height,contentHeight)));
            for(int i=0;i<gems.Count;i++)
            {
                var gem=gems[i];var cell=grid.Tile(i);Rect hit=new Rect(cell.X*u,cell.Y*u,cell.Width*u,cell.Height*u);
                var item=new EntryRewardPreview{Key="gem:"+gem.id,Name=BuildCatalog.GemName(gem.mechanic),Rarity=gem.rarity,
                    Icon=AuthoredIconArt.ForTint(UIIconAtlas.Utility("gem"),GameBalance.RarityColor(gem.rarity)),Description=GemRewardDescription(gem.mechanic,gem.rarity,gem.upgradeRank)};
                DrawEntryRewardIcon(hit,item,u);
                Text(new Rect(hit.x+2*u,hit.yMax-16*u,hit.width-4*u,16*u),"+"+gem.upgradeRank,Mathf.RoundToInt(10*u),pale,true,false,TextAnchor.MiddleRight);
                if(gem.mounted)DrawWornIconBadge(hit,u,"已镶嵌");
                InspectRewardItem(hit,item,true);
                if(entryRewardPopup==item)
                {
                    // Convert scroll-content coordinates directly into the root UI space.
                    entryRewardViewport=bounds;entryRewardAnchorIsRoot=true;
                    entryRewardAnchor=new Rect(bounds.x+hit.x-bagGemScroll.x,bounds.y+hit.y-bagGemScroll.y,hit.width,hit.height);
                }
            }
            EndTouchScroll();
        }
        private const string GemBagOdds="普通45% · 稀有30% · 史诗20% · 传说5%";
        private string bagPreviewKey;
        private int bagPreviewIndex=-1;
        private EntryRewardPreview GemBagPreview(ItemSlot slot,bool owned)
        {
            string name=GameBalance.SlotName(slot)+"宝石袋";
            return new EntryRewardPreview{Key="merchant:gem-bag:"+(int)slot,Name=name,Rarity=Rarity.Legendary,Icon=UIIconAtlas.Utility("bag"),BagSlot=owned?(int)slot:-1,PreviewBagSlot=(int)slot,
                Description=name+"\n"+GemBagOdds};
        }
        private float DrawGemBagContents(EntryRewardPreview bag,float available,float u,bool draw)
        {
            var pool=ProgressionService.GemBagPool(session.Progression.Profile.heroClass,(ItemSlot)bag.PreviewBagSlot);
            if(pool.Length==0)return 0;
            int columns=4,count=pool.Length*4;
            float oddsHeight=Style(Mathf.RoundToInt(11*u),false,true).CalcHeight(new GUIContent(GemBagOdds),available*u)/u+12;
            float detailsTop=oddsHeight+Mathf.Ceil((float)count/columns)*52+8;
            if(bagPreviewKey!=bag.Key){bagPreviewKey=bag.Key;bagPreviewIndex=0;}
            bagPreviewIndex=Mathf.Clamp(bagPreviewIndex,0,Mathf.Max(0,count-1));
            var selectedGem=pool[bagPreviewIndex/4];var selectedRarity=(Rarity)(bagPreviewIndex%4);
            var selected=new EntryRewardPreview{Key="gem:"+selectedGem,Name=BuildCatalog.GemName(selectedGem),Rarity=selectedRarity,Description=GemRewardDescription(selectedGem,selectedRarity)};
            float detailHeight=32+DrawEntryRewardRows(selected,available,u,false);
            if(!draw)return detailsTop+detailHeight;
            Text(new Rect(0,0,available*u,oddsHeight*u),GemBagOdds,Mathf.RoundToInt(11*u),pale,false,true);
            for(int i=0;i<count;i++)
            {
                var rarity=(Rarity)(i%4);var gem=pool[i/4];
                var item=new EntryRewardPreview{Key="gem:"+gem,Name=BuildCatalog.GemName(gem),Rarity=rarity,Icon=AuthoredIconArt.ForTint(UIIconAtlas.Utility("gem"),GameBalance.RarityColor(rarity)),Description=GemRewardDescription(gem,rarity)};
                Rect hit=new Rect((i%columns)*52*u,(oddsHeight+i/columns*52)*u,44*u,44*u);
                DrawEntryRewardIcon(hit,item,u);
                if(MobileControls.Active?GUI.Button(hit,GUIContent.none,invisibleButton):hit.Contains(Event.current.mousePosition))bagPreviewIndex=i;
                if(bagPreviewIndex==i)Border(new Rect(hit.x-2*u,hit.y-2*u,hit.width+4*u,hit.height+4*u),jade,u);
            }
            if(bagPreviewIndex>=0&&bagPreviewIndex<count)
            {
                var gem=pool[bagPreviewIndex/4];var rarity=(Rarity)(bagPreviewIndex%4);
                var item=new EntryRewardPreview{Key="gem:"+gem,Name=BuildCatalog.GemName(gem),Rarity=rarity,Icon=AuthoredIconArt.ForTint(UIIconAtlas.Utility("gem"),GameBalance.RarityColor(rarity)),Description=GemRewardDescription(gem,rarity)};
                detailHeight=32+DrawEntryRewardRows(item,available,u,false);
                GUI.BeginGroup(new Rect(0,detailsTop*u,available*u,detailHeight*u));
                float nameWidth=Mathf.Min(Style(Mathf.RoundToInt(13*u),true,false).CalcSize(new GUIContent(item.Name)).x,(available-68)*u);
                Text(new Rect(0,0,nameWidth,26*u),item.Name,Mathf.RoundToInt(13*u),item.QualityColor,true,false,TextAnchor.MiddleLeft);
                DrawDetailTag(new Rect(nameWidth+8*u,2*u,60*u,22*u),GameBalance.RarityName(item.Rarity),item.QualityColor,u);
                GUI.BeginGroup(new Rect(0,32*u,available*u,(detailHeight-32)*u));DrawEntryRewardRows(item,available,u,true);GUI.EndGroup();GUI.EndGroup();
            }
            return detailsTop+detailHeight;
        }
    }
}
