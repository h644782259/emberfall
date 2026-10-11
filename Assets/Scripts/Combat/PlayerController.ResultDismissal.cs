namespace Emberfall
{
    public sealed partial class PlayerController
    {
        // Closing the result opens a fresh action generation, without paying,
        // resetting skill cooldowns, moving the hero, or reopening enemy rewards.
        internal void RetireCastsForResultDismissal()
        {
            CombatEpoch++;EnsureCastReceiptEpoch();
            ClearMobilePinnedTarget();CancelCombatPose();
            if(targeting!=null)targeting.Cancel();
            if(charge!=null)charge.Cancel();
            CancelTransientInput();AimTarget=null;focusedEnemy=null;aimGeometry.Clear();
            SummonedCompanion.TransferPermanentPartners(this);
        }
    }
}
