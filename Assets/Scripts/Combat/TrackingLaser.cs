using UnityEngine;
namespace Emberfall
{
    // Detached cast: movement/other skills do not cancel it; death or room/owner changes do.
    internal sealed class TrackingLaser : MonoBehaviour
    {
        private PlayerController owner;private GameSession game;private EnemyController target;
        private int epoch,castId;private float age,duration,radius,scan,nextTick;private CombatDamage damage;
        private CastFirstHitReceipt receipt;private Material material;
        private readonly WorldTraversal.Route route=new WorldTraversal.Route();
        internal static void Spawn(PlayerController player,GameSession session,Vector3 at,int rank,CombatDamage tick,int id)
        {
            var root=new GameObject("巡星天柱 · tracking laser");
            root.transform.position=WorldTraversal.NearestWalkable(CombatFx.Flat(at),.2f);
            var beam=root.AddComponent<TrackingLaser>();beam.owner=player;beam.game=session;beam.epoch=player.CombatEpoch;beam.castId=id;
            beam.duration=5+Mathf.Clamp(rank,1,3);beam.radius=(rank==3?3.2f:3f)*GameBalance.SkillRangeMultiplier(rank);
            beam.damage=tick.WithoutCritical();beam.receipt=player.RetainCastReceipt(id);beam.nextTick=.25f;
            beam.material=new Material(Resources.Load<Shader>("TrackingLaser"));
            beam.material.SetColor("_Tint",new Color(.25f,.8f,1f,1));
            for(int i=0;i<3;i++)
            {
                float width=beam.radius*2*(i==0?1f:i==1?.5f:.18f);
                var part=ProceduralVisuals.Create("Rising laser layer",PrimitiveType.Cylinder,beam.material);part.transform.SetParent(root.transform,false);
                part.transform.localPosition=new Vector3(0,7,0);part.transform.localScale=new Vector3(width,7,width);
                var renderer=part.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            var baseLight=ProceduralVisuals.Create("Laser ground footprint",PrimitiveType.Cylinder,beam.material);baseLight.transform.SetParent(root.transform,false);
            baseLight.transform.localPosition=new Vector3(0,.08f,0);baseLight.transform.localScale=new Vector3(beam.radius*2,.04f,beam.radius*2);
            var baseRenderer=baseLight.GetComponent<MeshRenderer>();baseRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;baseRenderer.receiveShadows=false;
            GameAudio.Play(SoundCue.Cast);
        }
        private bool Valid(EnemyController enemy)
        {return enemy!=null&&!enemy.IsDead&&enemy.isActiveAndEnabled&&game.Enemies.Contains(enemy)&&CombatFx.Flat(enemy.transform.position-owner.transform.position).sqrMagnitude<=900;}
        private void Update()
        {
            if(owner==null||game==null||game.Player!=owner||owner.IsDead||!game.HasStarted||game.CombatEffectsEnded||owner.CombatEpoch!=epoch||owner.HeroClass!=HeroClass.Arcanist)
            {Destroy(gameObject);return;}
            if(game.InputBlocked||Time.deltaTime<=0)return;
            float dt=Time.deltaTime;age+=dt;
            if(age>=duration){Destroy(gameObject);return;}
            scan-=dt;
            if(scan<=0||(target!=null&&!Valid(target)))
            {
                scan=.2f;
                if(!Valid(target))target=null;
                if(target==null)
                {
                    float nearest=18*18;
                    foreach(var enemy in game.Enemies)
                    {
                        if(!Valid(enemy))continue;float d=CombatFx.Flat(enemy.transform.position-transform.position).sqrMagnitude;
                        if(d<nearest&&WorldTraversal.CanReach(transform.position,enemy.transform.position,.2f)){target=enemy;nearest=d;}
                    }
                }
            }
            if(target!=null)
            {
                Vector3 delta=CombatFx.Flat(target.transform.position-transform.position);
                Vector3 direction=route.Direction(transform.position,target.transform.position,.2f);
                transform.position=WorldTraversal.Move(transform.position,direction*Mathf.Min(delta.magnitude,4.5f*dt),.2f);
            }
            Vector3 at=transform.position;at.y=WorldTraversal.SurfaceHeight(at,.04f);transform.position=at;
            material.SetFloat("_Phase",age);material.SetFloat("_Fade",Mathf.Min(Mathf.Clamp01(age/.2f),Mathf.Clamp01((duration-age)/.4f)));
            // At most one damage pulse per frame: a stalled frame must not burst all overdue ticks.
            if(age>=nextTick){nextTick=age+.5f;owner.HitArea(transform.position,radius,damage,castId:castId,strictRadius:true);}
        }
        private void OnDisable(){receipt?.Release();receipt=null;}
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
