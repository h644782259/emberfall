using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool basicAttackSelected;
        private Vector2 basicAttackDetailScroll;
        private void DrawBasicAttackNode(Rect r,float u)
        {
            int tier=BasicAttackGrowth.Tier(session.Progression.Profile,session.Player.HeroClass);
            Surface(r,new Color(.12f,.095f,.045f,.96f));SurfaceFrame(r,basicAttackSelected?gold:new Color(.46f,.35f,.16f));
            Fill(new Rect(r.x,r.y,3*u,r.height),gold);
            DrawIcon(new Rect(r.x+12*u,r.center.y-16*u,32*u,32*u),UIIconAtlas.Utility("attack"),gold);
            Text(new Rect(r.x+54*u,r.y+4*u,r.width-160*u,24*u),"普通攻击",Mathf.RoundToInt(16*u),pale,true);
            Text(new Rect(r.x+54*u,r.y+29*u,r.width-160*u,18*u),"基础攻击",Mathf.RoundToInt(10*u),gold);
            SkillStateTag(new Rect(r.xMax-88*u,r.center.y-13*u,76*u,26*u),tier+" / 4 档",gold,Mathf.RoundToInt(12*u));
            if(MobileControls.Active?MobileSkillRowClicked(r):GUI.Button(r,GUIContent.none,invisibleButton))
            {basicAttackSelected=true;blinkFollowupSelected=false;CancelMobileScroll();}
        }
        private void DrawBasicAttackShape(Rect r,int tier,HeroClass hero,Color tint,float u)
        {
            Vector2 origin=new Vector2(r.center.x,r.yMax-6*u);
            bool melee=hero==HeroClass.Vanguard;
            int count=melee?7:BasicAttackGrowth.Shots(tier);
            for(int i=0;i<count;i++)
            {
                float degrees=melee?Mathf.Lerp(-BasicAttackGrowth.Angle(tier)*.5f,BasicAttackGrowth.Angle(tier)*.5f,i/6f):tier<3?0:Mathf.Lerp(-30,30,i/(float)(count-1));
                float distance=melee?(22+5*tier)*u:tier==2?(i==0?24:43)*u:40*u;
                Vector2 end=origin+new Vector2(Mathf.Sin(degrees*Mathf.Deg2Rad),-Mathf.Cos(degrees*Mathf.Deg2Rad))*distance;
                DrawIcon(new Rect(end.x-5*u,end.y-5*u,10*u,10*u),UIIconAtlas.Utility(melee?"attack":hero==HeroClass.Ranger?"attack":"gem"),tint);
            }
        }
        private void DrawBasicAttackDetail(Rect r,float u)
        {
            var p=session.Progression;int tier=BasicAttackGrowth.Tier(p.Profile,p.Profile.heroClass),cost=BasicAttackGrowth.Cost(tier);
            Surface(r,new Color(.012f,.024f,.038f,.99f));SurfaceFrame(r,gold*.4f);
            Text(new Rect(r.x+12*u,r.y+7*u,r.width-120*u,30*u),"普通攻击",Mathf.RoundToInt(18*u),pale,true);
            SkillStateTag(new Rect(r.xMax-96*u,r.y+10*u,84*u,24*u),"基础攻击",gold,Mathf.RoundToInt(11*u));
            Rect area=new Rect(r.x+10*u,r.y+46*u,r.width-20*u,Mathf.Max(20*u,r.height-112*u));
            bool compact=MobileControls.Active;
            float w=area.width-(compact?0:16*u);int columns=compact||w>=480*u?4:2;
            float gap=(compact?5:8)*u,tileWidth=(w-gap*(columns-1))/columns,tileHeight=(compact?94:134)*u;
            float contentHeight=(4/columns)*(tileHeight+gap)+(compact?0:32*u);
            basicAttackDetailScroll=BeginTouchScroll("basic-attack-detail",area,basicAttackDetailScroll,new Rect(0,0,w,contentHeight));
            for(int i=1;i<=4;i++)
            {
                bool current=i==tier;Color accent=current?gold:i<tier?jade:muted;
                Rect tile=new Rect(((i-1)%columns)*(tileWidth+gap),((i-1)/columns)*(tileHeight+gap),tileWidth,tileHeight);
                Fill(tile,current?new Color(.18f,.135f,.055f):card);Border(tile,accent,current?2*u:u);
                if(compact)
                {
                    float iconScale=Mathf.Min(42*u/54f,(tile.width-6*u)/70f);
                    Text(new Rect(tile.x+2*u,tile.y+3*u,tile.width-4*u,20*u),i+"档",Mathf.RoundToInt(11*u),accent,true,false,TextAnchor.MiddleCenter);
                    DrawBasicAttackShape(new Rect(tile.x+3*u,tile.y+26*u,tile.width-6*u,42*u),i,p.Profile.heroClass,accent,iconScale);
                    string label=p.Profile.heroClass==HeroClass.Vanguard?BasicAttackGrowth.Angle(i).ToString("0")+"°":i==1?"单发":i==2?"二连发":BasicAttackGrowth.Shots(i)+"发扇形";
                    DrawGemEffectText(new Rect(tile.x+2*u,tile.y+69*u,tile.width-4*u,21*u),label,Mathf.RoundToInt(10*u),current?pale:accent,true,TextAnchor.MiddleCenter);
                    continue;
                }
                Text(new Rect(tile.x+8*u,tile.y+5*u,tile.width-16*u,22*u),i+"档",Mathf.RoundToInt(13*u),accent,true);
                Text(new Rect(tile.xMax-60*u,tile.y+5*u,52*u,22*u),current?"当前":i<tier?"已解锁":"",Mathf.RoundToInt(10*u),accent,false,false,TextAnchor.MiddleRight);
                DrawBasicAttackShape(new Rect(tile.x+6*u,tile.y+30*u,tile.width-12*u,50*u),i,p.Profile.heroClass,accent,u);
                bool melee=p.Profile.heroClass==HeroClass.Vanguard;
                string title=melee?BasicAttackGrowth.Range(i).ToString("0.0")+"米":i==1?"单发":i==2?"二连发":"扇形 "+BasicAttackGrowth.Shots(i)+" 发";
                Text(new Rect(tile.x+5*u,tile.y+84*u,tile.width-10*u,24*u),title,Mathf.RoundToInt(15*u),current?pale:accent,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(tile.x+5*u,tile.y+110*u,tile.width-10*u,18*u),melee?BasicAttackGrowth.Angle(i).ToString("0")+"°挥斩":i==2?"间隔 0.12秒":i>2?"":"单体攻击",Mathf.RoundToInt(10*u),muted,false,false,TextAnchor.MiddleCenter);
            }
            float statsY=(4/columns)*(tileHeight+gap);
            bool ranged=p.Profile.heroClass!=HeroClass.Vanguard;
            float perShot=SkillDamageBudgets.BasicCoefficient(p.Profile.heroClass)*(ranged?BasicAttackGrowth.ProjectileMultiplier(tier):1f);
            if(!compact)Text(new Rect(0,statsY,w,24*u),(ranged?"每发 ":"伤害 ")+(perShot*100).ToString("0.#")+"% · "+(ranged?"单体上限 "+(SkillDamageBudgets.BasicCoefficient(p.Profile.heroClass)*BasicAttackGrowth.TargetMultiplier(tier)*100).ToString("0.#")+"% · ":"")+"回能 "+SkillDamageBudgets.BasicEnergyOnHit.ToString("0"),Mathf.RoundToInt(11*u),jade);
            EndTouchScroll();
            float footerY=r.yMax-54*u;
            string reason=p.BasicAttackUpgradeLockReason();
            Rect material=new Rect(r.x+12*u,footerY,r.width-174*u,42*u);
            Fill(material,new Color(.025f,.055f,.07f));Border(material,gold*.35f);
            if(tier>=4)
                Text(material,"✓ 满档",Mathf.RoundToInt(14*u),gold,true,false,TextAnchor.MiddleCenter);
            else
            {
                DrawIcon(new Rect(material.x+8*u,material.y+7*u,28*u,28*u),UIIconAtlas.Utility("basic-attack"),Color.white);
                string amount="<color="+(p.Profile.basicAttackMaterials>=cost?"#EAC77A":"#EE997F")+">"+p.Profile.basicAttackMaterials+"</color> / "+cost;
                Text(new Rect(material.x+42*u,material.y,material.width-48*u,material.height),amount,Mathf.RoundToInt(14*u),pale,true,false,TextAnchor.MiddleCenter);
                InspectRewardItem(material,ResourceItemPreview("basic-attack",cost),true);
            }
            Rect button=new Rect(r.xMax-150*u,footerY,138*u,42*u);
            bool clicked=PrimaryButton(button,"",gold,reason.Length==0,p.IsPracticeOnly?reason:null);
            string upgradeCaption=tier>=4?"满档":"升级";
            int upgradeFont=Mathf.RoundToInt(15*u);
            float captionWidth=Style(upgradeFont,true,false).CalcSize(new GUIContent(upgradeCaption)).x;
            float upgradeIconSize=24*u,upgradeGap=7*u;
            float upgradeStart=button.center.x-(upgradeIconSize+upgradeGap+captionWidth)*.5f;
            DrawIcon(new Rect(upgradeStart,button.center.y-upgradeIconSize*.5f,upgradeIconSize,upgradeIconSize),UIIconAtlas.Utility("basic-attack"),gold);
            Text(new Rect(upgradeStart+upgradeIconSize+upgradeGap,button.y,captionWidth,button.height),upgradeCaption,upgradeFont,reason.Length==0?pale:muted,true,false,TextAnchor.MiddleCenter);
            if(clicked){bool saved=p.UpgradeBasicAttack();session.Notify(saved?"普通攻击已升至 "+(tier+1)+"档":p.LastError);}
        }
        private bool blinkFollowupSelected;
        private Vector2 blinkFollowupDetailScroll;
        private Rect DrawCompactSkillTreeCard(Rect r,string name,string status,bool selected,bool learned,float u)
        {
            Color accent=selected?gold:learned?jade:muted;
            Surface(r,new Color(.018f,.036f,.054f,.99f));SurfaceFrame(r,new Color(accent.r,accent.g,accent.b,selected?.9f:.35f));
            float size=r.height-4*u;Rect icon=new Rect(r.x+2*u,r.y+2*u,size,size);
            float textX=icon.xMax+4*u,textWidth=Mathf.Max(1,r.xMax-textX-3*u);
            int font=Mathf.RoundToInt(10*u);while(font>Mathf.RoundToInt(7*u)&&Style(font,true,false).CalcSize(new GUIContent(name)).x>textWidth)font--;
            Text(new Rect(textX,r.y+2*u,textWidth,(r.height-4*u)*.5f),name,font,learned?pale:muted,true,false,TextAnchor.MiddleLeft);
            Text(new Rect(textX,r.center.y,textWidth,(r.height-4*u)*.5f),status,Mathf.RoundToInt(8*u),accent,false,false,TextAnchor.MiddleLeft);
            return icon;
        }
        private void DrawBlinkFollowupNode(Rect r,float u)
        {
            bool learned=session.Player.BlinkFollowupLearned;
            Color accent=blinkFollowupSelected?gold:learned?jade:muted;
            if(MobileControls.Active)
            {
                Rect icon=DrawCompactSkillTreeCard(r,session.Player.BlinkFollowupName,"Lv.5 · "+(learned?"已习得":"锁定"),blinkFollowupSelected,learned,u);
                DrawIcon(icon,AuthoredIconArt.BlinkFollowup(session.Progression.Profile.heroClass),accent);
            }
            else
            {
                Surface(r,blinkFollowupSelected?new Color(.12f,.20f,.23f):learned?new Color(.06f,.145f,.15f):card);
                SurfaceFrame(r,blinkFollowupSelected?gold:new Color(accent.r,accent.g,accent.b,learned?.7f:.25f));
                DrawIcon(new Rect(r.x+5,r.y+7,24,24),AuthoredIconArt.BlinkFollowup(session.Progression.Profile.heroClass),accent);
                Text(new Rect(r.x+32,r.y+7,107,24),session.Player.BlinkFollowupName,14,learned?pale:muted,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(r.x+5,r.y+35,134,18),"Lv.5 · "+(learned?"已习得":"未解锁"),11,muted,false,false,TextAnchor.MiddleCenter);
            }
            if(MobileControls.Active?MobileSkillRowClicked(r):GUI.Button(r,GUIContent.none,invisibleButton))
            {basicAttackSelected=false;blinkFollowupSelected=true;CancelMobileScroll();}
        }
        private void DrawBlinkFollowupDetail(Rect r,float u)
        {
            Surface(r,new Color(.012f,.024f,.038f,.99f));SurfaceFrame(r,new Color(jade.r,jade.g,jade.b,.28f));
            Text(new Rect(r.x+10*u,r.y+4*u,r.width-168*u,36*u),session.Player.BlinkFollowupName,Mathf.RoundToInt(16*u),pale,true,false,TextAnchor.MiddleLeft);
            SkillStateTag(new Rect(r.xMax-142*u,r.y+10*u,56*u,24*u),"主动",jade,Mathf.RoundToInt(12*u));
            SkillStateTag(new Rect(r.xMax-82*u,r.y+10*u,72*u,24*u),"衔接技",gold,Mathf.RoundToInt(12*u));
            r=new Rect(r.x+6*u,r.y+44*u,r.width-12*u,Mathf.Max(32*u,r.height-52*u));
            float x=16*u,w=r.width-48*u;
            string damage=(session.Player.BlinkFollowupDamageMultiplier*100).ToString("0")+"%攻击 + "+SkillDamageBudgets.BaseDamage(session.Progression.Profile.level,session.Player.BlinkFollowupDamageMultiplier).ToString("0.#");
            string effect=session.Player.HeroClass==HeroClass.Vanguard?"对前方3米、130°扇形内敌人造成"+damage+"伤害，击退并震慑0.2秒。":session.Player.HeroClass==HeroClass.Arcanist?"对周围3米内敌人造成"+damage+"伤害，小幅击退并震慑0.2秒。":session.Player.HeroClass==HeroClass.Ranger?"贯穿前方7米内敌人，造成"+damage+"伤害，小幅击退并震慑0.1秒。":"在6米内目标位置引爆，造成"+damage+"的2.6米范围伤害，震慑0.55秒。";
            string trigger=MobileControls.Active?"闪现后 1.2 秒内，点击原闪现按钮释放。":"闪现后 1.2 秒内，点击鼠标左键释放。";
            string note="5级自动习得。每次闪现可衔接一次，无额外资源消耗。";
            int effectFont=Mathf.RoundToInt(14*u),triggerFont=Mathf.RoundToInt(13*u),noteFont=Mathf.RoundToInt(12*u);
            float effectHeight=Style(effectFont,false,true).CalcHeight(new GUIContent(effect),w);
            float triggerHeight=Style(triggerFont,false,true).CalcHeight(new GUIContent(trigger),w);
            float noteHeight=Style(noteFont,false,true).CalcHeight(new GUIContent(note),w);
            float triggerY=42*u+effectHeight+12*u,noteY=triggerY+triggerHeight+12*u;
            float contentHeight=noteY+noteHeight+16*u;
            blinkFollowupDetailScroll=BeginTouchScroll("blink-followup-detail",r,blinkFollowupDetailScroll,new Rect(0,0,r.width-16*u,Mathf.Max(r.height,contentHeight)));
            Text(new Rect(x,8*u,w,24*u),session.Player.BlinkFollowupLearned?"Lv.5 · 已习得":"Lv.5 · 自动习得",Mathf.RoundToInt(12*u),jade,true);
            Text(new Rect(x,42*u,w,effectHeight),effect,effectFont,pale,false,true);
            Text(new Rect(x,triggerY,w,triggerHeight),trigger,triggerFont,jade,false,true);
            Text(new Rect(x,noteY,w,noteHeight),note,noteFont,muted,false,true);
            EndTouchScroll();
        }
        private int skillSection;
        private bool masteryResetConfirm;
        private Vector2 skillDevelopmentScroll;
        private void DrawSkillTabs(Rect rect)
        {
            float u=MobileControls.Active?TouchRatio:1,gap=8*u,w=(rect.width-2*gap)/3;
            for(int i=0;i<3;i++)
            {
                Rect hit=new Rect(rect.x+i*(w+gap),rect.y,w,rect.height);
                bool selected=skillSection==i;
                Text(hit,i==0?"战技":i==1?"职业精通":"切换职业",Mathf.RoundToInt(14*u),selected?gold:muted,selected,false,TextAnchor.MiddleCenter);
                if(selected)Fill(new Rect(hit.x+8*u,hit.yMax-5*u,hit.width-16*u,2*u),gold);
                if(GUI.Button(hit,GUIContent.none,invisibleButton)&&!selected){skillSection=i;CancelMobileScroll();BlockUITransition();}
            }
        }
        private bool DrawSkillSubsurface()
        {
            if(!masteryResetConfirm)return DrawClassSwitchSurface()||DrawBuildPlanSurface();
            float u=MobileControls.Active?TouchRatio:1;
            Rect r=Modal(Mathf.Min(460*u,width-24*u),Mathf.Min(240*u,height-24*u),"重置职业精通","");
            Text(new Rect(r.x+24*u,r.y+100*u,r.width-48*u,40*u),"确认重置所有精通加点并返还精通点？",Mathf.RoundToInt(14*u),pale,false,true);
            if(Button(new Rect(r.x+24*u,r.yMax-60*u,(r.width-60*u)/2,40*u),"取消",jade))masteryResetConfirm=false;
            if(Button(new Rect(r.center.x+6*u,r.yMax-60*u,(r.width-60*u)/2,40*u),"确认重置",gold))
            {bool saved=session.Progression.ResetMastery(true);masteryChangeNotice=saved?"职业精通已重置，精通点已返还":session.Progression.LastError;if(saved)masteryResetConfirm=false;}
            if(PopupCloseButton(new Rect(r.xMax-52*u,r.y+12*u,40*u,36*u)))masteryResetConfirm=false;
            return true;
        }
        private void DrawSkillDevelopment()
        {
            var p=session.Progression;
            if(MobileControls.Active)
            {
                var l=MobilePanelGeometry();
                if(DrawMobilePanelChrome(l,"职业精通","",showNotice:false,headerRightReserve:276))return;
                DrawSkillTabs(TouchRect(l.Close.X-276,8,264,44));
                if(skillSection!=1)return;
                var body=new MobilePanelLayout.Area(l.Body.X,l.Body.Y,l.Body.Width,l.Height-l.Body.Y-12);
                float mobileWidth=body.Width-16,h=8;DrawMobileWorkshopAbilities(ref h,mobileWidth,false);
                skillDevelopmentScroll=BeginTouchScroll("skill-development",MobilePanelRect(body),skillDevelopmentScroll,new Rect(0,0,mobileWidth*TouchRatio,Mathf.Max(body.Height,h)*TouchRatio));
                h=8;DrawMobileWorkshopAbilities(ref h,mobileWidth,true);EndTouchScroll();return;
            }
            Rect w=Modal(1160,660,"职业精通","");
            if(PopupCloseButton(new Rect(w.xMax-69,w.y+20,44,32)))ClosePanel();
            DrawSkillTabs(new Rect(w.x+330,w.y+20,264,36));
            if(skillSection!=1)return;
            Rect desktopBody=new Rect(w.x+26,w.y+108,w.width-52,w.height-122);float available=desktopBody.width-18;
            float desktopHeight=DrawSkillDevelopmentContent(available,1,false);
            skillDevelopmentScroll=BeginTouchScroll("skill-development",desktopBody,skillDevelopmentScroll,new Rect(0,0,available,Mathf.Max(desktopBody.height,desktopHeight)));
            DrawSkillDevelopmentContent(available,1,true);EndTouchScroll();
        }
        private string SkillMasterySummary()
        {var p=session.Progression;return "可用点数 "+p.Profile.skillPoints+" · "+MasteryProgressionRules.TierSummary+" · "+MasteryProgressionRules.CoreSummary;}
        private string MasteryNodeDescription(MasteryType mastery)
        {
            string description=BuildCatalog.MasteryDescription(mastery);
            int tier=description.IndexOf(MasteryProgressionRules.TierSummary,System.StringComparison.Ordinal);
            if(tier>=0)description=description.Substring(0,tier);
            return description+"\n"+SkillMasterySummary();
        }
        private int selectedMastery;
        private string masteryChangeNotice,masteryNoticeSlot;
        private float masteryChangedAt;
        private int masteryChangedNode=-1;
        private string MasteryBonusText(int index,int rank)
        {
            float amount=rank*(index==0?.3f:index==1?.5f:index==2?.75f:.1f);
            return new[]{"攻击","生命","护甲","普攻回能"}[index]+" +"+amount.ToString("0.##")+(index==3?"":"%");
        }
        private string MasteryCoreText(MasteryType mastery)
        {
            string text=BuildCatalog.MasteryDescription(mastery);
            int start=text.IndexOf("核心：",System.StringComparison.Ordinal);
            int end=text.IndexOf(MasteryProgressionRules.TierSummary,System.StringComparison.Ordinal);
            return text.Substring(start+3,end-start-3).Trim();
        }
        private float DrawSkillDevelopmentContent(float width,float u,bool draw)
        {
            var p=session.Progression;bool compact=MobileControls.Active;float y=compact?4:8;
            if(masteryNoticeSlot!=p.CurrentSlotId){masteryNoticeSlot=p.CurrentSlotId;masteryChangeNotice=null;masteryChangedNode=-1;}
            selectedMastery=Mathf.Clamp(selectedMastery,0,3);
            if(draw)
            {
                Text(new Rect(4*u,y*u,(width-132)*u,44*u),"可用精通点 "+p.Profile.skillPoints,Mathf.RoundToInt((compact?13:16)*u),gold,true);
                Rect reset=new Rect((width-116)*u,(y+5)*u,112*u,32*u);
                Surface(reset,new Color(.14f,.095f,.045f,.95f));SurfaceFrame(reset,gold*.8f);
                DrawIcon(new Rect(reset.x+9*u,reset.center.y-8*u,16*u,16*u),UIIconAtlas.Utility("reset"),gold);
                Text(new Rect(reset.x+29*u,reset.y,reset.width-34*u,reset.height),"重置精通",Mathf.RoundToInt(11*u),gold,true,false,TextAnchor.MiddleCenter);
                if(GUI.Button(reset,GUIContent.none,invisibleButton))masteryResetConfirm=true;
            }
            y+=compact?44:52;
            int cap=ProgressionService.MasteryCap(p.Profile.level);
            int statFont=Mathf.RoundToInt(13*u);
            float minimumTile=24+Mathf.Max(46,Style(statFont,true).CalcSize(new GUIContent("下一点")).x/u+8);
            float widestBonus=0;
            for(int i=0;i<4;i++)widestBonus=Mathf.Max(widestBonus,Style(statFont,true).CalcSize(new GUIContent(MasteryBonusText(i,cap))).x/u);
            minimumTile+=widestBonus+4;
            int columns=width>=minimumTile*4+30?4:width>=minimumTile*2+10?2:1;
            float tileWidth=(width-(columns-1)*10)/columns,coreHeight=0;
            for(int i=0;i<4;i++)coreHeight=Mathf.Max(coreHeight,Style(Mathf.RoundToInt((compact?11:12)*u),false,true).CalcHeight(new GUIContent(MasteryCoreText((MasteryType)i)),(tileWidth-(compact?16:24))*u)/u);
            float tileHeight=(compact?174:208)+coreHeight;
            for(int i=0;i<4;i++)
            {
                var mastery=(MasteryType)i;int rank=p.Profile.masteryRanks[i];
                string reason=p.MasteryLockReason(mastery);bool learnable=string.IsNullOrEmpty(reason);
                Rect tile=new Rect((i%columns)*(tileWidth+10)*u,(y+i/columns*(tileHeight+10))*u,tileWidth*u,tileHeight*u);
                if(!draw)continue;
                bool selected=selectedMastery==i,flashing=masteryChangedNode==i&&Time.unscaledTime-masteryChangedAt<1.2f;
                Fill(tile,flashing?new Color(.07f,.23f,.17f):card);Border(tile,p.HasMasteryCore(mastery)?gold:jade*.4f);
                DrawIcon(new Rect(tile.x+(compact?8:10)*u,tile.y+(compact?7:8)*u,(compact?22:28)*u,(compact?22:28)*u),UIIconAtlas.Mastery(mastery),pale);
                Text(new Rect(tile.x+(compact?36:46)*u,tile.y+(compact?4:8)*u,tile.width-(compact?100:110)*u,28*u),BuildCatalog.MasteryName(mastery),Mathf.RoundToInt((compact?12:14)*u),pale,true,false,TextAnchor.MiddleLeft);
                Text(new Rect(tile.xMax-56*u,tile.y+(compact?4:8)*u,48*u,28*u),"投入 "+rank,Mathf.RoundToInt(10*u),muted,false,false,TextAnchor.MiddleRight);
                int statSize=Mathf.RoundToInt(13*u);
                float labelWidth=Mathf.Max(46*u,Style(statSize,true).CalcSize(new GUIContent("下一点")).x+8*u);
                float statX=tile.x+12*u+labelWidth,statWidth=tile.width-24*u-labelWidth;
                Text(new Rect(tile.x+12*u,tile.y+(compact?32:42)*u,labelWidth,24*u),"当前",statSize,pale,true);
                Text(new Rect(statX,tile.y+(compact?32:42)*u,statWidth,24*u),MasteryBonusText(i,rank),statSize,pale,true);
                if(rank<cap)
                {
                    Text(new Rect(tile.x+12*u,tile.y+(compact?54:68)*u,labelWidth,24*u),"下一点",statSize,jade,true);
                    Text(new Rect(statX,tile.y+(compact?54:68)*u,statWidth,24*u),MasteryBonusText(i,rank+1),statSize,jade,true);
                }
                else Text(new Rect(tile.x+12*u,tile.y+(compact?54:68)*u,tile.width-24*u,24*u),cap==0?"Lv"+MasteryProgressionRules.FirstUnlockLevel+" 解锁":"已达当前等级上限",Mathf.RoundToInt(12*u),muted);
                if(GUI.Button(new Rect(tile.x,tile.y,tile.width,(compact?72:98)*u),GUIContent.none,invisibleButton))selectedMastery=i;
                string action=learnable?"+ 1点":cap==0?"尚未解锁":rank>=cap?"当前已满":"精通点不足";
                if(PrimaryButton(new Rect(tile.x+10*u,tile.y+(compact?82:108)*u,tile.width-20*u,36*u),action,jade,learnable))
                {
                    selectedMastery=i;bool saved=p.LearnMastery(mastery);
                    masteryChangeNotice=saved?BuildCatalog.MasteryName(mastery)+"："+MasteryBonusText(i,rank)+" → "+MasteryBonusText(i,p.Profile.masteryRanks[i])+"（消耗 1 精通点）":p.LastError;
                    if(!saved)session.Notify(p.LastError);
                    if(saved){masteryChangedNode=i;masteryChangedAt=Time.unscaledTime;}
                }
                bool active=p.HasMasteryCore(mastery);
                Text(new Rect(tile.x+(compact?8:12)*u,tile.y+(compact?126:156)*u,tile.width-(compact?16:24)*u,coreHeight*u),MasteryCoreText(mastery),Mathf.RoundToInt((compact?11:12)*u),active?gold:pale,false,true);
                bool canChooseCore=session.IsInCamp||p.Profile.masteryCore<0;
                string coreAction=active?"✓ 核心已启用":rank<MasteryCoreRules.InitialInvestment?"投入 "+MasteryCoreRules.InitialInvestment+" 点解锁":canChooseCore?"启用核心":"回营地切换";
                Rect coreRect=new Rect(tile.x+10*u,tile.yMax-(compact?40:44)*u,tile.width-20*u,36*u);
                if(active)
                {
                    Surface(coreRect,new Color(.24f,.17f,.065f,.98f));SurfaceFrame(coreRect,gold);
                    Text(coreRect,coreAction,Mathf.RoundToInt(13*u),gold,true,false,TextAnchor.MiddleCenter);
                }
                else if(Button(coreRect,coreAction,jade,canChooseCore&&rank>=MasteryCoreRules.InitialInvestment))
                {bool saved=p.SelectMasteryCore(mastery,session.IsInCamp);masteryChangeNotice=saved?"已启用 "+BuildCatalog.MasteryName(mastery)+"核心":p.LastError;if(!saved)session.Notify(p.LastError);}
            }
            y+=((4+columns-1)/columns)*(tileHeight+10);
            return y+8;
        }
    }
}
