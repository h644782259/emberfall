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
            if(Player==null||IsDead||InputBlocked)return;
            failedControl=control;controlFailure=shortReason;controlFailureUntil=Time.unscaledTime+1.1f;controlFailureEpoch=Player.CombatEpoch;
        }
        public string ControlFailure(string control)
        {return Player!=null&&!IsDead&&!InputBlocked&&Player.CombatEpoch==controlFailureEpoch&&Time.unscaledTime<controlFailureUntil&&failedControl==control?controlFailure:null;}
    }
}
