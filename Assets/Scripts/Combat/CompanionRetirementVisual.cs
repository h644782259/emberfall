using UnityEngine;
namespace Emberfall
{
    // Transfer only the collider-free model. The companion host retires immediately;
    // this visual owns no attacks, navigation, health or rewards, and copies no materials.
    public sealed class CompanionRetirementVisual:MonoBehaviour
    {
        private static int live;
        private bool leased;
        private CombatModel model;private PlayerController owner;private GameSession session;private int epoch;
        private CompanionRetirementReason reason;private float age,duration;
        private Vector3 position,scale;private Quaternion rotation;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry(){live=0;}
        public static bool Detach(CombatModel model,PlayerController owner,GameSession session,CompanionRetirementReason reason)
        {
            float duration=CompanionRetirementRules.Duration(reason);
            if(model==null||owner==null||session==null||duration<=0||live>=CompanionRetirementRules.MaximumVisuals)return false;
            model.transform.SetParent(null,true);
            var visual=model.gameObject.AddComponent<CompanionRetirementVisual>();visual.leased=true;live++;
            visual.model=model;visual.owner=owner;visual.session=session;visual.epoch=owner.CombatEpoch;visual.reason=reason;visual.duration=duration;
            visual.position=model.transform.position;visual.scale=model.transform.localScale;visual.rotation=model.transform.rotation;model.BeginDeath();return true;
        }
        private void Update()
        {
            if(model==null||owner==null||session==null||owner.IsDead||session.Player!=owner||!session.HasStarted||owner.CombatEpoch!=epoch){Retire();return;}
            if(session.InputBlocked)return;
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/duration);var pose=CompanionRetirementRules.Pose(reason,t);
            transform.position=position+Vector3.up*pose.Height;transform.rotation=rotation*Quaternion.Euler(0,0,pose.Roll);
            transform.localScale=scale*pose.Scale;model.SetDeathOpacity(pose.Opacity);
            if(age>=duration)Retire();
        }
        private void Retire(){gameObject.SetActive(false);Destroy(gameObject);}
        private void OnDisable(){Release();}
        private void OnDestroy(){Release();}
        private void Release(){if(!leased)return;leased=false;live=Mathf.Max(0,live-1);}
    }
}
