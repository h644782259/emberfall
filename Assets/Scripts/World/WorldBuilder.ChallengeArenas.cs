using UnityEngine;
namespace Emberfall
{
 public static partial class WorldBuilder
 {
  // Distinct arena themes share an open, planar combat floor.
  private static void BuildChallengeArena(Transform parent,WorldResources r,int mode)
  {
   if(mode==2)
   {
    RenderSettings.ambientSkyColor=new Color(.48f,.52f,.65f);
    RenderSettings.ambientEquatorColor=new Color(.32f,.35f,.46f);
    RenderSettings.ambientGroundColor=new Color(.2f,.22f,.3f);
    RenderSettings.fogColor=new Color(.13f,.15f,.23f);
    RenderSettings.fogDensity=.009f;
   }
   Material stone=r.Material(mode==0?new Color(.18f,.29f,.25f):mode==1?new Color(.27f,.19f,.17f):new Color(.29f,.33f,.43f));
   Material edge=r.Material(mode==0?new Color(.32f,.42f,.31f):mode==1?new Color(.43f,.29f,.21f):new Color(.43f,.44f,.56f));
   Material glow=r.Material(mode==0?new Color(.3f,.77f,.56f):mode==1?new Color(1,.34f,.08f):new Color(.56f,.45f,1),true);
   Primitive(parent,"Challenge island foundation",PrimitiveType.Cylinder,new Vector3(0,-.8f,0),new Vector3(39,.7f,39),stone);
   Vector2[] rim=new Vector2[32];for(int n=0;n<rim.Length;n++){float a=n*Mathf.PI*2/rim.Length;rim[n]=new Vector2(Mathf.Cos(a)*19,Mathf.Sin(a)*19);}
   Surface(parent,r,"Challenge playable surface",rim,0,stone);Skirt(parent,r,"Challenge outer cliffs",rim,-2.4f,edge);
   if(mode==0)
   {
    // Flat objective courtyard with decoration outside the combat floor.
    Primitive(parent,"Verdant seal court",PrimitiveType.Cylinder,new Vector3(0,.018f,0),new Vector3(7.2f,.016f,7.2f),edge);
    for(int side=-1;side<=1;side+=2)Tree(parent,r,new Vector3(side*20,0,6),1.2f,2+side);
    for(int k=0;k<8;k++){float a=k*Mathf.PI/4;Primitive(parent,"Seal boundary crystal",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*3.45f,.07f,Mathf.Sin(a)*3.45f),new Vector3(.38f,.04f,.38f),glow);}
   }
   else if(mode==1)
   {
    // Decorative molten seams stay flush and never divide the navigable floor.
    Ribbon(parent,r,"Molten floor seam",new[]{new Vector3(-12,0,0),new Vector3(12,0,0)},.12f,.018f,glow);
    for(int side=-1;side<=1;side+=2)Crystal(parent,r,new Vector3(side*20,1,7),.9f,glow);
   }
   else
   {
    // Floor rings distinguish the arena without creating choke points.
    for(int k=0;k<3;k++)
    {float a=k*Mathf.PI*2/3;Vector3 p=new Vector3(Mathf.Sin(a)*10,0,Mathf.Cos(a)*10);
     Primitive(parent,"Duel ring floor",PrimitiveType.Cylinder,p+Vector3.up*.018f,new Vector3(9,.016f,9),edge);
     // Duel rings are floor inlays, with no pillars interrupting combat.
    }
    Crystal(parent,r,new Vector3(0,2.5f,18.5f),1.5f,glow);
   }
   PointLight(parent,new Vector3(-8,3,7),glow.color,1.1f,12);
  }
  private static void BuildBreakablePockets(Transform parent,int layout)
  {
   int level=GameSession.Instance==null||GameSession.Instance.Progression==null?1:GameSession.Instance.Progression.Profile.level;
   for(int side=-1;side<=1;side+=2)
   {
    DestructiblePropFactory.Create(parent,ArenaSizeRules.Point(layout,new Vector3(side*12.5f,0,-7)),DestructibleKind.Crate,level,false,PropRecovery.Energy);
    DestructiblePropFactory.Create(parent,ArenaSizeRules.Point(layout,new Vector3(side*11.5f,0,7.5f)),DestructibleKind.Pot,level,false,PropRecovery.Health);
   }
  }
 }
}
