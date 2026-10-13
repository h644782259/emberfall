using UnityEngine;
namespace Emberfall
{
    public partial class GameUI
    {
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
            int columns=4,count=pool.Length*4;
            float oddsHeight=Style(Mathf.RoundToInt(11*u),false,true).CalcHeight(new GUIContent(GemBagOdds),available*u)/u+12;
            float detailsTop=oddsHeight+Mathf.Ceil((float)count/columns)*52+8;
            if(bagPreviewKey!=bag.Key){bagPreviewKey=bag.Key;bagPreviewIndex=0;}
            if(!draw)return detailsTop+150;
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
                GUI.BeginGroup(new Rect(0,detailsTop*u,available*u,150*u));
                Text(new Rect(0,0,available*u,26*u),item.Name,Mathf.RoundToInt(13*u),item.QualityColor,true);
                DrawItemDetailTags(new Rect(0,28*u,available*u,22*u),item,u);
                GUI.BeginGroup(new Rect(0,56*u,available*u,94*u));DrawEntryRewardRows(item,available,u,true);GUI.EndGroup();GUI.EndGroup();
            }
            return detailsTop+150;
        }
    }
}
