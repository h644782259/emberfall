// Only dependencies/UI are substituted; TrySkillInterrupt and TakeDamage are extracted unchanged.
using UnityEngine;
namespace UnityEngine {public static partial class Mathf {public static float Lerp(float a,float b,float t)=>a+(b-a)*t;}}
namespace Emberfall {
public enum HeroClass {Vanguard,Arcanist,Ranger,Summoner} public enum EnemyKind{Slime,Goblin,Wisp,Guardian} public enum ThreatTier{Normal,Elite,Boss}
public partial class GameSession {public bool PracticeActive=>false;public CampPracticeRecord PracticeRecord=>throw new System.InvalidOperationException("Chapter combat fixture is not a practice session");public bool CombatEnded;public float RoomSupportMultiplier(EnemyController e)=>1;public float ChapterSupportMultiplier(EnemyController e)=>1;public void SpawnCombatDamage(Vector3 p,string text,bool crit){}public void OnEnemyKilled(EnemyController e){} }
public partial class ProgressionService {public Stats GetStats()=>new Stats();public class Stats{public float Damage=10;}}
public class EnemyStatusEffects{public float DamageMultiplier=1;}
public class GuardArmorVisual {public void RecordImpact(bool armor,bool preparing){}}
public class CombatModel {public void Recoil(Vector3 p,float s){}}
public static class CombatReviewEvents {public static bool Enabled=false;public static void Emit(string kind,string actor,string target,float amount=0,string detail=""){} }
public static class CombatReviewObjectId {public static string Get(object o)=>"test";}
public static class HitFeedback {public static void Spawn(Vector3 p,Vector3 d,float s,bool c,CombatVisualPriority priority){}}
public enum SoundCue {CriticalHit,Hit}public static class GameAudio {public static void Play(SoundCue s){}}
public partial class EnemyController {
 GameSession session;LargeExpeditionBoss largeBoss;EnemyControlPolicy controlPolicy=new EnemyControlPolicy(EnemyControlTier.Boss);
 PlayerController controlOwner;int controlOwnerEpoch,attackNumber;bool preparing,aggro,deathReported;
 float hurtTime,nextImpactTime,nextFlinchAllowed,flinchUntil,chargeTime,stunTime,attackCooldown;Vector3 knockVelocity;
 EnemyKind Kind=EnemyKind.Guardian;ThreatTier Tier=ThreatTier.Boss;GuardArmorVisual guardArmorVisual;CombatModel model=new CombatModel();
 public EnemyStatusEffects StatusEffects=new EnemyStatusEffects();
 bool IsPreparingAttack=>preparing||largeBoss!=null&&largeBoss.State.Interruptible;
 void CancelAttack(bool hit=false){}static bool FinitePoint(Vector3 v)=>true;
}
}
