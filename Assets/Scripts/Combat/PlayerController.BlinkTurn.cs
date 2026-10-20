using UnityEngine;
namespace Emberfall
{
    public sealed partial class PlayerController
    {
        private bool blinkTurning;
        private float blinkTurnAge;
        private int blinkTurnEpoch;
        private Vector3 blinkTurnOrigin;
        private Quaternion blinkTurnFacing;
        private void BeginBlinkTurn(Vector3 origin)
        {
            blinkTurning=true;blinkTurnAge=0;blinkTurnEpoch=CombatEpoch;
            blinkTurnOrigin=origin;blinkTurnFacing=transform.rotation;
        }
        private void LateUpdate()
        {
            if(!blinkTurning||model==null)return;
            if(IsDead||CombatEpoch!=blinkTurnEpoch||session==null||!session.HasStarted)
            {blinkTurning=false;return;}
            blinkTurnAge+=Time.deltaTime;
            float t=Mathf.Clamp01(blinkTurnAge/.22f);
            if(t>=1)
            {
                blinkTurning=false;model.transform.localPosition=Vector3.zero;
                model.transform.localRotation=Quaternion.identity;return;
            }
            // Visual travel follows the already-resolved, collision-safe blink endpoint.
            // Only the model turns; aiming, movement and the camera keep their heading.
            float eased=t*t*(3-2*t);
            model.transform.position=Vector3.Lerp(blinkTurnOrigin,transform.position,eased);
            model.transform.rotation=blinkTurnFacing*Quaternion.Euler(0,360*eased,0);
        }
    }
}
