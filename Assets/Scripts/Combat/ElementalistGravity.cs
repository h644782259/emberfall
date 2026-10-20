using UnityEngine;
namespace Emberfall
{
    // Detached gravity field, bounded to the originating hero and encounter.
    internal sealed class ElementalistGravity:MonoBehaviour
    {
        private PlayerController owner;private GameSession game;private int epoch,cast,rank;
        private bool press,lifted,suspended,landed;private float age,radius,duration,nextTick;
        private CombatDamage damage;private CastFirstHitReceipt receipt;
        private ElementalistSpellVisual visual;
        internal static void Spawn(PlayerController owner,GameSession game,int skill,int rank,Vector3 at,CombatDamage damage,int cast)
        {
            var obj=new GameObject(skill==6?"上升重力 · rolling pressure sphere":"重力开关 · lift and fall");
            at.y=WorldTraversal.SurfaceHeight(at,.06f);obj.transform.position=at;
            var field=obj.AddComponent<ElementalistGravity>();field.owner=owner;field.game=game;field.epoch=owner.CombatEpoch;
            field.cast=cast;field.rank=rank;field.damage=damage;field.press=skill==6;field.receipt=owner.RetainCastReceipt(cast);
            field.radius=field.press?ElementalistGeometry.PressRadius(rank):ElementalistGeometry.SwitchRadius(rank);
            field.duration=field.press?ElementalistGeometry.PressDuration(rank):2.3f;field.nextTick=.15f;
            field.visual=new ElementalistSpellVisual(obj.transform,field.press?ElementalistSpellVisual.Kind.Pressure:ElementalistSpellVisual.Kind.Lift,field.radius);
        }

        private bool Current=>owner!=null&&game!=null&&game.Player==owner&&!owner.IsDead&&game.HasStarted&&!game.CombatEffectsEnded&&owner.CombatEpoch==epoch&&owner.HeroClass==HeroClass.Arcanist;
        private void Update()
        {
            if(!Current){Destroy(gameObject);return;}if(game.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=duration){Destroy(gameObject);return;}
            float fade=Mathf.Min(Mathf.Clamp01(age/.2f),Mathf.Clamp01((duration-age)/.3f));
            visual?.Draw(age,fade);
            if(press)
            {
                // A short renewed lease releases immediately after field expiry/room change.
                foreach(var enemy in game.Enemies)
                    if(enemy!=null&&!enemy.IsDead&&CombatFx.Flat(enemy.transform.position-transform.position).sqrMagnitude<=radius*radius&&CombatSight.Area(transform.position,enemy.transform.position))
                    {enemy.Provoke();enemy.StatusEffects?.Suppress(.3f);}
                if(age>=nextTick){nextTick=age+.5f;owner.HitArea(transform.position,radius,damage.WithoutCritical()*ElementalistGeometry.PressTick(rank),castId:cast,strictRadius:true);}
            }
            else
            {
                if(!lifted&&age>=.15f)
                {
                    lifted=true;owner.HitArea(transform.position,radius,damage*ElementalistGeometry.SwitchHit(rank),castId:cast,strictRadius:true);
                    foreach(var enemy in game.Enemies)
                        if(enemy!=null&&!enemy.IsDead&&CombatFx.Flat(enemy.transform.position-transform.position).sqrMagnitude<=radius*radius&&CombatSight.Area(transform.position,enemy.transform.position))enemy.StatusEffects?.Knockup(1.2f,3.2f,true);
                }
                if(!suspended&&age>=.95f)
                {suspended=true;owner.HitArea(transform.position,radius,damage*ElementalistGeometry.SwitchHit(rank),castId:cast,strictRadius:true);}
                // 3.2m launch uses the same 9.81m/s² ballistic motion as monster impacts.
                if(!landed&&age>=1.8f)
                {landed=true;owner.HitArea(transform.position,radius,damage*ElementalistGeometry.SwitchHit(rank),castId:cast,strictRadius:true);CombatFx.Ring(transform.position,radius,new Color(.7f,.5f,1f),.4f,.18f);}
            }
        }
        private void OnDisable(){receipt?.Release();receipt=null;}
        private void OnDestroy(){visual?.Dispose();}
    }
}
