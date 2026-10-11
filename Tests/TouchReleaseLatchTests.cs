using System;using Emberfall;
public static class TouchReleaseLatchTests
{
 public static string Run()
 {
  int n=0;var gate=new TouchReleaseLatch();
  Check(!gate.IsBlocked(0,false,false),"new gate idle",ref n);
  gate.Block(1,23);Check(gate.Finger==23&&gate.IsBlocked(1.39f,false,false),"minimum transition interval",ref n);
  Check(gate.IsBlocked(2,true,true),"initiating finger remains consumed",ref n);
  Check(!gate.IsBlocked(2,true,false),"unrelated held movement finger cannot indefinitely block",ref n);
  Check(!gate.IsBlocked(3,true,true),"finished transition cannot relatch without new action",ref n);
  gate.Block(4);Check(gate.IsBlocked(5,true,false),"destructive default waits for all input release",ref n);
  Check(!gate.IsBlocked(5,false,false),"default release clears",ref n);
  gate.Block(6,-2);Check(gate.IsBlocked(7,true,true)&&!gate.IsBlocked(7,false,false),"simulated/attached mouse uses same ownership",ref n);
  gate.Block(8,10);gate.Block(8.2f,11);Check(gate.Finger==11&&gate.IsBlocked(8.5f,false,false),"new transition replaces prior identity and minimum interval",ref n);
  Check(gate.IsBlocked(float.NaN,false,false),"invalid clock cannot bypass an open latch",ref n);
  return "PASS: "+n+" pointer-specific transition-release assertions";
 }
 static void Check(bool ok,string reason,ref int n){n++;if(!ok)throw new Exception(reason);}
}
