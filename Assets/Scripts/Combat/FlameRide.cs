using UnityEngine;
namespace Emberfall
{
    // One producer per cast. Footprints share a single tick and never multiply
    // damage when the rider circles, stops or doubles back over the same enemy.
    internal sealed class FlameRide : MonoBehaviour
    {
        private const float BurnRadius=2.2f,TrailLife=3.2f;
        private readonly System.Collections.Generic.Dictionary<EnemyController,float> contacts=new System.Collections.Generic.Dictionary<EnemyController,float>();
        private readonly float[] footprintAge=new float[24];
        private Vector3 previousPosition,lastFootprint;
        private float nextFootprint;
        private static Mesh tongueMesh;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetArt(){if(tongueMesh!=null)Destroy(tongueMesh);tongueMesh=null;}
        private PlayerController owner;
        private GameSession session;
        private int epoch,castId;
        private float duration,age,nextTick,attack;
        private CastFirstHitReceipt receipt;
        private readonly Vector3[] footprints=new Vector3[24];
        private readonly FilledSkillVfx.ContactTrackHandle[] footprintVisuals=new FilledSkillVfx.ContactTrackHandle[24];
        private int footprintCount,cursor;
        private Transform mount;
        private Material flame,core;
        internal static void Spawn(PlayerController player,GameSession game,float seconds,float power,int cast)
        {
            var ride=new GameObject("Fire path riding flame").AddComponent<FlameRide>();
            ride.owner=player;ride.session=game;ride.epoch=player.CombatEpoch;
            // The birth frame must already use the rider's real world pose;
            // rendering can occur before the producer's first Update.
            ride.transform.SetPositionAndRotation(player.transform.position,player.transform.rotation);
            ride.previousPosition=ride.lastFootprint=player.transform.position;
            GameAudio.SustainSkill(player,ride.transform,SkillPerformanceBody.Fire,seconds+TrailLife);
            ride.duration=seconds;ride.attack=power;ride.castId=cast;ride.receipt=player.RetainCastReceipt(cast);
            ride.flame=new Material(Resources.Load<Shader>("SpellEnergySurface"));ride.flame.SetColor("_Tint",new Color(1f,.27f,.045f));ride.flame.SetFloat("_Heat",.35f);
            SkillBodyMaterialLibrary.Bind(ride.flame);
            ride.core=new Material(Resources.Load<Shader>("SpellSolidSurface"));ride.core.SetColor("_Tint",new Color(.38f,.13f,.045f));var basalt=Resources.Load<Texture2D>("WorldArt/GravityBasalt");if(basalt!=null){ride.core.SetTexture("_RockTex",basalt);ride.core.SetFloat("_RockDetail",1);}
            SkillBodyMaterialLibrary.Apply(ride.core,SkillBodyMaterialLibrary.Matter.Rock,.35f);
            ride.mount=new GameObject("Flame surf mount").transform;ride.mount.SetParent(ride.transform,false);
            if(tongueMesh==null)tongueMesh=AuthoredSpellBases.Load("Flame");
            for(int i=0;i<7;i++)
            {
                var part=ProceduralVisuals.Create(i==0?"Riding flame core":"Trailing flame tongue",PrimitiveType.Sphere,i==0?ride.core:ride.flame).transform;
                part.SetParent(ride.mount,false);
                part.localPosition=i==0?new Vector3(0,.14f,0):new Vector3((i%3-1)*.3f,.12f,-.35f-(i/3)*.45f);
                part.localScale=i==0?new Vector3(1.15f,.22f,1.8f):new Vector3(.42f,.7f,.6f);
                if(i==0)part.GetComponent<MeshFilter>().sharedMesh=HeroSkillBodyAssets.Rock;
                if(i>0&&tongueMesh!=null){part.GetComponent<MeshFilter>().sharedMesh=tongueMesh;part.localRotation=Quaternion.Euler(55,0,0);}
                var renderer=part.GetComponent<Renderer>();
                renderer.shadowCastingMode=i==0?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows=i==0;
            }
            ride.DrawAppearance();
        }
        private void DrawAppearance()
        {
            // Coverage keeps the basalt opaque where present, including its
            // contact shadow. Fuel and heat build briefly, then retire together
            // before the already-authoritative producer reaches its endpoint.
            float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.14f));
            float leave=Mathf.Clamp01((duration-age)/.24f);
            float opacity=Mathf.Min(.3f+.7f*enter,leave);
            flame.SetFloat("_Opacity",opacity);core.SetFloat("_Opacity",opacity);
            flame.SetFloat("_Phase",age);core.SetFloat("_Phase",age);
            flame.SetFloat("_Heat",.35f*opacity);core.SetFloat("_Heat",.35f*opacity);
            mount.localPosition=new Vector3(0,-.06f*(1-opacity),0);
        }
        private bool Current {get{return owner!=null&&!owner.IsDead&&session!=null&&session.Player==owner&&session.HasStarted&&!session.CombatEffectsEnded&&owner.CombatEpoch==epoch;}}
        private void Update()
        {
            if(!Current){Destroy(gameObject);return;}
            transform.SetPositionAndRotation(owner.transform.position,owner.transform.rotation);
            if(session.InputBlocked||Time.deltaTime<=0)return;
            bool riding=age<duration&&owner.FrostStrideActive;
            if(!riding)duration=Mathf.Min(duration,age);
            if(age>=duration+TrailLife){Destroy(gameObject);return;}
            mount.gameObject.SetActive(riding);
            if(riding)
            {
                Vector3 current=owner.transform.position;
                using(var targets=CombatTargetSnapshot<EnemyController>.Capture(session.Enemies))
                for(int i=0;i<targets.Count;i++)
                {
                    var enemy=targets[i];if(enemy==null||enemy.IsDead)continue;
                    Vector3 segment=CombatFx.Flat(current-previousPosition),offset=CombatFx.Flat(enemy.transform.position-previousPosition);
                    float t=segment.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(offset,segment)/segment.sqrMagnitude);
                    Vector3 nearest=previousPosition+segment*t;
                    float radius=1.65f+enemy.NavigationRadius;
                    if(CombatFx.Flat(enemy.transform.position-nearest).sqrMagnitude>radius*radius||!CombatSight.Area(nearest,enemy.transform.position))continue;
                    float last;if(contacts.TryGetValue(enemy,out last)&&age-last<.8f)continue;
                    contacts[enemy]=age;owner.RegisterSkillHit(castId);
                    enemy.TakeDamage(attack*SkillDamageBudgets.FrostWalkerPulse(session.Progression.Profile.skillRanks[5]),current-previousPosition,.3f,0,practiceCastId:castId);
                    if(!enemy.IsDead)enemy.StatusEffects?.FrostMark(2f);
                }
                previousPosition=current;
                if(age>=nextFootprint&&(footprintCount==0||CombatFx.Flat(current-lastFootprint).sqrMagnitude>=.36f))
                {
                    nextFootprint=age+.12f;lastFootprint=current;
                    int slot=cursor;footprintVisuals[slot].Retire();footprints[slot]=current;footprintAge[slot]=age;
                    float expires=age+TrailLife;
                    footprintVisuals[slot]=FilledSkillVfx.ContactTrack(owner,current,BurnRadius,()=>Current?Mathf.Max(0,expires-age):0);
                    cursor=(cursor+1)%footprints.Length;footprintCount=Mathf.Min(footprints.Length,footprintCount+1);
                }
            }
            if(age>=nextTick)
            {
                nextTick=age+SkillDamageBudgets.FlameRideInterval;
                for(int i=session.Enemies.Count-1;i>=0&&Current;i--)
                {
                    var enemy=session.Enemies[i];if(enemy==null||enemy.IsDead)continue;
                    for(int p=0;p<footprintCount;p++)
                    {
                        if(age-footprintAge[p]>=TrailLife)continue;
                        Vector3 delta=CombatFx.Flat(enemy.transform.position-footprints[p]);
                        float radius=BurnRadius+(enemy.IsBoss?.85f:.4f)+enemy.HitFootprintBonus;
                        if(delta.sqrMagnitude>radius*radius||!CombatSight.Area(footprints[p],enemy.transform.position))continue;
                        owner.RegisterSkillHit(castId);
                        enemy.TakeDamage(attack*SkillDamageBudgets.FlameRideTick,delta.normalized,0,0,practiceCastId:castId);
                        ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Fire,.6f);break;
                    }
                }
            }
            age+=Time.deltaTime;
            if(riding)DrawAppearance();
        }
        private void OnDestroy(){foreach(var visual in footprintVisuals)visual.Retire();receipt?.Release();if(flame!=null)Destroy(flame);if(core!=null)Destroy(core);}
    }
}
