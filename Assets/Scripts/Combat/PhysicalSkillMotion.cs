using UnityEngine;
namespace Emberfall
{
    // Shared by rocks, ice, metal and liquid droplets. Presentation never deals damage.
    internal static class PhysicalSkillMotion
    {
        internal const float Gravity=9.81f;
        private static readonly RaycastHit[] contacts=new RaycastHit[24];
        internal static Vector3 BallisticPosition(Vector3 from,Vector3 to,float time,float duration)
        {
            duration=Mathf.Max(.01f,duration);time=Mathf.Clamp(time,0,duration);
            Vector3 velocity=(to-from)/duration+Vector3.up*(Gravity*duration*.5f);
            return from+velocity*time-Vector3.up*(Gravity*time*time*.5f);
        }
        internal static Vector3 BallisticVelocity(Vector3 from,Vector3 to,float time,float duration)
        {duration=Mathf.Max(.01f,duration);return (to-from)/duration+Vector3.up*(Gravity*(duration*.5f-time));}
        // The normal impulse owns restitution; friction only removes tangential
        // momentum. Sliding drag is expressed per second, independent of the
        // number of collision substeps needed by a device's current frame rate.
        internal static void ResolveContact(ref Vector3 velocity,Vector3 normal,float restitution,float friction,float delta)
        {
            if(normal.sqrMagnitude<.000001f)return;
            normal=normal.normalized;float toward=Vector3.Dot(velocity,normal);
            if(toward>=0)return;
            restitution=Mathf.Clamp01(restitution);friction=Mathf.Clamp01(friction);
            Vector3 tangent=velocity-normal*toward;
            bool bounce=-toward>.55f;
            float retention=bounce?friction:(float)System.Math.Pow(friction,Mathf.Max(0,delta)/.016f);
            velocity=tangent*retention+normal*(bounce?-toward*restitution:0);
            if(velocity.sqrMagnitude<.025f)velocity=Vector3.zero;
        }
        internal static bool AdvanceSupported(ref Vector3 position,ref Vector3 velocity,ref int supportRevision,float radius,float delta,float restitution=.3f,float friction=.65f)
        {
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return false;
            // Sleep only on registered support. Removing a prop or changing the
            // terrain revision wakes its fragments before their next movement.
            if(supportRevision==WorldTraversal.Revision&&velocity.sqrMagnitude<.000001f)return true;
            supportRevision=-1;
            bool touched=Advance(ref position,ref velocity,radius,delta,restitution,friction);
            if(touched&&velocity.sqrMagnitude<.000001f&&
                Mathf.Abs(position.y-WorldTraversal.PhysicalSurfaceHeight(position,radius,position.y)-radius)<.008f)
                supportRevision=WorldTraversal.Revision;
            return touched;
        }
        internal static bool Advance(ref Vector3 position,ref Vector3 velocity,float radius,float delta,float restitution=.3f,float friction=.65f)
        {
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return false;
            radius=Mathf.Max(.02f,radius);bool touched=false;
            // Bound distance as well as time: fast fragments cannot tunnel through thin cover.
            int steps=Mathf.Max(1,Mathf.Max(Mathf.CeilToInt(delta/.016f),Mathf.CeilToInt(velocity.magnitude*delta/.08f)));
            steps=Mathf.Min(128,steps);float dt=delta/steps;
            for(int step=0;step<steps;step++)
            {
                Vector3 move=velocity*dt-Vector3.up*(Gravity*dt*dt*.5f);velocity.y-=Gravity*dt;
                Vector3 next=position+move;RaycastHit contact;
                if(SweepScenery(position,next,radius,out contact))
                {
                    next=position+move.normalized*Mathf.Max(0,contact.distance-.003f);
                    ResolveContact(ref velocity,contact.normal,restitution,friction,dt);touched=true;
                }
                // Registered world solids remain authoritative even where no PhysX collider exists.
                if(!WorldTraversal.IsPhysicalSpaceClear(next,radius)&&WorldTraversal.IsPhysicalSpaceClear(position,radius))
                {
                    float low=0,high=1;
                    for(int n=0;n<8;n++){float mid=(low+high)*.5f;if(WorldTraversal.IsPhysicalSpaceClear(Vector3.Lerp(position,next,mid),radius))low=mid;else high=mid;}
                    Vector3 point=Vector3.Lerp(position,next,low);
                    bool xBlocked=!WorldTraversal.IsPhysicalSpaceClear(new Vector3(next.x,point.y,point.z),radius);
                    bool zBlocked=!WorldTraversal.IsPhysicalSpaceClear(new Vector3(point.x,point.y,next.z),radius);
                    bool yBlocked=!WorldTraversal.IsPhysicalSpaceClear(new Vector3(point.x,next.y,point.z),radius);
                    Vector3 normal=new Vector3(xBlocked||(!zBlocked&&!yBlocked)?-move.x:0,yBlocked?-move.y:0,zBlocked||(!xBlocked&&!yBlocked)?-move.z:0).normalized;
                    ResolveContact(ref velocity,normal,restitution,friction,dt);
                    next=point;touched=true;
                }
                float floor=WorldTraversal.PhysicalSurfaceHeight(next,radius,position.y)+radius;
                if(next.y<=floor&&velocity.y<=0)
                {
                    next.y=floor;ResolveContact(ref velocity,Vector3.up,restitution,friction,dt);touched=true;
                }
                position=next;
            }
            return touched;
        }
        internal static bool SweepScenery(Vector3 from,Vector3 to,float radius,out RaycastHit closest)
        {
            closest=default;Vector3 move=to-from;if(move.sqrMagnitude<.0000001f)return false;
            int count=Physics.SphereCastNonAlloc(from,radius,move.normalized,contacts,move.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            float distance=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var hit=contacts[i];var collider=hit.collider;if(collider==null)continue;
                // Actor damage uses the combat sweep; debris must never push actors or props.
                if(collider.GetComponentInParent<PlayerController>()!=null||collider.GetComponentInParent<EnemyController>()!=null||collider.GetComponentInParent<SummonedCompanion>()!=null)continue;
                if(hit.distance<distance){distance=hit.distance;closest=hit;}
            }
            return distance<float.PositiveInfinity;
        }
        internal static bool ClearFlight(Vector3 from,Vector3 to,float radius,WorldTraversal.ObstacleHandle ignored=null)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt((to-from).magnitude/.06f));
            for(int i=1;i<=steps;i++)
            {
                Vector3 point=Vector3.Lerp(from,to,i/(float)steps);
                if(!WorldTraversal.IsPhysicalSpaceClear(point,radius,ignored)||point.y-radius<=WorldTraversal.SurfaceHeight(point,.02f))return false;
            }
            return true;
        }
        internal static bool BodyContact(Vector3 from,Vector3 to,Vector3 feet,float height,float bodyRadius,float projectileRadius,out float fraction)
        {
            // Closest points between the swept projectile segment and the vertical body capsule.
            float cap=Mathf.Min(bodyRadius,height*.45f);
            Vector3 a=feet+Vector3.up*cap,b=feet+Vector3.up*Mathf.Max(cap,height-cap);
            Vector3 d1=to-from,d2=b-a,r=from-a;float aa=Vector3.Dot(d1,d1),e=Vector3.Dot(d2,d2),f=Vector3.Dot(d2,r),s,t;
            if(aa<=.000001f){s=0;t=e<=.000001f?0:Mathf.Clamp01(f/e);}
            else
            {
                float c=Vector3.Dot(d1,r);
                if(e<=.000001f){t=0;s=Mathf.Clamp01(-c/aa);}
                else
                {
                    float bdot=Vector3.Dot(d1,d2),denom=aa*e-bdot*bdot;
                    s=denom>.000001f?Mathf.Clamp01((bdot*f-c*e)/denom):0;t=(bdot*s+f)/e;
                    if(t<0){t=0;s=Mathf.Clamp01(-c/aa);}else if(t>1){t=1;s=Mathf.Clamp01((bdot-c)/aa);}
                }
            }
            fraction=s;float extent=cap+projectileRadius;
            if(((from+d1*s)-(a+d2*t)).sqrMagnitude>extent*extent)return false;
            // Damage and debris start at first surface entry, not the point nearest the body axis.
            Vector3 begin=from-a;begin.y=from.y-Mathf.Clamp(from.y,a.y,b.y);
            if(begin.sqrMagnitude<=extent*extent){fraction=0;return true;}
            float low=0,high=s;
            for(int i=0;i<12;i++)
            {
                float mid=(low+high)*.5f;Vector3 point=from+d1*mid;
                Vector3 nearest=new Vector3(a.x,Mathf.Clamp(point.y,a.y,b.y),a.z);
                if((point-nearest).sqrMagnitude<=extent*extent)high=mid;else low=mid;
            }
            fraction=high;return true;
        }
        internal static bool BoxContact(Vector3 from,Vector3 to,Vector3 center,Vector3 size,float radius,out float fraction)
        {
            Vector3 half=size*.5f,move=to-from;float radiusSquared=radius*radius;
            fraction=0;if(BoxDistanceSquared(from,center,half)<=radiusSquared)return true;
            // Distance to a convex box is convex along the flight segment. Search
            // its minimum, then the first rounded surface entry, including corners.
            float low=0,high=1;
            for(int i=0;i<24;i++)
            {
                float a=low+(high-low)/3,b=high-(high-low)/3;
                if(BoxDistanceSquared(from+move*a,center,half)<BoxDistanceSquared(from+move*b,center,half))high=b;else low=a;
            }
            float nearest=(low+high)*.5f;
            if(BoxDistanceSquared(from+move*nearest,center,half)>radiusSquared&&BoxDistanceSquared(to,center,half)>radiusSquared)return false;
            low=0;high=BoxDistanceSquared(to,center,half)<BoxDistanceSquared(from+move*nearest,center,half)?1:nearest;
            for(int i=0;i<18;i++){float mid=(low+high)*.5f;if(BoxDistanceSquared(from+move*mid,center,half)<=radiusSquared)high=mid;else low=mid;}
            fraction=high;return true;
        }
        private static float BoxDistanceSquared(Vector3 point,Vector3 center,Vector3 half)
        {Vector3 d=point-center;float x=Mathf.Max(0,Mathf.Abs(d.x)-half.x),y=Mathf.Max(0,Mathf.Abs(d.y)-half.y),z=Mathf.Max(0,Mathf.Abs(d.z)-half.z);return x*x+y*y+z*z;}
    }
}
