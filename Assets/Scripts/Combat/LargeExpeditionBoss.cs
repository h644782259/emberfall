using UnityEngine;

namespace Emberfall
{
    // Opt-in only for the fifth large-expedition room. EnemyController drives Tick,
    // so mechanic ownership cannot race its ordinary charge/slam/volley update.
    public sealed class LargeExpeditionBoss : MonoBehaviour
    {
        public LargeBossPhaseState State { get; private set; }
        private EnemyController boss;
        private GameSession game;
        private readonly DestructibleProp[] anchors = new DestructibleProp[3];
        private GameObject anchorRoot, beamRoot;
        private WorldResources resources;
        private EnemySweepPhysicalVfx sweepVisual;
        private Vector3 phaseCenter;
        private float startAngle;
        private bool chapterConfigured;
        private int ownerEpoch,ownerRoom,ownerSeed;
        private PlayerController owner;
        private float sweepSign=1;
        // This is the player-center danger capsule, including existing body compensation.
        private float BeamDangerRadius => (LargeBossPhaseState.BeamHalfWidth+.4f)*boss.BodyScale;
        public float BeamWorldAngle {get{return startAngle+(State==null?0:State.BeamAngle*sweepSign);}}
        private int seed;
        private bool stopped;

        public static LargeExpeditionBoss Configure(EnemyController boss, int tier, int seed)
        {
            if (boss == null || !boss.IsBoss || boss.IsDead) return null;
            var existing = boss.GetComponent<LargeExpeditionBoss>();
            if (existing != null) return existing;
            var value = boss.gameObject.AddComponent<LargeExpeditionBoss>();
            value.boss = boss; value.game = GameSession.Instance; value.seed = seed;
            value.State = new LargeBossPhaseState();
            boss.ConfigureLargeExpedition(value);
            return value;
        }
        public static LargeExpeditionBoss ConfigureChapter(EnemyController boss,int tier,int seed)
        {
            var value=Configure(boss,tier,seed);
            if(value==null||value.chapterConfigured)return value;
            // Explicit chapter opt-in only, before the first threshold; legacy Configure stays unchanged.
            if(value.State.PhaseNumber!=0)return value;
            value.chapterConfigured=true;
            value.State=new LargeBossPhaseState(true,
                value.game!=null&&value.game.ActiveChapterNode==ChapterNode.StarPlatform);
            value.owner=value.game==null?null:value.game.Player;
            value.ownerEpoch=value.owner==null?-1:value.owner.CombatEpoch;
            value.ownerRoom=value.game==null?-1:value.game.ChapterRoomIndex;value.ownerSeed=value.game==null?0:value.game.ChapterSeed;
            return value;
        }
        internal bool Tick(float delta)
        {
            if (stopped || State == null) return false;
            if(game!=null&&game.RevivalPending)return State.OwnsAttacks;
            bool alive = boss != null && !boss.IsDead && boss.enabled && gameObject.activeInHierarchy &&
                game != null && game.HasStarted && game.Player != null && !game.IsDead && ChapterOwnerValid;
            if (!alive) { StopEncounter(); return false; }
            bool active = !game.InputBlocked;
            if (!active) { State.Advance(delta, false, true); return State.OwnsAttacks; }
            LargeBossPhase before = State.Phase;
            if (State.TryBegin(boss.Health / Mathf.Max(1, boss.MaxHealth), !boss.IsStunned))
            {
                BeginPhasePresentation();
            }
            ReconcileAnchors();
            State.Advance(delta,true,!boss.IsDead);
            if(before==LargeBossPhase.Recovery&&State.Phase==LargeBossPhase.Windup)BeginPhasePresentation();
            if (State.Phase == LargeBossPhase.Exposed && before != LargeBossPhase.Exposed)
                AnnounceExposure();
            bool beamVisible = State.Phase == LargeBossPhase.Windup || State.Phase == LargeBossPhase.Beam;
            if (beamRoot != null) beamRoot.SetActive(beamVisible);
            if (!beamVisible) ReleaseAnchors();
            if (beamVisible)
            {
                Vector3 direction=Quaternion.Euler(0,BeamWorldAngle,0)*Vector3.forward;
                Vector3 from=phaseCenter+direction*(1.5f*boss.BodyScale),to=ClipBeam(from,phaseCenter+direction*(State.CurrentBeamLength*ArenaSizeRules.LayoutScale(game.DungeonLayout)));
                if(!WorldTraversal.HasClearSweepCapsule(from,from,BeamDangerRadius)){beamRoot.SetActive(false);return State.OwnsAttacks;}
                DrawBeam(from,to,direction);
                if (State.DamagePulse && !game.InputBlocked && !boss.IsDead && !game.Player.IsDead &&
                    BeamContains(game.Player.transform.position,from,to))
                    game.Player.TakeDamageFrom(boss.AttackDamage*.55f,"星环执政官 · 环流扫射",phaseCenter,PlayerHitReaction.Push);
            }
            return State.OwnsAttacks;
        }
        private bool ChapterOwnerValid
        {get{return !chapterConfigured||(game!=null&&game.ChapterActive&&!game.ChapterFinished&&game.Player==owner&&owner!=null&&!owner.IsDead&&owner.CombatEpoch==ownerEpoch&&game.ChapterRoomIndex==ownerRoom&&game.ChapterSeed==ownerSeed);}}
        private void BeginPhasePresentation()
        {
            boss.BeginLargeBossMechanic();phaseCenter=CombatFx.Flat(transform.position);
            Vector3 toward=CombatFx.Flat(game.Player.transform.position-phaseCenter);
            bool seededPattern=chapterConfigured;
            sweepSign=ChapterBossPattern.SweepSign(seededPattern,seed,State.PhaseNumber);
            startAngle=Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg+ChapterBossPattern.StartOffset(seededPattern,seed,State.PhaseNumber);
            if(State.IsFollowup) { sweepSign=1; startAngle=Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg-48f; }
            else CreateAnchors();
            EnsureBeam();
            game.SpawnMechanismText(transform.position+Vector3.up*(3.6f*boss.BodyScale),State.IsFollowup?"追加扫射预警 · 打断可中止":"断能蓄力 · 摧毁供能锚",new Color(.35f,.92f,1));

        }
        private bool unprovenAnchorRemoval;
        private void ReconcileAnchors()
        {
            for (int i=0;i<anchors.Length;i++)
                if ((State.LiveAnchorMask & (1<<i)) != 0 && (anchors[i]==null || anchors[i].Broken || !anchors[i].gameObject.activeInHierarchy))
                {
                    bool playerBreak=anchors[i]!=null&&anchors[i].WasBrokenBy(owner,ownerEpoch);
                    if(!playerBreak)unprovenAnchorRemoval=true;
                    if(State.DestroyAnchor(State.PhaseNumber,i)&&State.Phase==LargeBossPhase.Exposed&&playerBreak&&!unprovenAnchorRemoval&&ChapterOwnerValid)
                        game.RecordChapterAnchorExposure(boss);
                }
        }
        internal void InterruptWindup()
        {
            if(stopped||State==null||!State.Interruptible)return;
            if(chapterConfigured) CombatImpactBatch.Resolve(ResolveInterrupt);
            else ResolveInterrupt();
        }
        private void ResolveInterrupt()
        {
            if(stopped||State==null||boss==null||boss.IsDead||!ChapterOwnerValid)return;
            // Same attack may destroy its last anchor after enumerating the boss.
            ReconcileAnchors();
            bool anchorExposure=State.Phase==LargeBossPhase.Exposed;
            if(State.Phase!=LargeBossPhase.Exposed&&!State.InterruptWindup())return;
            if(!anchorExposure&&chapterConfigured)game.RecordChapterInterrupt(boss);
            if(beamRoot!=null)beamRoot.SetActive(false);
            ReleaseAnchors();
            if(State.Phase==LargeBossPhase.Exposed)AnnounceExposure();
            else game.SpawnMechanismText(transform.position+Vector3.up*(3.2f*boss.BodyScale),"施法中断 · 恢复 2 秒",new Color(.35f,.92f,1));
        }
        private void AnnounceExposure()
        {
            if(game==null||boss==null||boss.IsDead)return;
            game.SpawnMechanismText(transform.position+Vector3.up*(3.2f*boss.BodyScale),"核心暴露 · 伤害 +35%",new Color(1,.82f,.35f));
            PhysicalSkillVfx.Impact(transform.position, Vector3.up, new Color(.22f,.68f,.62f), .8f);
        }
        private void CreateAnchors()
        {
            ReleaseAnchors();unprovenAnchorRemoval=false;
            anchorRoot=new GameObject("Large boss power anchors");anchorRoot.transform.SetParent(transform,false);
            resources=anchorRoot.AddComponent<WorldResources>();
            Material shell=resources.Material(new Color(.25f,.34f,.43f),false,VisualSurface.Metal);
            Material trim=resources.Material(new Color(.7f,.49f,.24f),false,VisualSurface.Metal);
            Material glow=resources.Material(new Color(.2f,.87f,1),true,VisualSurface.Crystal);
            int mask=0;int level=game.Progression.Profile.level;
            for(int i=0;i<3;i++)
            {
                // Near-core anchors are reachable by existing one-tap boss-targeted AoEs.
                Vector3 point=Vector3.zero;bool safe=false;
                for(int attempt=0;attempt<6;attempt++)
                {
                    float angle=startAngle+i*120f+attempt*13f+((seed&15)-7)+
                        ChapterBossPattern.AnchorOffset(chapterConfigured,seed,State.PhaseNumber);
                    point=phaseCenter+Quaternion.Euler(0,angle,0)*Vector3.forward*(2.6f*boss.BodyScale);
                    if(DestructibleProp.CanPlace(point,.55f)&&WorldTraversal.HasGroundPath(phaseCenter,point,.15f)&&
                        CombatFx.Flat(point-game.Player.transform.position).magnitude>1.2f){safe=true;break;}
                }
                if(!safe)continue;
                Transform root=LargeBossRig.Joint(anchorRoot.transform,"Destructible power anchor",Vector3.zero);root.position=point;
                Transform model=LargeBossRig.Joint(root,"Anchor geometry",Vector3.zero);
                LargeBossRig.Part(model,"Power anchor plinth",PrimitiveType.Cylinder,new Vector3(0,.13f,0),new Vector3(.84f,.13f,.84f),shell);
                LargeBossRig.Part(model,"Breakable crystal",PrimitiveType.Cube,new Vector3(0,.82f,0),new Vector3(.46f,.88f,.46f),glow).localRotation=Quaternion.Euler(0,45,0);
                for(int arm=0;arm<3;arm++)
                {
                    Transform brace=LargeBossRig.Joint(model,"Anchor bronze cage",Vector3.zero);brace.localRotation=Quaternion.Euler(0,arm*120,0);
                    LargeBossRig.Part(brace,"Anchor claw",PrimitiveType.Capsule,new Vector3(0,.45f,.3f),new Vector3(.1f,.34f,.12f),trim).localRotation=Quaternion.Euler(-12,0,0);
                }
                EnemySilhouetteArt.ApplyAnchors(model);
                var prop=root.gameObject.AddComponent<DestructibleProp>();
                prop.Initialize(DestructibleKind.Crate,level,.55f,false,PropRecovery.None,model,shell);
                anchors[i]=prop;mask|=1<<i;
            }
            State.CommitAnchors(mask);
        }
        private void ReleaseAnchors()
        {
            if(anchorRoot!=null){anchorRoot.SetActive(false);Destroy(anchorRoot);anchorRoot=null;}
            for(int i=0;i<anchors.Length;i++)anchors[i]=null;
        }
        private void EnsureBeam()
        {
            if (beamRoot != null) return;
            sweepVisual = EnemySweepPhysicalVfx.Create(transform, BeamDangerRadius);
            beamRoot = sweepVisual.gameObject;
        }
        private void DrawBeam(Vector3 from, Vector3 to, Vector3 direction)
        {
            bool live = State.Phase == LargeBossPhase.Beam;
            float progress = Mathf.Clamp01(1 - State.Remaining / LargeBossPhaseState.WindupSeconds);
            if (sweepVisual != null) sweepVisual.Draw(from, to, progress, live, boss.CanBeSkillInterrupted);
        }
        private bool BeamContains(Vector3 point,Vector3 from,Vector3 to)
        {return CombatFx.SegmentDistance(point,from,to)<=BeamDangerRadius&&WorldTraversal.HasClearSweepCapsule(from,to,BeamDangerRadius);}
        private Vector3 ClipBeam(Vector3 from,Vector3 to)
        {
            if(WorldTraversal.HasClearSweepCapsule(from,to,BeamDangerRadius))return to;
            float low=0,high=1;for(int i=0;i<10;i++){float mid=(low+high)*.5f;if(WorldTraversal.HasClearSweepCapsule(from,Vector3.Lerp(from,to,mid),BeamDangerRadius))low=mid;else high=mid;}
            return Vector3.Lerp(from,to,low);
        }
        internal void StopEncounter()
        {
            if(stopped)return;stopped=true;if(State!=null)State.Dispose();ReleaseAnchors();
            if(beamRoot!=null){beamRoot.SetActive(false);Destroy(beamRoot);beamRoot=null;}
        }
        private void OnDisable(){StopEncounter();}
        // Runs even with timeScale zero: terminal menus must not retain live hazards.
        private void LateUpdate()
        {if(game!=null&&game.RevivalPending)return;if(!stopped&&(game==null||!game.HasStarted||game.IsDead||game.ModeFinished||!ChapterOwnerValid||boss==null||boss.IsDead))StopEncounter();}
        private void OnDestroy(){StopEncounter();}
    }
}
