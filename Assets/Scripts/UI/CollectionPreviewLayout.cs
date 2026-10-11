using System;
namespace Emberfall
{
    // Rectangles are in their caller's UI units. Mobile controls live in the detail
    // scroll content, not on the model image; desktop reserves a separate lower bank.
    public sealed class CollectionPreviewLayout
    {
        public readonly MobilePanelLayout.Area Model,View,Actions,Turns;
        public float ControlsBottom {get{return Turns.YMax;}}
        private CollectionPreviewLayout(MobilePanelLayout.Area model,float x,float y,float width,float height,float gap)
        {Model=model;View=new MobilePanelLayout.Area(x,y,width,height);Actions=new MobilePanelLayout.Area(x,y+height+gap,width,height);Turns=new MobilePanelLayout.Area(x,y+2*(height+gap),width,height);}
        public static CollectionPreviewLayout Desktop(MobilePanelLayout.Area frame)
        {
            const float height=42,gap=6;float bank=height*3+gap*2;
            return new CollectionPreviewLayout(new MobilePanelLayout.Area(frame.X,frame.Y,frame.Width,Math.Max(1,frame.Height-bank-gap)),frame.X,frame.YMax-bank,frame.Width,height,gap);
        }
        public static CollectionPreviewLayout Mobile(MobilePanelLayout.Area model,MobilePanelLayout.Area detail)
        {return new CollectionPreviewLayout(model,detail.X+8,detail.Y+8,detail.Width-26,48,6);}
        public MobilePanelLayout.Area Button(int group,int index)
        {
            int count=group==2?2:3;if(group<0||group>2||index<0||index>=count)throw new ArgumentOutOfRangeException();
            var row=group==0?View:group==1?Actions:Turns;float width=(row.Width-(count-1)*6)/count;
            return new MobilePanelLayout.Area(row.X+index*(width+6),row.Y,width,row.Height);
        }
    }
}
