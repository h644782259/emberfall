using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private sealed class PickupLine {public string Text;public Color Tint;public float Started;}
        private readonly List<PickupLine> pickupLines=new List<PickupLine>(4);
        private void AddPickupLine(string text,Color tint)
        {
            if(session.IsDead||session.PracticeActive||!session.HasStarted)return;
            if(pickupLines.Count==4)pickupLines.RemoveAt(0);
            pickupLines.Add(new PickupLine{Text=text,Tint=tint,Started=Time.unscaledTime});
        }
        private void QueueSupplyNotice(int coins,int potions)
        {
            if(coins>0)AddPickupLine("获得金币 ×"+coins,gold);
            if(potions>0)AddPickupLine("拾取生命药剂 ×"+potions,jade);
        }
        private void DrawPickupLines()
        {
            pickupLines.RemoveAll(line=>Time.unscaledTime-line.Started>=1.5f);
            if(panel!=Panel.None||session.Paused||session.InputBlocked||session.IsDead)return;
            float u=MobileControls.Active?TouchRatio:1;
            float available=MobileControls.Active?Mathf.Min(220,MobileControls.Layout.Potion.X-24):240;
            float bottom=MobileControls.Active?MobileControls.Layout.Height-34:height-50;
            for(int i=0;i<pickupLines.Count;i++)
            {
                var line=pickupLines[i];float age=Time.unscaledTime-line.Started;
                Rect row=MobileControls.Active?TouchRect(12,bottom-(pickupLines.Count-i)*26,Mathf.Max(100,available),24):new Rect(16,bottom-(pickupLines.Count-i)*26,available,24);
                float before=controlOpacity;controlOpacity*=Mathf.Clamp01((1.5f-age)/.2f);
                Fill(row,new Color(.015f,.025f,.035f,.65f));
                Text(new Rect(row.x+6*u,row.y,row.width-12*u,row.height),line.Text,Mathf.RoundToInt(12*u),line.Tint,true,false,TextAnchor.MiddleLeft);
                controlOpacity=before;
            }
        }
        private sealed class LootNotice { public string Id,Name;public ItemSlot Slot;public Rarity Rarity;public int Level;public ItemData Appearance;public float Started=-1; }
        private readonly List<LootNotice> lootNotices=new List<LootNotice>();
        private ProgressionService lootNoticeOwner;private string lootNoticeSlot;
        private void BindLootNotices()
        {
            var p=session.Progression;
            if(lootNoticeOwner==p&&lootNoticeSlot==p.CurrentSlotId)return;
            ReleaseLootNotices();lootNoticeOwner=p;lootNoticeSlot=p.CurrentSlotId;p.LootCollected+=QueueLootNotice;p.SuppliesCollected+=QueueSupplyNotice;
        }
        private void ReleaseLootNotices()
        {if(lootNoticeOwner!=null){lootNoticeOwner.LootCollected-=QueueLootNotice;lootNoticeOwner.SuppliesCollected-=QueueSupplyNotice;}lootNoticeOwner=null;lootNotices.Clear();pickupLines.Clear();}
        private void QueueLootNotice(ItemData item)
        {
            if(item!=null&&lootNoticeOwner==session.Progression&&lootNoticeSlot==session.Progression.CurrentSlotId)
                AddPickupLine("拾取 "+item.name+" ×1",GameBalance.RarityColor(item.rarity));
            if(item==null||!session.InDungeon||session.IsDead||session.PracticeActive||lootNoticeOwner!=session.Progression||lootNoticeSlot!=session.Progression.CurrentSlotId)return;
            if(lootNotices.Exists(n=>n.Id==item.id))return;
            lootNotices.Insert(0,new LootNotice{Id=item.id,Name=item.name,Slot=item.slot,Rarity=item.rarity,Level=item.level,Appearance=session.Progression.PreviewUpgrade(item,item.upgradeLevel)});
        }
        private bool LootNoticesVisible
        {
            get
            {
                if(!session.InDungeon||session.IsDead||session.Paused||session.PracticeActive||panel!=Panel.None||session.InputBlocked||session.RunChoices.AwaitingChoice||session.RoomBranchChoiceOpen)return false;
                // Notifications have priority in the shared compact-phone slot;
                // queued pickups keep their full display time when it becomes free.
                return !MobileControls.Active||!MobileControls.Layout.LootNotice(0).Overlaps(MobileControls.Layout.Notice)||string.IsNullOrEmpty(session.Notification);
            }
        }
        private bool MobileLootReplacesInformation
        {get{return MobileControls.Active&&LootNoticesVisible&&VisibleLootNoticeCount>0&&MobileControls.Layout.LootNotice(0).Overlaps(MobileControls.Layout.AdventureStatus);}}
        private int VisibleLootNoticeCount {get{int count=Mathf.Min(lootNotices.Count,MobileControls.Active?2:3);if(MobileControls.Active)while(count>0&&MobileControls.Layout.LootNotice(count-1).Width<=0)count--;return count;}}
        private Rect LootNoticeRect(int index)
        {if(MobileControls.Active)return TouchRect(MobileControls.Layout.LootNotice(index));float w=Mathf.Min(208,width*.35f);return new Rect(width-w-12,DesktopMinimapRect.yMax+10+index*60,w,54);}
        private Rect LootNoticeEquipRect(Rect r)
        {float u=MobileControls.Active?TouchRatio:1;return new Rect(r.xMax-52*u,r.y+(MobileControls.Active?0:5)*u,48*u,44*u);}
        private ItemData LootNoticeUpgrade(LootNotice n)
        {var item=session.Progression.Profile.inventory.Find(v=>v!=null&&v.id==n.Id);return IsEquipmentUpgrade(item)?item:null;}
        private float LootNoticeDuration {get{return MobileControls.Active?3.5f:5f;}}
        private const float LootNoticeFadeSeconds=.8f;
        private void PrepareLootNotices()
        {
            BindLootNotices();
            if(!session.InDungeon||session.IsDead||session.PracticeActive){lootNotices.Clear();return;}
            if(!LootNoticesVisible){foreach(var n in lootNotices)n.Started=-1;return;}
            lootNotices.RemoveAll(n=>n.Started>=0&&Time.time-n.Started>=LootNoticeDuration);
            for(int i=0;i<VisibleLootNoticeCount;i++)
            {
                var notice=lootNotices[i];if(notice.Started<0)notice.Started=Time.time;
                Rect r=LootNoticeRect(i);
                if(LootNoticeUpgrade(notice)==null)continue;
                Rect hit=LootNoticeEquipRect(r);blockedRects.Add(hit);
                if(hit.Contains(Mouse)&&(Event.current.type==EventType.MouseDown||Event.current.type==EventType.MouseUp||Event.current.type==EventType.MouseDrag||Event.current.type==EventType.ScrollWheel))
                {
                    if(GUI.enabled&&Event.current.type==EventType.MouseDown&&LootNoticeEquipRect(r).Contains(Mouse))
                    {var item=LootNoticeUpgrade(notice);if(item!=null){bool saved=session.Progression.Equip(item.id);Feedback(saved,saved?"装备已穿戴":session.Progression.LastError);}}
                    Event.current.Use();
                }
            }
        }
        private void DrawLootNotices()
        {
            DrawPickupLines();
            if(!LootNoticesVisible)return;
            float u=MobileControls.Active?TouchRatio:1;
            for(int i=0;i<VisibleLootNoticeCount;i++)
            {
                var notice=lootNotices[i];Rect r=LootNoticeRect(i);Color tint=GameBalance.RarityColor(notice.Rarity);bool upgrade=LootNoticeUpgrade(notice)!=null;
                float previousOpacity=controlOpacity;
                float remaining=notice.Started<0?LootNoticeDuration:LootNoticeDuration-(Time.time-notice.Started);
                controlOpacity*=Mathf.SmoothStep(0,1,Mathf.Clamp01(remaining/LootNoticeFadeSeconds));
                try
                {
                Fill(r,new Color(.025f,.045f,.065f,.94f));Border(r,tint);
                float iconSize=MobileControls.Active?28:36;float textX=MobileControls.Active?40:48;
                Rect icon=new Rect(r.x+6*u,r.center.y-iconSize*u/2,iconSize*u,iconSize*u);Border(icon,tint);DrawIcon(icon,UIIconAtlas.EquipmentCardIcon(notice.Appearance,session.Progression.Profile.heroClass),tint);
                Text(new Rect(r.x+textX*u,r.y+3*u,r.width-(textX+(upgrade?56:6))*u,(MobileControls.Active?20:26)*u),notice.Name,Mathf.RoundToInt(11*u),pale,false,false);
                Text(new Rect(r.x+textX*u,r.y+(MobileControls.Active?24:32)*u,r.width-(textX+(upgrade?56:6))*u,17*u),GameBalance.RarityName(notice.Rarity),Mathf.RoundToInt(10*u),tint);
                if(upgrade){Rect action=LootNoticeEquipRect(r);Fill(action,new Color(.08f,.3f,.18f));Border(action,jade);Text(action,"穿戴",Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleCenter);}
                }
                finally {controlOpacity=previousOpacity;}
            }
        }
    }
}
