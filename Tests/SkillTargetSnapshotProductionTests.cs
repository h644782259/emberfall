using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using Emberfall;using UnityEngine;
internal sealed class AllocatingSet<T>:IDisposable{readonly HashSet<T> values=new HashSet<T>();public bool Add(T v)=>values.Add(v);public bool Contains(T v)=>values.Contains(v);public int Count=>values.Count;public void Dispose(){}}
internal sealed class AllocatingSnapshot<T>:IDisposable{readonly T[] values;AllocatingSnapshot(IList<T> source){values=new T[source.Count];source.CopyTo(values,0);}public static AllocatingSnapshot<T> Capture(IList<T> source)=>new AllocatingSnapshot<T>(source);public int Count=>values.Length;public T this[int i]=>values[i];public void Dispose(){}}
internal sealed class ThrowingList:IList<int>{public int Count=>2;public int this[int i]{get{throw new InvalidOperationException("copy boundary");}set{throw new NotSupportedException();}}public bool IsReadOnly=>true;public void Add(int value)=>throw new NotSupportedException();public void Clear()=>throw new NotSupportedException();public bool Contains(int v)=>false;public void CopyTo(int[] v,int i)=>throw new NotSupportedException();public IEnumerator<int> GetEnumerator()=>throw new NotSupportedException();IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();public int IndexOf(int v)=>-1;public void Insert(int i,int v)=>throw new NotSupportedException();public bool Remove(int v)=>false;public void RemoveAt(int i)=>throw new NotSupportedException();}
class SkillTargetSnapshotProductionTests
{
 static int checks;static void Check(bool good,string oracle){checks++;if(!good)throw new Exception(oracle);}static void Near(float a,float b,string oracle){Check(Math.Abs(a-b)<.00001f,oracle);}static void Sequence(List<int> actual,string expected,string oracle){Check(string.Join(",",actual)==expected,oracle);}
 static GameSession Game(int count=3){var g=new GameSession();new PlayerController(g);for(int i=0;i<count;i++)g.Enemies.Add(new EnemyController(i,0,i*.2f+1));return g;}
 static void ClearedBuffers(Type type,string oracle){var buffers=(IEnumerable)type.GetField("buffers",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);foreach(var buffer in buffers){var b=buffer.GetType();Check(!(bool)b.GetField("InUse",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(buffer),oracle);var values=b.GetField("Targets",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(buffer);Check((int)values.GetType().GetProperty("Count").GetValue(values)==0,oracle);}}
 static void SnapshotContract()
 {
  var source=new List<int>{1,2,3};using(var outer=CombatTargetSnapshot<int>.Capture(source))
  {source.Clear();source.Add(4);using(var inner=CombatTargetSnapshot<int>.Capture(source)){Check(inner.Count==1&&inner[0]==4&&outer.Count==3&&outer[0]==1&&outer[2]==3,"nested target snapshots preserve admission and order");}Check(outer.Count==3&&outer[1]==2,"nested target snapshots survive inner release");}
  var lease=CombatTargetSnapshot<int>.Capture(source);var stale=lease;lease.Dispose();using(var next=CombatTargetSnapshot<int>.Capture(source)){stale.Dispose();Check(next.Count==1&&next[0]==4,"stale copied lease cannot release the reused generation");}
  bool threw=false;try{CombatTargetSnapshot<int>.Capture(new ThrowingList());}catch(InvalidOperationException){threw=true;}Check(threw,"copy boundary failure remains visible");ClearedBuffers(typeof(CombatTargetSnapshot<int>),"copy failure releases buffers and clears references");
  using(var empty=CombatTargetSnapshot<int>.Capture(null))Check(empty.Count==0,"null source is an empty impact boundary");
  using(var outer=CombatTargetSet<int>.Capture()){outer.Add(1);using(var inner=CombatTargetSet<int>.Capture()){Check(!inner.Contains(1)&&outer.Contains(1),"nested chain visited sets remain isolated");inner.Add(2);}Check(!outer.Contains(2),"inner bounce cannot pollute outer chain");}
  ClearedBuffers(typeof(CombatTargetSet<int>),"visited sets release target references");
 }
 static void DispatchBehavior()
 {
  foreach(int path in new[]{0,1,2,3})
  {
   var g=Game();var p=g.Player;var first=g.Enemies[0];var removed=g.Enemies[1];var late=new EnemyController(9);first.Hit=()=>{removed.IsDead=true;g.Enemies.Remove(removed);g.Enemies.Add(late);};Trace.Clear();
   if(path==0)p.RunFrostPulse();else if(path==1)p.ElementalAdvancedArea(Vector3.zero,5,10,7,false);else if(path==2)SummonerSpell.Cast(p,g,0,1,Vector3.zero,10,castId:7);else new SummonedCompanion{session=g}.RunTreant(10);
   Sequence(Trace.Damage,"0,2","live dispatch keeps old snapshot order, skips dead removals and excludes new spawns");Check(late.Health==1000000,"new spawn does not receive the earlier impact");
  }
  {
   var g=Game(4);var p=g.Player;p.HeroClass=HeroClass.Ranger;Trace.Clear();float direct=p.ResolveSkillImpact(g.Enemies[0],0,7,10);Sequence(Trace.Poison,"1,2","venom spread still admits the first two eligible targets in list order");Near(direct,12.4f,"venom bonus and spread coefficient preserved");ClearedBuffers(typeof(CombatTargetSnapshot<EnemyController>),"early spread break releases snapshot");
  }
  {
   var g=Game();var p=g.Player;p.Specialization=ElementalistSpecialization.Burn;g.Enemies[0].StatusEffects.BeginFinale=()=>p.CombatEpoch++;Trace.Clear();p.ElementalAdvancedArea(Vector3.zero,5,10,7,true);Check(Trace.Damage.Count==0,"burn settlement epoch change prevents all following direct impacts");ClearedBuffers(typeof(CombatTargetSnapshot<EnemyController>),"epoch return releases snapshot");
   g.Enemies[0].StatusEffects.BeginFinale=null;p.Specialization=ElementalistSpecialization.Balanced;p.ElementalAdvancedArea(Vector3.zero,5,10,7,false);Sequence(Trace.Damage,"0,1,2","new epoch cast receives clean query buffers");
  }
  {
   var g=Game();var p=g.Player;p.HeroClass=HeroClass.Summoner;var removed=g.Enemies[1];g.Enemies[0].Hit=()=>{removed.IsDead=true;g.Enemies.Remove(removed);g.Enemies.Add(new EnemyController(9));};Trace.Clear();p.RunSummonerDispatch(0,1,Vector3.zero,1,7);Sequence(Trace.Damage,"0,2","summoner damage capture excludes the mid-impact wave");Sequence(Trace.Interrupt,"0,2,9","summoner interrupt capture happens after the impulse, admitting its live post-impact list");
  }
  {
   var g=Game();var pet=new SummonedCompanion{session=g};g.Enemies[0].Hit=()=>{g.Enemies[0].Hit=null;pet.RunTreant(10);};Trace.Clear();pet.RunTreant(10);Sequence(Trace.Damage,"0,0,1,2,1,2","same producer reentry cannot replace the outer treant snapshot");
  }
  {
   var g=Game();var p=g.Player;p.Specialization=ElementalistSpecialization.Balanced;g.Enemies[0].Hit=()=>throw new InvalidOperationException("damage boundary");Trace.Clear();bool threw=false;try{p.ElementalAdvancedArea(Vector3.zero,5,10,7,false);}catch(InvalidOperationException){threw=true;}Check(threw&&!CombatImpactBatch.InAction,"damage exception releases action and query boundaries");ClearedBuffers(typeof(CombatTargetSnapshot<EnemyController>),"damage exception clears retained target references");
  }
  {
   var g=Game();var seq=new AdvancedSkillSequence{owner=g.Player,session=g};g.Enemies[0].Hit=()=>{g.Enemies[0].Hit=null;seq.RunChain(3);};Trace.Clear();seq.RunChain(3);Sequence(Trace.Damage,"0,0,1,2,1,2","nested chain casts retain independent visited targets");ClearedBuffers(typeof(CombatTargetSet<EnemyController>),"completed chains clear all retained targets");
  }
  {
   var g=Game();var seq=new AdvancedSkillSequence{owner=g.Player,session=g};var late=new EnemyController(9,0,1.01f);g.Enemies[0].Hit=()=>{g.Enemies[0].Hit=null;g.Enemies.Add(late);};Trace.Clear();seq.RunChain(3);Sequence(Trace.Damage,"0,9,1","chain preserves live admission on each bounce rather than freezing the cast list");
  }
 }
 static void PullAndSchedule()
 {
  foreach(float distance in new[]{0f,.6f,.6001f,2f,4.4f,4.4001f})
  {
   var g=Game(0);var e=new EnemyController(1,0,distance);g.Enemies.Add(e);var mark=new SummonerSpell();mark.BindForFixture(g.Player,g);mark.transform.position=Vector3.zero;Time.deltaTime=.1f;Trace.Clear();mark.RunForFixture();Check(e.Pulls==(distance>.6f&&distance<=4.4f?1:0),"mark pull preserves inclusive outer and exclusive inner radius");if(e.Pulls>0)Near(e.LastPull.magnitude,Math.Min(distance-.6f,.45f),"mark pull distance and per-frame displacement preserved");
   e.Pulls=0;var seq=new AdvancedSkillSequence{owner=g.Player,session=g};seq.RunPull(Vector3.zero,4.4f,3.5f);Check(e.Pulls==(distance>.6f&&distance<4.4f?1:0),"vortex pull preserves both exclusive radius boundaries");
  }
  {
   var g=Game();var p=g.Player;var mark=new SummonerSpell();mark.BindForFixture(p,g);Time.deltaTime=.1f;p.OnRegister=()=>g.InputBlocked=true;Trace.Clear();mark.RunForFixture();Sequence(Trace.Damage,"0","claimed mark tick pauses immediately after first delivery");var next=g.Enemies[1];g.Enemies.Remove(next);g.Enemies.Add(new EnemyController(9));p.OnRegister=null;g.InputBlocked=false;mark.RunForFixture();Sequence(Trace.Damage,"0,2","pending mark tick excludes later spawns and skips removed membership on resume");
   g.InputBlocked=true;int count=Trace.Damage.Count;mark.RunForFixture();Check(Trace.Damage.Count==count,"blocked mark cannot advance clock or damage");g.InputBlocked=false;p.CombatEpoch++;mark.RunForFixture();Check(!mark.gameObject.activeInHierarchy&&Trace.Damage.Count==count,"stale mark retires without damaging the next epoch");
  }
  {
   var g=Game(1);var p=g.Player;var mark=new SummonerSpell();mark.BindForFixture(p,g);Time.deltaTime=.5f;Trace.Clear();for(int i=0;i<6;i++)mark.RunForFixture();Check(Trace.Damage.Count==SummonerDamageRules.MarkTicks+1&&!mark.gameObject.activeInHierarchy&&g.Enemies[0].StatusEffects.Knockups==1,"mark delivers six scheduled pulses plus exactly one finisher");Near(1000000-g.Enemies[0].Health,44,"mark finite tick and finisher damage budget preserved");
  }
 }
 static long Measure(Action cast){for(int i=0;i<64;i++){cast();Trace.Clear();}long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1024;i++){cast();Trace.Clear();}return GC.GetAllocatedBytesForCurrentThread()-before;}
 static void WarmAllocations()
 {
  var g=Game(24);var p=g.Player;p.guardRadius=20;var seq=new AdvancedSkillSequence{owner=p,session=g,range=4};var pet=new SummonedCompanion{session=g,BodyScale=5};
  var cases=new (string,Action)[]{("frost",()=>p.RunFrostPulse()),("venom spread",()=>{p.HeroClass=HeroClass.Ranger;p.ResetProc();p.ResolveSkillImpact(g.Enemies[0],0,7,10);}), ("elemental finale",()=>{p.Specialization=ElementalistSpecialization.Balanced;p.ElementalAdvancedArea(Vector3.zero,20,10,7,false);}), ("summoner impulse",()=>SummonerSpell.Cast(p,g,0,1,Vector3.zero,10,castId:7)), ("summoner interrupt dispatch",()=>p.RunSummonerDispatch(0,1,Vector3.zero,1,7)), ("treant",()=>pet.RunTreant(10)), ("chain",()=>seq.RunChain(10))};
  foreach(var pair in cases){long bytes=Measure(pair.Item2);Check(bytes==0,"warm "+pair.Item1+" managed allocation");Console.WriteLine("PASS warm "+pair.Item1+": "+bytes+" bytes / 1024 live dispatches");}
  long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1024;i++){var old=g.Enemies.ToArray();Check(old.Length==24,"array baseline keeps target count");}long baseline=GC.GetAllocatedBytesForCurrentThread()-before;Check(baseline>=1024*24*IntPtr.Size,"ToArray allocation contrast records copied references");Console.WriteLine("ToArray baseline: "+baseline+" bytes / 1024 target captures (24 enemies)");
  ClearedBuffers(typeof(CombatTargetSnapshot<EnemyController>),"all warm snapshots released");ClearedBuffers(typeof(CombatTargetSet<EnemyController>),"all warm chain sets released");
 }
 static void Main(){SnapshotContract();DispatchBehavior();PullAndSchedule();WarmAllocations();Console.WriteLine("PASS: "+checks+" live target admission/order/reentry/pause/epoch/pull/budget/allocation assertions; managed boundaries only, no device FPS claim");}
}
