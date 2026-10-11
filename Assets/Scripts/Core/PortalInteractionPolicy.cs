namespace Emberfall
{
 public static class PortalInteractionPolicy
 {
  public const float EntranceRadius=4.3f;
  public static bool IsNear(float distanceSquared){return distanceSquared>=0&&distanceSquared<EntranceRadius*EntranceRadius;}
  public static bool CanRequest(bool started,bool dead,bool inDungeon,bool selectionOpen,bool blocked,float distanceSquared)
  {return started&&!dead&&!inDungeon&&!selectionOpen&&!blocked&&IsNear(distanceSquared);}
 }
}
