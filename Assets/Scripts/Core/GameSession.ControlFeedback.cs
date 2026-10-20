using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        private string failedControl,controlFailure;
        private float controlFailureUntil;
        private int controlFailureEpoch;
        public void ReportControlFailure(string control,string shortReason)
        {
            // Cooldown is already visible on the action icon; repeated input is silent.
            if(shortReason=="冷却")
            {
                if(failedControl==control){controlFailure=null;controlFailureUntil=0;}
                return;
            }
            if(Player==null||IsDead||InputBlocked)return;
            failedControl=control;controlFailure=shortReason;controlFailureUntil=Time.unscaledTime+1.1f;controlFailureEpoch=Player.CombatEpoch;
        }
        public string ControlFailure(string control)
        {return Player!=null&&!IsDead&&!InputBlocked&&Player.CombatEpoch==controlFailureEpoch&&Time.unscaledTime<controlFailureUntil&&failedControl==control?controlFailure:null;}
    }
}
