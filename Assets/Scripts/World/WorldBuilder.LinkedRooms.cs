using UnityEngine;
namespace Emberfall
{
 public static partial class WorldBuilder
 {
  private static void BuildLinkedRoom(Transform parent,WorldResources r,int room)
  {
   Material floor=r.Material(new Color(.19f,.23f,.27f)),wall=r.Material(new Color(.28f,.32f,.37f)),trim=r.Material(new Color(.45f,.56f,.58f));
   Material glow=r.Material(new Color(.27f,.8f,.75f),true);
   Primitive(parent,"Room foundation",PrimitiveType.Cylinder,new Vector3(0,-.8f,0),new Vector3(39,.7f,39),floor);
   Primitive(parent,"Room floor",PrimitiveType.Cube,new Vector3(0,-.04f,0),new Vector3(31,.08f,32),floor);
   for(int side=-1;side<=1;side+=2)
   {
    GameObject outerWall=Primitive(parent,"Outer hall wall",PrimitiveType.Cube,new Vector3(side*16,1.5f,0),new Vector3(1.1f,3,29),wall,cameraOccluder:true);
    RegisterSceneryBox(new Vector3(side*16,0,0),new Vector2(1.1f,29),outerWall);
    Pillar(parent,r,new Vector3(side*3.4f,0,15),3.5f,true);
   }
   BuildRoomPortalShell(parent,r,new Vector3(0,0,14));
   if(room==0)
   {
    for(int i=0;i<4;i++){int side=i%2==0?-1:1;Tree(parent,r,new Vector3(side*10,0,-6+i*4),1.2f,i+12);}
    Primitive(parent,"Broken entrance stair inlay",PrimitiveType.Cube,new Vector3(0,.02f,-8),new Vector3(12,.02f,4),trim);
   }
   else if(room==1)
   {
    for(int i=0;i<4;i++){float x=i%2==0?-7:7,z=-8+i*5;GameObject beam=Primitive(parent,"Archive fallen beam",PrimitiveType.Cube,new Vector3(x,.8f,z),new Vector3(8,1.6f,1.2f),wall,cameraOccluder:true);RegisterSceneryBox(new Vector3(x,0,z),new Vector2(8,1.2f),beam);}
   }
   else if(room==2)
   {
    Vector3[] stream={new Vector3(-16,0,0),new Vector3(0,0,1),new Vector3(16,0,0)};
    WorldTraversal.SetRiver(stream,3.2f,new Rect(-3.4f,-3,6.8f,7));BuildWaterSurface(parent,r,"Sunken room water",stream,3.2f,.02f,WaterEnvironment.Courtyard);
    Primitive(parent,"Sunken courtyard bridge",PrimitiveType.Cube,new Vector3(0,.05f,.5f),new Vector3(6.8f,.04f,6),trim);
    BuildBridgeWaterContact(parent,r,new Rect(-3.4f,-2.5f,6.8f,6),.07f);
    Rock(parent,r,new Vector3(-9,0,7),1.6f,11);Rock(parent,r,new Vector3(9,0,-7),1.7f,7);
   }
   else if(room==3)
   {
    Primitive(parent,"Star spring basin",PrimitiveType.Cylinder,new Vector3(0,.03f,2),new Vector3(7,.03f,7),trim);
    Crystal(parent,r,new Vector3(0,1.1f,2),.9f,glow);
    for(int side=-1;side<=1;side+=2)Primitive(parent,"Rest chamber bench",PrimitiveType.Cube,new Vector3(side*7,.45f,0),new Vector3(1.5f,.9f,5),wall);
   }
   else if(room==4)
   {
    for(int side=-1;side<=1;side+=2){Pillar(parent,r,new Vector3(side*8,0,-2),3.3f,true);Pillar(parent,r,new Vector3(side*8,0,7),4.1f,true);}
    Primitive(parent,"Throne battle seal",PrimitiveType.Cylinder,new Vector3(0,.024f,5),new Vector3(10,.02f,10),trim);
    Crystal(parent,r,new Vector3(0,3,17),1.6f,glow);
   }
   PointLight(parent,new Vector3(0,4,10),glow.color,1.3f,16);
  }
 }
}
