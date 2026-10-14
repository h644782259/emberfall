using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        private bool PlayerBodyPositionClear(Vector3 feet)
        {
            if(!WorldTraversal.IsActorCapsuleClear(feet,ActorBodyCollision.PlayerRadius,ActorBodyCollision.PlayerHeight))return false;
            foreach(var enemy in Enemies)
            {
                if(enemy==null||enemy.IsDead||!enemy.gameObject.activeInHierarchy)continue;
                if(!ActorBodyCollision.IsClear(feet,ActorBodyCollision.PlayerRadius,ActorBodyCollision.PlayerHeight,
                    enemy.transform.position,enemy.NavigationRadius,enemy.PhysicalBodyHeight))return false;
            }
            return true;
        }
        private bool PlayerBodyStepClear(Vector3 from,Vector3 to)
        {
            if(!WorldTraversal.IsActorCapsuleStepClear(from,to,ActorBodyCollision.PlayerRadius,ActorBodyCollision.PlayerHeight))return false;
            foreach(var enemy in Enemies)
            {
                if(enemy==null||enemy.IsDead||!enemy.gameObject.activeInHierarchy)continue;
                if(!ActorBodyCollision.PermitsStep(from,to,ActorBodyCollision.PlayerRadius,ActorBodyCollision.PlayerHeight,
                    enemy.transform.position,enemy.NavigationRadius,enemy.PhysicalBodyHeight))return false;
            }
            return true;
        }
        private Vector3 ClipPlayerBodyMotion(Vector3 from,Vector3 desired)
        {
            int count=Mathf.Clamp(Mathf.CeilToInt((desired-from).magnitude/.08f),1,256);
            Vector3 position=from;
            Vector3 step=(desired-from)/count;
            bool horizontalBlocked=false;
            for(int i=1;i<=count;i++)
            {
                Vector3 next=position+step;
                if(horizontalBlocked){next.x=position.x;next.z=position.z;}
                if(!PlayerBodyStepClear(position,next))
                {
                    // A wall blocks horizontal travel, but must not stop gravity
                    // or the vertical part of a jump. Keep sweeping vertically
                    // at the last clear XZ; never resume beyond the blocked body.
                    Vector3 vertical=new Vector3(position.x,next.y,position.z);
                    if(PlayerBodyStepClear(position,vertical)){horizontalBlocked=true;next=vertical;}
                    else
                    {
                        // A body below the feet may support the actor for this
                        // step. Allow horizontal escape before gravity resumes.
                        next.y=position.y;
                        if(horizontalBlocked||!PlayerBodyStepClear(position,next))break;
                    }
                }
                position=next;
            }
            return position;
        }
        internal Vector3 ClipPlayerAirMotion(Vector3 from,Vector3 desired)
        {return ClipPlayerBodyMotion(from,desired);}
        internal Vector3 MovePlayerWithBodies(Vector3 from,Vector3 delta,float dt,ref float fallSpeed,float radius=.45f)
        {using(PerformanceDiagnostics.Measure(PerformanceDiagnostics.Section.ActorSlide))return MovePlayerWithBodiesMeasured(from,delta,dt,ref fallSpeed,radius);}
        private Vector3 MovePlayerWithBodiesMeasured(Vector3 from,Vector3 delta,float dt,ref float fallSpeed,float radius)
        {
            float initialFall=fallSpeed;
            Vector3 intended=WorldTraversal.MovePlayer(from,delta,dt,ref fallSpeed,radius);
            Vector3 result=ClipPlayerBodyMotion(from,intended);
            if((result-intended).sqrMagnitude<.000001f)return result;
            // Try the two wall tangents through the original ground rules. These
            // retries start from the same gravity state, rather than applying a
            // second fall step or letting a slide enter water or a raised wall.
            float xFall=initialFall,zFall=initialFall;
            Vector3 x=ClipPlayerBodyMotion(from,WorldTraversal.MovePlayer(from,new Vector3(delta.x,0,0),dt,ref xFall,radius));
            Vector3 z=ClipPlayerBodyMotion(from,WorldTraversal.MovePlayer(from,new Vector3(0,0,delta.z),dt,ref zFall,radius));
            if((x-from).sqrMagnitude>(result-from).sqrMagnitude){result=x;fallSpeed=xFall;}
            if((z-from).sqrMagnitude>(result-from).sqrMagnitude){result=z;fallSpeed=zFall;}
            // Axis-only retries cannot slide past an actor directly ahead.
            if(CombatFx.Flat(result-from).sqrMagnitude<CombatFx.Flat(delta).sqrMagnitude*.25f)
            {
                for(int angle=30;angle<=75;angle+=15)
                {
                    for(int side=-1;side<=1;side+=2)
                    {
                        float slideFall=initialFall;
                        Vector3 slide=Quaternion.Euler(0,angle*side,0)*delta;
                        Vector3 candidate=ClipPlayerBodyMotion(from,WorldTraversal.MovePlayer(from,slide,dt,ref slideFall,radius));
                        if(Vector3.Dot(CombatFx.Flat(candidate-from),CombatFx.Flat(delta))>
                           Vector3.Dot(CombatFx.Flat(result-from),CombatFx.Flat(delta))+.000001f)
                        {result=candidate;fallSpeed=slideFall;}
                    }
                }
            }
            if(fallSpeed>0&&result.y>intended.y+.001f)fallSpeed=0;
            return result;
        }
        internal Vector3 ResolvePlayerSkillLanding(Vector3 from,Vector3 direction,float distance,float radius,float bound)
        {return ResolvePlayerLanding(from,WorldTraversal.ResolveSkillLanding(from,direction,distance,radius,bound));}
        internal Vector3 ResolvePlayerLanding(Vector3 from,Vector3 desired)
        {
            if(PlayerBodyPositionClear(desired))return desired;
            float length=CombatFx.Flat(desired-from).magnitude;
            int count=Mathf.Clamp(Mathf.CeilToInt(length/.08f),1,256);
            for(int i=count-1;i>0;i--)
            {
                Vector3 point=Vector3.Lerp(from,desired,i/(float)count);point.y=WorldTraversal.SurfaceHeight(point);
                if(WorldTraversal.CanStand(point)&&PlayerBodyPositionClear(point))return point;
            }
            return from;
        }
        internal Vector3 ResolveEnemyBodyMove(Vector3 from,Vector3 desired,EnemyController enemy)
        {
            if(enemy==null||(desired-from).sqrMagnitude==0)return from;
            int count=Mathf.Clamp(Mathf.CeilToInt((desired-from).magnitude/.08f),1,256);
            Vector3 position=from;
            for(int i=1;i<=count;i++)
            {
                Vector3 next=Vector3.Lerp(from,desired,i/(float)count);
                if(!WorldTraversal.IsActorCapsuleStepClear(position,next,enemy.NavigationRadius,Mathf.Max(enemy.NavigationRadius*2,enemy.PhysicalBodyHeight)))break;
                if(Player!=null&&!Player.IsDead&&!ActorBodyCollision.PermitsStep(position,next,enemy.NavigationRadius,enemy.PhysicalBodyHeight,
                    Player.transform.position,ActorBodyCollision.PlayerRadius,ActorBodyCollision.PlayerHeight))break;
                position=next;
            }
            return position;
        }
    }
}
