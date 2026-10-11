using System;
using Emberfall;
using UnityEngine;
// Host/Unity object substitutes; queried session methods and traversal are extracted production code.
namespace Emberfall
{
    public sealed class EnemyController
    {
        public bool IsDead,IsBoss;
        public bool isActiveAndEnabled=true;
        public GameObject gameObject=new GameObject();
        public Transform transform=new Transform();
        public float NavigationRadius=.6f;
    }
    public partial class GameSession
    {
        // This suite executes ordinary room queries only; practice has its own host suite.
        public bool PracticeActive=>false;
        public float PracticeSupportMultiplier(EnemyController enemy){throw new Exception("practice outside room LOS fixture scope");}
        public bool RoomCaptureActive=true;
        bool RoomPurifyLive=false;FakeRoom RoomChainRun=new FakeRoom();
        class FakeRoom {public bool SealComplete(int i)=>false;}
        bool RoomSealInside(int i)=>false;Vector3 RoomSealPoint(int i)=>Vector3.zero;
        bool IsRoomContestingPoint(EnemyController e,Vector3 p)=>false;
        public Vector3 RoomObjectivePoint=Vector3.zero;
        public EnemyController roomSupplier=new EnemyController();
        public bool RoomSupplyActive {get{return LiveRoomEnemy(roomSupplier);}}
    }
}
namespace UnityEngine
{
    public sealed class Transform {public Vector3 position;}
    public sealed class GameObject {public bool activeInHierarchy=true;}
}
public static class RoomTacticalLosProductionTests
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static EnemyController At(float x){var e=new EnemyController();e.transform.position=new Vector3(x,0,0);return e;}
    static void Reset(){WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.TestLineOfSightCalls=0;}
    public static string Run()
    {
        var session=new GameSession();checks=0;Reset();
        for(int i=0;i<8;i++)Check(!session.IsRoomContesting(At(8+i)),"far enemies cannot contest");
        Check(WorldTraversal.TestLineOfSightCalls==0,"far contest candidates must skip LOS");
        for(int i=0;i<8;i++)Check(session.RoomSupportMultiplier(At(8+i))==1,"far enemies are unsupported");
        Check(WorldTraversal.TestLineOfSightCalls==0,"far support candidates must skip LOS");
        foreach(float radius in new[]{.45f,.6f,.9f,1.25f})
        foreach(float offset in new[]{-.001f,0,.001f})
        {
            Reset();var enemy=At(2.4f+radius+offset);enemy.NavigationRadius=radius;
            Check(session.IsRoomContesting(enemy)==(offset<=0),"exact footprint overlap decides contest");
            Check(WorldTraversal.TestLineOfSightCalls==(offset<=0?1:0),"only overlapping footprints query LOS");
        }
        foreach(float distance in new[]{5.999f,6f,6.001f})
        {
            Reset();var enemy=At(distance);
            Check(session.RoomSupportMultiplier(enemy)==(distance<=6?.7f:1),"exact six metre support boundary");
            Check(WorldTraversal.TestLineOfSightCalls==(distance<=6?1:0),"only support range queries LOS");
        }
        Reset();var near=At(2);
        Check(session.IsRoomContesting(near)&&session.RoomSupportMultiplier(near)==.7f,"clear near enemy contests and receives support");
        var wall=WorldTraversal.AddDynamicBox(new Vector3(1,0,0),new Vector2(.4f,3));
        Check(!session.IsRoomContesting(near)&&session.RoomSupportMultiplier(near)==1,"same frame wall immediately blocks capture and damage protection");
        WorldTraversal.RemoveDynamicObstacle(wall);
        Check(session.IsRoomContesting(near)&&session.RoomSupportMultiplier(near)==.7f,"same frame removed wall immediately restores both");
        near.transform.position=new Vector3(7,0,0);
        Check(!session.IsRoomContesting(near)&&session.RoomSupportMultiplier(near)==1,"same frame displacement immediately breaks both");
        Check(WorldTraversal.TestLineOfSightCalls==6,"each eligible repeated query uses current traversal");
        Reset();near=At(1);near.IsDead=true;
        Check(!session.IsRoomContesting(near)&&session.RoomSupportMultiplier(near)==1,"dead enemy excluded");
        near.IsDead=false;near.isActiveAndEnabled=false;
        Check(!session.IsRoomContesting(near)&&session.RoomSupportMultiplier(near)==1,"disabled enemy excluded");
        near.isActiveAndEnabled=true;near.gameObject.activeInHierarchy=false;
        Check(!session.IsRoomContesting(near)&&session.RoomSupportMultiplier(near)==1,"inactive enemy excluded");
        near.gameObject.activeInHierarchy=true;near.IsBoss=true;
        Check(session.RoomSupportMultiplier(near)==1&&session.RoomSupportMultiplier(session.roomSupplier)==1,"boss and supplier receive no support");
        session.RoomCaptureActive=false;near.IsBoss=false;
        Check(!session.IsRoomContesting(near),"inactive capture excluded");
        session.roomSupplier.IsDead=true;
        Check(session.RoomSupportMultiplier(near)==1,"dead supplier excluded");
        Check(WorldTraversal.TestLineOfSightCalls==0,"ineligible enemies require no LOS");
        session.RoomCaptureActive=true;session.roomSupplier.IsDead=false;
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity})
        {
            near=At(invalid);
            Check(!session.IsRoomContesting(near)&&session.RoomSupportMultiplier(near)==1,"invalid positions reject");
        }
        near=At(1);near.NavigationRadius=-1;
        Check(!session.IsRoomContesting(near),"invalid footprint rejects");
        Check(WorldTraversal.TestLineOfSightCalls==0,"invalid geometry requires no LOS");
        return "PASS: "+checks+" actual session method / traversal checks (managed, not Unity runtime)";
    }
}
