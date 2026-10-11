namespace Emberfall
{
 /// <summary>A terminal adventure freezes both damage and economy callbacks in the same frame.</summary>
 public static class AdventureResultPolicy
 {
  public static bool AcceptsDamage(bool started,bool terminal){return started&&!terminal;}
  public static bool AcceptsKill(bool started,bool terminal,bool registered){return AcceptsDamage(started,terminal)&&registered;}
  // A dismissed victory allows scenery/skill play while terminal combat and
  // economy admission remain closed. Practice and failed runs do not reopen.
  public static bool AcceptsPlayerEffects(bool started,bool terminal,bool dungeon,bool cleared,bool practice,bool dismissed)
  {return started&&(!terminal||dungeon&&cleared&&!practice&&dismissed);}
 }
}
