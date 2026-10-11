using System;using Emberfall;using UnityEngine;
public static class MobileOpportunityInputProductionTests
{
 static int count;static void C(bool value,string text){count++;if(!value)throw new Exception(text);}
 static void Tap(MobileControls c,int id,Vector2 point){c.ProcessPointer(id,TouchPhase.Began,point);c.ProcessPointer(id,TouchPhase.Ended,point);}
 static CombatOpportunityState Window()=>new CombatOpportunityState(CombatOpportunityKind.Shatter,1.5f,blockReason:"缺能",duration:4);
 public static string Run()
 {
  // Raw-DPI replay uses a prior runtime safe area; corrected replay adds the
  // hypothesized native display dimensions. Neither is new rendering evidence.
  // Real Layout/GUI/pointer methods execute; Unity delivery remains a boundary.
  var viewports=new System.Collections.Generic.List<float[]>{new[]{568f,320f,0f,0f,568f,320f,163f,568f,320f,0f},new[]{1920f,883f,136f,44f,1648f,839f,460f,1920f,883f,0f},new[]{1920f,883f,136f,44f,1648f,839f,460f,2622f,1206f,0f}};
  // Native specifications and synthetic 75-percent render cases execute the
  // same actual iPad Layout/GUI/pointer paths, never a substituted Pad geometry.
  foreach(var device in new[]{new[]{2048f,1536f,264f},new[]{2266f,1488f,326f},new[]{2388f,1668f,264f},new[]{2732f,2048f,264f}})
  foreach(float fraction in new[]{1f,.75f})viewports.Add(new[]{device[0]*fraction,device[1]*fraction,0f,0f,device[0]*fraction,device[1]*fraction,device[2],device[0],device[1],1f});
  foreach(var viewport in viewports)
  foreach(int preset in new[]{-1,0,1})for(int slot=0;slot<12;slot++)
  {
   Screen.width=viewport[0];Screen.height=viewport[1];Screen.dpi=viewport[6];Display.main.systemWidth=viewport[7];Display.main.systemHeight=viewport[8];EffectPreferences.TouchPosition=preset;
   MobileControls.SafeArea=new Rect(viewport[2],viewport[3],viewport[4],viewport[5]);MobileControls.ResetInput();
   float expectedDpi=viewport[6]*Math.Min(viewport[0]/viewport[7],viewport[1]/viewport[8]);
   float expectedScale=Math.Max(.25f,Math.Min(expectedDpi/163f,Math.Min(viewport[5]/320f,viewport[4]/568f)));
   MobileControls.IsIPad=viewport[9]>0;
   float expectedZoom=MobileControls.IsIPad?Math.Max(1f,Math.Min(1.4f,Math.Min(viewport[4]/(568f*expectedScale),viewport[5]/(320f*expectedScale)))):1f;
   expectedScale*=expectedZoom;
   var actualLayout=MobileControls.Layout;
   C(Math.Abs(actualLayout.Scale-expectedScale)<.0001f,"render-grid DPI governs the actual Layout getter");
   C(object.ReferenceEquals(actualLayout,MobileControls.Layout),"stable render/native dimensions reuse actual layout cache");
   var session=new GameSession();var hero=new PlayerController(session);var ui=new GameUI{mobileSkillPage=slot>=5&&slot<9?1:0};var controls=new MobileControls(session,ui);ui.BindInputSessionForFixture(session);var world=controls.Control(MobileControls.Layout.CombatView);var enemy=new EnemyController(8);session.Enemies.Add(enemy);hero.PinMobileTarget(enemy);
   if(slot==0){C(ui.SwapBindingForFixture(3,0),"valid binding swap");var moved=ui.MobileOpportunityArea(0);C(moved.X==MobileControls.Layout.Skills[3].X,"hint follows remapped physical slot");}
   var area=slot<10?ui.MobileOpportunityArea(slot):slot==10?MobileControls.Layout.CounterOpportunity:MobileControls.Layout.ComboOpportunity;
   if(area.Width==0||area.Height==0){C(slot==3||slot==8,"only hidden passive identities have no opportunity hit area");continue;}
   foreach(var skillBox in MobileControls.Layout.Skills)C(!area.Overlaps(skillBox),"hint interception never enlarges or replaces a skill button");
   var point=controls.Control(area);C(!MobileControls.IsScreenPointOverControls(point),"invisible hint preserves world hit area");
   if(slot<10)hero.OpportunityWindows[slot]=Window();else if(slot==10)hero.CounterWindow=Window();else hero.ComboWindow=Window();
   C(MobileControls.IsScreenPointOverControls(point),"visible hint is reported as UI control");
   float energy=hero.Energy,pitch=AdventureCamera.Pitch;int casts=hero.Casts;
   C(controls.ProcessPointer(81,TouchPhase.Began,point),"visible hint owns pointer");
   controls.ProcessPointer(81,TouchPhase.Moved,new Vector2(point.x+20,point.y));controls.ProcessPointer(81,TouchPhase.Ended,world);
   C(hero.MobilePinnedTarget==enemy&&hero.Casts==casts&&hero.Energy==energy&&!MobileControls.AttackHeld&&AdventureCamera.Pitch==pitch,"visible hint never changes pin or combat state");
   hero.targeting.BeginAimForTest();var aim=hero.targeting.TargetPoint;
   Tap(controls,82,point);C(hero.targeting.AimWrites==0&&hero.targeting.Confirms==0&&hero.targeting.TargetPoint.sqrMagnitude==aim.sqrMagnitude,"hint never updates or confirms live ground targeting");hero.targeting.Cancel();
   controls.ProcessPointer(83,TouchPhase.Began,point);if(slot<10)hero.OpportunityWindows[slot]=default;else if(slot==10)hero.CounterWindow=default;else hero.ComboWindow=default;
   C(!MobileControls.IsScreenPointOverControls(point),"expired hint restores world area immediately");controls.ProcessPointer(83,TouchPhase.Ended,point);C(hero.MobilePinnedTarget==enemy,"expiry cannot turn an owned hint pointer into world tap");
   Tap(controls,84,point);C(hero.MobilePinnedTarget==null,"new tap at expired hint follows original world path");
   if(slot<10)hero.OpportunityWindows[slot]=Window();else if(slot==10)hero.CounterWindow=Window();else hero.ComboWindow=Window();
   session.InputBlocked=true;C(!MobileControls.IsScreenPointOverControls(point)&&!controls.ProcessPointer(85,TouchPhase.Began,point),"pause suppresses hint input admission");session.InputBlocked=false;
   hero.PinMobileTarget(enemy);controls.ProcessPointer(86,TouchPhase.Began,point);hero.CombatEpoch++;controls.ProcessPointer(86,TouchPhase.Canceled,point);C(hero.Casts==casts&&!MobileControls.AttackHeld,"epoch and cancel cannot release a hint as action");
   Tap(controls,87,world);C(hero.MobilePinnedTarget==null,"nonhint world tap remains functional");
   var attack=controls.Control(MobileControls.Layout.Attack);controls.ProcessPointer(88,TouchPhase.Began,attack);C(MobileControls.AttackHeld,"original attack button still owns attack");controls.ProcessPointer(88,TouchPhase.Ended,attack);C(!MobileControls.AttackHeld,"original attack release unchanged");
   for(int button=0;button<MobileSkillPolicy.ButtonCount;button++)
   {
    int bound=ui.BoundForInputFixture(button);float beforeEnergy=hero.Energy;int beforeCasts=hero.Casts;
    var key=controls.Control(MobileControls.Layout.Skills[button]);controls.ProcessPointer(90+button,TouchPhase.Began,key);
    C(ui.CapturedSkillForFixture==bound,"actual GameUI captures the physical slot's current-page binding");
    C(hero.Energy==beforeEnergy&&hero.Casts==beforeCasts,"skill button capture never spends or emits before release");
    controls.ProcessPointer(90+button,TouchPhase.Canceled,key);
    C(ui.CapturedSkillForFixture==-1&&hero.Energy==beforeEnergy&&hero.Casts==beforeCasts,"actual skill cancellation releases capture without casting");
   }
   if(MobileControls.IsIPad)
   {
    // Check the formerly overlapping jump corner as well as its center. The
    // real GUI must not capture an adjacent skill before ProcessPointer jumps.
    var jump=actualLayout.Jump;
    foreach(var logical in new[]{new Vector2(jump.X+.5f,jump.Y+.5f),new Vector2(jump.X+jump.Width-.5f,jump.Y+.5f),new Vector2(jump.X+.5f,jump.Y+jump.Height-.5f),new Vector2(jump.X+jump.Width-.5f,jump.Y+jump.Height-.5f),new Vector2(jump.X+jump.Width/2,jump.Y+jump.Height/2)})
    {
     var jumpPoint=new Vector2(MobileControls.SafeArea.x+logical.x*actualLayout.Scale,MobileControls.SafeArea.yMax-logical.y*actualLayout.Scale);
     Tap(controls,110,jumpPoint);
     C(MobileControls.ConsumeJump()&&ui.CapturedSkillForFixture==-1,"actual iPad jump corners never capture adjacent skill");
    }
    ui.CompanionCommandsVisible=true;int commands=ui.CommandActivations;var potion=actualLayout.Potion;
    foreach(var logical in new[]{new Vector2(potion.X+.5f,potion.Y+.5f),new Vector2(potion.X+potion.Width-.5f,potion.Y+.5f),new Vector2(potion.X+.5f,potion.Y+potion.Height-.5f),new Vector2(potion.X+potion.Width-.5f,potion.Y+potion.Height-.5f),new Vector2(potion.X+potion.Width/2,potion.Y+potion.Height/2)})
    {
     var potionPoint=new Vector2(MobileControls.SafeArea.x+logical.x*actualLayout.Scale,MobileControls.SafeArea.yMax-logical.y*actualLayout.Scale);
     Tap(controls,111,potionPoint);
     C(MobileControls.ConsumePotion()&&ui.CommandActivations==commands,"actual iPad potion input never activates recall");
    }
    Tap(controls,112,controls.Control(actualLayout.RecallCommand));
    C(ui.CommandActivations==commands+1&&ui.LastCommandRecall&&!MobileControls.ConsumePotion()&&ui.CapturedSkillForFixture==-1,"actual iPad recall target owns input separately from potion and skills");
    Tap(controls,113,controls.Control(actualLayout.FocusCommand));
    C(ui.CommandActivations==commands+2&&!ui.LastCommandRecall,"actual iPad focus target remains separate from recall");
    ui.CompanionCommandsVisible=false;
   }
   int oldPage=ui.mobileSkillPage;Time.frameCount++;
   var pagePoint=controls.Control(MobileControls.Layout.SkillPage);Tap(controls,99,pagePoint);
   C(ui.mobileSkillPage==(oldPage+1)%MobileSkillPolicy.PageCount&&ui.CapturedSkillForFixture==-1,"actual page switch changes binding page without capture or cast");
  }
  return "PASS: "+count+" actual ProcessPointer/IsScreenPointOverControls/GameUI skill-capture assertions, phone logical/historical/corrected safe-area replays and 4 iPad native specifications x native/75%-render x 3 presets, hidden passive areas explicitly excluded; opportunity observations, lifecycle admission, command recipients and Unity delivery/rendering are explicit boundaries";
 }
}
