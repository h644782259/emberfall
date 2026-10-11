using System;
namespace Emberfall
{
 public sealed class TouchScrollGesture
 {
  public int Finger {get;private set;}=-1000;public bool Dragging {get;private set;}
  public float Position {get;private set;}private float originX,originY,initial,maximum,threshold;
  public bool Begin(int finger,float pointerX,float pointerY,float position,float max,float dragThreshold)
  {if(Finger!=-1000)return false;Finger=finger;originX=pointerX;originY=pointerY;initial=Position=position;maximum=Math.Max(0,max);threshold=Math.Max(1,dragThreshold);Dragging=false;return true;}
  public bool Advance(int finger,float pointerX,float pointerY,bool ended,bool cancelled)
  {
   if(Finger!=finger||Finger==-1000)return false;
   if(cancelled){bool was=Dragging;Cancel();return was;}
   // Moving in any direction cancels the child button's tap, even when the
   // content can only scroll vertically. Keep that decision until release.
   float deltaX=pointerX-originX,deltaY=pointerY-originY;
   if(!Dragging&&deltaX*deltaX+deltaY*deltaY>=threshold*threshold)Dragging=true;
   if(Dragging)Position=Math.Max(0,Math.Min(maximum,initial-deltaY));
   bool consume=Dragging;if(ended){Finger=-1000;Dragging=false;}return consume;
  }
  public void Cancel(){Finger=-1000;Dragging=false;}
 }
}
