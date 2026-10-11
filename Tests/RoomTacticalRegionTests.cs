using System;
using Emberfall;
using UnityEngine;
public static class RoomTacticalRegionTests
{
    static int n;
    static void Check(bool condition,string message){n++;if(!condition)throw new Exception(message);}
    static float Distance(Vector3 a,Vector3 b){return CombatFx.Flat(a-b).sqrMagnitude;}
    static bool Contest(Vector3 enemy,float footprint,Vector3 point)
    {return RoomTacticalRegion.Contests(Distance(enemy,point),footprint,WorldTraversal.HasLineOfSight(enemy,point));}
    static bool Supported(Vector3 enemy,Vector3 supplier)
    {return RoomTacticalRegion.ReceivesSupport(Distance(enemy,supplier),WorldTraversal.HasLineOfSight(enemy,supplier));}
    public static string Run()
    {
        n=0;float r=RoomTacticalRegion.CaptureRadius;
        Check(r==2.4f,"tactical room keeps its authored 2.4m boundary");
        Check(ExpeditionModeState.HoldPointRadius==3.2f&&ExpeditionModeState.InsideHoldPoint(3.2f*3.2f),"ordinary trial remains a 3.2m circle");
        Check(RoomTacticalRegion.ContainsPlayer(r*r),"player on painted boundary is inside");
        Check(!RoomTacticalRegion.ContainsPlayer((r+.001f)*(r+.001f)),"player just outside cannot capture");
        foreach(float body in new[]{.45f,.6f,.9f,1.25f})
        {
            float edge=r+body;
            Check(RoomTacticalRegion.Contests((edge-.001f)*(edge-.001f),body,true),"footprint just inside always contests");
            Check(RoomTacticalRegion.Contests(edge*edge,body,true),"footprint touching counts at every production body size");
            Check(!RoomTacticalRegion.Contests((edge+.001f)*(edge+.001f),body,true),"knockback beyond footprint boundary stops contest immediately");
            Check(!RoomTacticalRegion.Contests(1,body,false),"wall-hidden footprint cannot contest");
        }
        foreach(float value in new[]{float.NaN,float.PositiveInfinity,-1})
        {
            Check(!RoomTacticalRegion.ContainsPlayer(value),"invalid player distance rejected");
            Check(!RoomTacticalRegion.Contests(value,.6f,true),"invalid enemy distance rejected");
            Check(!RoomTacticalRegion.Contests(1,value,true),"invalid footprint rejected");
        }
        WorldTraversal.Reset(ZoneKind.Dungeon);
        Vector3 target=Vector3.zero,near=new Vector3(2.9f,2,0),far=new Vector3(3.01f,0,0);
        Check(Contest(near,.6f,target),"height does not change the ground capture footprint");
        Check(!Contest(far,.6f,target),"old 3.8m center rule no longer freezes capture");
        WorldTraversal.AddBox(new Vector3(1.5f,0,0),new Vector2(.4f,3));
        Check(!Contest(near,.6f,target),"production wall LOS excludes enemy even with footprint overlap");
        WorldTraversal.Reset(ZoneKind.Dungeon);
        Check(Supported(new Vector3(5.9f,0,0),target),"5.9m visible ally actually receives support");
        Check(Supported(new Vector3(5.999f,0,0),target),"5.999m ally receives support");
        Check(!Supported(new Vector3(6.001f,0,0),target),"6.001m ally is severed");
        Check(Supported(new Vector3(6,0,0),target),"exact 6m boundary receives support");
        Check(!Supported(new Vector3(6.1f,0,0),target),"6.1m ally is severed while supplier survives");
        WorldTraversal.AddBox(new Vector3(3,0,0),new Vector2(.4f,3));
        Check(!Supported(new Vector3(5.9f,0,0),target),"5.9m behind wall is severed by actual traversal LOS");
        var run=new RoomChainState(0);for(int i=0;i<run.Room.EnemyCount;i++)run.Register(run.Room,i);
        run.Advance(.25f,true,true,false);float saved=run.Progress;
        run.Advance(.25f,true,true,true);Check(run.Progress==saved,"contest preserves capture progress");
        run.Advance(.25f,true,false,false);Check(run.Progress==saved,"leaving visible ring preserves progress");
        WorldTraversal.Reset(ZoneKind.Dungeon);
        run.Advance(.25f,true,true,Contest(far,.6f,target));Check(run.Progress==saved+.25f,"knockback out of footprint resumes from saved progress");
        var severed=new RoomSupportSnapshot(true,0,RoomTargetSupport.Severed);
        var view=RoomObjectivePresentation.Create(run,true,2,false,severed);
        Check(view.Hint.Contains("争夺2敌")&&view.Hint.Contains("双环"),"HUD identifies actual count and enemy mark");
        Check(view.SupportHint.Contains("供能存活")&&view.SupportHint.Contains("全断援成功"),"full LOS/range separation explicitly succeeds without lying that supplier died");
        Check(new RoomSupportSnapshot(true,2,RoomTargetSupport.Supported).Hint.Contains("目标受援"),"target receives actual protection");
        Check(new RoomSupportSnapshot(true,2,RoomTargetSupport.Severed).Hint.Contains("目标已断援"),"target can be severed while others remain supported");
        Check(new RoomSupportSnapshot(false,2,RoomTargetSupport.Severed).SupportedCount==0,"dead supplier cannot retain supported count");
        var hunt=new RoomChainState(1);for(int i=0;i<hunt.Room.EnemyCount;i++)hunt.Register(hunt.Room,i);
        RoomObjectivePresentation.Create(hunt,true,0,false,severed);
        hunt.Advance(.25f,true,true,false);Check(!hunt.DoorUnlocked,"tactical severing success never bypasses Hunt kill goal");
        hunt.Defeat(hunt.Room,0);Check(hunt.DoorUnlocked,"Hunt opens only on supplier defeat");
        return "PASS: "+n+" room region / actual traversal LOS / support / capture preservation checks (managed, not Unity play)";
    }
}
