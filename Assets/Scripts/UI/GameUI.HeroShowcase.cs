using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private sealed class HeroShowcase
        {
            internal readonly CollectionModelPreview Model=new CollectionModelPreview {TransparentBackground=true,MaximumFrameRate=24};
            internal int LastSeenFrame;
        }
        private readonly Dictionary<string,HeroShowcase> heroShowcases=new Dictionary<string,HeroShowcase>();
        private readonly List<string> expiredShowcases=new List<string>();
        private readonly Texture2D[] heroStageArt=new Texture2D[4];
        private void ReconcileHeroShowcases()
        {
            expiredShowcases.Clear();
            foreach(var pair in heroShowcases)
                if(Time.frameCount-pair.Value.LastSeenFrame>3)expiredShowcases.Add(pair.Key);
            foreach(var key in expiredShowcases){heroShowcases[key].Model.Dispose();heroShowcases.Remove(key);}
        }
        private void ReleaseHeroShowcases()
        {
            foreach(var value in heroShowcases.Values)value.Model.Dispose();heroShowcases.Clear();
            for(int i=0;i<heroStageArt.Length;i++)if(heroStageArt[i]!=null){Destroy(heroStageArt[i]);heroStageArt[i]=null;}
        }
        private void DrawHeroPortrait(Rect area,HeroClass hero,string key,SaveSlotInfo save=null)
        {
            bool inScene=key=="home";
            if(!inScene)DrawHeroStage(area,hero);
            HeroShowcase display;
            if(!heroShowcases.TryGetValue(key,out display)){display=new HeroShowcase();heroShowcases.Add(key,display);}
            display.LastSeenFrame=Time.frameCount;
            var preview=display.Model;
            preview.SceneLighting=inScene;preview.FullSilhouette=true;preview.HideTurnRing=true;
            preview.SetCenterOnAvatar(true);
            preview.SetYaw(20+Mathf.Sin(Time.unscaledTime*.45f+(int)hero)*7);
            float pixelWidth=area.width*Mathf.Abs(GUI.matrix.m00),pixelHeight=area.height*Mathf.Abs(GUI.matrix.m11);
            // Render at physical display resolution; single portraits get modest supersampling.
            float resolutionScale=inScene?1.25f:1f;
            preview.SetViewport(pixelWidth*resolutionScale,pixelHeight*resolutionScale,MobileControls.Active);
            Texture image=preview.RenderSafe(hero,save?.PreviewWeapon,save?.PreviewArmor,save?.PreviewRelic,save?.PreviewWings,save?.PreviewFashionWeapon);
            if(image!=null)GUI.DrawTexture(area,image,ScaleMode.ScaleToFit,true);
        }
        private bool HomeButton(Rect area,string caption,bool primary,int font)
        {
            bool hover=GUI.enabled&&area.Contains(Mouse);
            Color edge=new Color(.66f,.53f,.32f,hover?.95f:primary?.72f:.38f);
            Color ink=hover?new Color(.15f,.16f,.17f,.96f):new Color(.045f,.06f,.075f,primary?.94f:.78f);
            if(hover&&Input.GetMouseButton(0))ink=Color.Lerp(ink,Color.black,.25f);
            Surface(new Rect(area.x,area.y+2,area.width,area.height),new Color(0,0,0,.25f));
            Surface(area,ink,true);SurfaceFrame(area,edge);
            if(primary)
            {
                Fill(new Rect(area.x+12,area.y+2,area.width-24,1),new Color(.91f,.78f,.51f,hover?.42f:.22f));
                Fill(new Rect(area.center.x-18,area.yMax-2,36,1),edge);
            }
            Text(area,caption,font,hover?new Color(1,.92f,.74f):primary?new Color(.94f,.84f,.64f):new Color(.78f,.79f,.76f),primary,false,TextAnchor.MiddleCenter);
            bool clicked=GUI.Button(area,GUIContent.none,invisibleButton);
            if(clicked)GameAudio.Play(SoundCue.UI);
            return clicked;
        }
        private void DrawHeroStage(Rect area,HeroClass hero)
        {
            int index=Mathf.Clamp((int)hero,0,3);
            if(heroStageArt[index]==null)heroStageArt[index]=CreateHeroStage(hero);
            GUI.DrawTexture(area,heroStageArt[index],ScaleMode.StretchToFill,true);
            Color tint=GameBalance.ClassColor(hero);
            // A few drifting embers animate the cached painted backdrop without extra scene cameras or particles.
            if(Event.current.type==EventType.Repaint)
                for(int i=0;i<9;i++)
                {
                    float phase=Mathf.Repeat(Time.unscaledTime*.055f+i*.137f,1);
                    float px=.13f+Mathf.Repeat(i*.381966f,1)*.74f;
                    float alpha=Mathf.Sin(phase*Mathf.PI)*.65f;
                    float size=Mathf.Max(1,area.width*.006f);
                    Fill(new Rect(area.x+px*area.width,area.yMax-phase*area.height*.88f-size,size,size),new Color(1,.83f,.5f,alpha));
                }
        }
        private Texture2D CreateHeroStage(HeroClass hero)
        {
            const int w=256,h=320;
            var pixels=new Color32[w*h];Color tint=GameBalance.ClassColor(hero);
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float u=(x+.5f)/w,v=(y+.5f)/h,dx=(u-.5f)*2;
                float halo=Mathf.Exp(-(dx*dx*3.2f+(v-.59f)*(v-.59f)*8));
                Color c=Color.Lerp(new Color(.025f,.045f,.075f),tint,.10f+halo*.16f);
                float floor=Mathf.Sqrt(dx*dx+Mathf.Pow((v-.14f)*6.5f,2));
                float mist=Mathf.Exp(-floor*floor*2.8f);
                c+=tint*mist*.1f;
                float edge=Mathf.SmoothStep(0,1,Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v))*7);
                float haze=halo*.5f+mist*.12f;
                c.a=edge*haze;pixels[y*w+x]=c;
            }
            var art=new Texture2D(w,h,TextureFormat.RGBA32,false){name="Hero stage / "+hero,hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            art.SetPixels32(pixels);art.Apply(false,true);return art;
        }
    }
}
