using System;using Emberfall;
public static class DeferredRoomChoiceTests
{
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 public static string Run()
 {
  var queued=new DeferredRoomChoice();var room=new object();
  Check(!queued.Request(null,1,10),"no room cannot request");
  int hitBudget=6,applied=0;bool modal=false;
  for(int hit=0;hit<hitBudget;hit++)
  {
   applied++;
   if(hit==0)Check(queued.Request(room,4,100),"goal met on first chain hop queues choice");
   modal|=queued.TryClaim(room,4,100,true,true);
   Check(!modal,"choice cannot interrupt remaining damage in same frame");
  }
  Check(applied==hitBudget&&queued.Pending,"entire existing event budget completed before modal");
  Check(!queued.Request(room,4,101),"reentrant goal cannot replace or postpone queued choice");
  Check(!queued.TryClaim(room,4,101,true,false)&&queued.Pending,"pause keeps a valid request without opening modal");
  Check(queued.TryClaim(room,4,102,true,true)&&!queued.Pending,"next eligible session tick claims once");
  Check(!queued.TryClaim(room,4,103,true,true),"reentry cannot reopen modal");
  queued.Request(room,4,200);Check(!queued.TryClaim(room,5,201,true,true)&&!queued.Pending,"new combat epoch retires old choice");
  queued.Request(room,4,200);Check(!queued.TryClaim(new object(),4,201,true,true)&&!queued.Pending,"same-index replacement room cannot reuse old identity");
  queued.Request(room,4,200);Check(!queued.TryClaim(room,4,201,false,false)&&!queued.Pending,"death/failure/exit cancel even when advancement blocked");
  queued.Request(room,4,200);queued.Cancel();queued.Cancel();Check(!queued.Pending&&!queued.TryClaim(room,4,201,true,true),"explicit teardown is idempotent");
  Check(queued.Request(new object(),6,300),"new encounter can request after teardown");
  return "PASS: "+n+" deferred room-choice frame/epoch/reentry/budget assertions";
 }
}
