using System;using Emberfall;
public static class DecorationBudgetTests
{
 public static string Run()
 {
  int n=0;var budget=new DecorationBudget();var leases=new bool[25];
  Action<bool> check=ok=>{n++;if(!ok)throw new Exception("Decoration lease invariant failed");};
  for(int cycle=0;cycle<100;cycle++)
  {
   for(int i=0;i<24;i++)check(budget.Acquire(ref leases[i],24));
   check(!budget.Acquire(ref leases[24],24));check(budget.Active==24);
   budget.Release(ref leases[3]);budget.Release(ref leases[3]);check(budget.Active==23);
   check(budget.Acquire(ref leases[24],24));check(!budget.Acquire(ref leases[3],24));
   for(int i=0;i<25;i++)budget.Release(ref leases[i]);check(budget.Active==0);
  }
  check(DecorationBudget.Particles(.25f)<DecorationBudget.Particles(1));
  check(DecorationBudget.DeathMotes(true,true)<DecorationBudget.DeathMotes(true,false));
  return "PASS: "+n+" production decoration-budget assertions";
 }
}
