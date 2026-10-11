using System;
namespace Emberfall
{
    // Image-led town cards retain fixed travel actions at the smallest landscape size.
    public readonly struct TownTravelCardLayout
    {
        public readonly MobilePanelLayout.Area Tile,Image,Title,UnlockHint,Action;
        public TownTravelCardLayout(float width,float height,bool hasNotice,int index)
        {
            if(index<0||index>=3)throw new ArgumentOutOfRangeException(nameof(index));
            float top=hasNotice?32:0,w=(width-16)/3,h=height-top,x=index*(w+8);
            Tile=new MobilePanelLayout.Area(x,top,w,h);
            Image=new MobilePanelLayout.Area(x+1,top+1,w-2,h-54);
            Title=new MobilePanelLayout.Area(x+6,top+6,w-12,36);
            UnlockHint=new MobilePanelLayout.Area(x+8,Image.YMax-32,w-16,28);
            Action=new MobilePanelLayout.Area(x+6,Tile.YMax-50,w-12,44);
        }
    }
}
