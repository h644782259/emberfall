using UnityEngine;
namespace Emberfall
{
    internal sealed class MeteorShower:MonoBehaviour
    {
        private PlayerController owner;private GameSession game;private int epoch,rank,cast,emitted;
        private Vector3 origin,forward,side;private float age,next;private CombatDamage damage;private CastFirstHitReceipt receipt;
        internal static void Spawn(PlayerController owner,GameSession game,int rank,CombatDamage damage,int cast)
        {
            var rain=new GameObject("流星雨 · front and rear rectangles").AddComponent<MeteorShower>();
            rain.owner=owner;rain.game=game;rain.rank=rank;rain.epoch=owner.CombatEpoch;rain.cast=cast;
            rain.origin=owner.transform.position;rain.forward=CombatFx.Flat(owner.transform.forward).normalized;rain.side=Vector3.Cross(Vector3.up,rain.forward);
            rain.damage=damage*MeteorShowerRules.Coefficient(rank);rain.receipt=owner.RetainCastReceipt(cast);
        }
        private void Update()
        {
            if(owner==null||game==null||game.Player!=owner||owner.IsDead||!game.HasStarted||game.CombatEffectsEnded||owner.CombatEpoch!=epoch||owner.HeroClass!=HeroClass.Arcanist){Destroy(gameObject);return;}
            if(game.InputBlocked||Time.deltaTime<=0)return;age+=Time.deltaTime;
            if(emitted>=MeteorShowerRules.Count(rank)){Destroy(gameObject);return;}
            if(age<next)return;next=age+MeteorShowerRules.Interval;
            bool rear=emitted%5==4;
            float radius=MeteorShowerRules.Radius(rank,rear),half=MeteorShowerRules.HalfWidth(rank,rear);
            float lateral=(Mathf.Repeat(emitted*.618034f+.25f,1)*2-1)*(half-radius);
            float depth=rear?-Mathf.Lerp(radius,MeteorShowerRules.Rear(rank)-radius,Mathf.Repeat(emitted*.37f,1)):
                Mathf.Lerp(radius,MeteorShowerRules.Front(rank)-radius,Mathf.Repeat(emitted*.381966f+.12f,1));
            // Half of the impacts follow monsters inside the footprint; the others sweep the whole field.
            if(emitted%2==0&&game.Enemies.Count>0)
            {
                for(int i=0;i<game.Enemies.Count;i++)
                {
                    var enemy=game.Enemies[(i+emitted/2)%game.Enemies.Count];if(enemy==null||enemy.IsDead)continue;
                    Vector3 offset=CombatFx.Flat(enemy.transform.position-origin);float z=Vector3.Dot(offset,forward),x=Vector3.Dot(offset,side);
                    if(!MeteorShowerRules.Contains(z,x,rank)||!CombatSight.Area(origin,enemy.transform.position))continue;
                    rear=z<0;radius=MeteorShowerRules.Radius(rank,rear);half=MeteorShowerRules.HalfWidth(rank,rear);
                    lateral=Mathf.Clamp(x,-half+radius,half-radius);depth=rear?Mathf.Clamp(z,-MeteorShowerRules.Rear(rank)+radius,-radius):Mathf.Clamp(z,radius,MeteorShowerRules.Front(rank)-radius);break;
                }
            }
            Vector3 target=origin+forward*depth+side*lateral;
            // Terrain rejects occluded landings rather than relocating impacts outside the advertised field.
            if(CombatSight.Area(origin,target))
                CombatArea.Spawn(owner,game,target,radius,damage,0,MeteorShowerRules.FallTime,0,1,new Color(1f,.48f,.19f),fallingMeteor:true,castId:cast,visual:SkillVisualRecipe.Fire);
            emitted++;
        }
        private void OnDisable(){receipt?.Release();receipt=null;}
    }
}
