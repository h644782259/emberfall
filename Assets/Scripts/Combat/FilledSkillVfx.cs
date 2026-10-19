using UnityEngine;

namespace Emberfall
{
    // All primary silhouettes are filled world-space meshes. No damage, physics,
    // screen flashes, character sprites, or camera-facing replacement billboards.
    internal sealed class FilledSkillVfx : MonoBehaviour
    {
        private sealed class Piece
        {
            public Transform Transform; public Renderer Renderer; public MeshFilter Filter;
            public Vector3 Position, Scale; public Quaternion Rotation;
            public float Delay, Phase, TravelScale=1, SimulatedAge; public int Motion, BodySurface; public bool Anchored, Secondary; public Mesh OwnedMesh;
            public Vector3 WorldPosition, Velocity, Spin;
        }
        private static Mesh identityBlade,identityFork,identityContract,identityProtection;
        private static Mesh icePrimary,firePrimary,energyCore,poisonRoot;
        private static Mesh stompWind, crescent, crystal, flame, sword, lightning, arcane, rupture, arcaneShard, arrow, vine;
        private static Material sharedMaterial,bodyMaterial;
        private SkillFlowVolume flowVolume;
        private static bool assetsReady;
        private static int active;
        private readonly Piece[] pieces=new Piece[FilledVfxRecipes.MaximumParts];
        private MaterialPropertyBlock block;
        private void Awake() { EnsurePropertyBlock(); }
        private void EnsurePropertyBlock() { if (block == null) block = new MaterialPropertyBlock(); }
        private int count,epoch,allocated;
        private bool arrowGeometryReady,arrowGeometryFinal,arrowGeometryReduced;
        private int arrowGeometryRevision;
        private GameSession session;
        private bool pooled,disposing,sculpted,settling;
        private ulong rentGeneration;
        // A retained reference must identify the rental, not just the pooled component.
        internal readonly struct ArrowBatchHandle : System.IDisposable
        {
            private readonly FilledSkillVfx effect;
            private readonly ulong generation;
            internal ArrowBatchHandle(FilledSkillVfx effect)
            {this.effect=effect;generation=effect!=null?effect.rentGeneration:0;}
            public bool IsValid => effect!=null && effect.rentGeneration==generation &&
                !effect.pooled && !effect.disposing && effect.gameObject.activeInHierarchy &&
                effect.owner!=null && !effect.owner.IsDead && effect.owner.CombatEpoch==effect.epoch &&
                effect.session==GameSession.Instance;
            public void ArrowBeat(Vector3 at,float radius,bool final)
            {if(IsValid)effect.ArrowBeat(at,radius,final);}
            public void Retire(){if(IsValid)effect.Retire();}
            public void CompleteAftermath(bool finale=false){if(IsValid)effect.CompleteAftermath(finale);}
            public void Dispose(){Retire();}
        }
        // Footprint handles follow the producer's damage slots across pool reuse.
        internal readonly struct ContactTrackHandle
        {
            private readonly FilledSkillVfx effect;
            private readonly ulong generation;
            internal ContactTrackHandle(FilledSkillVfx effect)
            {this.effect=effect;generation=effect!=null?effect.rentGeneration:0;}
            public bool IsValid => effect!=null && effect.rentGeneration==generation && effect.contactTrack &&
                !effect.pooled && !effect.disposing && effect.gameObject.activeInHierarchy &&
                effect.owner!=null && !effect.owner.IsDead && effect.owner.CombatEpoch==effect.epoch &&
                effect.session==GameSession.Instance;
            public void Retire(){if(IsValid)effect.Retire();}
        }
        private const int PoolCapacity=8;
        private static readonly System.Collections.Generic.Stack<FilledSkillVfx> idle=new System.Collections.Generic.Stack<FilledSkillVfx>();
        private static readonly System.Collections.Generic.HashSet<FilledSkillVfx> instances=new System.Collections.Generic.HashSet<FilledSkillVfx>();
        private PlayerController owner;
        private FilledVfxKind kind;
        private Color tint;
        private float age,life,size;
        private bool registered, confirmedFinale, contactTrack;
        private System.Func<float> contactRemaining;
        // A scheduled main arrow batch belongs to its gameplay producer. It has
        // ordinary contact animation, but optional VFX pressure cannot evict it.
        private System.Func<bool> actionActive;
        private float trackRemaining;
        private int finaleCast;
        private float terminalAge;
        private static readonly System.Collections.Generic.List<FilledSkillVfx> finales=new System.Collections.Generic.List<FilledSkillVfx>();
        internal static void SkipFinales(GameSession game)
        {if(game==null)return;foreach(var fx in finales.ToArray())if(fx!=null&&fx.owner==game.Player)fx.Retire();}
        internal static void ConfirmFinale(PlayerController hero,int castId)
        {foreach(var fx in finales)if(fx!=null&&fx.owner==hero&&fx.epoch==hero.CombatEpoch&&fx.finaleCast==castId)fx.confirmedFinale=true;}
        private void RegisterFinale(int castId)
        {
            // Registration starts at the actual final beat, never at charge/early arrows.
            if(castId==0)return;
            finaleCast=castId;confirmedFinale=false;terminalAge=0;
            if(!finales.Contains(this))finales.Add(this);
        }
        private CombatVisualLease lease;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAssets()
        {
            assetsReady=false;
            foreach(var fx in new System.Collections.Generic.List<FilledSkillVfx>(instances))if(fx!=null){fx.disposing=true;fx.gameObject.SetActive(false);Destroy(fx.gameObject);}
            instances.Clear();idle.Clear();
            finales.Clear();
            if(identityBlade!=null)Destroy(identityBlade);if(identityFork!=null)Destroy(identityFork);identityBlade=identityFork=identityContract=identityProtection=null;

            if(energyCore!=null)Destroy(energyCore);energyCore=null;
            if(poisonRoot!=null)Destroy(poisonRoot);poisonRoot=null;
            if(icePrimary!=null)Destroy(icePrimary);if(firePrimary!=null)Destroy(firePrimary);icePrimary=firePrimary=null;
            if(stompWind!=null)Destroy(stompWind);stompWind=null;
            if(crescent!=null)Destroy(crescent);if(crystal!=null)Destroy(crystal);if(flame!=null)Destroy(flame);
            if(sword!=null)Destroy(sword);if(lightning!=null)Destroy(lightning);if(arcane!=null)Destroy(arcane);
            if(arrow!=null)Destroy(arrow);if(vine!=null)Destroy(vine);arrow=vine=null;
            if(bodyMaterial!=null)Destroy(bodyMaterial);bodyMaterial=null;
            if(sharedMaterial!=null)Destroy(sharedMaterial);crescent=crystal=flame=sword=lightning=arcane=rupture=arcaneShard=null;sharedMaterial=null;active=0;
        }
        private static Mesh Mesh(FilledMeshRecipe data,string name)
        {
            var vertices=new Vector3[data.Positions.Length/3];var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++){vertices[i]=new Vector3(data.Positions[i*3],data.Positions[i*3+1],data.Positions[i*3+2]);uv[i]=new Vector2(data.Uv[i*2],data.Uv[i*2+1]);}
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=data.Triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        internal static System.Collections.IEnumerator Prewarm()
        {
            var flowNoise=SkillFlowVolume.FlowNoise;
            yield return FloatingNumber.Prewarm();
            yield return HitFeedback.Prewarm();
            VanguardSwordWave.Prewarm();yield return null;
            if(assetsReady){if(poisonRoot==null){var cachedRoot=PoisonRoot;}yield break;}
            if(identityBlade==null)identityBlade=AuthoredSpellBases.Load("Sword");
            yield return null;
            if(identityFork==null)identityFork=AuthoredSpellBases.Identity("ForkPulse");
            yield return null;
            if(identityContract==null)identityContract=HeroSkillBodyAssets.Spirit;
            yield return null;
            if(identityProtection==null)identityProtection=HeroSkillBodyAssets.Armor;
            VanguardWardVisual.Prewarm();
            yield return null;
            if(icePrimary==null)icePrimary=AuthoredSpellBases.Load("IcePrimary");
            yield return null;
            if(firePrimary==null)firePrimary=AuthoredSpellBases.Load("FirePrimary");
            yield return null;
            if(energyCore==null)energyCore=Mesh(FilledVfxRecipes.EnergyCore(),"Elemental luminous core");
            if(crescent==null)crescent=AuthoredSpellBases.Load("Crescent")??Mesh(FilledVfxRecipes.Crescent(),"Filled curved crescent volume");
            yield return null;
            if(crystal==null)crystal=AuthoredSpellBases.Load("Crystal")??Mesh(FilledVfxRecipes.Crystal(),"Faceted ice spear");
            yield return null;
            if(flame==null)flame=AuthoredSpellBases.Load("Flame")??Mesh(FilledVfxRecipes.Flame(),"Curved flame tongue volume");
            yield return null;
            if(sword==null)sword=AuthoredSpellBases.Load("Sword")??Mesh(FilledVfxRecipes.Sword(),"Ridged sword blade guard and grip");
            yield return null;
            if(lightning==null)lightning=AuthoredSpellBases.Load("Lightning")??Mesh(FilledVfxRecipes.Lightning(),"Angular branched lightning volume");
            yield return null;
            if(arcane==null)arcane=AuthoredSpellBases.Load("Crystal");
            yield return null;
            if(stompWind==null)stompWind=Mesh(FilledVfxRecipes.Crescent(),"Ranger landing / curved wind volume");
            if(rupture==null)rupture=HeroSkillBodyAssets.GroundBreak;
            yield return null;
            if(arcaneShard==null)arcaneShard=HeroSkillBodyAssets.Rock;
            yield return null;
            if(arrow==null)arrow=AuthoredSpellBases.Load("Arrow")??Mesh(FilledVfxRecipes.Arrow(),"Narrow arrow shaft head and fletching");
            yield return null;
            if(vine==null)vine=AuthoredSpellBases.Load("Vine")??Mesh(FilledVfxRecipes.Vine(),"Branching poison vine");
            yield return null;
            var rootedPoison=PoisonRoot;
            yield return null;
            EnsureAssets();
        }
        private static void EnsureAssets()
        {
            if(!Application.isMobilePlatform&&Camera.main!=null)Camera.main.depthTextureMode|=DepthTextureMode.Depth;
            if(assetsReady)return;
            if(identityBlade==null)identityBlade=AuthoredSpellBases.Load("Sword");
            if(identityFork==null)identityFork=AuthoredSpellBases.Identity("ForkPulse");
            if(identityContract==null)identityContract=HeroSkillBodyAssets.Spirit;
            if(identityProtection==null)identityProtection=HeroSkillBodyAssets.Armor;
            if(icePrimary==null)icePrimary=AuthoredSpellBases.Load("IcePrimary");
            if(firePrimary==null)firePrimary=AuthoredSpellBases.Load("FirePrimary");
            if(energyCore==null)energyCore=Mesh(FilledVfxRecipes.EnergyCore(),"Elemental luminous core");
            if(crescent==null)crescent=AuthoredSpellBases.Load("Crescent")??Mesh(FilledVfxRecipes.Crescent(),"Filled curved crescent volume");
            if(crystal==null)crystal=AuthoredSpellBases.Load("Crystal")??Mesh(FilledVfxRecipes.Crystal(),"Faceted ice spear");
            if(flame==null)flame=AuthoredSpellBases.Load("Flame")??Mesh(FilledVfxRecipes.Flame(),"Curved flame tongue volume");
            if(sword==null)sword=AuthoredSpellBases.Load("Sword")??Mesh(FilledVfxRecipes.Sword(),"Ridged sword blade guard and grip");
            if(lightning==null)lightning=AuthoredSpellBases.Load("Lightning")??Mesh(FilledVfxRecipes.Lightning(),"Angular branched lightning volume");
            if(arcane==null)arcane=AuthoredSpellBases.Load("Crystal");
            if(stompWind==null)stompWind=Mesh(FilledVfxRecipes.Crescent(),"Ranger landing / curved wind volume");
            if(rupture==null)rupture=HeroSkillBodyAssets.GroundBreak;
            if(arcaneShard==null)arcaneShard=HeroSkillBodyAssets.Rock;
            if(arrow==null)arrow=AuthoredSpellBases.Load("Arrow")??Mesh(FilledVfxRecipes.Arrow(),"Narrow arrow shaft head and fletching");
            if(vine==null)vine=AuthoredSpellBases.Load("Vine")??Mesh(FilledVfxRecipes.Vine(),"Branching poison vine");
            if(sharedMaterial==null)
            {Shader shader=Resources.Load<Shader>("FilledSpell");sharedMaterial=new Material(shader!=null?shader:Shader.Find("Sprites/Default"));sharedMaterial.renderQueue=3070;SkillBodyMaterialLibrary.Bind(sharedMaterial);}
            if(bodyMaterial==null)
            {bodyMaterial=new Material(Resources.Load<Shader>("HeroSkillBody"));var texture=Resources.Load<Texture2D>("WorldArt/GravityBasalt");if(texture!=null)bodyMaterial.SetTexture("_BodyTex",texture);SkillBodyMaterialLibrary.Bind(bodyMaterial);}
            assetsReady=true;
        }
        internal static Mesh PoisonRoot
        {
            get
            {
                if(poisonRoot!=null)return poisonRoot;
                var source=Resources.Load<TextAsset>("ActorModules/BarkTorso");
                var stem=source!=null?AuthoredActorMeshes.Decode(source.bytes,"Bent living poison root"):null;
                if(stem==null)return null;
                var vertices=stem.vertices;
                for(int i=0;i<vertices.Length;i++)
                {
                    Vector3 point=vertices[i];float t=(point.y+1)*.5f;
                    point.x=point.x*.31f*(1-t*.6f)+Mathf.Sin(t*3.6f)*.12f;
                    point.z=point.z*.35f*(1-t*.55f)+t*t*.17f;
                    point.y=t*1.15f;vertices[i]=point;
                }
                stem.vertices=vertices;stem.RecalculateNormals();stem.RecalculateBounds();
                // Thick crooked bark carries the silhouette; only two substantial
                // offshoots remain, instead of a repeated mat of hairline stems.
                poisonRoot=new Mesh{name="Thick rooted poison shrub"};
                poisonRoot.CombineMeshes(new[]{
                    new CombineInstance{mesh=stem,transform=Matrix4x4.identity},
                    new CombineInstance{mesh=stem,transform=Matrix4x4.TRS(new Vector3(.05f,.3f,.025f),Quaternion.Euler(10,25,-55),new Vector3(.7f,.55f,.7f))},
                    new CombineInstance{mesh=stem,transform=Matrix4x4.TRS(new Vector3(.04f,.53f,.09f),Quaternion.Euler(-10,150,48),new Vector3(.6f,.43f,.6f))}
                },true,true);
                poisonRoot.RecalculateBounds();Destroy(stem);return poisonRoot;
            }
        }
        internal static float PoisonMudScale(float radius)
        {
            float extent=.01f;foreach(var point in EnemySkillPhysicalVfx.GelMesh.vertices)extent=Mathf.Max(extent,new Vector2(point.x,point.z).magnitude);
            return radius*.96f/extent;
        }
        private static FilledSkillVfx Create(PlayerController hero,Vector3 at,Vector3 forward,FilledVfxKind type,float radius,Color color,float duration,Transform parent=null,CombatVisualPriority priority=CombatVisualPriority.Decoration,bool essentialTrack=false,System.Func<bool> actionActive=null,bool essentialBody=false)
        {
            if(hero==null||hero.IsDead||!Finite(radius)||radius<=0||!Finite(forward.x)||!Finite(forward.y)||!Finite(forward.z)||!Finite(duration)||duration<=0||!Finite(at.x)||!Finite(at.y)||!Finite(at.z))return null;
            EnsureAssets();FilledSkillVfx fx=null;
            while(idle.Count>0&&fx==null){var candidate=idle.Pop();if(candidate!=null&&!candidate.disposing)fx=candidate;}
            if(fx==null){var fresh=new GameObject("Filled effect");fresh.SetActive(false);fx=fresh.AddComponent<FilledSkillVfx>();instances.Add(fx);}
            fx.EnsurePropertyBlock();
            var root=fx.gameObject;checked{fx.rentGeneration++;}fx.pooled=false;root.name="Filled "+type+" effect";root.transform.SetParent(parent,false);root.transform.localScale=Vector3.one;root.transform.position=at;
            root.transform.rotation=Quaternion.LookRotation(forward.sqrMagnitude>.0001f?forward.normalized:Vector3.forward);
            fx.owner=hero;fx.session=GameSession.Instance;fx.epoch=hero.CombatEpoch;fx.kind=type;fx.size=Mathf.Clamp(radius,.15f,14);
            fx.contactTrack=essentialTrack;fx.contactRemaining=null;fx.trackRemaining=0;
            fx.actionActive=actionActive;
            fx.sculpted=fx.settling=false;fx.tint=color;fx.life=Mathf.Clamp(duration,.12f,12);fx.age=0;fx.terminalAge=0;fx.finaleCast=0;fx.confirmedFinale=false;
            ulong generation=fx.rentGeneration;
            // The three real riding footprints are authoritative contact indicators;
            // optional decoration pressure must not leave invisible damaging ground.
            fx.lease=essentialTrack||actionActive!=null||essentialBody?null:CombatVisualLease.Attach(root,priority,()=>{if(fx.rentGeneration==generation)fx.Retire();});
            if(!essentialTrack&&actionActive==null&&!essentialBody&&fx.lease==null)return null;
            root.SetActive(true);fx.Register();return fx;
        }
        public static void Crescent(PlayerController hero,Vector3 at,Vector3 forward,float radius,Color color,int swingSide=1,CombatVisualPriority priority=CombatVisualPriority.Decoration,int castId=0,bool essentialBody=false)
        {
            // A basic swing is bounded by its .34s lifetime and attack interval.
            // Decorative lease saturation must never hide that primary action.
            var fx=Create(hero,at+Vector3.up*.82f,forward,FilledVfxKind.Crescent,radius,color,.34f,priority:priority,essentialBody:essentialBody);if(fx==null)return;
            if(priority==CombatVisualPriority.Finale)fx.RegisterFinale(castId);
            fx.sculpted=true;
            // One broad forged blade preserves the attack direction. Several
            // crossed silhouettes read as a geometric star at the game camera.
            fx.Add(sword,new Vector3(.12f,.1f,fx.size*.25f),
                new Vector3(.95f,Mathf.Min(1.45f,fx.size*.65f),1.7f),Quaternion.Euler(68,12*swingSide,22),
                0,15,swingSide,fx.size*.8f,"Forged swinging blade",true);
            for(int i=0;i<3;i++)fx.Add(arcaneShard,new Vector3((i-1)*.25f,.1f,.6f)*fx.size,
                new Vector3(.11f,.1f,.14f),Quaternion.Euler(85,i*33,0),.025f,4,i);
        }
        internal static void RangerStomp(PlayerController hero,Vector3 at,float radius)
        {
            // One bounded landing body per real vault; decorative pressure cannot
            // erase the stomp. Geometry follows terrain, with no damage colliders.
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.Fault,radius,new Color(.62f,1f,.87f),1.5f,
                priority:CombatVisualPriority.ActionBody,essentialBody:true);
            if(fx==null)return;
            fx.gameObject.name="Ranger vault / fractured landing and outward gusts";
            fx.Add(rupture,Vector3.up*.05f,new Vector3(radius*.85f,.85f,radius*.85f),Quaternion.identity,
                0,12,0,radius,"Stomp ground fracture",true);
            int gusts=EffectPreferences.ReducedEffects?2:4;
            for(int i=0;i<gusts;i++)
            {
                float angle=i*Mathf.PI*2/gusts;
                Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                fx.Add(stompWind,radial*radius*.24f+Vector3.up*.25f,new Vector3(radius*.6f,.32f,radius*.6f),
                    Quaternion.Euler(8,i*360f/gusts,0),i*.025f,28,i,radius*.7f,"Outward landing wind");
            }
            PhysicalSkillVfx.Impact(at,Vector3.down,new Color(.48f,.41f,.31f),radius,priority:CombatVisualPriority.RealContact);
        }
        public static void Impact(PlayerController hero,Vector3 at,float radius,FilledVfxKind type,Color color,CombatVisualPriority priority=CombatVisualPriority.ActionBody,int castId=0,bool elementalist=false,bool monumentalSword=false)
        {
            if(type==FilledVfxKind.Vine){PoisonVines(hero,at,radius,color);return;}
            if(type==FilledVfxKind.ArrowRain){ArrowRain(hero,at,radius,color,priority==CombatVisualPriority.Finale,priority,castId);return;}
            if(type==FilledVfxKind.Fault)
            {
                var contact=Create(hero,at,Vector3.forward,type,radius,color,1.25f,priority:priority);if(contact==null)return;
                contact.sculpted=true;
                contact.Add(rupture,Vector3.up*.045f,new Vector3(contact.size*.65f,.65f,contact.size*.65f),Quaternion.identity,0,5,0,contact.size*.75f,"Actual fractured landing ground",true);
                PhysicalSkillVfx.Impact(at,Vector3.down,new Color(.43f,.37f,.28f),radius);
                return;
            }
            if(type!=FilledVfxKind.Ice&&type!=FilledVfxKind.Fire&&type!=FilledVfxKind.Summon&&type!=FilledVfxKind.Sword&&type!=FilledVfxKind.Lightning&&type!=FilledVfxKind.Arcane)return;
            var fx=Create(hero,at,type==FilledVfxKind.Sword?hero.transform.forward:Vector3.forward,type,radius,color,priority==CombatVisualPriority.Finale?(elementalist?ElementalistFieldTuning.UltimateVisualLife:2.1f):type==FilledVfxKind.Fire?1.55f:1.65f,priority:priority);if(fx==null)return;
            if(monumentalSword&&type==FilledVfxKind.Sword)fx.life=2.45f;
            if(type==FilledVfxKind.Fire)GroundSkillSurface.Ensure(fx.transform,hero,at,radius,GroundSkillSurface.Deposit.Scorch,()=>fx.life-fx.age);
            if(type==FilledVfxKind.Fire||type==FilledVfxKind.Ice)ElementalCombatVfx.Burst(hero,at,radius,type==FilledVfxKind.Fire?ElementalCombatVfx.Element.Fire:ElementalCombatVfx.Element.Ice);
            if(priority==CombatVisualPriority.Finale)fx.RegisterFinale(castId);
            float unit=Mathf.Min(1.6f,fx.size*.55f);
            if(type==FilledVfxKind.Fire)
            {
                fx.flowVolume=new SkillFlowVolume(fx.transform,SkillFlowVolume.Kind.Fire,fx.size,Mathf.Min(1.8f,.9f+fx.size*.18f),color,airy:true);
                fx.flowVolume.Draw(0,.25f);
                return;
            }
            if(type==FilledVfxKind.Ice||type==FilledVfxKind.Summon||type==FilledVfxKind.Arcane)
                fx.flowVolume=new SkillFlowVolume(fx.transform,type==FilledVfxKind.Ice?SkillFlowVolume.Kind.Frost:SkillFlowVolume.Kind.Spirit,fx.size,Mathf.Min(1.5f,fx.size*.45f),color);
            fx.sculpted=type==FilledVfxKind.Sword||type==FilledVfxKind.Summon||type==FilledVfxKind.Arcane;
            if(elementalist&&(type==FilledVfxKind.Ice||type==FilledVfxKind.Fire||type==FilledVfxKind.Lightning))
            {fx.sculpted=true;fx.ElementalistImpact(type,unit);return;}
            // Allocate landing base, identity silhouette and contact flash BEFORE repeated ornaments.
            // Add's actual budget is still the last authority: mobile 10, reduced 7.
            if(type!=FilledVfxKind.Fire&&type!=FilledVfxKind.Ice)fx.Add(rupture,Vector3.up*.07f,new Vector3(fx.size*.58f,.8f,fx.size*.58f),Quaternion.identity,0,5,0,fx.size*.7f,"Landing base",true);
            Mesh main=type==FilledVfxKind.Sword?sword:type==FilledVfxKind.Lightning?(identityFork??lightning):type==FilledVfxKind.Arcane?arcane:type==FilledVfxKind.Ice?(icePrimary??crystal):type==FilledVfxKind.Fire?(firePrimary??flame):identityContract;
            int motion=type==FilledVfxKind.Sword?8:type==FilledVfxKind.Lightning?(identityFork!=null?17:9):type==FilledVfxKind.Arcane?10:type==FilledVfxKind.Ice?(icePrimary!=null?13:1):type==FilledVfxKind.Fire?(firePrimary!=null?14:2):(identityContract!=null?16:3);
            Vector3 dimensions=type==FilledVfxKind.Sword?new Vector3(Mathf.Min(1.2f,unit*.7f),Mathf.Min(priority==CombatVisualPriority.Finale?2.5f:1.85f,unit*1.55f),Mathf.Min(1.8f,unit*1.1f)):type==FilledVfxKind.Lightning?new Vector3(1.3f,2.2f,1.3f)*unit:Vector3.one*unit*1.65f;
            // The judgment blade rises above companions; keep its forged silhouette readable from the gameplay camera.
            if(monumentalSword&&type==FilledVfxKind.Sword)dimensions=new Vector3(1.4f,4.4f,1.95f);
            Vector3 primaryAt=type==FilledVfxKind.Sword&&CombatFx.Flat(at-hero.transform.position).sqrMagnitude<1.5f?Vector3.forward*Mathf.Min(.9f,fx.size*.3f):Vector3.zero;
            // Sculpted Sword has its actual tip at local Y=0 and its pommel at Y=1.3.
            fx.Add(main,primaryAt,dimensions,Quaternion.identity,0,motion,0,
                type==FilledVfxKind.Sword?unit*.55f:type==FilledVfxKind.Lightning?unit*.85f:type==FilledVfxKind.Summon?unit*1.8f+fx.size*.3f:unit*2f,"Primary "+type,true);
            if(type==FilledVfxKind.Fire||type==FilledVfxKind.Ice||type==FilledVfxKind.Lightning||type==FilledVfxKind.Arcane||type==FilledVfxKind.Summon)
            {
                Vector3 body=type==FilledVfxKind.Fire?new Vector3(1.6f,1.9f,1.6f):type==FilledVfxKind.Ice?new Vector3(1.3f,.85f,1.3f):type==FilledVfxKind.Lightning?new Vector3(.7f,2.3f,.7f):new Vector3(.65f,1.3f,.65f);
                fx.Add(energyCore,Vector3.up*.08f,body*unit,Quaternion.identity,0,18,2,unit*1.3f,"Luminous elemental body",true);
            }
            if(type!=FilledVfxKind.Ice)fx.Add(rupture,Vector3.up*.11f,new Vector3(fx.size*.3f,.65f,fx.size*.3f),Quaternion.Euler(0,65,0),0,5,1,fx.size*.38f,"Contact flash",true);
            if(priority==CombatVisualPriority.Finale)
                fx.Add(rupture,Vector3.up*.13f,new Vector3(fx.size*.2f,.35f,fx.size*.2f),Quaternion.identity,.12f,5,0,fx.size*.25f,"Finale short tail",true);
            int n=type==FilledVfxKind.Ice?4:EffectPreferences.ReducedEffects?5:type==FilledVfxKind.Summon?6:10;
            for(int i=0;i<n;i++)
            {
                float angle=i*2.399963f,r=(.18f+(i%3)*.24f)*fx.size;Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                // Replace two repeated rubble slots with curved forged blade
                // bodies. They use the same budget and never claim another hit.
                if(type==FilledVfxKind.Sword&&i<2)
                    fx.Add(crescent,new Vector3(0,.5f+i*.12f,0),new Vector3(fx.size*.54f,.22f,fx.size*.54f),
                        Quaternion.Euler(i==0?8:-11,i*180,0),.035f+i*.045f,6,i,fx.size*.55f,"Tempered sword impact curl");
                else if(type==FilledVfxKind.Ice)
                    fx.Add(crystal,radial*r,new Vector3(.32f,1.1f+(i%3)*.45f,.37f)*unit,Quaternion.Euler(radial.z*23,angle*Mathf.Rad2Deg,-radial.x*23),i*.016f,1,angle,unit*1.2f);
                else if(type==FilledVfxKind.Fire)
                    fx.Add(flame,radial*r*.55f,new Vector3(.95f,1.6f+(i%3)*.3f,.95f)*unit,Quaternion.Euler(radial.z*17,i*51,-radial.x*17),i*.012f,22,angle,unit*1.8f,"Grounded fire tongue",true);
                else if(type==FilledVfxKind.Summon)
                    fx.Add(identityContract,radial*fx.size*.23f+Vector3.up*.25f,new Vector3(.38f,.38f,.38f)*fx.size,Quaternion.Euler(0,angle*Mathf.Rad2Deg,0),i*.024f,25,angle,fx.size*.8f,"Emerging spirit animal");
                else
                    fx.Add(type==FilledVfxKind.Sword?rupture:type==FilledVfxKind.Arcane?arcaneShard:main,radial*r+Vector3.up*.16f,Vector3.one*unit*.35f,Quaternion.Euler(0,i*51,0),(type==FilledVfxKind.Arcane?.34f:.08f)+i*.018f,type==FilledVfxKind.Sword?5:type==FilledVfxKind.Arcane?3:motion,angle,unit*.6f+(type==FilledVfxKind.Arcane?fx.size*.3f:0));
            }
        }
        // Solid silhouettes precede secondary pieces even on the seven-piece reduced tier.
        // All erupting bodies are clipped at construction; animation only contracts XZ.
        private void ElementalistImpact(FilledVfxKind type,float unit)
        {
            if(type!=FilledVfxKind.Fire&&type!=FilledVfxKind.Ice)Add(rupture,Vector3.up*.045f,new Vector3(size*.72f,.32f,size*.72f),Quaternion.identity,0,5,0,size*.8f,"Elemental landing fracture",true);
            Mesh body=type==FilledVfxKind.Ice?(icePrimary??crystal):type==FilledVfxKind.Fire?(firePrimary??flame):(identityFork??lightning);
            int motion=type==FilledVfxKind.Ice?21:type==FilledVfxKind.Fire?22:23;
            Vector3 dimensions=type==FilledVfxKind.Ice?new Vector3(1.45f,2.1f,1.45f):type==FilledVfxKind.Fire?new Vector3(1.35f,2.4f,1.35f):new Vector3(1.4f,3.1f,1.4f);
            Add(body,Vector3.zero,dimensions*unit,Quaternion.Euler(0,23,0),0,motion,0,unit*2,"Sculpted elemental primary",true);
            // Narrow interior light leaves the outer silhouette and shaded sides exposed.
            if(type!=FilledVfxKind.Fire)Add(energyCore,Vector3.up*.08f,new Vector3(.38f,1.4f,.38f)*unit,Quaternion.identity,0,18,1,unit*.4f,"Elemental inner light",true);
            for(int i=0;i<4;i++)
            {
                float angle=(i*137.5f+23)*Mathf.Deg2Rad;
                Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                Vector3 at=radial*size*(type==FilledVfxKind.Ice?.48f:type==FilledVfxKind.Fire?.14f+.025f*i:.3f);
                Vector3 scale=type==FilledVfxKind.Ice?new Vector3(.48f,1.8f+(i%2)*.5f,.52f):type==FilledVfxKind.Fire?new Vector3(.7f,1.65f+(i%2)*.5f,.7f):new Vector3(.52f,1.5f,.52f);
                Quaternion tilt=Quaternion.Euler(radial.z*(type==FilledVfxKind.Ice?24:18),i*90,-radial.x*(type==FilledVfxKind.Ice?24:18));
                Add(type==FilledVfxKind.Ice?crystal:type==FilledVfxKind.Fire?flame:lightning,at,scale*unit,tilt,.035f+i*.025f,motion,i+1,unit*1.6f,"Elemental crown",true);
            }
            // Extra quality tiers add fragments, not another luminous shell over the body.
            for(int i=0;i<(type==FilledVfxKind.Fire?0:3);i++)
            {
                float angle=i*2.399963f;
                Add(type==FilledVfxKind.Ice?crystal:arcaneShard,new Vector3(Mathf.Cos(angle),.3f,Mathf.Sin(angle))*size*.38f,
                    new Vector3(.12f,.38f,.16f)*unit,Quaternion.Euler(35,i*67,20),.18f+i*.025f,3,angle,unit*.6f+size*.3f,"Elemental fragments");
            }
        }
        internal static void TrapRelease(PlayerController hero,Vector3 at,float radius,Color color)
        {
            if(hero.HeroClass==HeroClass.Ranger)radius*=1.2f;
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.Thrust,radius,color,.55f,priority:CombatVisualPriority.RealContact);if(fx==null)return;
            fx.sculpted=true;
            fx.Add(rupture,Vector3.up*.045f,new Vector3(radius*.75f,.3f,radius*.75f),Quaternion.identity,0,5,0,radius,"Trap pressure crown",true);
            for(int i=0;i<6;i++)
            {
                float angle=i*Mathf.PI/3;
                fx.Add(crystal,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius*.45f,
                    new Vector3(.18f,.45f,.18f),Quaternion.Euler(25,i*60,18),i*.012f,24,i,radius*.2f,"Trap release splinter",true);
            }
        }
        internal static ArrowBatchHandle BeginArrowBatch(PlayerController hero,Vector3 at,float radius,Color color,bool final=false,CombatVisualPriority priority=CombatVisualPriority.ActionBody,int castId=0,System.Func<bool> actionActive=null)
        {
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.ArrowRain,radius,color,12,priority:final?CombatVisualPriority.Finale:priority,actionActive:actionActive);
            if(fx!=null){fx.finaleCast=castId;fx.kind=FilledVfxKind.Charge;}
            return new ArrowBatchHandle(fx);
        }
        internal static ArrowBatchHandle ArrowRain(PlayerController hero,Vector3 at,float radius,Color color,bool final=false,CombatVisualPriority priority=CombatVisualPriority.ActionBody,int castId=0,System.Func<bool> actionActive=null)
        {
            // Immediate rain has no charge phase: avoid building and clipping a discarded envelope.
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.ArrowRain,radius,color,final?1.65f:1.1f,priority:final?CombatVisualPriority.Finale:priority,actionActive:actionActive);
            if(fx!=null){fx.finaleCast=castId;fx.ArrowBeat(at,radius,final);}
            return new ArrowBatchHandle(fx);
        }
        private void ArrowBeat(Vector3 at,float radius,bool final)
        {
            if(!gameObject.activeInHierarchy||owner==null||owner.IsDead||owner.CombatEpoch!=epoch)return;
            if(final)actionActive=null; // The final contact owns only its finite aftermath.
            if(owner.HeroClass==HeroClass.Ranger)radius*=1.2f;
            at.y=WorldTraversal.SurfaceHeight(at,.04f);float nextSize=Mathf.Clamp(radius,.15f,16);
            bool reuse=arrowGeometryReady&&arrowGeometryFinal==final&&arrowGeometryReduced==EffectPreferences.ReducedEffects&&
                arrowGeometryRevision==WorldTraversal.Revision&&(transform.position-at).sqrMagnitude==0&&size==nextSize;
            if(reuse)
            {
                age=0;life=final?1.65f:1.1f;
                for(int i=0;i<count;i++)Animate(pieces[i]);
                return;
            }
            ClearPieces();sculpted=true;kind=FilledVfxKind.ArrowRain;transform.position=at;age=0;life=final?1.65f:1.1f;size=nextSize;
            if(final){RegisterFinale(finaleCast);if(lease!=null)lease.Promote(CombatVisualPriority.Finale);}
            Add(rupture,Vector3.up*.08f,Vector3.one*(final?size*.65f:.3f),Quaternion.identity,0,5,0,final?size:.4f,"Arrow landing contact",true);
            float shaft=final?2.4f:1.05f;
            Add(arrow,Vector3.up*(shaft*1.7f-.035f),new Vector3(final?2.1f:1.55f,shaft,final?4.3f:3.4f),Quaternion.Euler(180,0,0),0,12,0,.3f,"Arrow embedded on the actual hit",true);
            Add(arcaneShard,Vector3.up*.1f,Vector3.one*.32f,Quaternion.identity,0,4,0,final?3.5f:2.3f,"Arrow impact fragments");
            bool ultimate=finaleCast>0;
            if(ultimate&&final)
            {
                Add(rupture,Vector3.up*.13f,Vector3.one*(size*.8f),Quaternion.Euler(0,45,0),.08f,5,0,size,"Ultimate expanding crown",true);
            }
            int maximum=ultimate?(EffectPreferences.ReducedEffects?5:10):(EffectPreferences.ReducedEffects?3:4);
            for(int i=0;i<maximum;i++)
            {
                float a=i*2.39996f,r=size*(ultimate?(.25f+(i%4)*.18f):(.25f+(i%3)*.25f));float shaftHeight=ultimate?.9f:.7f+(i%2)*.16f;
                var offset=new Vector3(Mathf.Cos(a)*r,shaftHeight*1.7f-.03f,Mathf.Sin(a)*r);
                offset.y+=WorldTraversal.SurfaceHeight(transform.position+new Vector3(offset.x,0,offset.z),.04f)-transform.position.y;
                Add(arrow,offset,new Vector3(ultimate?1.6f:1.4f,shaftHeight,ultimate?4f:3.2f),Quaternion.Euler(180,i*47,0),0,12,0,.25f,"Short embedded arrow");
            }
            arrowGeometryReady=true;arrowGeometryFinal=final;arrowGeometryReduced=EffectPreferences.ReducedEffects;arrowGeometryRevision=WorldTraversal.Revision;
        }
        internal static void PoisonVines(PlayerController hero,Vector3 at,float radius,Color color)
        {
            if(hero.HeroClass==HeroClass.Ranger)radius*=1.2f;
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.Vine,radius,color,1.4f,priority:CombatVisualPriority.SustainedBackground);if(fx==null)return;
            fx.sculpted=true;
            fx.flowVolume=new SkillFlowVolume(fx.transform,SkillFlowVolume.Kind.Poison,radius,hero.HeroClass==HeroClass.Ranger?1.6f:1.1f,color);
            GroundSkillSurface.Ensure(fx.transform,hero,at,radius,GroundSkillSurface.Deposit.WetPoison,()=>fx.life-fx.age);
            int roots=EffectPreferences.ReducedEffects?2:3;float rootScale=Mathf.Min(hero.HeroClass==HeroClass.Ranger?1.4f:1f,radius*.45f);
            for(int i=0;i<roots;i++)
            {
                float angle=i*2.399963f+.35f;
                fx.Add(PoisonRoot,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius*.42f,
                    Vector3.one*rootScale,Quaternion.Euler(0,i*137,0),i*.03f,24,i,radius*.3f,"Thick poison root",true);
            }
        }
        public static void Charge(Transform parent,PlayerController hero,Vector3 at,float radius,Color color,float duration,int identity=0,bool protectionEnvelope=false,int protectionStyle=0)
        {
            if(identity==0)return;
            var fx=Create(hero,at,hero.transform.forward,FilledVfxKind.Charge,Mathf.Min(radius,3.5f),color,duration,parent,CombatVisualPriority.SustainedBackground);if(fx==null)return;
            Mesh authored=identity==1?identityBlade:identity==2?identityFork:identity==3?identityContract:identity==4?identityProtection:null;
            if(authored!=null)
            {
                // Persistent protection encloses the body, independent of gameplay radius.
                // Ordinary charge/contact envelopes keep their original dimensions.
                float width=fx.size*.55f,height=width;
                if(protectionEnvelope&&identity==4){width=1.45f;height=1.4f;}
                fx.Add(authored,Vector3.up*.08f,new Vector3(width,height,width),Quaternion.identity,0,protectionEnvelope&&identity==4?20:18,protectionEnvelope&&identity==4?Mathf.Clamp(protectionStyle,0,3):identity,
                    protectionEnvelope&&identity==4?Mathf.Max(fx.size*.8f,width*.7f):fx.size*.8f,"Identity preparation",true);return;
            }
            for(int i=0;i<3;i++)fx.Add(sword,new Vector3((i-1)*.25f,.2f,.25f),new Vector3(.4f,.9f,.5f),Quaternion.Euler(24,i*35,0),0,19,i);
        }
        // Only called after the real health mutation. No unconditional success flash at full HP.
        internal static void HealingPulse(PlayerController hero,Color color,Transform recipient=null)
        {
            if(hero==null)return;
            if(recipient==null&&hero.HeroClass==HeroClass.Vanguard&&SkillLifeBanner.Pulse(hero))return;
            if(recipient==null){Charge(hero.transform,hero,hero.transform.position,2.6f,color,.32f,4,true,3);return;}
            // Companion success is a short low contact accent at the actual recipient,
            // not a player-sized shield or a claim that the caster was healed.
            var fx=Create(hero,recipient.position,Vector3.forward,FilledVfxKind.Charge,.8f,color,.32f,recipient,CombatVisualPriority.RealContact);
            if(fx!=null)fx.Add(identityProtection??crescent,Vector3.up*.06f,new Vector3(.8f,.6f,.8f),Quaternion.identity,0,20,3,.8f,"Companion actual healing",true);
        }
        // Visual endpoints consume the actual caller's release/hit position; no target search or damage lives here.
        internal static bool IdentityContact(PlayerController hero,Vector3 at,Vector3 forward,float radius,Color color,int identity,CombatVisualPriority priority=CombatVisualPriority.ActionBody)
        {
            EnsureAssets();Mesh mesh=identity==2?identityFork:identity==3?identityContract:identity==4?identityProtection:identityBlade;if(mesh==null)return false;
            var fx=Create(hero,at,forward,identity==2?FilledVfxKind.Lightning:FilledVfxKind.Summon,radius,color,.6f,priority:priority);if(fx==null)return true;
            fx.sculpted=true;float unit=Mathf.Min(1.6f,radius);fx.Add(mesh,Vector3.up*.07f,new Vector3(1.1f,identity==2?1.6f:1.1f,1.1f)*unit,Quaternion.identity,0,identity==2?23:identity==3?16:19,0,unit,"Identity actual event",true);
            if(identity==3)
            {
                fx.flowVolume=new SkillFlowVolume(fx.transform,SkillFlowVolume.Kind.Spirit,radius,1.4f,color);
            }
            return true;
        }
        internal static void ContractArrival(PlayerController hero,Vector3 at,bool ember,Color tint)
        {
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.Summon,1.8f,tint,1.1f,priority:CombatVisualPriority.ActionBody);
            if(fx!=null)fx.flowVolume=new SkillFlowVolume(fx.transform,ember?SkillFlowVolume.Kind.Fire:SkillFlowVolume.Kind.Spirit,1.8f,1.8f,tint);
        }
        public static void Thrust(PlayerController hero,Vector3 start,Vector3 end,Color color,float lifetime,float width,CombatVisualPriority priority=CombatVisualPriority.ActionBody)
        {
            if(!Finite(end.x)||!Finite(end.y)||!Finite(end.z)||!Finite(width)||width<=0)return;
            Vector3 delta=end-start;if(delta.sqrMagnitude<.001f)return;
            var fx=Create(hero,start,Vector3.forward,FilledVfxKind.Thrust,1,color,Mathf.Min(lifetime,.7f),priority:priority);if(fx==null)return;
            fx.sculpted=true;
            Mesh body=hero.HeroClass==HeroClass.Ranger?arrow:hero.HeroClass==HeroClass.Arcanist?crystal:sword;
            float length=hero.HeroClass==HeroClass.Ranger?1.1f:1.4f;
            Vector3 direction=delta.normalized;
            fx.Add(body,delta-direction*.22f,new Vector3(Mathf.Max(.22f,width*2),length,Mathf.Max(.22f,width*2)),
                Quaternion.FromToRotation(Vector3.up,direction),0,19,0,0,"Solid thrust contact");
            for(int i=0;i<3;i++)fx.Add(arcaneShard,delta,new Vector3(.09f,.1f,.12f),Quaternion.Euler(i*39,i*67,23),0,4,i,0,"Contact chips");
        }
        internal static void BurnContact(PlayerController hero,Vector3 at,bool finale)
        {
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.Fire,.4f,new Color(1,.56f,.16f),.32f,
                priority:CombatVisualPriority.RealContact);
            if(fx==null)return;
            fx.sculpted=true;
            GroundSkillSurface.Ensure(fx.transform,hero,at,.4f,GroundSkillSurface.Deposit.Scorch,()=>fx.life-fx.age);
            fx.flowVolume=new SkillFlowVolume(fx.transform,SkillFlowVolume.Kind.Fire,.4f,.65f,new Color(1,.4f,.05f));
        }
        internal static ContactTrackHandle ContactTrack(PlayerController hero,Vector3 at,float radius,System.Func<float> remaining)
        {
            if(remaining==null)return default(ContactTrackHandle);
            float left=remaining();if(!Finite(left)||left<=0)return default(ContactTrackHandle);
            at.y=WorldTraversal.SurfaceHeight(at,.015f);
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.Fire,radius,new Color(1,.46f,.095f),left,
                priority:CombatVisualPriority.RealContact,essentialTrack:true);
            if(fx==null)return default(ContactTrackHandle);
            fx.gameObject.name="Riding flame / actual ground footprint";
            fx.contactRemaining=remaining;fx.trackRemaining=left;fx.sculpted=true;
            GroundSkillSurface.Ensure(fx.transform,hero,at,radius,GroundSkillSurface.Deposit.Scorch,
                ()=>fx.contactTrack?fx.trackRemaining:0,independentLifetime:true);
            fx.flowVolume=new SkillFlowVolume(fx.transform,SkillFlowVolume.Kind.Fire,radius,.65f,new Color(1,.4f,.05f));
            return new ContactTrackHandle(fx);
        }
        private void Add(Mesh mesh,Vector3 at,Vector3 dimensions,Quaternion rotation,float delay,int motion,float phase,float footprint=0,string label="Repeated ornament",bool anchored=false)
        {
            if(mesh==null)return;
            int bodySurface=kind==FilledVfxKind.Vine&&mesh==EnemySkillPhysicalVfx.GelMesh?5:mesh==poisonRoot?6:mesh==sword||mesh==arrow||mesh==crescent||mesh==identityBlade||mesh==identityProtection?1:
                mesh==crystal||mesh==icePrimary||mesh==arcane?2:mesh==vine?3:mesh==identityContract?4:
                mesh==rupture||mesh==arcaneShard?0:-1;
            bool secondary=label=="Repeated ornament"&&((kind==FilledVfxKind.Ice&&icePrimary!=null)||(kind==FilledVfxKind.Fire&&firePrimary!=null));
            if(secondary)dimensions*=.6f; // Keep authored primary readable; footprint reservation stays conservative.
            int cap=EffectPreferences.ReducedEffects?FilledVfxRecipes.ReducedParts:Application.isMobilePlatform?10:FilledVfxRecipes.MaximumParts;
            if(count>=cap)return;
            float fit=1;
            bool attached=anchored&&(kind==FilledVfxKind.Fire||kind==FilledVfxKind.Vine);
            if(attached&&!GroundSkillSurface.TryPlaceAttached(transform,mesh,ref at,ref dimensions,rotation,size,1.15f))return;
            if(footprint>0&&!anchored)
            {
                float px,pz;
                if(!FilledVfxPlacement.TryPlace(at.x,at.z,size,footprint,(x,z,r)=>{
                    Vector3 candidate=transform.position+new Vector3(x,0,z);
                    // The center ray must be damage-visible; only the destination needs a full disk.
                    return CombatSight.Area(transform.position,candidate)&&CombatSight.VisualFootprint(candidate,candidate,r);
                },out px,out pz,out fit))return;
                at.x=px;at.z=pz;dimensions*=fit;
            }
            Mesh owned=null;
            if(anchored)
            {
                owned=AnchoredImpactMesh.Create(mesh,transform,at,dimensions*1.15f,rotation);
                if(attached)
                {
                    // Contract around the fitted dry contact, not the area center:
                    // a river-centered field must not drag its bank roots into water.
                    Vector3 contact=transform.TransformPoint(at);contact.y=WorldTraversal.SurfaceHeight(contact,.015f);at=transform.InverseTransformPoint(contact);
                    var vertices=owned.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]-=at;owned.vertices=vertices;owned.RecalculateBounds();
                }
                else at=Vector3.zero;
                mesh=owned;dimensions=Vector3.one/1.15f;rotation=Quaternion.identity;
            }
            Piece piece=pieces[count];
            if(piece==null){var obj=new GameObject();obj.transform.SetParent(transform,false);piece=new Piece{Transform=obj.transform,Filter=obj.AddComponent<MeshFilter>(),Renderer=obj.AddComponent<MeshRenderer>()};pieces[count]=piece;allocated=Mathf.Max(allocated,count+1);}
            var renderer=piece.Renderer;piece.Transform.gameObject.name="Filled spell surface / "+label;piece.Filter.sharedMesh=mesh;renderer.sharedMaterial=bodySurface>=0?bodyMaterial:sharedMaterial;MobileSceneDepth.Track(renderer);renderer.enabled=true;
            renderer.shadowCastingMode=bodySurface>=0&&!EffectPreferences.ReducedEffects?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=bodySurface>=0;renderer.sortingOrder=10;
            piece.BodySurface=bodySurface;piece.SimulatedAge=0;piece.WorldPosition=transform.TransformPoint(at);
            Vector3 radial=new Vector3(Mathf.Cos(phase),0,Mathf.Sin(phase));piece.Velocity=transform.TransformDirection(radial*(1.1f+(phase%3)*.35f)+Vector3.up*(2.1f+(phase%2)*.5f));piece.Spin=new Vector3(73+phase*13,91+phase*11,59);
            piece.Position=at;piece.Scale=dimensions;piece.Rotation=rotation;piece.Delay=delay;piece.Motion=motion;piece.Phase=phase;piece.TravelScale=footprint>0?fit:1;piece.Anchored=anchored;piece.Secondary=secondary;piece.OwnedMesh=owned;count++;
            Animate(piece); // The mesh exists at the actual hit frame, before the next Update.
        }
        private void Update()
        {
            var game=GameSession.Instance;
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||game==null||game!=session||game.Player!=owner||!game.HasStarted){Retire();return;}
            if(settling&&game.CombatEffectsEnded){Retire();return;}
            if(game.CombatEffectsEnded)
            {
                if(!confirmedFinale){Retire();return;}
                terminalAge+=Time.unscaledDeltaTime;age+=Time.unscaledDeltaTime;
                if(terminalAge>=.65f||age>=life){Retire();return;}
                flowVolume?.Draw(age,Mathf.SmoothStep(0,1,Mathf.Min(.65f-terminalAge,life-age)/.65f));
                for(int i=0;i<count;i++)Animate(pieces[i]);
                return; // Visual-only: no scheduled combat or reward callbacks.
            }
            if(game.InputBlocked||Time.deltaTime<=0)return;
            if(contactTrack)
            {
                trackRemaining=contactRemaining!=null?contactRemaining():0;
                if(!Finite(trackRemaining)||trackRemaining<=0){Retire();return;}
                age+=Time.deltaTime;
            }
            else
            {
                age+=Time.deltaTime;
                if(age>=life)
                {
                    if(actionActive!=null&&actionActive())
                    {
                        // Let this contact fade normally while retaining the batch
                        // for the next scheduled pulse, including frame catch-up.
                        for(int i=0;i<count;i++)pieces[i].Transform.gameObject.SetActive(false);
                        return;
                    }
                    Retire();return;
                }
            }
            flowVolume?.Draw(age,Mathf.Min(Mathf.SmoothStep(0,1,age/.12f),Mathf.SmoothStep(0,1,(contactTrack?trackRemaining:life-age)/Mathf.Min(.65f,life*.5f))));
            for(int i=0;i<count;i++)Animate(pieces[i]);
        }
        internal void CompleteAftermath(bool finale=false)
        {
            // Preserve the last contact and its pool rental; no new contact is
            // emitted and no producer callback can revive a completed damage slot.
            if(pooled||disposing||contactTrack||settling)return;
            settling=true;actionActive=null;
            transform.SetParent(null,true);
            life=Mathf.Max(life,age+SkillVisualAftermathTiming.Tail(finale));
        }
        private void Animate(Piece p)
        {
            float local=age-p.Delay;if(local<0){p.Transform.gameObject.SetActive(false);return;}
            p.Transform.gameObject.SetActive(true);var f=FilledVfxRecipes.Sample(kind,local,Mathf.Max(.1f,life-p.Delay));
            Vector3 at=p.Position,scale=p.Scale;Quaternion rotation=p.Rotation;
            float t=contactTrack?0:f.Progress;
            switch(p.Motion)
            {
                // Authored primary visual beats only: visible at release, quick rise, readable hold, then contraction.
                // Radial scale only contracts; anchored certification and actual hit time remain unchanged.
                case 15: float sliceHand=p.Phase<0?-1:1;rotation*=Quaternion.Euler(0,Mathf.Lerp(-12,42,t)*sliceHand,0);scale*=1-t*.15f;break;
                case 16: scale.y*=.82f+.18f*Mathf.SmoothStep(0,1,local/.08f);break;
                case 17: scale.y*=1-Mathf.Clamp01(local/.45f)*.25f;break;
                // Persistent body protection must not grow through head ornaments at birth.
                case 20: break;
                case 21: // Ice erupts, holds its facets, then fractures back into the floor.
                    float iceHold=life>2.1f?life-.85f:.48f;
                    scale.y*=local<.13f?Mathf.Lerp(.18f,1,Mathf.SmoothStep(0,1,local/.13f)):local<iceHold?1:Mathf.Lerp(1,.12f,Mathf.SmoothStep(0,1,(local-iceHold)/.65f));
                    scale.x*=1-Mathf.Clamp01((local-.5f)/.5f)*.28f;scale.z=scale.x;break;
                case 22: // Staggered tongues rise, fold inward and cool separately.
                    float fireHold=life>2.1f?life-.85f:.2f;
                    scale.y*=local<.12f?Mathf.Lerp(.28f,1,Mathf.SmoothStep(0,1,local/.12f)):1-Mathf.SmoothStep(0,1,(local-fireHold)/.65f)*.62f;
                    scale.x*=1-Mathf.Clamp01(local/.8f)*.38f;scale.z*=1-Mathf.Clamp01(local/.8f)*.38f;break;
                case 23: // A full branching strike on contact, followed by a short re-strike.
                    scale.y*=local<.1f?1:local<.2f?.7f:local<.28f?.95f:1-Mathf.Clamp01((local-.28f)/.6f)*.7f;
                    scale.x*=1-Mathf.Clamp01(local/.8f)*.35f;scale.z*=1-Mathf.Clamp01(local/.8f)*.35f;break;
                case 24: // Roots emerge vertically without expanding their certified footprint.
                    scale.y*=local<.18f?Mathf.Lerp(.2f,1,Mathf.SmoothStep(0,1,local/.18f)):1-Mathf.SmoothStep(0,1,(local-.65f)/.4f)*.6f;break;
                case 28: // Fast expanding, rising air; the stone contact outlives it.
                    float gust=Mathf.Clamp01(local/.65f);
                    at.x*=.5f+.5f*gust;at.z*=.5f+.5f*gust;at.y+=gust*.65f;
                    scale*=.5f+.5f*gust;rotation*=Quaternion.Euler(0,gust*32,0);break;
                case 27: scale.y*=.94f+.06f*Mathf.Sin(local*5+p.Phase);break; // Rooted fuel flickers below the mount.
                case 26: break; // Wet soil rests on the floor throughout the actual contact pulse.
                case 25: // Spirited creatures emerge with a continuous upward/outward impulse.
                    float drift=.55f*Mathf.SmoothStep(0,1,local/.55f);at+=new Vector3(Mathf.Cos(p.Phase),0,Mathf.Sin(p.Phase))*drift*size*.3f*p.TravelScale;
                    at.y+=.65f*(1-Mathf.Exp(-drift*3));rotation*=Quaternion.Euler(-drift*12,drift*32,0);break;
                case 18: scale.y*=.65f+.35f*Mathf.Min(1,local/.18f);break;
                case 19: scale.y*=.82f+.18f*Mathf.Min(1,local/.1f);break;
                case 13: scale.y*=local<.09f?Mathf.Lerp(.45f,1,local/.09f):local<.42f?1:Mathf.Lerp(1,.62f,Mathf.Clamp01((local-.42f)/.5f));break;
                case 14: scale.y*=local<.07f?Mathf.Lerp(.55f,1,local/.07f):local<.23f?1:Mathf.Lerp(1,.48f,Mathf.Clamp01((local-.23f)/.5f));scale.x*=1-Mathf.Clamp01((local-.23f)/.5f)*.24f;scale.z*=1-Mathf.Clamp01((local-.23f)/.5f)*.24f;break;
                case 0: float handed=p.Phase<0?-1:1;scale*=.85f+t*.3f;rotation*=Quaternion.Euler(0,Mathf.Lerp(-18,38,t)*handed,-t*11*handed);break;
                case 1: scale.y*=.25f+.75f*Mathf.Min(1,local*18);at.y-=Mathf.Max(0,t-.55f)*1.3f;break;
                case 2: scale*=f.Expansion;scale.y*=1+t*.55f;at.y+=t*.65f;rotation*=Quaternion.Euler(0,t*45,0);break;
                case 3:
                case 4:
                    float pending=Mathf.Max(0,local-p.SimulatedAge);
                    // Same gravity, swept ground and wall response as all physical debris.
                    PhysicalSkillMotion.Advance(ref p.WorldPosition,ref p.Velocity,Mathf.Max(.035f,p.Scale.magnitude*.1f),pending,.26f,.58f);
                    p.SimulatedAge=local;at=transform.InverseTransformPoint(p.WorldPosition);
                    rotation*=Quaternion.Euler(p.Spin*local);break;
                case 5: scale*=f.Expansion;rotation*=Quaternion.Euler(0,t*45,0);break;
                case 6: scale*=.8f+Mathf.Sin(t*Mathf.PI)*.2f;rotation*=Quaternion.Euler(0,local*(p.Phase%2==0?95:-80),0);at.y+=Mathf.Sin(local*3+p.Phase)*.06f;break;
                case 7: scale.x*=1-t*.75f;scale.z*=1-t*.75f;break;
                // The tip is already on the real landing frame; settle does not fake a delayed hit.
                case 8: at.y-=Mathf.Min(.12f,t*.5f)*p.TravelScale;scale.y*=1-Mathf.Max(0,t-.55f)*.3f;break;
                case 9: scale.x*=t<.12f?1:t<.28f?.78f:1-t*.5f;scale.z*=1-t*.4f;break;
                case 11: break; // Embedded shafts keep their length after contact.
                case 12: break;
                case 10: scale*=t<.18f?Mathf.Lerp(1,.58f,t/.18f):t<.42f?Mathf.Lerp(.58f,1.08f,(t-.18f)/.24f):1.08f-(t-.42f)*.55f;rotation*=Quaternion.Euler(t*30,t*55,0);break;
            }
            if(p.Anchored){at=p.Position;rotation=p.Rotation;float radial=Mathf.Min(1,Mathf.Min(scale.x,scale.z));scale.x=scale.z=radial;} // Uniform radial contraction preserves each certified LOS ray; anisotropic scaling does not.
            p.Transform.localPosition=at;p.Transform.localScale=scale;p.Transform.localRotation=rotation;
            // Placement reserved the complete motion footprint once. Do not toggle a whole
            // primary silhouette each frame when a growing bounds circle grazes a wall.
            Color color=tint;if(p.Motion==17)color.a*=local<.07f?1:local<.14f?.48f:local<.22f?.82f:.55f;if(p.Secondary)color.a*=.55f*Mathf.Clamp01((.62f-local)/.2f);color.a*=(kind==FilledVfxKind.Charge?.35f:.9f)*Mathf.Lerp(.55f,1,EffectPreferences.EffectsScale);
            if(sculpted&&p.Motion==18)color.a*=.3f;
            if(p.Motion==23)color.a*=local<.09f?1:local<.18f?.3f:local<.27f?.95f:.5f;
            float opacity=contactTrack?Mathf.Min(.35f+.65f*Mathf.Clamp01(age/.12f),Mathf.Clamp01(trackRemaining/.24f)):f.Opacity;
            if(p.Motion==28)opacity*=Mathf.Clamp01((.75f-local)/.25f);
            if(p.Motion==20)
            {
                // Protective armor remains fitted to the shoulders and chest. State
                // pulses alter surface light rather than expanding through the actor.
                float intensity=p.Phase<.5f?.30f:p.Phase<1.5f?(local<.28f?Mathf.Lerp(.62f,.20f,local/.28f):.20f):p.Phase<2.5f?.18f:.58f*Mathf.Clamp01(1-local/.32f);
                color=tint;color.a*=intensity*Mathf.Lerp(.55f,1,EffectPreferences.EffectsScale);
                opacity=p.Phase<2.5f?1:Mathf.Clamp01(1-local/.32f);
            }
            if(p.BodySurface>=0)
            {
                Color bodyTint=p.BodySurface==0?new Color(.42f,.34f,.25f):p.BodySurface==1?Color.Lerp(new Color(.38f,.42f,.48f),tint,owner!=null&&owner.HeroClass==HeroClass.Vanguard?.80f:.28f):p.BodySurface==5?new Color(.24f,.29f,.12f):p.BodySurface==6?new Color(.32f,.37f,.14f):tint;
                block.SetColor("_Tint",bodyTint);block.SetFloat("_Surface",p.BodySurface);block.SetFloat("_Phase",local);
                block.SetFloat("_Opacity",SkillVisualAftermathTiming.Coverage(life-age));block.SetFloat("_Heat",p.Motion==20?(p.Phase<2.5f?.14f:Mathf.Clamp01(1-local/.32f)*.5f):Mathf.Clamp01(1-local/.18f)*.3f);
                bool forged=p.BodySurface==1&&kind!=FilledVfxKind.ArrowRain;
                block.SetFloat("_Engraving",forged?1:0);block.SetFloat("_CoreLight",forged?.18f+.16f*(float)System.Math.Exp(-local*.9f):0);
                p.Renderer.SetPropertyBlock(block);return;
            }
            block.SetFloat("_Sculpted",sculpted&&p.Motion!=18?1:0);
            block.SetFloat("_Element",kind==FilledVfxKind.Fire?1:kind==FilledVfxKind.Ice?2:kind==FilledVfxKind.Lightning?3:kind==FilledVfxKind.Vine?4:kind==FilledVfxKind.Summon||kind==FilledVfxKind.Arcane?5:0);
            block.SetFloat("_Seed",p.Phase*.37f+p.Delay*3);
            block.SetFloat("_ImpactLight",contactTrack||p.Secondary||p.Motion==20?0:Mathf.Clamp01(1-local/.16f)*EffectPreferences.EffectsScale);
            block.SetColor("_Color",color);block.SetFloat("_Opacity",opacity);block.SetFloat("_Progress",t);
            block.SetFloat("_EnvelopeMode",p.Motion==20?p.Phase+1:0);block.SetFloat("_EnvelopeAge",local);
            block.SetFloat("_Style",contactTrack?0:kind==FilledVfxKind.Fire||kind==FilledVfxKind.Summon?1:.35f);p.Renderer.SetPropertyBlock(block);
        }
        private void Register(){if(registered)return;registered=true;active++;}
        private void Release(){if(!registered)return;registered=false;active=Mathf.Max(0,active-1);}
        private void OnDisable(){flowVolume?.Dispose();flowVolume=null;foreach(var ground in GetComponentsInChildren<GroundSkillSurface>(true))ground.Retire();finales.Remove(this);Release();}
        private void OnEnable()
        {
            if(!gameObject.activeInHierarchy||owner==null||registered)return;
            Register();
        }
        internal void Retire()
        {
            if(pooled||disposing)return;
            pooled=true;gameObject.SetActive(false);finales.Remove(this);Release();
            var ticket=GetComponent<CombatVisualLease>();if(ticket!=null)ticket.Suspend();lease=null;
            ClearPieces();owner=null;session=null;settling=false;contactTrack=false;contactRemaining=null;actionActive=null;trackRemaining=0;epoch=0;age=life=size=terminalAge=0;kind=default(FilledVfxKind);tint=default(Color);finaleCast=0;confirmedFinale=false;
            transform.SetParent(null,false);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;transform.localScale=Vector3.one;
            if(idle.Count<PoolCapacity)idle.Push(this);else{disposing=true;Destroy(gameObject);}
        }
        private void ClearPieces()
        {
            arrowGeometryReady=false;
            EnsurePropertyBlock();
            block.SetColor("_Color",new Color(0,0,0,0));block.SetFloat("_Opacity",0);block.SetFloat("_Progress",0);block.SetFloat("_Style",0);block.SetFloat("_EnvelopeMode",0);block.SetFloat("_EnvelopeAge",0);
            for(int i=0;i<allocated;i++)
            {
                var p=pieces[i];if(p==null)continue;
                if(p.OwnedMesh!=null)Destroy(p.OwnedMesh);p.OwnedMesh=null;
                if(p.Transform!=null){p.Transform.gameObject.SetActive(false);p.Transform.localPosition=Vector3.zero;p.Transform.localScale=Vector3.one;p.Transform.localRotation=Quaternion.identity;}
                if(p.Filter!=null)p.Filter.sharedMesh=null;
                if(p.Renderer!=null){p.Renderer.SetPropertyBlock(block);p.Renderer.sharedMaterial=null;p.Renderer.enabled=false;}
                p.Position=Vector3.zero;p.Scale=Vector3.one;p.Rotation=Quaternion.identity;p.Delay=p.Phase=0;p.TravelScale=1;p.SimulatedAge=0;p.WorldPosition=p.Velocity=p.Spin=Vector3.zero;p.BodySurface=-1;p.Motion=0;p.Anchored=p.Secondary=false;
            }
            count=0;
        }
        private void OnDestroy(){disposing=true;instances.Remove(this);finales.Remove(this);Release();ClearPieces();owner=null;session=null;contactRemaining=null;actionActive=null;contactTrack=false;trackRemaining=0;}
        private static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
