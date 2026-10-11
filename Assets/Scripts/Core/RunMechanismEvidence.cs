using System;
namespace Emberfall
{
    // Attempt-local only. One token per generated mechanic area, never per target/tick.
    public sealed class RunMechanismEvidence
    {
        private int generation;
        private readonly int[] counts=new int[4];
        public int[] Snapshot(){return (int[])counts.Clone();}
        public void Reset(){generation++;Array.Clear(counts,0,counts.Length);}
        public Instance Register(object owner,int epoch,int mechanic)
        {
            if(owner==null||mechanic<0||mechanic>1)return null;
            int slot=mechanic*2;counts[slot]=Math.Min(int.MaxValue-1,counts[slot])+1;
            return new Instance(this,owner,epoch,generation,slot);
        }
        public sealed class Instance
        {
            private readonly RunMechanismEvidence ledger;
            private readonly object owner;
            private readonly int epoch,generation,slot;
            private bool effective;
            internal Instance(RunMechanismEvidence ledger,object owner,int epoch,int generation,int slot)
            {this.ledger=ledger;this.owner=owner;this.epoch=epoch;this.generation=generation;this.slot=slot;}
            public void Record(object currentOwner,int currentEpoch,float actualHealthLoss)
            {
                if(effective||!ReferenceEquals(owner,currentOwner)||epoch!=currentEpoch||generation!=ledger.generation||
                    actualHealthLoss<=0||float.IsNaN(actualHealthLoss)||float.IsInfinity(actualHealthLoss))return;
                effective=true;ledger.counts[slot+1]=Math.Min(int.MaxValue-1,ledger.counts[slot+1])+1;
            }
        }
    }
}
