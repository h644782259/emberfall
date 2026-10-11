using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool travelReturnPause;
        private string travelError;
        private HubNpcKind inventoryHubNpc;
        private bool merchantShopOpen;
        private bool merchantExchangeOpen;
        private bool MerchantServiceActive {get{return (MobileControls.Active?merchantShopOpen:inventoryHubNpc==HubNpcKind.Merchant)&&HubServicesAvailable;}}

        private bool HubServicesAvailable {get{return session!=null&&session.HasStarted&&!session.IsDead&&!session.InDungeon&&!session.PracticeActive;}}

        private void OpenHubService(HubNpcKind kind)
        {
            if(UITransitionBlocked||!HubServicesAvailable||!CanSwitchFunction)return;
            if(kind!=HubNpcKind.Merchant&&kind!=HubNpcKind.Blacksmith)return;
            PrepareFunctionSwitch();inventoryHubNpc=kind;panel=Panel.Inventory;
            if(MobileControls.Active)
            {
                merchantShopOpen=kind==HubNpcKind.Merchant;smithShopOpen=kind==HubNpcKind.Blacksmith;
                npcShopScroll=Vector2.zero;mobileInventoryNpcRequest=kind;
            }
            else
            {hubServiceScroll=Vector2.zero;inventoryFilter=-1;mobileInventoryNpcRequest=HubNpcKind.None;}
            if(kind==HubNpcKind.Blacksmith){smithPreviewMechanic=EquipmentMechanic.None;selectedItem=session.Progression.Profile.weaponId;}
            if(kind==HubNpcKind.Merchant){bool first=session.Progression.Profile.pendingFirstClearReward&&!session.Progression.Profile.firstClearRewardClaimed;merchantGemRarity=Rarity.Common;SelectMerchantMode(first?1:0,true);}
            session.SetUIBlocking(true);BlockUITransition();
        }

        private bool smithShopOpen;
        private bool SmithServiceActive {get{return (MobileControls.Active?smithShopOpen:inventoryHubNpc==HubNpcKind.Blacksmith)&&HubServicesAvailable;}}
        private Vector2 npcShopScroll;
        private void DrawMerchantShop(){DrawMerchantService();}

        private void DrawBlacksmithShop(){DrawSmithService();}

        private string HubNpcServiceSubtitle(string fallback) {return fallback;}

        private string HubInventoryTitle
        {
            get
            {
                if (!HubServicesAvailable) return "行囊";
                return inventoryHubNpc == HubNpcKind.Merchant ? "商人 · 行囊与补给" :
                    inventoryHubNpc == HubNpcKind.Blacksmith ? "铁匠 · 装备与强化" : "行囊";
            }
        }

        private string HubInventoryHint
        {
            get
            {
                if (!HubServicesAvailable) return "";
                return inventoryHubNpc == HubNpcKind.Merchant ? "下方购买药剂 · 背包右侧出售闲置装备" :
                    inventoryHubNpc == HubNpcKind.Blacksmith ? "右侧强化装备部位 · 换装继承部位等级" : "";
            }
        }

        private void OpenTravelMap()
        {
            if (UITransitionBlocked || session == null || !session.HasStarted || session.IsDead || exitRequest.Open || saveFlow.Open) return;
            if(!CanSwitchFunction)return;
            PrepareFunctionSwitch();travelReturnPause=false;
            travelError = null;travelMapTab=0;
            CancelHotbarPointer();
            panel = Panel.TravelMap;
            session.SetUIBlocking(true);
            session.SetPaused(false);
            BlockUITransition();
        }

        private bool CloseTravelMap()
        {
            if (panel != Panel.TravelMap) return false;
            panel = Panel.None;
            session.SetUIBlocking(false);
            session.SetPaused(travelReturnPause);
            travelReturnPause = false;
            BlockUITransition();
            return true;
        }

        private int travelMapTab;
        private void DrawTravelMap()
        {
            float u=MobileControls.Active?TouchRatio:1.35f;
            float w=Mathf.Min(700,width/u-24),h=Mathf.Min(450,height/u-24);
            Rect r=new Rect((width-w*u)*.5f,(height-h*u)*.5f,w*u,h*u);
            blockedRects.Add(r);Box(r,jade);
            DrawAdventureMapCaption(new Rect(r.x+16*u,r.y+10*u,r.width-76*u,30*u),Mathf.RoundToInt(20*u),pale,"地图 · ",TextAnchor.MiddleLeft);
            Rect close=new Rect(r.xMax-52*u,r.y+6*u,44*u,44*u);

            if(PopupCloseButton(close,!UITransitionBlocked)){CloseTravelMap();return;}
            if(PauseSidebarTab(new Rect(r.x+16*u,r.y+50*u,120*u,44*u),"当前地图",travelMapTab==0,u))travelMapTab=0;
            if(PauseSidebarTab(new Rect(r.x+144*u,r.y+50*u,120*u,44*u),"城镇旅行",travelMapTab==1,u))travelMapTab=1;
            Rect body=new Rect(r.x+16*u,r.y+102*u,r.width-32*u,r.height-114*u);
            if(travelMapTab==0){DrawExpandedMap(body,u);return;}
            string hint=!string.IsNullOrEmpty(travelError)?travelError:!session.CanOpenTravelMap?"挑战中或正在战斗，暂不可旅行。":"";
            bool hasNotice=!string.IsNullOrEmpty(hint);
            if(hasNotice)Text(new Rect(body.x,body.y,body.width,28*u),hint,Mathf.RoundToInt(11*u),string.IsNullOrEmpty(travelError)?muted:gold,false,true);
            var profile=session.Progression.Profile;int mask=HubTravelRules.UnlockedMask(profile.unlockedHubMask,profile.level,profile.clearedRuns);
            for(int hub=0;hub<HubTravelRules.Count;hub++)
            {
                bool unlocked=HubTravelRules.IsUnlocked(mask,hub),current=hub==session.CurrentHub;
                var layout=new TownTravelCardLayout(body.width/u,body.height/u,hasNotice,hub);
                Rect tile=TownTravelRect(layout.Tile,body,u),art=TownTravelRect(layout.Image,body,u);
                Fill(tile,card);DrawTownPostcard(art,hub,unlocked);Border(tile,current?gold:jade);
                Rect title=TownTravelRect(layout.Title,body,u);Fill(new Rect(art.x,art.y,art.width,title.height+12*u),new Color(.025f,.04f,.055f,.58f));
                Text(title,HubTravelRules.Name(hub),Mathf.RoundToInt(16*u),unlocked?pale:muted,true,false,TextAnchor.MiddleCenter);
                if(!unlocked)
                {
                    Rect hintArea=TownTravelRect(layout.UnlockHint,body,u);Fill(hintArea,new Color(.025f,.04f,.055f,.72f));
                    Text(hintArea,HubTravelRules.UnlockHint(hub),Mathf.RoundToInt(11*u),pale,false,true,TextAnchor.MiddleCenter);
                }
                if(Button(TownTravelRect(layout.Action,body,u),current?"当前城镇":unlocked?"前往":"尚未解锁",jade,unlocked&&!current&&session.CanOpenTravelMap&&!UITransitionBlocked))
                {
                    if(session.TravelToHub(hub)){travelReturnPause=false;CloseTravelMap();return;}
                    travelError=string.IsNullOrEmpty(session.Progression.LastError)?"旅行未完成，请安全返回营地后重试。":session.Progression.LastError;
                }
            }
        }
        private static Rect TownTravelRect(MobilePanelLayout.Area area,Rect body,float u)
        {return new Rect(body.x+area.X*u,body.y+area.Y*u,area.Width*u,area.Height*u);}
        private static readonly string[] TownTravelArtPaths={"TownTravel/WhisperCamp","TownTravel/RedrockOutpost","TownTravel/StarwatchCity"};
        private readonly Texture2D[] townTravelArt=new Texture2D[HubTravelRules.Count];
        private bool townTravelArtLoaded;
        private void PrewarmTownTravelArtwork()
        {
            if(townTravelArtLoaded)return;
            for(int hub=0;hub<townTravelArt.Length;hub++)townTravelArt[hub]=Resources.Load<Texture2D>(TownTravelArtPaths[hub]);
            townTravelArtLoaded=true;
        }
        private void DrawTownPostcard(Rect area,int hub,bool unlocked)
        {
            // Imported paintings are prepared before gameplay. The cards crop
            // their centered architecture without stretching or per-frame baking.
            Fill(area,new Color(.04f,.07f,.10f));
            if(hub>=0&&hub<townTravelArt.Length&&townTravelArt[hub]!=null)
            {
                Color before=GUI.color;
                GUI.color=new Color(before.r,before.g,before.b,before.a*(unlocked?1:.5f));
                GUI.DrawTexture(area,townTravelArt[hub],ScaleMode.ScaleAndCrop,true);GUI.color=before;
            }
            Border(area,unlocked?jade*.4f:muted*.3f);
        }

        private void DrawExpandedMap(Rect body,float u)
        {
            float mapWidth=body.width-132*u,size=Mathf.Min(mapWidth,body.height);
            Rect map=new Rect(body.x+(mapWidth-size)*.5f,body.y+(body.height-size)*.5f,size,size);
            Fill(map,ink);DrawMinimapTerrain(map);Border(map,jade);
            Color portal=new Color(.4f,.65f,1),camp=new Color(.9f,.9f,.85f),ordinary=new Color(.54f,.77f,.5f),aggro=new Color(1,.58f,.35f),boss=new Color(1,.32f,.3f);
            if(!session.InDungeon){MapDot(map,new Vector3(0,0,11),portal,7*u);MapDot(map,new Vector3(0,0,-10),camp,6*u);}
            else if(session.DungeonReturnAvailable)MapDot(map,session.DungeonReturnPosition,portal,7*u);
            foreach(var enemy in session.Enemies)if(enemy!=null&&!enemy.IsDead)
                MapDot(map,enemy.transform.position,enemy.IsBoss?boss:enemy.Tier==EnemyController.ThreatTier.Elite?gold:enemy.IsAggro?aggro:ordinary,enemy.IsBoss?7*u:4*u);
            if(session.Player!=null)MapDot(map,session.Player.transform.position,jade,7*u);
            string[] names={"你的位置","入口 / 出口","营地","普通敌人","战斗中","精英","首领"};Color[] colors={jade,portal,camp,ordinary,aggro,gold,boss};
            float x=body.xMax-116*u;
            Text(new Rect(x,body.y,116*u,22*u),"图例",Mathf.RoundToInt(13*u),pale,true);
            for(int i=0;i<names.Length;i++)
            {
                float y=body.y+(27+i*18)*u;Fill(new Rect(x,y+5*u,8*u,8*u),colors[i]);
                Text(new Rect(x+16*u,y,100*u,18*u),names[i],Mathf.RoundToInt(11*u),pale);
            }
        }
    }
}
