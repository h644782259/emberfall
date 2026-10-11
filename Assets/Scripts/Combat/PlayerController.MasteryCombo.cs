using UnityEngine;
namespace Emberfall
{
    public sealed partial class PlayerController
    {
        internal bool MasteryComboReady { get { return session != null && session.Player == this && session.HasStarted && !session.InputBlocked && !session.CombatEnded && !IsDead && masteryCore.ComboRemaining > 0; } }
        // A separate noncritical damage event; never ResolveSkillImpact, poison,
        // a skill-hit notification, or an area attack (which could recurse).
        private void SettleMasteryCombo(EnemyController enemy)
        {
            if (!MasteryComboReady || !ValidAimTarget(enemy)) return;
            float coefficient = masteryCore.BasicHit();
            if (coefficient <= 0) return;
            float before = enemy.Health;
            Vector3 point = enemy.transform.position;
            enemy.TakeDamage(CombatAttack * coefficient, Vector3.zero, impact: false, critical: false);
            if (enemy.Health >= before) return;
            session.RecordCombatAction("破敌兑现");
            session.SpawnMechanismText(point + Vector3.up * 2f, "连击兑现", new Color(.55f, .9f, .85f));
        }
    }
}
