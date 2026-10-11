using System;
namespace Emberfall
{
    public enum EscapeRole { None, GateGuard, Pursuer, GateSupplier, SideFlanker }
    // Escape-only posts; other rooms and all bosses retain their normal controller.
    public sealed class EscapePostPolicy
    {
        public readonly EscapeRole Role;
        private float chaseSeconds,restSeconds;
        private bool returning;
        public EscapePostPolicy(EscapeRole role){Role=role;}
        public bool ReturnToPost(float delta,float fromPost,float targetFromPost,float movementSeconds=-1,bool committedAttack=false)
        {
            if(Role==EscapeRole.None||Role==EscapeRole.Pursuer)return false;
            if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return returning;
            restSeconds=Math.Max(0,restSeconds-delta);
            if(!committedAttack&&returning&&fromPost<=.25f){returning=false;chaseSeconds=0;restSeconds=1;}
            float leash=Role==EscapeRole.GateGuard?5:Role==EscapeRole.GateSupplier?2.6f:4.5f;
            float acquire=Role==EscapeRole.GateGuard?7:Role==EscapeRole.GateSupplier?7.5f:6;
            // Explicit host samples count actual walking only. The legacy three-argument
            // caller describes a moving chase and retains its previous contract.
            float moving=movementSeconds<0?delta:float.IsNaN(movementSeconds)||float.IsInfinity(movementSeconds)?0:Math.Max(0,movementSeconds);
            if(Role==EscapeRole.GateGuard&&fromPost>.8f)chaseSeconds+=moving;
            if(!committedAttack&&fromPost<=.8f)chaseSeconds=0;
            if(fromPost>leash||targetFromPost>acquire||Role==EscapeRole.GateGuard&&chaseSeconds>=3.5f-.0001f)returning=true;
            // A warned attack owns its release. Remember the return request, but never
            // cancel it merely because its stationary windup outlived a chase budget.
            return !committedAttack&&(returning||restSeconds>0);
        }
    }
}
