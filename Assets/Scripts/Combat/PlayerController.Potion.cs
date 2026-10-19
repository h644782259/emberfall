using UnityEngine;

namespace Emberfall
{
    public sealed partial class PlayerController
    {
        public const float PotionCooldownDuration = 60f;
        public float PotionCooldownRemaining { get; private set; }
        public void StartPotionCooldown() { PotionCooldownRemaining = PotionCooldownDuration; }
        private void TickPotionCooldown(float deltaTime)
        {
            if (deltaTime > 0) PotionCooldownRemaining = Mathf.Max(0, PotionCooldownRemaining - deltaTime);
        }
    }
}
