using UnityEngine;
namespace Emberfall
{
    public sealed partial class EnemyController
    {
        public DungeonEnemyRole DungeonRole { get; private set; }
        public void ConfigureDungeonRole(DungeonEnemyRole role)
        {
            if (IsBoss) return;
            DungeonRole = role;
            string title = role == DungeonEnemyRole.Protector ? "护援魔灵" : role == DungeonEnemyRole.Artillery ? "轰击魔灵" :
                role == DungeonEnemyRole.Leaper ? "跃袭史莱姆" : role == DungeonEnemyRole.Crusher ? "碎地守卫" : null;
            if (title == null) return;
            DisplayName = title; gameObject.name = title;
            // Recognizable health-bar tint without additional lights or particles.
            if (healthFillMaterial != null) healthFillMaterial.color = RoleTint;
        }
        private Color RoleTint => DungeonRole == DungeonEnemyRole.Protector ? new Color(.25f,.9f,.65f) :
            DungeonRole == DungeonEnemyRole.Artillery ? new Color(.8f,.4f,1f) : new Color(.94f,.38f,.16f);
        private string DungeonRoleDescription => DungeonRole == DungeonEnemyRole.Protector ? "护援：6米内可见同伴减伤30%；击杀、控制或引离可解除。" :
            DungeonRole == DungeonEnemyRole.Artillery ? "轰击：锁定地面后蓄力1.25秒；离开隆起区域或打断施法。" :
            DungeonRole == DungeonEnemyRole.Leaper ? "跃袭：从更远处跳扑；落点提前锁定，可侧移躲避。" :
            DungeonRole == DungeonEnemyRole.Crusher ? "碎地：蓄力更久，震击范围扩大；退开后反击。" : "";
    }
}
