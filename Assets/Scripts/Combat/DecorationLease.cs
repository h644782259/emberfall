using UnityEngine;
namespace Emberfall
{
    internal sealed class DecorationLease : MonoBehaviour
    {
        private static DecorationBudget particles=new DecorationBudget(),bolts=new DecorationBudget(),deaths=new DecorationBudget();
        private DecorationBudget budget;private bool leased;private int limit;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){particles=new DecorationBudget();bolts=new DecorationBudget();deaths=new DecorationBudget();}
        public static bool Attach(GameObject obj,int kind)
        {
            var lease=obj.AddComponent<DecorationLease>();lease.budget=kind==0?particles:kind==1?bolts:deaths;
            lease.limit=kind==0?(EffectPreferences.ReducedEffects?10:24):kind==1?(EffectPreferences.ReducedEffects?5:16):8;
            if(!lease.budget.Acquire(ref lease.leased,lease.limit))return false;
            if(CombatVisualLease.Attach(obj,CombatVisualPriority.Decoration)!=null)return true;
            lease.budget.Release(ref lease.leased);return false;
        }
        private void OnDisable(){if(budget!=null)budget.Release(ref leased);}
        private void OnEnable(){if(budget!=null&&!budget.Acquire(ref leased,limit))gameObject.SetActive(false);}
        private void OnDestroy(){if(budget!=null)budget.Release(ref leased);}
    }
}
