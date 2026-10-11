using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        // Fits the existing 188x76 phone card: title 4..20, rows 21..52,
        // support 54..66 and aggregate 69..71. Chapter rows retain their original layout.
        private void DrawRoomSeals(Rect area,float u,bool compact,ChapterSealPresentation first,ChapterSealPresentation second,RoomObjectivePresentation objective)
        {
            float stride=compact?16:22;
            for(int i=0;i<2;i++)
            {
                var seal=i==0?first:second;if(seal==null)return;
                Rect row=new Rect(area.x,area.y+i*stride*u,area.width,(stride-1)*u);
                if(seal.Occupied)Fill(row,new Color(.16f,.23f,.19f,.95f));
                Color tint=seal.Complete?jade:seal.Contested?gold:seal.Occupied?pale:muted;
                Text(new Rect(row.x+3*u,row.y,row.width-6*u,(stride-4)*u),seal.Label,Mathf.RoundToInt((compact?10:11)*u),tint,seal.Occupied,false,TextAnchor.MiddleLeft);
                Bar(new Rect(row.x+3*u,row.yMax-2*u,row.width-6*u,u),seal.Seconds/3f,tint);
            }
            Text(new Rect(area.x,area.y+(stride*2+1)*u,area.width,(compact?12:15)*u),objective.SupportHint,Mathf.RoundToInt(10*u),muted,false,false,TextAnchor.MiddleCenter);
            Bar(new Rect(area.x+2*u,area.y+(stride*2+(compact?16:20))*u,area.width-4*u,2*u),objective.Fraction,jade);
        }
        private void DrawChapterSeals(Rect area,float u,ChapterSealPresentation first,ChapterSealPresentation second)
        {
            for(int i=0;i<2;i++)
            {
                var seal=i==0?first:second;if(seal==null)return;
                Rect row=new Rect(area.x,area.y+i*22*u,area.width,21*u);
                if(seal.Occupied)Fill(row,new Color(.16f,.23f,.19f,.95f));
                Color tint=seal.Complete?jade:seal.Contested?gold:seal.Occupied?pale:muted;
                Text(new Rect(row.x+3*u,row.y,row.width-6*u,17*u),seal.Label,Mathf.RoundToInt(11*u),tint,seal.Occupied,false,TextAnchor.MiddleLeft);
                Bar(new Rect(row.x+3*u,row.yMax-3*u,row.width-6*u,2*u),seal.Seconds/3f,tint);
            }
        }
    }
}
