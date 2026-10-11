using UnityEngine;
namespace Emberfall
{
    public static class ActorBodyCollision
    {
        public const float PlayerRadius=.45f,PlayerHeight=1.8f;
        public static bool IsClear(Vector3 feet,float radius,float height,Vector3 body,float bodyRadius,float bodyHeight)
        {
            if(feet.y>=body.y+bodyHeight-.015f||feet.y+height<=body.y+.015f)return true;
            float sum=radius+bodyRadius-.01f,dx=feet.x-body.x,dz=feet.z-body.z;
            return dx*dx+dz*dz>=sum*sum;
        }
        // A body already overlapping may leave the overlap; it cannot move
        // deeper through the centre. Vertical separation allows a real jump over
        // a short creature and avoids blocking actors on different platforms.
        public static bool PermitsStep(Vector3 from,Vector3 to,float radius,float height,Vector3 body,float bodyRadius,float bodyHeight)
        {
            if(to.y>=body.y+bodyHeight-.015f||to.y+height<=body.y+.015f)return true;
            float sum=radius+bodyRadius-.01f;
            float dx=to.x-body.x,dz=to.z-body.z,distance=dx*dx+dz*dz;
            if(distance>=sum*sum)return true;
            if(from.y>=body.y+bodyHeight-.015f||from.y+height<=body.y+.015f)return false;
            float oldX=from.x-body.x,oldZ=from.z-body.z;
            return distance>=oldX*oldX+oldZ*oldZ-.000001f;
        }
    }
}
