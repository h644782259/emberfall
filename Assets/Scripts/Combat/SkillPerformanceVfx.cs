using UnityEngine;
namespace Emberfall
{
    internal enum SkillPerformanceBody { Tornado, Gravity, Meteor, ArrowVolley, Poison, Spirit, Frost, Fault, Fire, Lightning, GravityCollapse, Pressure }
    // Real 3D bodies, textured turbulent energy and bounded particles. This never selects or damages targets.
    internal sealed partial class SkillPerformanceVfx : MonoBehaviour
    {
        private static Mesh windWall,orb,lensQuad;
        private static Material energy,lens,body;
        private static int lights;
        private PlayerController owner;private GameSession session;private int epoch;
        private Transform authority;private bool bound,retired,ownsLight,awaitingContact,settling;
        private System.Func<bool> stateActive;
        private SkillPerformanceBody kind;private float age,life,size,beatAge,beatDuration;
        private Vector3 launch,landing;private CombatModel model;
        private Vector3[] flightFrom,flightTo,windVelocity,spiritVelocity;private float flightRadius;private bool flightArmed;private int windLayers,beatPreparedFrame=-1;
        private Transform[] parts;private Renderer[] surfaces;private MaterialPropertyBlock block;
        private readonly System.Collections.Generic.List<Mesh> ownedBodies=new System.Collections.Generic.List<Mesh>();
        private Light glow;private ParticleSystem particles;
        private SkillFlowVolume flowVolume;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){ResetDischarges();if(windWall!=null)Destroy(windWall);if(orb!=null)Destroy(orb);if(energy!=null)Destroy(energy);if(body!=null)Destroy(body);body=null;windWall=orb=null;energy=null;if(lens!=null)Destroy(lens);lens=null;if(lensQuad!=null)Destroy(lensQuad);lensQuad=null;lights=0;}
        private static void Assets()
        {
            if(!Application.isMobilePlatform&&Camera.main!=null)Camera.main.depthTextureMode|=DepthTextureMode.Depth;
            if(energy==null){energy=new Material(Resources.Load<Shader>("SpellEnergySurface"));SkillBodyMaterialLibrary.Bind(energy);}
            if(lens==null)lens=new Material(Resources.Load<Shader>(Application.isMobilePlatform?"BlackHoleLensMobile":"BlackHoleLens"));
            // These are visual meshes only. CreatePrimitive implicitly requests
            // collider types that IL2CPP can strip from the player build.
            // Clone the shared explicit mesh so Reset owns only its own copy.
            if(lensQuad==null)lensQuad=Instantiate(ProceduralVisuals.Shape(PrimitiveType.Quad));
            if(windWall==null)windWall=WindCloud();
            if(body==null){body=new Material(Resources.Load<Shader>("HeroSkillBody"));var texture=Resources.Load<Texture2D>("WorldArt/GravityBasalt");if(texture!=null)body.SetTexture("_BodyTex",texture);SkillBodyMaterialLibrary.Bind(body);}
            if(orb==null){orb=Instantiate(ProceduralVisuals.Shape(PrimitiveType.Sphere));orb.name="Spell absorption sphere";}
        }
        private static Mesh WindCloud()
        {
            // Closed irregular dust volumes expose shaded fronts and backs, with no
            // helical tubes, uniform funnel walls or rigid geometric wind ribbons.
            const int rings=12,sides=24;var vertices=new Vector3[(rings+1)*(sides+1)];var uv=new Vector2[vertices.Length];var triangles=new int[rings*sides*6];
            for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++)
            {
                float t=y/(float)rings,a=x/(float)sides*Mathf.PI*2,p=t*Mathf.PI;
                float bulge=1+.16f*Mathf.Sin(a*3+t*17)+.1f*Mathf.Cos(a*5-t*13);
                float r=Mathf.Sin(p)*bulge;int v=y*(sides+1)+x;
                vertices[v]=new Vector3(Mathf.Cos(a)*r,Mathf.Cos(p),Mathf.Sin(a)*r);uv[v]=new Vector2(x/(float)sides,t);
                if(y<rings&&x<sides){int at=(y*sides+x)*6,n=v+sides+1;triangles[at]=v;triangles[at+1]=v+1;triangles[at+2]=n;triangles[at+3]=v+1;triangles[at+4]=n+1;triangles[at+5]=n;}
            }
            var mesh=new Mesh{name="Irregular closed dust body"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        internal static SkillPerformanceVfx Sustain(PlayerController hero,Vector3 at,float radius,float duration,SkillPerformanceBody type,Transform follow=null,System.Func<bool> active=null,bool producerBoundActionBody=false)
        {
            if(hero==null||hero.IsDead)return null;
            GameAudio.SustainSkill(hero,follow,type,duration+SkillVisualAftermathTiming.FieldTail,active);
            var root=new GameObject("Skill performance / "+type);root.transform.position=type==SkillPerformanceBody.Poison?new Vector3(at.x,WorldTraversal.SurfaceHeight(at,.04f),at.z):at;
            // Producer-bounded arrows are the main release-to-contact trajectory.
            // Their finite owner/epoch/session lifecycle remains the authority;
            // dense hit debris must not erase that trajectory halfway through.
            if(!(producerBoundActionBody&&type==SkillPerformanceBody.ArrowVolley)&&CombatVisualLease.Attach(root,type==SkillPerformanceBody.GravityCollapse?CombatVisualPriority.Finale:CombatVisualPriority.SustainedBackground)==null)return null;
            Assets();var fx=root.AddComponent<SkillPerformanceVfx>();fx.owner=hero;fx.session=GameSession.Instance;fx.epoch=hero.CombatEpoch;fx.kind=type;fx.life=Mathf.Max(.8f,duration)+SkillVisualAftermathTiming.FieldTail;fx.size=radius;
            fx.authority=follow;fx.bound=follow!=null;fx.stateActive=active;fx.model=hero.GetComponentInChildren<CombatModel>();fx.block=new MaterialPropertyBlock();
            fx.Build();return fx;
        }
        private void Add(int i,Mesh mesh,Vector3 scale,Vector3 at,Quaternion rotation,Color color,bool dark=false)
        {
            var part=new GameObject(dark?"Absorption depth core":"Turbulent volumetric layer");part.transform.SetParent(transform,false);part.transform.localPosition=at;part.transform.localScale=scale;part.transform.localRotation=rotation;
            part.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=part.AddComponent<MeshRenderer>();bool isSolid=kind==SkillPerformanceBody.GravityCollapse&&i>0||kind==SkillPerformanceBody.ArrowVolley||kind==SkillPerformanceBody.Meteor&&i==0||kind==SkillPerformanceBody.Frost||kind==SkillPerformanceBody.Fault||kind==SkillPerformanceBody.Spirit||kind==SkillPerformanceBody.Poison||kind==SkillPerformanceBody.Tornado&&i>=windLayers;renderer.sharedMaterial=kind==SkillPerformanceBody.Lightning?DischargeMaterial():(kind==SkillPerformanceBody.Gravity||kind==SkillPerformanceBody.GravityCollapse&&i==0)?lens:isSolid?body:energy;renderer.shadowCastingMode=isSolid&&!EffectPreferences.ReducedEffects?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=isSolid;MobileSceneDepth.Track(renderer);
            parts[i]=part.transform;surfaces[i]=renderer;var props=new MaterialPropertyBlock();props.SetColor("_Tint",color);props.SetFloat("_Surface",kind==SkillPerformanceBody.ArrowVolley||kind==SkillPerformanceBody.Tornado&&i>=windLayers&&i%2==0?1:kind==SkillPerformanceBody.Frost?2:kind==SkillPerformanceBody.Spirit?4:kind==SkillPerformanceBody.Poison?3:0);props.SetFloat("_DarkCore",dark?1:0);props.SetFloat("_Heat",dark?0:kind==SkillPerformanceBody.Meteor&&i==0?.18f:1);props.SetFloat("_Wispy",kind==SkillPerformanceBody.Tornado?1:0);props.SetFloat("_Engraving",kind==SkillPerformanceBody.Tornado&&i>=windLayers&&i%2==0?1:0);props.SetFloat("_CoreLight",kind==SkillPerformanceBody.Tornado&&i>=windLayers&&i%2==0?.50f:0);if(kind==SkillPerformanceBody.Tornado&&i<windLayers)props.SetFloat("_Opacity",i==0?.20f:.09f);renderer.SetPropertyBlock(props);
        }
        private void Build()
        {
            if(kind==SkillPerformanceBody.GravityCollapse){BuildGravityCollapse();return;}
            // These are active damaging field bodies, not preparation cues.
            // The actual CombatArea tick is the only release authority.
            awaitingContact=kind==SkillPerformanceBody.Fire||kind==SkillPerformanceBody.Poison;
            windLayers=EffectPreferences.ReducedEffects?1:2;
            int count=kind==SkillPerformanceBody.Tornado?(EffectPreferences.ReducedEffects?5:10):kind==SkillPerformanceBody.Gravity||kind==SkillPerformanceBody.Pressure?1:kind==SkillPerformanceBody.Frost?9:kind==SkillPerformanceBody.ArrowVolley?(EffectPreferences.ReducedEffects?5:12):kind==SkillPerformanceBody.Meteor?3:kind==SkillPerformanceBody.Poison?(EffectPreferences.ReducedEffects?2:3):EffectPreferences.ReducedEffects?3:5;
            if(kind==SkillPerformanceBody.Fire)count=0;
            if(kind==SkillPerformanceBody.Meteor)count=1;
            if(kind==SkillPerformanceBody.Frost)count=5;
            // Wind dust is secondary decoration. The field body, flying arrows,
            // meteors and real release/contact beats keep their full identity.
            if(kind==SkillPerformanceBody.Tornado&&!EffectPreferences.ReducedEffects)
                count=windLayers+Mathf.Max(3,Mathf.RoundToInt((count-windLayers)*GraphicsQualityRuntime.Current.DecorativeEffectsScale));
            parts=new Transform[count];surfaces=new Renderer[count];windVelocity=new Vector3[count];spiritVelocity=new Vector3[count];
            Color tint=kind==SkillPerformanceBody.Gravity?new Color(1f,.32f,.025f):kind==SkillPerformanceBody.Meteor||kind==SkillPerformanceBody.Fire?new Color(1,.32f,.045f):kind==SkillPerformanceBody.Poison?new Color(.32f,.43f,.16f):kind==SkillPerformanceBody.Frost||kind==SkillPerformanceBody.Lightning?new Color(.3f,.75f,1f):GameBalance.ClassColor(owner.HeroClass);
            if(kind==SkillPerformanceBody.Fire||kind==SkillPerformanceBody.Poison||kind==SkillPerformanceBody.Frost||kind==SkillPerformanceBody.Spirit)
                flowVolume=new SkillFlowVolume(transform,kind==SkillPerformanceBody.Fire?SkillFlowVolume.Kind.Fire:kind==SkillPerformanceBody.Poison?SkillFlowVolume.Kind.Poison:kind==SkillPerformanceBody.Frost?SkillFlowVolume.Kind.Frost:SkillFlowVolume.Kind.Spirit,size,kind==SkillPerformanceBody.Fire?1.25f:1.2f,tint,airy:kind==SkillPerformanceBody.Fire);
            for(int i=0;i<count;i++)
            {
                if(kind==SkillPerformanceBody.ArrowVolley)Add(i,HeroSkillBodyAssets.Spell("Arrow"),new Vector3(1.55f,.84f,3.4f),Vector3.zero,Quaternion.identity,new Color(1,.58f,.055f));
                else if(kind==SkillPerformanceBody.Meteor)Add(i,i==0?(HeroSkillBodyAssets.Rock??orb):HeroSkillBodyAssets.Spell("Flame"),i==0?Vector3.one*2.1f:new Vector3(.8f,3f,.8f),Vector3.up*12,Quaternion.identity,i==0?new Color(.4f,.1f,.025f):tint);
                else if(kind==SkillPerformanceBody.Gravity)Add(i,lensQuad,new Vector3(size*1.4f,size*1.4f,1),Vector3.up*1.45f,Quaternion.identity,tint);
                else if(kind==SkillPerformanceBody.Pressure)Add(i,orb,Vector3.one*size*.9f,Vector3.up*3.6f,Quaternion.identity,new Color(.35f,.9f,.85f));
                else if(kind==SkillPerformanceBody.Spirit)Add(i,HeroSkillBodyAssets.Spirit,Vector3.one*(.4f+(i%2)*.1f),new Vector3(Mathf.Sin(i*1.7f)*size*.35f,.6f+i*.15f,Mathf.Cos(i*2.1f)*size*.3f),Quaternion.Euler(0,i*72,0),tint);
                else if(kind==SkillPerformanceBody.Lightning)Add(i,DischargeMesh(i),Vector3.one,Vector3.zero,Quaternion.identity,tint);
                else if(kind==SkillPerformanceBody.Fault)
                {
                    Vector3 at=new Vector3((i-(count-1)*.5f)*size*.35f,.22f+(i%2)*.12f,Mathf.Sin(i*2.4f)*.35f);
                    Mesh grounded=AnchoredImpactMesh.Create(HeroSkillBodyAssets.Rock,transform,at,new Vector3(.7f,.6f+(i%3)*.24f,.8f),Quaternion.Euler(i*19,i*67,22));
                    ownedBodies.Add(grounded);Add(i,grounded,Vector3.one,Vector3.zero,Quaternion.identity,new Color(.4f,.34f,.26f));
                }
                else if(kind==SkillPerformanceBody.Tornado)
                {
                    if(i<windLayers)Add(i,windWall,new Vector3(size*(i==0?.50f:.34f),i==0?.65f:.45f,size*(i==0?.46f:.36f)),Vector3.up*(i==0?.95f:1.6f),Quaternion.Euler(0,i*120,0),CombatFx.VanguardColor(2));
                    else
                    {
                        float angle=i*2.399963f;Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                        bool blade=i%2==0;
                        Add(i,blade?HeroSkillBodyAssets.Spell("Sword"):HeroSkillBodyAssets.Rock,blade?new Vector3(1f,1.45f,.85f):Vector3.one*(.16f+(i%3)*.1f),radial*size*.34f+Vector3.up*(.45f+(i%4)*.36f),Quaternion.Euler(i*29,i*67,20),blade?CombatFx.VanguardColor(2):new Color(.4f,.34f,.25f));
                        windVelocity[i]=Vector3.Cross(Vector3.up,radial)*3f+Vector3.up*1.8f;
                    }
                }
                else if(kind==SkillPerformanceBody.Poison)
                {
                    float rootScale=Mathf.Min(1,size*.45f),angle=i*2.399963f+.35f;
                    Vector3 at=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*size*.42f;
                    Vector3 dimensions=Vector3.one*rootScale;Quaternion rotation=Quaternion.Euler(0,i*137,0);
                    bool dry=GroundSkillSurface.TryPlaceAttached(transform,FilledSkillVfx.PoisonRoot,ref at,ref dimensions,rotation,size);
                    Mesh clipped=dry?AnchoredImpactMesh.Create(FilledSkillVfx.PoisonRoot,transform,at,dimensions,rotation):new Mesh{name="Poison contact / no dry root support"};ownedBodies.Add(clipped);
                    Add(i,clipped,Vector3.one,Vector3.zero,Quaternion.identity,new Color(.28f,.31f,.16f));
                    surfaces[i].GetPropertyBlock(block);block.SetFloat("_Surface",(float)SkillBodyMaterialLibrary.Matter.Wood);block.SetFloat("_Heat",0);surfaces[i].SetPropertyBlock(block);
                }
                else
                {
                    Mesh shape=HeroSkillBodyAssets.Spell(kind==SkillPerformanceBody.Frost?"Crystal":"Flame");
                    Vector3 at=new Vector3(Mathf.Cos(i*2.4f),0,Mathf.Sin(i*2.4f))*size*.5f;
                    Vector3 dimensions=new Vector3(.6f,1.7f+i*.35f,.6f);Quaternion rotation=Quaternion.Euler(0,i*120,0);
                    if(kind==SkillPerformanceBody.Frost)
                    {
                        float angle=i*2.399963f;
                        at=i==0?Vector3.zero:new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*size*Mathf.Sqrt(i/(float)count)*.72f;
                        dimensions=new Vector3(size*.22f,EnemyImpactMotion.FrostContactHeight*(1-i*.08f),size*.22f);
                    }
                    bool dry=kind!=SkillPerformanceBody.Fire||GroundSkillSurface.TryPlaceAttached(transform,shape,ref at,ref dimensions,rotation,size);
                    Mesh grounded=dry?AnchoredImpactMesh.Create(shape,transform,at,dimensions,rotation):new Mesh{name="Fire contact / no dry fuel support"};ownedBodies.Add(grounded);
                    Add(i,grounded,Vector3.one,Vector3.zero,Quaternion.identity,tint);
                }
            }
            if(kind==SkillPerformanceBody.Meteor)flowVolume=new SkillFlowVolume(parts[0],SkillFlowVolume.Kind.Fire,.75f,2.2f,tint,false);
            if(awaitingContact||kind==SkillPerformanceBody.ArrowVolley||kind==SkillPerformanceBody.Meteor)foreach(var surface in surfaces)surface.enabled=false;
            var element=kind==SkillPerformanceBody.Lightning?ElementalCombatVfx.Element.Lightning:kind==SkillPerformanceBody.Meteor||kind==SkillPerformanceBody.Fire?ElementalCombatVfx.Element.Fire:kind==SkillPerformanceBody.Poison?ElementalCombatVfx.Element.Poison:ElementalCombatVfx.Element.Ice;
            float decoration=GraphicsQualityRuntime.Current.DecorativeEffectsScale;
            particles=ElementalCombatVfx.Create(transform,"Field sparks / dust motes",element,EffectPreferences.ReducedEffects?12:Mathf.Max(12,Mathf.RoundToInt(35*decoration)),Mathf.Min(size*.65f,3));
            if(particles!=null)
            {
                // AddComponent may have started the native system before Create
                // disables playOnAwake. Stop and clear before configuring seeds.
                particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.useAutoRandomSeed=false;particles.randomSeed=127;var main=particles.main;main.maxParticles=EffectPreferences.ReducedEffects?36:Mathf.Max(36,Mathf.RoundToInt(96*decoration));main.startSize=new ParticleSystem.MinMaxCurve(.07f,.22f);main.startColor=new ParticleSystem.MinMaxGradient(tint*.65f,tint);
                var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
                velocity.orbitalY=kind==SkillPerformanceBody.Gravity?-3:kind==SkillPerformanceBody.Tornado?5:1;
                velocity.radial=kind==SkillPerformanceBody.Gravity?-2f:kind==SkillPerformanceBody.Tornado?.7f:0;
                // Create supplies two-constant curves; reset every axis together before switching to constants.
                velocity.x=0f;velocity.z=0f;velocity.y=kind==SkillPerformanceBody.Tornado?1.8f:.25f;
                if(!awaitingContact)particles.Play();
            }
            if(!EffectPreferences.ReducedEffects&&lights<3)
            {glow=gameObject.AddComponent<Light>();glow.type=LightType.Point;glow.color=tint;glow.range=Mathf.Min(8,size+2);glow.intensity=awaitingContact?0:1.3f;glow.shadows=LightShadows.None;ownsLight=true;lights++;}
        }
        internal void PrepareBeat(Vector3 at,float until,float impactRadius=-1)
        {
            landing=at;landing.y=WorldTraversal.SurfaceHeight(at,.04f);beatAge=0;beatPreparedFrame=Time.frameCount;beatDuration=Mathf.Max(0,until);
            flightRadius=impactRadius>=0?impactRadius:size;
            if(kind==SkillPerformanceBody.ArrowVolley&&owner.HeroClass==HeroClass.Ranger)flightRadius*=1.2f;
            flightArmed=until>0;
            launch=owner.transform.position+Vector3.up*1.2f;
            Vector3 muzzle;if(model!=null&&model.TryGetWeaponVisualAnchor(WeaponVisualAnchor.BowArrowRest,out muzzle))launch=muzzle;
            if(kind!=SkillPerformanceBody.ArrowVolley&&kind!=SkillPerformanceBody.Meteor)return;
            if(flightFrom==null||flightFrom.Length!=parts.Length){flightFrom=new Vector3[parts.Length];flightTo=new Vector3[parts.Length];}
            for(int i=0;i<parts.Length;i++)
            {
                if(kind==SkillPerformanceBody.ArrowVolley)
                {
                    float angle=i*2.399963f;
                    Vector3 point=landing+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*flightRadius*Mathf.Sqrt((i+.5f)/parts.Length)*.85f;
                    if(!CombatSight.Area(landing,point))point=CombatSight.BoundaryPoint(CombatSightKind.Area,landing,point);
                    point.y=WorldTraversal.SurfaceHeight(point,.04f)+.025f;flightFrom[i]=launch;flightTo[i]=point;
                }
                else
                {
                    // The preceding event schedule controls descent time. A falling
                    // body already has downward momentum instead of accelerating from
                    // an arbitrary visual height after its damage has happened.
                    flightTo[i]=landing+Vector3.up*(i==0?1.05f:1.05f+i*.55f);
                    flightFrom[i]=flightTo[i]+new Vector3(-3.2f,Mathf.Max(4,2.5f*beatDuration+PhysicalSkillMotion.Gravity*beatDuration*beatDuration*.5f),2.1f);
                }
                surfaces[i].enabled=flightArmed;
            }
            if(flightArmed)AnimateFlight();
        }
        internal void Beat()
        {
            if(awaitingContact)
            {
                awaitingContact=false;foreach(var surface in surfaces)surface.enabled=true;
                if(particles!=null)particles.Play();if(glow!=null)glow.intensity=1.2f;
            }
            if(kind==SkillPerformanceBody.Poison||kind==SkillPerformanceBody.Fire)
                GroundSkillSurface.Ensure(transform,owner,transform.position,size,kind==SkillPerformanceBody.Poison?GroundSkillSurface.Deposit.WetPoison:GroundSkillSurface.Deposit.Scorch,()=>life-age);
            if(kind==SkillPerformanceBody.Lightning)LatchDischarges();
            if(kind==SkillPerformanceBody.ArrowVolley||kind==SkillPerformanceBody.Meteor)
            {
                // Beat is the authoritative damage/contact event. No second launch,
                // post-hit shaft shrink or delayed cosmetic impact is scheduled.
                if(flightArmed){beatAge=beatDuration;AnimateFlight();}flightArmed=false;
                foreach(var surface in surfaces)surface.enabled=false;
            }
            if(particles!=null)particles.Emit(EffectPreferences.ReducedEffects?5:16);
        }
        internal void CompleteAftermath(bool finale=false)
        {
            if(retired||settling)return;
            settling=true;bound=false;authority=null;stateActive=null;flightArmed=false;
            life=age+(finale&&owner.HeroClass==HeroClass.Arcanist?ElementalistFieldTuning.UltimateVisualTail:SkillVisualAftermathTiming.Tail(finale));
            if(particles!=null)particles.Stop(true,ParticleSystemStopBehavior.StopEmitting);
        }
        private void AnimateFlight()
        {
            if(!flightArmed||flightFrom==null)return;
            for(int i=0;i<parts.Length;i++)
            {
                Vector3 point=PhysicalSkillMotion.BallisticPosition(flightFrom[i],flightTo[i],beatAge,beatDuration);
                Vector3 tangent=PhysicalSkillMotion.BallisticVelocity(flightFrom[i],flightTo[i],beatAge,beatDuration);
                if(kind==SkillPerformanceBody.ArrowVolley)
                {
                    Vector3 direction=tangent.normalized;
                    parts[i].position=point-direction*(1.7f*parts[i].localScale.y);
                    parts[i].rotation=Quaternion.FromToRotation(Vector3.up,direction);
                }
                else
                {parts[i].position=point;parts[i].rotation=i==0?Quaternion.Euler(age*95,age*65,20):Quaternion.FromToRotation(Vector3.up,-tangent.normalized);}
            }
        }
        private void AnimateWind(int i)
        {
            if(i<windLayers){parts[i].localRotation=Quaternion.Euler(Mathf.Sin(age*1.8f+i)*7,age*(i==0?36:-24)+i*120,Mathf.Cos(age*1.5f+i)*8);return;}
            if(i%2==0)
            {
                // Suspended forged blades are the skill's primary silhouette;
                // turbulent dust and ballistic fragments remain subordinate.
                float angle=age*4.1f+i*2.399963f;
                Vector3 bladeOrbit=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                parts[i].localPosition=bladeOrbit*(size*.66f)+Vector3.up*(.85f+(i%3)*.32f);
                parts[i].rotation=Quaternion.FromToRotation(Vector3.up,Vector3.Cross(Vector3.up,bladeOrbit)+Vector3.down*.18f);
                return;
            }
            Vector3 position=parts[i].position,offset=CombatFx.Flat(position-transform.position);
            float radius=Mathf.Max(.35f,offset.magnitude);Vector3 radial=offset/radius;
            Vector3 targetVelocity=Vector3.Cross(Vector3.up,radial)*(2.2f+radius*.65f)-radial*(radius-size*.3f)*2.8f;
            targetVelocity.y=position.y-transform.position.y<3.2f?1.5f:-.8f;
            float dt=Time.deltaTime;windVelocity[i]+=(targetVelocity-windVelocity[i])*Mathf.Min(1,dt*3f)+Vector3.up*PhysicalSkillMotion.Gravity*dt;
            PhysicalSkillMotion.Advance(ref position,ref windVelocity[i],.12f,dt,.2f,.7f);parts[i].position=position;
            parts[i].Rotate(new Vector3(65,48,72)*dt,Space.Self);
        }
        private void AnimateSpirit(int i)
        {
            Vector3 target=transform.position+new Vector3(Mathf.Sin(age*1.7f+i*1.6f)*size*.48f,.7f+Mathf.Sin(age*2.1f+i)*.23f,Mathf.Cos(age*1.1f+i*2.3f)*size*.36f);
            Vector3 position=parts[i].position;float dt=Time.deltaTime;
            spiritVelocity[i]+=(target-position)*dt*10f;spiritVelocity[i]*=Mathf.Exp(-3.8f*dt);
            Vector3 next=position+spiritVelocity[i]*dt;
            if(WorldTraversal.IsPhysicalSpaceClear(next,.16f))parts[i].position=next;else spiritVelocity[i]*=-.25f;
            if(spiritVelocity[i].sqrMagnitude>.015f)parts[i].rotation=Quaternion.Slerp(parts[i].rotation,Quaternion.LookRotation(spiritVelocity[i],Vector3.up),Mathf.Min(1,dt*6));
        }
        private void LateUpdate()
        {
            if(retired)return;
            if(owner==null||owner.IsDead||session==null||session!=GameSession.Instance||!session.HasStarted||session.Player!=owner||owner.CombatEpoch!=epoch||session.CombatEffectsEnded||bound&&(authority==null||!authority.gameObject.activeInHierarchy)||stateActive!=null&&!stateActive()){Retire();return;}
            if(session.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(beatPreparedFrame!=Time.frameCount)beatAge+=Time.deltaTime;if(age>=life){Retire();return;}
            if(kind==SkillPerformanceBody.GravityCollapse){AnimateGravityCollapse();return;}
            if(bound)transform.position=authority.position;
            float envelope=Mathf.Min(Mathf.SmoothStep(0,1,age/.22f),Mathf.SmoothStep(0,1,(life-age)/.55f));
            if(kind==SkillPerformanceBody.ArrowVolley||kind==SkillPerformanceBody.Meteor)envelope=1;
            flowVolume?.Draw(age,awaitingContact||kind==SkillPerformanceBody.Meteor&&!flightArmed?0:envelope);
            for(int i=0;i<parts.Length;i++)
            {
                if(kind==SkillPerformanceBody.Tornado)AnimateWind(i);
                else if(kind==SkillPerformanceBody.Spirit)AnimateSpirit(i);
                else if(kind==SkillPerformanceBody.Gravity&&Camera.main!=null)parts[i].rotation=Quaternion.LookRotation(parts[i].position-Camera.main.transform.position,Camera.main.transform.up);
                else if(kind==SkillPerformanceBody.ArrowVolley||kind==SkillPerformanceBody.Meteor){/* Flight is evaluated once below. */}
                else if(kind!=SkillPerformanceBody.Fault&&kind!=SkillPerformanceBody.Lightning&&kind!=SkillPerformanceBody.Poison&&kind!=SkillPerformanceBody.Frost&&kind!=SkillPerformanceBody.Fire)parts[i].localRotation=Quaternion.Euler(Mathf.Sin(age*2+i)*12,age*32+i*71,0);
                surfaces[i].GetPropertyBlock(block);float opacity=(kind==SkillPerformanceBody.Lightning?DischargeOpacity(i):envelope)*(kind==SkillPerformanceBody.Tornado?(i<windLayers?(i==0?.20f:.09f):1):kind==SkillPerformanceBody.Gravity&&i>0?.68f:1);
                if(kind==SkillPerformanceBody.Pressure)
                {parts[i].localPosition=Vector3.up*(3.6f-.25f*Mathf.SmoothStep(0,1,age/.5f)+(settling?.6f*Mathf.Clamp01(beatAge):0));opacity*=.55f;}
                if(surfaces[i].sharedMaterial==body)
                {
                    opacity=SkillVisualAftermathTiming.Coverage(life-age);
                    if(kind==SkillPerformanceBody.Spirit)opacity*=Mathf.SmoothStep(0,1,age/.22f);
                }
                if(settling)opacity*=SkillVisualAftermathTiming.Coverage(life-age,.6f);
                block.SetFloat("_Opacity",opacity);block.SetFloat("_Phase",age);surfaces[i].SetPropertyBlock(block);
            }
            if(flightArmed)AnimateFlight();
            if(glow!=null)glow.intensity=awaitingContact?0:kind==SkillPerformanceBody.Lightning?DischargeOpacity(0)*1.7f:envelope*(1.05f+.08f*Mathf.Sin(age*3.2f));
        }
        internal void Retire(){if(retired)return;retired=true;gameObject.SetActive(false);Destroy(gameObject);}
        private void OnDisable(){flowVolume?.Dispose();flowVolume=null;if(ownsLight){ownsLight=false;lights=Mathf.Max(0,lights-1);}}
        private void OnDestroy(){foreach(var mesh in ownedBodies)if(mesh!=null)Destroy(mesh);ownedBodies.Clear();}
    }
}
