using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        // Skill state intensity must survive authored-art tint normalization.
        // Identity hue remains in the photograph and the thin surrounding rim.
        private static void DrawSkillMaterial(Rect rect,Texture2D icon,float intensity,float opacity=1f)
        {
            if(icon==null)return;
            icon=UIIconAtlas.ForDisplay(icon,Mathf.Max(rect.width,rect.height)*Mathf.Abs(GUI.matrix.lossyScale.x));
            Color previous=GUI.color;
            GUI.color=new Color(intensity,intensity,intensity,opacity*controlOpacity);
            if(MobileControls.Active)GUI.DrawTexture(rect,icon,ScaleMode.ScaleToFit,true,0,Color.white,0,rect.width*.5f);
            else GUI.DrawTexture(rect,icon,ScaleMode.ScaleToFit,true);
            GUI.color=previous;
        }

        private void DrawRecoveringSkill(Rect rect,Texture2D icon,int skill,bool ready)
        {
            var player=session.Player;
            float remaining=player==null?0:player.SkillCooldownRemaining(skill);
            float period=GameBalance.EffectiveCooldown(session.Progression.Profile.heroClass,skill,session.Progression.Profile.skillRanks[skill]);
            if(player!=null&&skill==SkillStockRules.Skill(session.Progression.Profile.heroClass)&&player.SkillCharges(skill)==0)
            {remaining=player.SkillRechargeRemaining(skill);period=player.SkillRechargePeriod(skill);}
            // One identity-colored inner frame; no common green/blue outer rim.
            if(MobileControls.Active)
            {
                DrawIcon(rect,UIIconAtlas.ControlDisc(),new Color(.02f,.035f,.05f,.18f));
            }
            if(remaining>.01f&&period>0)
            {
                float fill=1-Mathf.Clamp01(remaining/period),filled=rect.height*fill;
                DrawSkillMaterial(rect,AuthoredIconArt.Cooldown(icon),.25f);
                if(filled>0)
                {
                    GUI.BeginGroup(new Rect(rect.x,rect.yMax-filled,rect.width,filled));
                    DrawSkillMaterial(new Rect(0,filled-rect.height,rect.width,rect.height),icon,1f);
                    GUI.EndGroup();
                }
                // Cooldown belongs to the glyph center; the icon fill is the only recovery meter.
                string countdown=remaining.ToString(remaining>=10?"0":"0.0");
                int font=Mathf.RoundToInt(Mathf.Clamp(rect.width*.3f,14,32));
                float offset=Mathf.Max(1,rect.width*.025f);
                Color shadow=new Color(.005f,.01f,.02f,1);
                Text(new Rect(rect.x-offset,rect.y,rect.width,rect.height),countdown,font,shadow,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(rect.x+offset,rect.y,rect.width,rect.height),countdown,font,shadow,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(rect.x,rect.y-offset,rect.width,rect.height),countdown,font,shadow,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(rect.x,rect.y+offset,rect.width,rect.height),countdown,font,shadow,true,false,TextAnchor.MiddleCenter);
                Text(rect,countdown,font,Color.white,true,false,TextAnchor.MiddleCenter);
            }
            else
            {
                // Fully restored material colors provide the ready highlight.
                DrawSkillMaterial(rect,ready?icon:AuthoredIconArt.Cooldown(icon),ready?1f:.35f);
            }
            // The material-filled artwork has an opaque center. Draw its state
            // rim afterwards so readiness remains visible at the circular edge.
            if(MobileControls.Active)
            {
                Color frame=UIIconAtlas.SkillColor(session.Progression.Profile.heroClass,skill);
                if(ready)DrawIcon(rect,UIIconAtlas.ControlRing(true),new Color(frame.r,frame.g,frame.b,.2f));
                DrawIcon(rect,UIIconAtlas.ControlRing(),new Color(frame.r,frame.g,frame.b,ready?.9f:.3f));
            }
        }

        private void DrawHotbarKey(Rect icon,string key,bool disabled)
        {
            int font=key.Length>2?11:15;
            Rect corner=new Rect(icon.x+3,icon.y+1,icon.width-6,19);
            Color outline=new Color(.02f,.025f,.035f,.95f);
            // An outlined glyph sits on the artwork; no separate keycap or row.
            foreach(var offset in new[]{new Vector2(-1,0),new Vector2(1,0),new Vector2(0,-1),new Vector2(0,1)})
                Text(new Rect(corner.x+offset.x,corner.y+offset.y,corner.width,corner.height),key,font,outline,true,false,TextAnchor.UpperLeft);
            Text(corner,key,font,disabled?muted:Color.white,true,false,TextAnchor.UpperLeft);
        }

        private void DrawOutlinedIconText(Rect rect,string value,int font,Color color,TextAnchor alignment)
        {
            Color outline=new Color(.015f,.02f,.03f,1f);
            for(int x=-1;x<=1;x++)
                for(int y=-1;y<=1;y++)
                    if(x!=0||y!=0)Text(new Rect(rect.x+x,rect.y+y,rect.width,rect.height),value,font,outline,true,false,alignment);
            Text(rect,value,font,color,true,false,alignment);
        }

        private void DrawFunctionKey(Rect icon,string key)
        {
            // Small corner gutter separates the shortcut from the artwork.
            Rect corner=new Rect(icon.x+2,icon.y+1,icon.width-4,12);
            DrawOutlinedIconText(corner,key,key.Length>2?7:10,Color.white,TextAnchor.UpperLeft);
        }

        private void DrawExperienceBadge(Rect rect,float unit)
        {
            Rect badge=new Rect(rect.x+unit,rect.y+unit,24*unit,12*unit);
            Fill(badge,new Color(.015f,.035f,.055f,.95f));
            Text(badge,"EXP",Mathf.RoundToInt(8*unit),new Color(.45f,1f,.8f),true,false,TextAnchor.MiddleCenter);
        }

        // Same identity on lists, tree nodes, details and battle controls. State
        // captions live outside the glyph; rank marks are not replacement text.
        private void DrawSkillIdentity(Rect r, HeroClass hero, int skill, int rank, bool available, int rasterSize = 32)
        {
            bool passive = GameBalance.IsPassive(skill);
            Color accent = passive ? new Color(.75f,.65f,.94f) : GameBalance.ClassColor(hero);
            Fill(r, new Color(.035f,.065f,.085f,.9f));
            Border(r, available ? accent : muted * .55f);
            float unit = Mathf.Max(.5f,r.width/32f);
            if (passive) Border(new Rect(r.x+2*unit,r.y+2*unit,r.width-4*unit,r.height-4*unit),available?accent*.65f:muted*.4f);
            float inset = 4*unit;
            DrawIcon(new Rect(r.x+inset,r.y+inset,r.width-2*inset,r.height-2*inset),UIIconAtlas.Skill(hero,skill,rasterSize),available?Color.white:new Color(.5f,.55f,.6f,.85f));
            for(int mark=0;mark<3;mark++)
                Fill(new Rect(r.center.x+(mark-1)*6*unit-1.5f*unit,r.yMax-4*unit,3*unit,2*unit),mark<rank?accent:muted*.35f);
        }
    }
}
