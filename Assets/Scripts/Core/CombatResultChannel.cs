namespace Emberfall
{
    // Presentation receipt only. Independent of opportunity clocks and combat state.
    public sealed class CombatResultChannel
    {
        private object owner, target;
        private int epoch, suppressedReceipt;
        private CombatOpportunityKind suppressedKind;
        private bool initialized;
        public string Observe(CombatOpportunityState result,object currentOwner,int currentEpoch,object currentTarget,bool blocked)
        {
            bool scopeChanged=initialized&&(!ReferenceEquals(owner,currentOwner)||epoch!=currentEpoch);
            // Receipt sequences are local to an owner/epoch (companion bonds restart
            // at one). Never carry a suppression key into a new sequence namespace.
            if(scopeChanged){suppressedReceipt=0;suppressedKind=CombatOpportunityKind.None;}
            bool changed=scopeChanged||initialized&&!ReferenceEquals(target,currentTarget);
            owner=currentOwner;epoch=currentEpoch;target=currentTarget;initialized=true;
            if(blocked||changed||currentTarget==null)
            {if(result.Kind!=CombatOpportunityKind.None){suppressedReceipt=result.Receipt;suppressedKind=result.Kind;}return "";}
            if(result.Kind==suppressedKind&&result.Receipt==suppressedReceipt)return "";
            return result.Caption;
        }
    }
}
