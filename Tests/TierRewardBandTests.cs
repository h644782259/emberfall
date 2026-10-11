using System;using Emberfall;
public static class TierRewardBandTests
{
 public static string Run()
 {
  int checks=0;
  foreach(int tier in new[]{int.MinValue,-1,0,1,4,5,9,10,19,20,39,40,100,int.MaxValue})
  {
   int clamp=Math.Max(1,Math.Min(100,tier));int band=clamp<5?0:clamp<10?1:clamp<20?2:clamp<40?3:4;
   if(TierRewardBand.Of(tier)!=band)throw new Exception("shared tier boundary");checks++;
   for(int basis=1;basis<=4;basis++){if(TierRewardBand.Materials(basis,tier)!=basis+band)throw new Exception("mode and large clear materials");checks++;}
   if(TierRewardBand.Materials(0,tier)!=0||TierRewardBand.Materials(-1,tier)!=0||TierRewardBand.Materials(int.MaxValue,tier)>8)throw new Exception("invalid base reward bounded");checks++;
  }
  return "PASS: "+checks+" shared ordinary/trial/large-expedition tier reward assertions";
 }
}
