using System;
namespace Emberfall
{
 public sealed class MobileCameraGesture
 {
  public const float MinimumPitch=-18,MaximumPitch=65,Threshold=9;
  public int Finger {get;private set;}=-1000;private float startX,startY,lastX,lastY;private bool dragging;
  public float YawDelta {get;private set;}
  public bool TapCompleted {get;private set;}
  public bool Begin(int finger,float y){return Begin(finger,0,y);}
  public bool Begin(int finger,float x,float y){if(Finger!=-1000)return false;Finger=finger;startX=lastX=x;startY=lastY=y;dragging=false;TapCompleted=false;YawDelta=0;return true;}
  public float Move(int finger,float y,float density,bool ended,bool cancelled)
  {return Move(finger,0,y,density,ended,cancelled);}
  public float Move(int finger,float x,float y,float density,bool ended,bool cancelled)
  {
   TapCompleted=false;YawDelta=0;
   if(Finger!=finger)return 0;if(cancelled){Cancel();return 0;}
   density=float.IsNaN(density)||density<=0?1:density;float delta=0;
   if(!dragging&&((x-startX)*(x-startX)+(y-startY)*(y-startY))/(density*density)>=Threshold*Threshold){dragging=true;delta=(y-startY)/density;YawDelta=(x-startX)/density;}
   else if(dragging){delta=(y-lastY)/density;YawDelta=(x-lastX)/density;}
   YawDelta=float.IsNaN(YawDelta)||float.IsInfinity(YawDelta)?0:Math.Max(-18,Math.Min(18,YawDelta*.22f));lastX=x;
   lastY=y;if(ended){bool tap=!dragging;float yaw=YawDelta;Cancel();YawDelta=yaw;TapCompleted=tap;}return float.IsNaN(delta)||float.IsInfinity(delta)?0:Math.Max(-18,Math.Min(18,delta*.18f));
  }
  public void Cancel(){Finger=-1000;dragging=false;TapCompleted=false;YawDelta=0;}
  public static float Apply(float pitch,float delta){if(float.IsNaN(delta)||float.IsInfinity(delta))return pitch;return Math.Max(MinimumPitch,Math.Min(MaximumPitch,pitch-delta));}
 }
}
