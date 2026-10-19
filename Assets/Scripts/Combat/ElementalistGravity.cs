using UnityEngine;
namespace Emberfall
{
    // Detached gravity field, bounded to the originating hero and encounter.
    internal sealed class ElementalistGravity:MonoBehaviour
    {
        private PlayerController owner;private GameSession game;private int epoch,cast,rank;
        private bool press,lifted,landed;private float age,radius,duration,nextTick;
        private CombatDamage damage;private CastFirstHitReceipt receipt;
        private Material material;private Transform ball;private readonly LineRenderer[] winds=new LineRenderer[6];
        private readonly Vector3[] points=new Vector3[40];
        internal static void Spawn(PlayerController owner,GameSession game,int skill,int rank,Vector3 at,CombatDamage damage,int cast)
        {
            var obj=new GameObject(skill==6?"上升重力 · rolling pressure sphere":"重力开关 · lift and fall");
            at.y=WorldTraversal.SurfaceHeight(at,.06f);obj.transform.position=at;
            var field=obj.AddComponent<ElementalistGravity>();field.owner=owner;field.game=game;field.epoch=owner.CombatEpoch;
            field.cast=cast;field.rank=rank;field.damage=damage;field.press=skill==6;field.receipt=owner.RetainCastReceipt(cast);
            field.radius=field.press?ElementalistGeometry.PressRadius(rank):ElementalistGeometry.SwitchRadius(rank);
            field.duration=field.press?ElementalistGeometry.PressDuration(rank):2.3f;field.nextTick=.15f;
            field.material=CombatFx.NewGlow();field.material.color=new Color(.57f,.3f,1f,.6f);
            if(field.press)
            {
                var sphere=ProceduralVisuals.Create("Rolling gravity core",PrimitiveType.Sphere,field.material);
                field.ball=sphere.transform;field.ball.SetParent(obj.transform,false);
                field.ball.localPosition=Vector3.up*2.8f;field.ball.localScale=Vector3.one*field.radius*1.05f;
                for(int ring=0;ring<3;ring++)
                {
                    var band=new GameObject("Rolling core meridian").AddComponent<LineRenderer>();band.transform.SetParent(field.ball,false);
                    band.transform.localRotation=Quaternion.Euler(ring*60,ring*45,ring*30);band.sharedMaterial=field.material;
                    band.useWorldSpace=false;band.positionCount=49;band.widthMultiplier=.055f;
                    band.startColor=band.endColor=new Color(.8f,.7f,1f,1f);
                    for(int j=0;j<49;j++){float a=j*Mathf.PI*2/48;band.SetPosition(j,new Vector3(Mathf.Cos(a)*.51f,Mathf.Sin(a)*.51f,0));}
                    band.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;band.receiveShadows=false;
                }
                var renderer=sphere.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            for(int i=0;i<field.winds.Length;i++)
            {
                var line=new GameObject("Rising orbital air current").AddComponent<LineRenderer>();line.transform.SetParent(obj.transform,false);
                line.sharedMaterial=field.material;line.useWorldSpace=false;line.positionCount=field.points.Length;line.widthMultiplier=field.press?.09f:.07f;
                line.startColor=new Color(.4f,.65f,1f,.3f);line.endColor=new Color(.85f,.65f,1f,.85f);
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;field.winds[i]=line;
            }
        }
        private bool Current=>owner!=null&&game!=null&&game.Player==owner&&!owner.IsDead&&game.HasStarted&&!game.CombatEffectsEnded&&owner.CombatEpoch==epoch&&owner.HeroClass==HeroClass.Arcanist;
        private void Update()
        {
            if(!Current){Destroy(gameObject);return;}if(game.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=duration){Destroy(gameObject);return;}
            if(ball!=null)ball.localRotation=Quaternion.Euler(age*77f,age*53f,age*31f);
            float fade=Mathf.Min(Mathf.Clamp01(age/.2f),Mathf.Clamp01((duration-age)/.3f));
            material.color=new Color(.57f,.3f,1f,.48f*fade);
            for(int i=0;i<winds.Length;i++)
            {
                for(int j=0;j<points.Length;j++)
                {
                    float t=j/(float)(points.Length-1);float angle=i*Mathf.PI/3+t*Mathf.PI*2+age*2.8f;
                    float r=press?radius*(.7f+.25f*Mathf.Sin(t*Mathf.PI)):radius*(.85f+.1f*Mathf.Sin(t*6));
                    float rise=Mathf.Repeat(t+age*.55f,1f);
                    points[j]=new Vector3(Mathf.Cos(angle)*r,.15f+rise*(press?5.6f:3.5f),Mathf.Sin(angle)*r);
                }
                winds[i].SetPositions(points);
            }
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
                // 3.2m launch uses the same 9.81m/s² ballistic motion as monster impacts.
                if(!landed&&age>=1.8f)
                {landed=true;owner.HitArea(transform.position,radius,damage*ElementalistGeometry.SwitchHit(rank),castId:cast,strictRadius:true);CombatFx.Ring(transform.position,radius,new Color(.7f,.5f,1f),.4f,.18f);}
            }
        }
        private void OnDisable(){receipt?.Release();receipt=null;}
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
