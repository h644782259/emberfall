using System;
namespace Emberfall
{
    // Independent leases prevent hidden effects and repeated retirement from leaking capacity.
    public sealed class DecorationBudget
    {
        public int Active {get;private set;}
        public bool Acquire(ref bool leased,int limit)
        {if(leased)return true;if(Active>=Math.Max(0,limit))return false;leased=true;Active++;return true;}
        public void Release(ref bool leased)
        {if(!leased)return;leased=false;Active=Math.Max(0,Active-1);}
        public static int Particles(float scale){return Math.Max(8,(int)(90*Math.Max(.25f,Math.Min(1,scale))));}
        public static int DeathMotes(bool boss,bool reduced){return reduced?(boss?4:2):(boss?18:10);}
    }
}
