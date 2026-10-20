using UnityEngine;
namespace Emberfall
{
    public sealed partial class MobileControls
    {
        private GUIStyle controlLabel,pinnedTargetLabel;
        private readonly MobileOpportunityMeter counterMeter=new MobileOpportunityMeter(),comboMeter=new MobileOpportunityMeter();
        private bool LimitedHealing {get{return session.ChallengeRun&&session.InDungeon;}}
        private int PotionCount {get{return LimitedHealing?session.HealingCharges:session.Progression.Profile.potions;}}
        private void CheckPotionFeedback()
        {
            var hero=session.Player;if(hero==null)return;
            string reason=hero.PotionCooldownRemaining>0?"冷却 "+Mathf.CeilToInt(hero.PotionCooldownRemaining)+"秒":MobileCombatPresentation.Potion(PotionCount,LimitedHealing,hero.Health>=hero.MaxHealth-.5f);
            if(reason.Length>0)session.ReportControlFailure("potion",reason);
        }
        private void CheckDodgeFeedback()
        {
            var hero=session.Player;if(hero==null)return;
            if(hero.BlinkFollowupRemaining>0)return;
            string reason=MobileCombatPresentation.Dodge(hero.DodgeCooldown,hero.IsJumping);
            if(reason.Length>0)session.ReportControlFailure("dodge",reason);
        }
        private void DrawAvailability()
        {
            if(controlLabel==null)controlLabel=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=12,fontStyle=FontStyle.Bold,font=GameFont.Shared};
            controlLabel.fontSize=Mathf.RoundToInt(12*EffectPreferences.InterfaceTextScale);
            controlLabel.normal.textColor=Color.white;
            var hero=session.Player;
            string potionState=hero.PotionCooldownRemaining>0?"冷却 "+Mathf.CeilToInt(hero.PotionCooldownRemaining)+"秒":MobileCombatPresentation.Potion(PotionCount,LimitedHealing,hero.Health>=hero.MaxHealth-.5f);
            string dodgeState=MobileCombatPresentation.Dodge(hero.DodgeCooldown,hero.IsJumping);
            // Keep the count on the bottle with a small text outline, without a badge background.
            Rect potionVisual=PotionVisualRect();
            controlLabel.fontSize=Mathf.RoundToInt(10*EffectPreferences.InterfaceTextScale);
            var countContent=new GUIContent(PotionCount.ToString());
            Vector2 countSize=controlLabel.CalcSize(countContent);
            float countWidth=Mathf.Max(18,countSize.x),countHeight=Mathf.Max(16,countSize.y);
            float countRight=Mathf.Min(potionVisual.xMax,Layout.Width-4),countBottom=Mathf.Min(potionVisual.yMax,Layout.Height-4);
            Rect countBadge=new Rect(countRight-countWidth,countBottom-countHeight,countWidth,countHeight);
            GUI.color=Color.white;
            controlLabel.normal.textColor=new Color(.015f,.025f,.04f,.9f);
            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)if(dx!=0||dy!=0)
                GUI.Label(new Rect(countBadge.x+dx,countBadge.y+dy,countBadge.width,countBadge.height),countContent,controlLabel);
            controlLabel.normal.textColor=Color.white;
            GUI.Label(countBadge,countContent,controlLabel);
            controlLabel.fontSize=Mathf.RoundToInt(12*EffectPreferences.InterfaceTextScale);
            string failure=session.ControlFailure("potion");
            string potionCaption=!string.IsNullOrEmpty(failure)?failure:potionState;
            if(hero.PotionCooldownRemaining<=0&&potionCaption.Length>0&&potionCaption!="满血"&&!potionCaption.StartsWith("冷却"))GUI.Label(new Rect(Potion.x-8,Potion.y-18,Potion.width+16,16),potionCaption,controlLabel);
            if(hero.PotionCooldownRemaining>0)
            {
                Rect r=potionVisual;string caption=hero.PotionCooldownRemaining.ToString(hero.PotionCooldownRemaining>=10?"0":"0.0");
                int prior=controlLabel.fontSize;controlLabel.fontSize=Mathf.RoundToInt(Mathf.Clamp(r.width*.3f,14,32));
                controlLabel.normal.textColor=new Color(.005f,.01f,.02f,1);
                GUI.Label(new Rect(r.x-1,r.y,r.width,r.height),caption,controlLabel);GUI.Label(new Rect(r.x+1,r.y,r.width,r.height),caption,controlLabel);
                GUI.Label(new Rect(r.x,r.y-1,r.width,r.height),caption,controlLabel);GUI.Label(new Rect(r.x,r.y+1,r.width,r.height),caption,controlLabel);
                controlLabel.normal.textColor=Color.white;GUI.Label(r,caption,controlLabel);controlLabel.fontSize=prior;
            }
            if(hero.BlinkFollowupRemaining>0)
            {
                LabelControl(Dodge,hero.BlinkFollowupName,false);
                Rect action=VisualRect(Dodge);GUI.color=new Color(1f,.75f,.25f,EffectPreferences.TouchOpacity);
                GUI.DrawTexture(new Rect(action.x,action.yMax+2,action.width*Mathf.Clamp01(hero.BlinkFollowupRemaining/1.2f),3),Texture2D.whiteTexture);GUI.color=Color.white;
            }
            else if(hero.DodgeCooldown>.01f)
            {
                Rect action=VisualRect(Dodge);
                string countdown=hero.DodgeCooldown.ToString(hero.DodgeCooldown>=10?"0":"0.0");
                int oldSize=controlLabel.fontSize;
                controlLabel.fontSize=Mathf.RoundToInt(Mathf.Clamp(action.width*.3f,14,32));
                controlLabel.normal.textColor=new Color(.005f,.01f,.02f,1);
                GUI.Label(new Rect(action.x-1,action.y,action.width,action.height),countdown,controlLabel);
                GUI.Label(new Rect(action.x+1,action.y,action.width,action.height),countdown,controlLabel);
                GUI.Label(new Rect(action.x,action.y-1,action.width,action.height),countdown,controlLabel);
                GUI.Label(new Rect(action.x,action.y+1,action.width,action.height),countdown,controlLabel);
                controlLabel.normal.textColor=Color.white;GUI.Label(action,countdown,controlLabel);
                controlLabel.fontSize=oldSize;
            }
            else if(dodgeState.Length>0)LabelControl(Dodge,dodgeState,true);
            var combo=hero.BasicOpportunityWindow(true);var ca=Layout.ComboOpportunity;
            comboMeter.Draw(new Rect(ca.X,ca.Y,ca.Width,ca.Height),combo,hero,hero.CombatEpoch,1,EffectPreferences.TouchOpacity);
            var counter=hero.BasicOpportunityWindow();var co=Layout.CounterOpportunity;
            counterMeter.Draw(new Rect(co.X,co.Y,co.Width,co.Height),counter,hero,hero.CombatEpoch,1,EffectPreferences.TouchOpacity);
            string basicReason=hero.MobilePinnedActionReason(-1);
            failure=session.ControlFailure("attack");if(!string.IsNullOrEmpty(failure))basicReason=failure;
            if(basicReason.Length==0)basicReason=counter.Window?counter.BlockReason:combo.Window?combo.BlockReason:"";
            if(basicReason.Length>0&&basicReason!="无目标")LabelControl(Attack,basicReason,true);
            var pinned=hero.MobilePinnedTarget;
            if(pinned!=null&&Camera.main!=null)
            {
                Vector3 screen=Camera.main.WorldToScreenPoint(pinned.transform.position+Vector3.up*2.6f);
                if(screen.z>Camera.main.nearClipPlane)
                {
                    Vector2 p=ToUI(new Vector2(screen.x,screen.y));
                    var pending=hero.GetComponent<SkillChargeController>();
                    if(pinnedTargetLabel==null)pinnedTargetLabel=new GUIStyle(controlLabel)
                    {wordWrap=true,clipping=TextClipping.Overflow,padding=new RectOffset(4,4,3,3)};
                    pinnedTargetLabel.fontSize=controlLabel.fontSize;
                    pinnedTargetLabel.normal.textColor=Color.white;
                    string title=pinned.DisplayName+"\n"+(pending!=null&&pending.IsCharging?"下次固定":"固定目标");
                    var content=new GUIContent(title);
                    float labelWidth=Mathf.Min(Layout.Width-16,Mathf.Max(140,Mathf.Min(240,pinnedTargetLabel.CalcSize(content).x+8)));
                    float labelHeight=pinnedTargetLabel.CalcHeight(content,labelWidth)+4;
                    Rect labelRect=new Rect(Mathf.Clamp(p.x-labelWidth*.5f,8,Layout.Width-labelWidth-8),
                        Mathf.Clamp(p.y-labelHeight,80,Layout.Height-labelHeight-8),labelWidth,labelHeight);
                    GUI.Label(labelRect,content,pinnedTargetLabel);
                }
            }
            failure=session.ControlFailure("dodge");if(hero.BlinkFollowupRemaining<=0&&hero.DodgeCooldown<=.01f&&!string.IsNullOrEmpty(failure))LabelControl(Dodge,failure,true);
        }
        // Keep generous touch targets while reducing only the iPad combat artwork.
        public static float CombatButtonVisualScale { get { return EffectPreferences.TouchVisualScale*(IsIPad?.9f:1f); } }
        private static Rect VisualRect(Rect hit)
        {float ratio=CombatButtonVisualScale;return new Rect(hit.center.x-hit.width*ratio*.5f,hit.center.y-hit.height*ratio*.5f,hit.width*ratio,hit.height*ratio);}
        private void LabelControl(Rect area,string text,bool center,bool available=true)
        {
            if(session.Player!=null&&session.Player.IsJumping&&center)return;
            if(text=="需落地"||text=="空中")return;
            area=VisualRect(area);
            Rect r=center?new Rect(area.x,area.center.y-9,area.width,18):new Rect(area.x,area.yMax-18,area.width,16);
            GUI.color=new Color(.015f,.025f,.04f,.9f*EffectPreferences.TouchOpacity);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;controlLabel.normal.textColor=available?Color.white:new Color(.58f,.61f,.65f);GUI.Label(r,text,controlLabel);controlLabel.normal.textColor=Color.white;
        }
    }
}
