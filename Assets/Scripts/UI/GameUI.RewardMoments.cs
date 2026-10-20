using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private ProgressionService rewardMomentOwner;
        private string rewardMomentSlot;
        private long rewardMomentSeen;
        private RewardMoment rewardMoment;
        private float rewardMomentStarted;
        private Panel rewardMomentPanel;
        private CollectionModelPreview rewardMomentModel;
        private void ClearRewardMoment()
        {rewardMoment=null;if(rewardMomentModel!=null)rewardMomentModel.Dispose();rewardMomentModel=null;}
        private bool RewardMomentSafe {get{return session.IsInCamp&&!session.Paused&&!session.IsDead&&(panel==Panel.HubUtility||panel==Panel.Fashion||panel==Panel.Inventory);}}
        private ItemData RewardUpgradeItem()
        {
            if(rewardMoment==null||rewardMoment.Item==null)return null;
            var item=session.Progression.Profile.inventory.Find(v=>v!=null&&v.id==rewardMoment.Item.id);
            return IsEquipmentUpgrade(item)?item:null;
        }
        private Rect RewardEquipRect(Rect area)
        {float u=MobileControls.Active?TouchRatio:1;return new Rect(area.x+8*u,area.yMax-48*u,area.width-16*u,44*u);}
        private void DrawRewardEquip(Rect area)
        {
            Rect action=RewardEquipRect(area);Fill(action,new Color(.08f,.3f,.18f));Border(action,jade);
            Text(action,"穿戴",MobileControls.Active?TouchFont(14):14,pale,true,false,TextAnchor.MiddleCenter);
        }
        private bool CompactGemRewardMoment => rewardMoment!=null&&rewardMoment.Kind==RewardMomentKind.MechanicExchange&&rewardMoment.Item==null&&rewardMoment.Fashion==null;
        private Rect RewardMomentRect()
        {
            float u=MobileControls.Active?TouchRatio:1;
            float w=Mathf.Min((CompactGemRewardMoment?330:RewardMomentSafe?330:196)*u,width-24*u),h=Mathf.Min((CompactGemRewardMoment?64:RewardMomentSafe?252:RewardUpgradeItem()!=null?104:52)*u,height-84*u);
            return new Rect(width-w-12*u,64*u,w,h);
        }
        // Runs before panel input, so skip cannot click through to a transaction below.
        private void PrepareRewardMoment()
        {
            var p=session.Progression;var latest=p.LastRewardMoment;
            if(rewardMomentOwner!=p||rewardMomentSlot!=p.CurrentSlotId)
            {ClearRewardMoment();rewardMomentOwner=p;rewardMomentSlot=p.CurrentSlotId;rewardMomentSeen=latest==null?0:latest.Sequence;return;}
            if(latest!=null&&latest.Sequence!=rewardMomentSeen)
            {
                rewardMomentSeen=latest.Sequence;ClearRewardMoment();
                if(session.PracticeActive||session.IsDead||session.Paused||latest.SlotId!=p.CurrentSlotId||latest.HeroClass!=p.Profile.heroClass)return;
                if(session.InDungeon&&latest.Kind==RewardMomentKind.StrictUpgrade)return;
                rewardMoment=latest;rewardMomentStarted=Time.unscaledTime;rewardMomentPanel=panel;
                var cue=latest.Kind==RewardMomentKind.FirstCore||latest.Kind==RewardMomentKind.FashionExchange?SoundCue.Victory:
                    latest.Kind==RewardMomentKind.Ascension?SoundCue.LevelUp:latest.Kind==RewardMomentKind.MechanicExchange?SoundCue.Loot:
                    latest.Kind==RewardMomentKind.StrictUpgrade?SoundCue.Cast:SoundCue.UI;
                GameAudio.Play(RewardMomentSafe?cue:SoundCue.UI);
            }
            if(rewardMoment==null)return;
            if(session.RunChoices.AwaitingChoice){ClearRewardMoment();return;}
            if(panel!=rewardMomentPanel||session.Paused||session.IsDead||session.PracticeActive||Time.unscaledTime-rewardMomentStarted>(RewardUpgradeItem()!=null?8f:EffectPreferences.ReducedEffects?1.5f:3.2f))
            {ClearRewardMoment();return;}
            bool interactive=RewardMomentSafe||RewardUpgradeItem()!=null;
            if(interactive)blockedRects.Add(RewardMomentRect());
            if(interactive&&RewardMomentRect().Contains(Mouse)&&(Event.current.type==EventType.MouseDown||Event.current.type==EventType.MouseUp||Event.current.type==EventType.MouseDrag||Event.current.type==EventType.ScrollWheel))
            {
                if(Event.current.type==EventType.MouseDown)
                {
                    var item=RewardUpgradeItem();
                    if(item!=null&&RewardEquipRect(RewardMomentRect()).Contains(Mouse))
                    {bool saved=session.Progression.Equip(item.id);Feedback(saved,saved?"装备已穿戴":session.Progression.LastError);if(saved)ClearRewardMoment();}
                    else if(CompactGemRewardMoment||RewardMomentSafe&&RewardMomentSkipRect(RewardMomentRect()).Contains(Mouse))ClearRewardMoment();
                }
                BlockUITransition();Event.current.Use();
            }
        }
        private Rect RewardMomentSkipRect(Rect area)
        {float u=MobileControls.Active?TouchRatio:1;return new Rect(area.xMax-52*u,area.y+2*u,48*u,48*u);}
        private string RewardMomentTitle()
        {
            if(rewardMoment.Attachment!=null)return BuildCatalog.GemName(rewardMoment.Attachment.mechanic);
            if(rewardMoment.Item!=null)return rewardMoment.Item.name;
            if(rewardMoment.Fashion!=null)return rewardMoment.Fashion.name;
            if(rewardMoment.MaterialsDelta!=0)return "星烬碎片 ×"+rewardMoment.MaterialsDelta;
            if(rewardMoment.ThreadsDelta!=0)return "星纹 ×"+rewardMoment.ThreadsDelta;
            if(rewardMoment.GoldDelta!=0)return "金币 ×"+rewardMoment.GoldDelta;
            return "奖励已获得";
        }
        private Texture2D RewardMomentIcon()
        {
            if(rewardMoment.Attachment!=null)return UIIconAtlas.Utility("gem");
            if(rewardMoment.Item!=null)return UIIconAtlas.EquipmentCardIcon(rewardMoment.Item,session.Progression.Profile.heroClass);
            if(rewardMoment.Fashion!=null)return UIIconAtlas.FashionCardIcon(rewardMoment.Fashion,session.Progression.Profile.heroClass);
            return rewardMoment.MaterialsDelta!=0?UIIconAtlas.Reward(1):rewardMoment.ThreadsDelta!=0?UIIconAtlas.Reward(2):rewardMoment.GoldDelta!=0?UIIconAtlas.Reward(0):UIIconAtlas.Utility("bag");
        }
        private void DrawCompactGemRewardMoment(Rect area,float u)
        {
            var rarity=rewardMoment.Attachment!=null?rewardMoment.Attachment.rarity:Rarity.Rare;
            Color accent=GameBalance.RarityColor(rarity);
            Fill(area,new Color(.025f,.045f,.065f,.98f));Border(area,accent);
            Rect icon=new Rect(area.x+8*u,area.center.y-24*u,48*u,48*u);
            DrawEntryRewardIcon(icon,new EntryRewardPreview{Icon=AuthoredIconArt.ForTint(RewardMomentIcon(),accent),Rarity=rarity},u);
            Rect tag=new Rect(area.xMax-68*u,area.center.y-12*u,60*u,24*u);
            Rect name=new Rect(icon.xMax+10*u,area.y,tag.x-icon.xMax-20*u,area.height);
            string title=RewardMomentTitle();int font=Mathf.RoundToInt(14*u);
            while(font>Mathf.RoundToInt(10*u)&&Style(font,true,false).CalcSize(new GUIContent(title)).x>name.width)font--;
            Text(name,title,font,pale,true,false,TextAnchor.MiddleLeft);
            DrawDetailTag(tag,GameBalance.RarityName(rarity),accent,u);
        }
        private void DrawRewardMoment()
        {
            if(rewardMoment==null)return;
            if(session.RunChoices.AwaitingChoice){ClearRewardMoment();return;}
            Rect area=RewardMomentRect();float u=MobileControls.Active?TouchRatio:1;
            if(CompactGemRewardMoment){DrawCompactGemRewardMoment(area,u);return;}
            bool canEquip=RewardUpgradeItem()!=null;
            Color accent=rewardMoment.Attachment!=null?GameBalance.RarityColor(rewardMoment.Attachment.rarity):rewardMoment.Item!=null?GameBalance.RarityColor(rewardMoment.Item.rarity):rewardMoment.Fashion!=null?GameBalance.RarityColor(rewardMoment.Fashion.rarity):jade;
            Fill(area,new Color(.025f,.045f,.065f,.98f));Border(area,accent);
            if(!RewardMomentSafe)
            {
                DrawIcon(new Rect(area.x+6*u,area.y+6*u,38*u,38*u),RewardMomentIcon(),accent);
                Text(new Rect(area.x+50*u,area.y+5*u,area.width-56*u,42*u),RewardMomentTitle(),Mathf.RoundToInt(12*u),pale,false,true);if(canEquip)DrawRewardEquip(area);return;
            }
            DrawIcon(RewardMomentSkipRect(area),UIIconAtlas.Utility("cancel"),muted);
            string title=RewardMomentTitle();
            Text(new Rect(area.x+10*u,area.y+6*u,area.width-66*u,38*u),title,Mathf.RoundToInt(14*u),accent,true,true);
            int rows=(rewardMoment.MaterialsDelta!=0?1:0)+(rewardMoment.ThreadsDelta!=0?1:0)+(rewardMoment.GoldDelta!=0?1:0);
            Rect body=new Rect(area.x+8*u,area.y+52*u,area.width-16*u,area.height-60*u-(canEquip?48*u:0));
            float resources=rows*32*u;
            if(rewardMoment.Attachment!=null)
            {
                var a=rewardMoment.Attachment;
                DrawIcon(new Rect(body.x,body.y,64*u,64*u),UIIconAtlas.Utility("gem"),GameBalance.RarityColor(a.rarity));
                Text(new Rect(body.x+72*u,body.y,body.width-72*u,64*u),GameBalance.RarityName(a.rarity)+" · "+a.upgradeRank+"/9阶 · 升华 "+Mathf.Max(0,a.ascensionRank)+"/3",Mathf.RoundToInt(13*u),pale,false,true);
            }
            if(rewardMoment.Item!=null||rewardMoment.Fashion!=null)
            {
                var p=session.Progression;var item=rewardMoment.Item;var fashion=rewardMoment.Fashion;
                var weapon=p.Equipped(ItemSlot.Weapon);var armor=p.Equipped(ItemSlot.Armor);var relic=p.Equipped(ItemSlot.Relic);
                var wings=p.EquippedFashion(FashionSlot.Wings);var wornWeapon=p.EquippedFashion(FashionSlot.Weapon);
                if(item!=null){if(item.slot==ItemSlot.Weapon){weapon=item;wornWeapon=null;}else if(item.slot==ItemSlot.Armor)armor=item;else relic=item;}
                if(fashion!=null){if(fashion.slot==FashionSlot.Wings)wings=fashion;else wornWeapon=fashion;}
                if(rewardMomentModel==null)rewardMomentModel=new CollectionModelPreview();
                rewardMomentModel.SetComposition(CollectionPreviewComposition.Full);rewardMomentModel.SetEquipmentFraming(item!=null,true);rewardMomentModel.SetEquipmentHighlight(item==null?-1:(int)item.slot);rewardMomentModel.SetYaw(20);
                Rect art=new Rect(body.x,body.y,body.width,Mathf.Max(1,body.height-resources));
                rewardMomentModel.SetViewport(art.width*Mathf.Abs(GUI.matrix.m00),art.height*Mathf.Abs(GUI.matrix.m11),MobileControls.Active);
                var texture=rewardMomentModel.RenderSafe(p.Profile.heroClass,weapon,armor,relic,wings,wornWeapon);
                if(texture!=null)GUI.DrawTexture(art,texture,ScaleMode.ScaleToFit,false);
                if(!EffectPreferences.ReducedEffects)DrawRewardRadiance(art,accent,Mathf.Clamp01((Time.unscaledTime-rewardMomentStarted)/1.2f));
            }
            float y=body.yMax-resources;
            if(rewardMoment.GoldDelta!=0){DrawRewardToken(new Rect(body.x,y,body.width,32*u),0,rewardMoment.GoldDelta,u);y+=32*u;}
            if(rewardMoment.MaterialsDelta!=0){DrawRewardToken(new Rect(body.x,y,body.width,32*u),1,rewardMoment.MaterialsDelta,u);y+=32*u;}
            if(rewardMoment.ThreadsDelta!=0)DrawRewardToken(new Rect(body.x,y,body.width,32*u),2,rewardMoment.ThreadsDelta,u);
            if(canEquip)DrawRewardEquip(area);
        }
    }
}
