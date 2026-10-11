using System.Collections.Generic;
namespace Emberfall
{
    // Cast ids increase per player. Keep recent out-of-order completions and a retired-id floor.
    // Old receipts cannot become eligible again after capacity eviction; owner/epoch changes clear this scope.
    public sealed class BurnFinaleReceipts
    {
        private readonly SortedSet<int> recent=new SortedSet<int>();
        private int retiredThrough;
        public bool TryEnter(int castId)
        {
            if(castId<=0||castId<=retiredThrough||!recent.Add(castId))return false;
            if(recent.Count>32){retiredThrough=recent.Min;recent.Remove(retiredThrough);}
            return true;
        }
        public void Clear(){recent.Clear();retiredThrough=0;}
    }
}
