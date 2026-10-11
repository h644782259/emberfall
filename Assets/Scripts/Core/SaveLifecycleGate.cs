using System;
namespace Emberfall
{
 /// <summary>Focus loss and app suspension often arrive together; one successful save owns that transition.</summary>
 public sealed class SaveLifecycleGate
 {
  private bool saved;
  public bool Observe(bool background,bool activeCharacter,Func<bool> save)
  {if(!background){saved=false;return true;}if(!activeCharacter||saved)return true;saved=save();return saved;}
 }
}
