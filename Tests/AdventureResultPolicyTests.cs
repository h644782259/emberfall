using System;using Emberfall;
public static class AdventureResultPolicyTests
{
 public static string Run()
 {
  if(!AdventureResultPolicy.AcceptsKill(true,false,true))throw new Exception("active kill rejected");
  if(AdventureResultPolicy.AcceptsKill(true,true,true)||AdventureResultPolicy.AcceptsDamage(true,true))throw new Exception("late timeout/win callback must not damage or pay");
  if(AdventureResultPolicy.AcceptsKill(false,false,true)||AdventureResultPolicy.AcceptsKill(true,false,false))throw new Exception("title/old-scene callback rejected");
  return "Terminal adventure late-damage/kill and scene identity tests passed";
 }
}
