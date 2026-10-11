using UnityEngine;
namespace Emberfall
{
    public sealed partial class PlayerController
    {
        // One presentation receipt for the most recent cast. Per-target settlement
        // deduplication remains owned by EnemyStatusEffects/BurnFinaleReceipts.
        private int burnFeedbackCast, burnFeedbackEpoch, burnFeedbackTargets;
        private float burnFeedbackAt;
        private void RecordBurnCash(int castId,int epoch,Vector3 point)
        {
            if(IsDead||CombatEpoch!=epoch||session==null||session.Player!=this)return;
            if(burnFeedbackCast!=castId||burnFeedbackEpoch!=epoch)
            {burnFeedbackCast=castId;burnFeedbackEpoch=epoch;burnFeedbackTargets=0;}
            burnFeedbackTargets++;burnFeedbackAt=Time.time;
            CombatFx.BurnContact(this,point,true);
        }
        internal bool BurnCashFeedback(out int targets,out float remaining,bool includeBlocked=false)
        {
            targets=0;remaining=0;
            if(IsDead||session==null||session.Player!=this||!includeBlocked&&session.InputBlocked||!session.HasStarted||burnFeedbackEpoch!=CombatEpoch||burnFeedbackTargets<=0)return false;
            remaining=Mathf.Max(0,2f-(Time.time-burnFeedbackAt));
            if(remaining<=0)return false;
            targets=burnFeedbackTargets;return true;
        }
    }
}
