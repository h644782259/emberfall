using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // One travelling front, one hit per enemy. Rendering never owns damage lifetime.
    internal sealed class VanguardSwordWave : MonoBehaviour
    {
        private static Mesh mesh;
        private static Material material;
        private readonly HashSet<EnemyController> hit=new HashSet<EnemyController>();
        private PlayerController owner;private GameSession session;private int epoch,castId,rank;
        private Vector3 forward;private float traveled,width,range,age;
        private CombatDamage damage;private CastFirstHitReceipt receipt;
        private Renderer surface;private MaterialPropertyBlock block;
        internal static float DamageCoefficient(int rank)=>3f*(1f+(Mathf.Clamp(rank,1,3)-1)*.3f);
        internal static float Range(int rank)=>9f*GameBalance.SkillRangeMultiplier(rank);
        internal static float HalfWidth(int rank)=>3.4f*GameBalance.SkillRangeMultiplier(rank);
        internal static void Prewarm()
        {
            if(mesh!=null)return;
            const int columns=64,rows=20;
            var vertices=new Vector3[(columns+1)*(rows+1)];var uv=new Vector2[vertices.Length];
            var triangles=new int[columns*rows*6];int cursor=0;
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float u=x/(float)columns,v=y/(float)rows,s=u*2-1,curve=1-s*s;
                int i=y*(columns+1)+x;
                // A single bowed crest curls over a long wake. Height and depth
                // give the front a readable silhouette at the overhead camera.
                float roll=Mathf.Sin(v*Mathf.PI);
                vertices[i]=new Vector3(s*(1-.12f*v),.12f+curve*(.40f+2.9f*roll*Mathf.Pow(1-v,.25f)),
                    curve*.6f-v*4.2f+roll*.9f);
                uv[i]=new Vector2(u,v);
                if(x==columns||y==rows)continue;
                triangles[cursor++]=i;triangles[cursor++]=i+columns+1;triangles[cursor++]=i+1;
                triangles[cursor++]=i+1;triangles[cursor++]=i+columns+1;triangles[cursor++]=i+columns+2;
            }
            mesh=new Mesh{name="Continuous rolling sword wave"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();
            var bounds=mesh.bounds;bounds.Expand(.8f);mesh.bounds=bounds;
            material=new Material(Resources.Load<Shader>("SwordWave")){name="Gold blade with blue flowing wake"};
        }
        internal static void Spawn(PlayerController hero,GameSession game,int rank,CombatDamage damage,int cast)
        {
            Prewarm();var wave=new GameObject("Travelling sword energy").AddComponent<VanguardSwordWave>();
            wave.owner=hero;wave.session=game;wave.epoch=hero.CombatEpoch;wave.castId=cast;wave.rank=rank;wave.damage=damage;
            wave.receipt=hero.RetainCastReceipt(cast);wave.forward=CombatFx.Flat(hero.transform.forward).normalized;
            wave.width=HalfWidth(rank);wave.range=Range(rank);
            wave.transform.SetPositionAndRotation(hero.transform.position,Quaternion.LookRotation(wave.forward));
            wave.transform.localScale=new Vector3(wave.width,1,1);
            wave.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;wave.surface=wave.gameObject.AddComponent<MeshRenderer>();
            wave.surface.sharedMaterial=material;MobileSceneDepth.Track(wave.surface);wave.surface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;wave.surface.receiveShadows=false;
            wave.block=new MaterialPropertyBlock();wave.Draw();
        }
        private void Update()
        {
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||session==null||session.Player!=owner||session.CombatEffectsEnded){Destroy(gameObject);return;}
            if(session.InputBlocked||Time.deltaTime<=0)return;
            float step=Mathf.Min(7f*Time.deltaTime,range-traveled);
            Vector3 previous=transform.position,desired=previous+forward*step;
            Vector3 next=CombatSight.GroundPoint(previous,desired);
            bool blocked=CombatFx.Flat(next-desired).sqrMagnitude>.0025f;
            transform.position=next;traveled+=CombatFx.Flat(next-previous).magnitude;age+=Time.deltaTime;
            using(var targets=CombatTargetSnapshot<EnemyController>.Capture(session.Enemies))
            for(int i=0;i<targets.Count;i++)
            {
                var enemy=targets[i];if(enemy==null||enemy.IsDead||hit.Contains(enemy))continue;
                Vector3 offset=CombatFx.Flat(enemy.transform.position-previous);
                float along=Vector3.Dot(offset,forward),advance=CombatFx.Flat(next-previous).magnitude;
                float side=Vector3.Dot(offset,Vector3.Cross(Vector3.up,forward));
                float padding=enemy.NavigationRadius;
                if(along<-.35f-padding||along>advance+.6f+padding||Mathf.Abs(side)>width+padding)continue;
                Vector3 contact=previous+forward*Mathf.Clamp(along,0,advance);
                // Test body overlap, not just feet: the low cutting edge catches
                // small ground enemies while the crest can meet taller bodies.
                float feet=enemy.ImpactFeet.y-contact.y;
                if(feet>3.5f||feet+enemy.PhysicalBodyHeight<.10f||!CombatSight.Area(contact,enemy.transform.position))continue;
                hit.Add(enemy);
                enemy.TakeDamage(owner.ResolveSkillImpact(enemy,4,castId,damage.Amount,damage.IsCritical,damage.CriticalMultiplier),forward,1.6f+(rank-1)*.5f,.15f,critical:damage.IsCritical,practiceCastId:castId);
                owner.RegisterSkillHit(castId);
            }
            Draw();if(blocked||traveled>=range-.01f||age>3f)Destroy(gameObject);
        }
        private void Draw()
        {block.SetFloat("_Age",age);block.SetFloat("_Opacity",Mathf.Min(Mathf.Clamp01((age+.05f)/.14f),Mathf.Clamp01((range-traveled)/1.2f)));surface.SetPropertyBlock(block);}
        private void OnDestroy(){receipt?.Release();}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);mesh=null;material=null;}
    }
}
