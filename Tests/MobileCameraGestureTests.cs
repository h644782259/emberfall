using System;using Emberfall;
public static class MobileCameraGestureTests
{
 public static string Run()
 {
  var g=new MobileCameraGesture();if(!g.Begin(3,100)||g.Begin(4,100))throw new Exception("ownership");
  if(g.Move(3,105,1,false,false)!=0||g.Move(4,150,1,false,false)!=0)throw new Exception("threshold/other finger");
  float low=g.Move(3,120,1,false,false);g.Cancel();g.Begin(3,200);float high=g.Move(3,240,2,false,false);if(Math.Abs(low-high)>.001f)throw new Exception("density invariance");
  if(MobileCameraGesture.Apply(60,100)!=65||MobileCameraGesture.Apply(30,-100)!=25)throw new Exception("safe camera pitch bounds");
  g.Move(3,250,2,false,true);if(g.Finger!=-1000||g.Move(3,300,2,false,false)!=0)throw new Exception("cancel stale");
  g.Begin(4,0);g.Move(4,100,1,true,false);if(g.Finger!=-1000)throw new Exception("release");
  g.Begin(8,100,100);g.Move(8,140,100,1,false,false);if(g.YawDelta<=0)throw new Exception("horizontal turn");
  g.Move(8,120,100,1,true,false);if(g.YawDelta>=0||g.Finger!=-1000)throw new Exception("reverse turn and release");
  return "Mobile camera gesture  tests passed";
 }
}
