using System;
using System.Collections.Generic;
namespace Emberfall
{
    public enum CombatVisualPriority { Decoration=0, SustainedBackground=1, ActionBody=2, RealContact=3, Finale=4,
        // Compatibility names for existing integrations; production emitters use explicit roles.
        Sustained=SustainedBackground, Primary=ActionBody, Contact=RealContact }
    // One shared bounded pool. New important feedback can retire the oldest lower-priority lease.
    public sealed class CombatVisualBudget
    {
        // Configured by the active graphics profile. Zero preserves the lease's
        // platform default before preferences have initialized or in fixtures.
        internal static int ConfiguredLimit;
        public sealed class Ticket {internal Action Retire;internal CombatVisualBudget Owner;internal bool Active;public CombatVisualPriority Priority {get;internal set;}}
        private readonly List<Ticket> tickets=new List<Ticket>();
        public int Active {get{return tickets.Count;}}
        public Ticket Acquire(CombatVisualPriority priority,int maximum,Action retire)
        {
            if(maximum<=0)return null;
            while(tickets.Count>=maximum)
            {
                Ticket victim=null;
                foreach(var ticket in tickets)if(ticket.Priority<priority&&(victim==null||ticket.Priority<victim.Priority))victim=ticket;
                // A fresh physical hit must be readable even when old contact
                // debris fills the pool. Keep finales above ordinary contacts.
                if(victim==null&&(priority==CombatVisualPriority.RealContact||priority==CombatVisualPriority.Finale))
                    foreach(var ticket in tickets)if(ticket.Priority==priority){victim=ticket;break;}
                if(victim==null)return null;
                Release(victim);victim.Retire?.Invoke();
            }
            var result=new Ticket{Priority=priority,Retire=retire,Owner=this,Active=true};tickets.Add(result);return result;
        }
        public void Promote(Ticket ticket,CombatVisualPriority priority)
        {if(ticket!=null&&ticket.Owner==this&&ticket.Active&&priority>ticket.Priority)ticket.Priority=priority;}
        public void Release(Ticket ticket)
        {if(ticket==null||ticket.Owner!=this||!ticket.Active)return;ticket.Active=false;tickets.Remove(ticket);}
    }
}
