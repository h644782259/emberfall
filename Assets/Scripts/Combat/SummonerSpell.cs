using UnityEngine;

namespace Emberfall
{
    internal sealed class SummonerSpell : MonoBehaviour
    {
        private CastFirstHitReceipt castReceipt;
        private PlayerController owner;
        private GameSession session;
        private int rank, epoch, castId;
        private float damage, age, nextTick;
        private CombatDamage finisherDamage;
        private readonly ScheduledImpactBatch<EnemyController> pendingTargets = new ScheduledImpactBatch<EnemyController>();
        private bool pendingFinisher;
        private bool IsCurrentCast { get { return owner != null && session != null && session.Player == owner && !owner.IsDead && session.HasStarted && !session.CombatEffectsEnded && owner.CombatEpoch == epoch; } }
        private float Radius { get { return 4.4f * GameBalance.SkillRangeMultiplier(rank); } }
        public static void Cast(PlayerController player, GameSession game, int skill, int rank, Vector3 target, float damage, EnemyController commandTarget = null, bool preserveTargetPoint = false, int castId = 0)
        {
            int propCast=castId==0?player.NewCastId():castId;
            float range = GameBalance.SkillRangeMultiplier(rank);
            Color color = GameBalance.ClassColor(HeroClass.Summoner);
            if (skill == 2 || skill == 4 || skill == 6 || skill == 9)
            {
                var form = skill == 2 ? SummonedCompanion.Kind.Wolf : skill == 4 ? SummonedCompanion.Kind.Spirit : skill == 6 ? SummonedCompanion.Kind.Wisp : SummonedCompanion.Kind.Treant;
                Vector3 position = CombatSight.GroundPoint(player.transform.position,target);
                SummonedCompanion partner = SummonedCompanion.CastContract(player, game, form, rank, position, damage,
                    game.Progression.Profile.summonerRoute == SummonerRoute.Pack, preserveTargetPoint ? commandTarget : commandTarget != null ? commandTarget : player.AimTarget, preserveTargetPoint);
                if (partner != null && form == SummonedCompanion.Kind.Wolf)
                {
                    SkillPerformanceVfx.Sustain(player,partner.transform.position,1.8f,1.8f,SkillPerformanceBody.Spirit);
                    FilledSkillVfx.Impact(player,partner.transform.position,1.8f,FilledVfxKind.Summon,color,CombatVisualPriority.ActionBody);
                }
                else if (partner != null)
                {
                    FilledSkillVfx.ContractArrival(player,partner.transform.position,form==SummonedCompanion.Kind.Wisp,color);
                    // The actual turret, ember body or treant is the contract's
                    // identity. A generic wolf apparition misidentified all three.
                    PhysicalSkillVfx.Impact(partner.transform.position,Vector3.down,
                        form==SummonedCompanion.Kind.Wisp?new Color(.58f,.28f,.08f):new Color(.36f,.29f,.18f),1.4f);
                }
            }
            else if (skill == 0)
            {
                if(!FilledSkillVfx.IdentityContact(player,player.transform.position,player.transform.forward,1.6f*range,color,3)) CombatFx.Slash(player.transform.position, player.transform.forward, 5f * range, color);
                CombatDamage impact = player.RollDirectDamage(damage * SummonerDamageRules.ImpulseCoefficient);
                DestructibleProp.StrikeCone(player,player.transform.position,player.transform.forward,5f*range,110,impact,propCast);
                using (var impactTargets = CombatTargetSnapshot<EnemyController>.Capture(game.Enemies))
                for (int targetIndex = 0; targetIndex < impactTargets.Count; targetIndex++)
                {
                    EnemyController enemy = impactTargets[targetIndex];
                    if (enemy == null || enemy.IsDead) continue;
                    Vector3 delta = CombatFx.Flat(enemy.transform.position - player.transform.position);
                    if (delta.magnitude <= 5f * range && (delta.sqrMagnitude < .1f || Vector3.Angle(player.transform.forward, delta) < 55) && CombatSight.Melee(player.transform.position,enemy.transform.position))
                    {
                        float contactHealth=enemy.Health;
                        enemy.TakeDamage(impact.Amount, delta.normalized, 1.25f + rank * .2f, .2f, critical: impact.IsCritical,practiceCastId:propCast);
                        if(enemy.Health<contactHealth)FilledSkillVfx.IdentityContact(player,enemy.transform.position,player.transform.forward,.6f,color,3,CombatVisualPriority.RealContact);
                        player.RegisterSkillHit(propCast);
                    }
                }
            }
            else if (skill == 1)
                CombatArea.Spawn(player, game, target, 3.2f * range, damage * SummonerDamageRules.ThornTickCoefficient, 0, SummonerDamageRules.ThornStartup, SummonerDamageRules.ThornDuration(rank), SummonerDamageRules.ThornInterval, color, statusSkill: 1, statusRank: rank,castId:propCast,visual:SkillVisualRecipe.Poison);
            else if(skill==5)
                CombatProjectile.Friendly(player,game,player.transform.position,player.transform.forward,player.RollDirectDamage(damage*2.6f),color,true,false,false,range,22f,castId:propCast);
            else if (skill == 7)
            {
                var obj = new GameObject("引力印记"); obj.transform.position = target;
                var spell = obj.AddComponent<SummonerSpell>();
                spell.owner = player; spell.session = game; spell.rank = rank; spell.damage = damage; spell.epoch = player.CombatEpoch; spell.castId=propCast;spell.castReceipt=player.RetainCastReceipt(propCast);
                spell.finisherDamage = player.RollDirectDamage(damage * SummonerDamageRules.MarkFinisherCoefficient);
                spell.pressure=SkillPerformanceVfx.Sustain(player,target,4.4f*range,SummonerDamageRules.MarkFinisherTime,SkillPerformanceBody.Pressure,obj.transform,()=>spell!=null&&spell.IsCurrentCast);
                AdvancedSkillVfx.Rune(player, target, 4.4f * range, color, SummonerDamageRules.MarkFinisherTime, rank + 1);
            }
        }
        private void Update()
        {
            if (!IsCurrentCast) { Retire(); return; }
            if (session.InputBlocked || Time.deltaTime <= 0) return;
            age += Time.deltaTime;
            float radius = Radius;
            foreach (var enemy in session.Enemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                Vector3 delta = CombatFx.Flat(transform.position - enemy.transform.position);
                float distance = delta.magnitude;
                if (distance > radius || !CombatSight.Area(transform.position,enemy.transform.position)) continue;
                enemy.Provoke();
                if(age<=SummonerDamageRules.MarkFinisherTime)enemy.StatusEffects?.Suppress(.3f);
            }
            for (int tick=0;tick<ScheduledTickWindow.MaximumCatchUp;tick++)
            {
                if (!IsCurrentCast) { Retire(); return; }
                if (session.InputBlocked) return;
                if (!pendingTargets.Pending)
                {
                    pendingFinisher = ScheduledTickWindow.Collect(ref nextTick, age, SummonerDamageRules.MarkLastTick, SummonerDamageRules.MarkInterval, 1) == 0;
                    if (pendingFinisher && (age < SummonerDamageRules.MarkFinisherTime || !ScheduledTickWindow.Drained(nextTick,SummonerDamageRules.MarkLastTick))) break;
                    pendingTargets.Begin(session.Enemies);
                    if (!pendingFinisher && tick == 0)
                        DestructibleProp.StrikeArea(owner,transform.position,Radius,damage*SummonerDamageRules.MarkTickCoefficient,castId);
                }
                while (pendingTargets.Pending)
                {
                    if (!IsCurrentCast) { Retire(); return; }
                    if (session.InputBlocked) return;
                    EnemyController enemy;
                    if (!pendingTargets.TryTake(out enemy)) break;
                    if (enemy == null || enemy.IsDead || !session.Enemies.Contains(enemy)) continue;
                    Vector3 delta = CombatFx.Flat(transform.position - enemy.transform.position);
                    if (delta.magnitude > Radius || !CombatSight.Area(transform.position,enemy.transform.position)) continue;
                    if (pendingFinisher)
                    {
                        enemy.TakeDamage(finisherDamage.Amount, -delta.normalized, .3f, .25f, critical: finisherDamage.IsCritical,practiceCastId:castId);
                        owner.RegisterSkillHit(castId);
                        if (!enemy.IsDead) enemy.StatusEffects.Knockdown(1.4f,true);
                    }
                    else
                    {
                        enemy.TakeDamage(damage*SummonerDamageRules.MarkTickCoefficient,Vector3.zero,impact:false,practiceCastId:castId);
                        owner.RegisterSkillHit(castId);
                    }
                }
                if (pendingFinisher)
                {
                    DestructibleProp.StrikeArea(owner,transform.position,Radius,finisherDamage,castId);
                    if(pressure!=null){pressure.CompleteAftermath();pressure=null;}
                    Retire();return;
                }
            }
        }
        private void Retire() { pendingTargets.Clear(); Destroy(gameObject); }
        private SkillPerformanceVfx pressure;
        private void OnDisable() { if(pressure!=null){pressure.Retire();pressure=null;}castReceipt?.Release();castReceipt=null;pendingTargets.Clear(); }
        private void OnDestroy() { OnDisable(); }
    }
}
