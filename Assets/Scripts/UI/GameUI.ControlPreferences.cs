using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool mobileBindingEditor;
        private string mobileBindingOwner;
        private int[] mobileBindings;
        private Rect MobileVisualRect(Rect hit,bool combatButton=false)
        {float k=combatButton?MobileControls.CombatButtonVisualScale:EffectPreferences.TouchVisualScale;return new Rect(hit.center.x-hit.width*k*.5f,hit.center.y-hit.height*k*.5f,hit.width*k,hit.height*k);}
        private void EnsureMobileBindings()
        {
            string key="Emberfall.TouchBindings."+session.Progression.CurrentSlotId+"."+(int)session.Progression.Profile.heroClass;
            if(mobileBindings!=null&&mobileBindingOwner==key)return;
            mobileBindingOwner=key;mobileBindings=MobileSkillPolicy.DefaultBindings(session.Progression.Profile.heroClass==HeroClass.Summoner);
            string[] saved=PlayerPrefs.GetString(key,"").Split(',');
            int[] parsed=new int[saved.Length];bool valid=true;
            for(int i=0;i<saved.Length;i++)if(!int.TryParse(saved[i],out parsed[i]))valid=false;
            if(valid&&MobileSkillPolicy.ValidBindings(parsed))mobileBindings=parsed;
            bindingDragSource=-1;bindingDragFinger=-1000;CancelMobileCast();
        }
        private int BoundMobileSkill(int button,int page)
        {
            EnsureMobileBindings();int index=MobileSkillPolicy.BindingIndex(button,page);
            return index<0?-1:mobileBindings[index];
        }
        public int MobileKeyboardSkill(int key)
        {
            int page,button;
            if(key>=(int)KeyCode.Alpha1&&key<=(int)KeyCode.Alpha5){page=0;button=key-(int)KeyCode.Alpha1;}
            else
            {
                page=1;button=key==(int)KeyCode.Z?0:key==(int)KeyCode.X?1:key==(int)KeyCode.C?2:key==(int)KeyCode.V?3:key==(int)KeyCode.B?4:-1;
            }
            if(button<0)return -1;
            mobileSkillPage=page;return BoundMobileSkill(button,page);
        }
        public MobileControlLayout.Area MobileOpportunityArea(int skill)
        {
            var layout=MobileControls.Layout;
            for(int button=0;button<5;button++)if(BoundMobileSkill(button,mobileSkillPage)==skill)
            {
                return layout.SkillOpportunityForButton(button);
            }
            return new MobileControlLayout.Area(-10000,-10000,0,0);
        }
        private int bindingDragSource=-1,bindingDragFinger=-1000,bindingTouchFrame=-1;
        private Vector2 bindingDragOrigin,bindingDragPoint;
        private void DrawMobileControlPreferences(float x,float y,float width)
        {
            if(NavigationButton(TouchRect(x,y,width,48),"技能按键配置",jade))
            {mobileBindingEditor=true;bindingDragSource=-1;bindingDragFinger=-1000;CancelMobileScroll();BlockUITransition();}
            if(Button(TouchRect(x,y+58,width,48),"界面字号："+EffectPreferences.InterfaceTextSizeName,jade))EffectPreferences.CycleInterfaceTextScale();
        }
        private void FinishBindingDrag(Rect[] slots,Vector2 point,bool cancelled)
        {
            int source=bindingDragSource;bindingDragSource=-1;bindingDragFinger=-1000;
            if(cancelled||source<0||(point-bindingDragOrigin).sqrMagnitude<36*TouchRatio*TouchRatio)return;
            for(int target=0;target<slots.Length;target++)if(slots[target].Contains(point))
            {
                if(target!=source&&MobileSkillPolicy.SwapBinding(mobileBindings,target,mobileBindings[source]))
                {PlayerPrefs.SetString(mobileBindingOwner,string.Join(",",mobileBindings));PlayerPrefs.Save();CancelMobileCast();GameAudio.Play(SoundCue.UI);}
                return;
            }
        }
        private Rect BindingPreviewRect(MobileControlLayout.Area area,float minX,float minY,float x,float y,float zoom)
        {return TouchRect(x+(area.X-minX)*zoom,y+(area.Y-minY)*zoom,area.Width*zoom,area.Height*zoom);}
        private void DrawMobileBindingEditor()
        {
            if(mobilePositionEditor){DrawMobilePositionEditor();return;}
            EnsureMobileBindings();var layout=MobileControls.Layout;float u=TouchRatio;

            float panelHeight=Mathf.Min(layout.Height,height/u)-24;
            float panelWidth=Mathf.Min(880,layout.Width-32),panelX=(layout.Width-panelWidth)*.5f;
            float x=panelX+20,contentWidth=panelWidth-40;
            Box(TouchRect(panelX,12,panelWidth,panelHeight),jade,false);
            Text(TouchRect(x,28,contentWidth-220,34),"技能按键配置",TouchFont(21),pale,true);
            if(QuietAction(TouchRect(x+contentWidth-96,24,96,40),"返回设置"))
            {mobileBindingEditor=false;bindingDragSource=-1;bindingDragFinger=-1000;BlockUITransition();return;}
            if(QuietAction(TouchRect(x+contentWidth-212,24,108,40),"调整位置"))
            {BeginMobilePositionEditor();return;}
            Text(TouchRect(x,70,contentWidth,40),"拖动技能交换位置，可跨页拖动；固定按键不参与交换。位置微调请点“调整位置”。",TouchFont(12),muted,false,true);
            Rect[] slots=new Rect[8];float pageWidth=(contentWidth-16)*.5f;
            for(int page=0;page<2;page++)
            {
                float left=x+page*(pageWidth+16),top=118,pageHeight=panelHeight-top+12-20;
                Fill(TouchRect(left,top,pageWidth,pageHeight),new Color(.025f,.055f,.075f,.9f));
                Text(TouchRect(left+16,top+12,pageWidth-32,26),page==0?"第一页":"第二页",TouchFont(14),jade,true);
                // Include every control edge, including the iPad ultimate and page switch.
                float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
                var previewAreas=new[]{layout.Skills[0],layout.Skills[1],layout.Skills[2],layout.Skills[3],layout.Skills[4],layout.Attack,layout.Dodge,layout.Jump,layout.SkillPage};
                foreach(var area in previewAreas){minX=Mathf.Min(minX,area.X);minY=Mathf.Min(minY,area.Y);maxX=Mathf.Max(maxX,area.X+area.Width);maxY=Mathf.Max(maxY,area.Y+area.Height);}
                float availableHeight=Mathf.Max(1,pageHeight-64);
                float previewScale=Mathf.Min((pageWidth-40)/(maxX-minX),availableHeight/(maxY-minY));
                float originX=left+(pageWidth-(maxX-minX)*previewScale)*.5f;
                float originY=top+48+(availableHeight-(maxY-minY)*previewScale)*.5f;
                for(int button=0;button<4;button++)
                {
                    int index=page*4+button,skill=mobileBindings[index];
                    Rect hit=slots[index]=BindingPreviewRect(layout.Skills[button],minX,minY,originX,originY,previewScale);
                    bool target=bindingDragSource>=0&&hit.Contains(bindingDragPoint);
                    if(skill>=0)
                    {
                        float size=hit.width;
                        DrawIcon(hit,UIIconAtlas.ControlRing(),UIIconAtlas.SkillColor(session.Progression.Profile.heroClass,skill));
                        DrawIcon(new Rect(hit.center.x-size*.5f,hit.center.y-size*.5f,size,size),UIIconAtlas.SkillGlyph(session.Progression.Profile.heroClass,skill,48),index==bindingDragSource?muted:UIIconAtlas.SkillColor(session.Progression.Profile.heroClass,skill));
                        if(hit.Contains(Mouse))tooltip=GameBalance.SkillName(session.Progression.Profile.heroClass,skill);
                    }
                    else Text(hit,"空位",TouchFont(11),muted,false,false,TextAnchor.MiddleCenter);
                }
                var fixedAreas=new[]{layout.Skills[4],layout.Attack,layout.Dodge,layout.Jump};
                string[] captions={"大招","普攻","闪现","跳跃"};string[] icons={"","attack","blink","jump"};
                for(int i=0;i<fixedAreas.Length;i++)
                {
                    Rect hit=BindingPreviewRect(fixedAreas[i],minX,minY,originX,originY,previewScale);
                    if(i!=0)DrawMobileControlSurface(hit,false,false);
                    else DrawIcon(hit,UIIconAtlas.ControlRing(),UIIconAtlas.SkillColor(session.Progression.Profile.heroClass,9));
                    float size=hit.width*(i==0?1f:.64f);
                    DrawIcon(new Rect(hit.center.x-size*.5f,hit.center.y-size*.5f,size,size),i==0?UIIconAtlas.SkillGlyph(session.Progression.Profile.heroClass,9,48):i==1?AuthoredIconArt.BasicAttack(session.Progression.Profile.heroClass):UIIconAtlas.Utility(icons[i]),i==0?UIIconAtlas.SkillColor(session.Progression.Profile.heroClass,9):muted);
                    if(hit.Contains(Mouse))tooltip=captions[i]+" · 固定按键";
                    DrawIcon(new Rect(hit.xMax-10*u,hit.y,10*u,10*u),UIIconAtlas.EquipmentLock(true),muted);
                }
                Rect pageArrow=BindingPreviewRect(layout.SkillPage,minX,minY,originX,originY,previewScale);
                float arrowSize=24*u*previewScale;
                DrawIcon(new Rect(pageArrow.center.x-arrowSize*.5f,pageArrow.center.y-arrowSize*.5f,arrowSize,arrowSize),UIIconAtlas.SkillPageArrow(),Color.white);

            }
            if(Input.touchCount>0)
            {
                if(bindingTouchFrame!=Time.frameCount)
                {
                    bindingTouchFrame=Time.frameCount;bool found=false;
                    for(int i=0;i<Input.touchCount;i++)
                    {
                        var touch=Input.GetTouch(i);Vector2 point=ScreenToUI(touch.position);
                        if(touch.phase==TouchPhase.Began&&bindingDragSource<0&&!UITransitionBlocked)
                            for(int slot=0;slot<slots.Length;slot++)if(slots[slot].Contains(point)&&mobileBindings[slot]>=0)
                            {bindingDragSource=slot;bindingDragFinger=touch.fingerId;bindingDragOrigin=point;break;}
                        if(bindingDragFinger!=touch.fingerId)continue;
                        found=true;bindingDragPoint=point;
                        if(touch.phase==TouchPhase.Ended||touch.phase==TouchPhase.Canceled)
                            FinishBindingDrag(slots,point,touch.phase==TouchPhase.Canceled);
                    }
                    if(!found){bindingDragSource=-1;bindingDragFinger=-1000;}
                }
            }
            else
            {
                if(bindingDragFinger>=0){bindingDragSource=-1;bindingDragFinger=-1000;}
                var e=Event.current;
                if(e.type==EventType.MouseDown&&e.button==0&&!UITransitionBlocked)
                    for(int slot=0;slot<slots.Length;slot++)if(slots[slot].Contains(Mouse)&&mobileBindings[slot]>=0)
                    {bindingDragSource=slot;bindingDragOrigin=bindingDragPoint=Mouse;e.Use();break;}
                if(bindingDragSource>=0&&e.type==EventType.MouseDrag){bindingDragPoint=Mouse;e.Use();}
                if(bindingDragSource>=0&&e.type==EventType.MouseUp){FinishBindingDrag(slots,Mouse,false);e.Use();}
            }
            if(bindingDragSource>=0)
                DrawIcon(new Rect(bindingDragPoint.x-22*u,bindingDragPoint.y-22*u,44*u,44*u),UIIconAtlas.SkillGlyph(session.Progression.Profile.heroClass,mobileBindings[bindingDragSource],48),UIIconAtlas.SkillColor(session.Progression.Profile.heroClass,mobileBindings[bindingDragSource]));
        }
        private bool mobilePositionEditor,positionValid=true;
        private float[] positionDraft,positionDragStart;
        private int positionButton=-1,positionFinger=-1,positionFrame=-1;
        private Vector2 positionOrigin;
        private MobileControlLayout.Area positionGroupOrigin;
        private bool positionIPad;
        private Vector2 positionViewport;
        private string positionHint;
        private void BeginMobilePositionEditor()
        {
            EnsureMobileBindings();positionDraft=MobileControls.SavedSkillPositions();
            mobilePositionEditor=true;positionButton=-1;positionFinger=-1;positionValid=true;
            positionIPad=MobileControls.IsIPad;positionViewport=new Vector2(MobileControls.Layout.Width,MobileControls.Layout.Height);
            positionHint="拖动右下角任意按钮，整组一起移动；战斗已暂停。";
            MobileControls.PreviewSkillPositions(positionDraft);CancelMobileCast();CancelMobileScroll();BlockUITransition();
        }
        private void CancelMobilePositionEditor()
        {
            mobilePositionEditor=false;positionButton=-1;positionFinger=-1;positionDraft=null;
            MobileControls.PreviewSkillPositions(null);
        }
        private void StartPositionDrag(Vector2 point,int finger)
        {
            if(UITransitionBlocked||positionButton>=0)return;
            var l=MobileControls.Layout;
            var buttons=l.CombatButtons();
            for(int i=0;i<buttons.Length;i++)if(TouchRect(buttons[i]).Contains(point))
            {
                positionButton=i;positionFinger=finger;positionOrigin=point/TouchRatio;
                positionGroupOrigin=l.CombatButtonBounds();
                positionDragStart=(float[])positionDraft.Clone();return;
            }
        }
        private void MovePositionDrag(Vector2 point)
        {
            if(positionButton<0)return;
            var l=MobileControls.Layout;Vector2 delta=point/TouchRatio-positionOrigin;
            var bounds=positionGroupOrigin;
            float dx=Mathf.Clamp(delta.x,8-bounds.X,l.Width-8-bounds.X-bounds.Width);
            float dy=Mathf.Clamp(delta.y,8-bounds.Y,l.Height-20-bounds.Y-bounds.Height);
            positionDraft[0]=positionDragStart[0]+dx/l.Width;
            positionDraft[1]=positionDragStart[1]+dy/l.Height;
            MobileControls.PreviewSkillPositions(positionDraft);l=MobileControls.Layout;
            positionValid=l.CanPlaceCombatButtons();
            var group=TouchRect(l.CombatButtonBounds());
            float toolbarWidth=Mathf.Min(640,l.Width-24);
            if(group.Overlaps(TouchRect((l.Width-toolbarWidth)*.5f,10,toolbarWidth,92)))positionValid=false;
            positionHint=positionValid?"松手保留当前位置，点“保存”后生效。":"与其他按键或信息区重叠；请移到空白处，松手将退回原位。";
        }
        private void EndPositionDrag(bool cancelled)
        {
            if(positionButton<0)return;
            if(cancelled||!positionValid){positionDraft=positionDragStart;MobileControls.PreviewSkillPositions(positionDraft);}
            positionButton=-1;positionFinger=-1;positionValid=true;
        }
        private void DrawMobilePositionEditor()
        {
            var l=MobileControls.Layout;
            // A rotation or device change cancels the draft instead of saving against another viewport.
            if(positionIPad!=MobileControls.IsIPad||positionViewport!=new Vector2(l.Width,l.Height))
            {CancelMobilePositionEditor();BlockUITransition();return;}
            float barWidth=Mathf.Min(640,l.Width-24),left=(l.Width-barWidth)*.5f;
            Box(TouchRect(left,10,barWidth,92),jade,false);
            Text(TouchRect(left+16,18,barWidth-328,30),"整体调整位置",TouchFont(18),pale,true);
            bool idle=positionButton<0;
            if(Button(TouchRect(left+barWidth-304,16,96,36),"恢复默认",muted,idle))
            {positionDraft=new float[2];MobileControls.PreviewSkillPositions(positionDraft);positionHint="已预览默认位置，保存后生效；取消可恢复之前的布局。";}
            if(Button(TouchRect(left+barWidth-200,16,88,36),"取消",muted,idle))
            {CancelMobilePositionEditor();BlockUITransition();return;}
            if(PrimaryButton(TouchRect(left+barWidth-104,16,88,36),"保存",jade,idle))
            {MobileControls.SaveSkillPositions(positionDraft);mobilePositionEditor=false;positionDraft=null;BlockUITransition();return;}
            Text(TouchRect(left+16,60,barWidth-32,32),positionHint,TouchFont(12),positionValid?muted:gold,false,true);
            // HUD below this overlay already draws the exact live layout; only add selection outlines.
            var buttons=l.CombatButtons();
            for(int i=0;i<buttons.Length;i++)
            {
                Rect r=MobileVisualRect(TouchRect(buttons[i]),combatButton:true);
                int skill=i<5?BoundMobileSkill(i,mobileSkillPage):-1;
                if(i<5&&!CombatSkillLearned(session.Progression.Profile,skill))Text(r,skill<0?"空位":"未解锁",TouchFont(11),pale,false,false,TextAnchor.MiddleCenter);
                DrawIcon(r,UIIconAtlas.ControlRing(),positionButton>=0?(positionValid?jade:Color.red):new Color(1,1,1,.35f));
            }
            if(Input.touchCount>0)
            {
                if(positionFrame==Time.frameCount)return;positionFrame=Time.frameCount;bool found=false;
                for(int i=0;i<Input.touchCount;i++)
                {
                    var t=Input.GetTouch(i);Vector2 point=ScreenToUI(t.position);
                    if(t.phase==TouchPhase.Began)StartPositionDrag(point,t.fingerId);
                    if(t.fingerId!=positionFinger||positionButton<0)continue;
                    found=true;MovePositionDrag(point);
                    if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)EndPositionDrag(t.phase==TouchPhase.Canceled);
                }
                if(!found&&positionButton>=0)EndPositionDrag(true);
            }
            else
            {
                if(positionFinger>=0)EndPositionDrag(true);
                var e=Event.current;
                if(e.type==EventType.MouseDown&&e.button==0){StartPositionDrag(Mouse,-1);if(positionButton>=0)e.Use();}
                if(e.type==EventType.MouseDrag&&positionButton>=0){MovePositionDrag(Mouse);e.Use();}
                if(e.type==EventType.MouseUp&&positionButton>=0){MovePositionDrag(Mouse);EndPositionDrag(false);e.Use();}
            }
        }
    }
}
