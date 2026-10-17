namespace Emberfall
{
 public static class PortalInteractionPolicy
 {
  public const float EntranceRadius=4.3f;
  public const float ChapterX=-12f,DungeonX=12f,EntranceZ=-24f,SanctuaryRadius=8f;
  public static bool InSanctuary(float x,float z,float bodyRadius=0)
  {float radius=SanctuaryRadius+bodyRadius,dz=z-EntranceZ;return (x-ChapterX)*(x-ChapterX)+dz*dz<radius*radius||(x-DungeonX)*(x-DungeonX)+dz*dz<radius*radius;}
  public static bool IsNear(float distanceSquared){return distanceSquared>=0&&distanceSquared<EntranceRadius*EntranceRadius;}
  public static bool CanRequest(bool started,bool dead,bool inDungeon,bool selectionOpen,bool blocked,float distanceSquared)
  {return started&&!dead&&!inDungeon&&!selectionOpen&&!blocked&&IsNear(distanceSquared);}
 }
}
