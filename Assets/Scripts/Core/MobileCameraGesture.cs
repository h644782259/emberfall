using System;
namespace Emberfall
{
 public sealed class MobileCameraGesture
 {
  public const float MinimumPitch=25,MaximumPitch=65,Threshold=9;
  public int Finger {get;private set;}=-1000;private float startX,startY,lastY;private bool dragging;
  public bool TapCompleted {get;private set;}
  public bool Begin(int finger,float y){return Begin(finger,0,y);}
  public bool Begin(int finger,float x,float y){if(Finger!=-1000)return false;Finger=finger;startX=x;startY=lastY=y;dragging=false;TapCompleted=false;return true;}
  public float Move(int finger,float y,float density,bool ended,bool cancelled)
  {return Move(finger,0,y,density,ended,cancelled);}
  public float Move(int finger,float x,float y,float density,bool ended,bool cancelled)
  {
   TapCompleted=false;
   if(Finger!=finger)return 0;if(cancelled){Cancel();return 0;}
   density=float.IsNaN(density)||density<=0?1:density;float delta=0;
   if(!dragging&&((x-startX)*(x-startX)+(y-startY)*(y-startY))/(density*density)>=Threshold*Threshold){dragging=true;delta=(y-startY)/density;}
   else if(dragging)delta=(y-lastY)/density;
   lastY=y;if(ended){bool tap=!dragging;Cancel();TapCompleted=tap;}return float.IsNaN(delta)||float.IsInfinity(delta)?0:Math.Max(-18,Math.Min(18,delta*.18f));
  }
  public void Cancel(){Finger=-1000;dragging=false;TapCompleted=false;}
  public static float Apply(float pitch,float delta){if(float.IsNaN(delta)||float.IsInfinity(delta))return pitch;return Math.Max(MinimumPitch,Math.Min(MaximumPitch,pitch+delta));}
 }
}
