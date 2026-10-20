using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        private static void BuildRoomIdentity(Transform parent,WorldResources r,int index)
        {
            int theme=(int)((uint)index%5);
            Color[] stoneColors={new Color(.23f,.32f,.27f),new Color(.31f,.23f,.19f),new Color(.18f,.29f,.35f),new Color(.30f,.23f,.38f),new Color(.34f,.30f,.22f)};
            Color[] lights={new Color(.42f,.85f,.54f),new Color(1,.48f,.18f),new Color(.25f,.72f,1),new Color(.72f,.42f,1),new Color(1,.8f,.38f)};
            var floor=r.Material(stoneColors[theme]);var trim=r.Material(lights[theme]*.55f,false,VisualSurface.Metal);var glow=r.Material(lights[theme],true);
            float edge=WorldTraversal.SceneLocalArenaRadius+2;
            // Flush inlays do not register obstacles or raise the traversal surface.
            if(theme==0) {
                for(int i=0;i<7;i++)Primitive(parent,"Garden avenue paving",PrimitiveType.Cube,new Vector3(0,.025f,-10+i*3.3f),new Vector3(9,.018f,2.4f),floor);
                for(int side=-1;side<=1;side+=2)for(int z=-8;z<=8;z+=8)Tree(parent,r,new Vector3(side*edge,0,z),1.6f,z+73);
            } else if(theme==1) {
                for(int side=-1;side<=1;side+=2){Primitive(parent,"Forge outer buttress",PrimitiveType.Cube,new Vector3(side*edge,3,0),new Vector3(2,6,14),floor);for(int z=-8;z<=8;z+=8)Crystal(parent,r,new Vector3(side*edge,3,z),1.1f,glow);}
                for(int i=-2;i<=2;i++)Primitive(parent,"Forge flush brass lanes",PrimitiveType.Cube,new Vector3(i*3,.025f,0),new Vector3(.20f,.015f,24),trim);
            } else if(theme==2) {
                for(int i=0;i<3;i++)Ring(parent,r,"Observatory concentric floor",new Vector3(0,.025f,1),4+i*3,.12f,trim,false);
                for(int side=-1;side<=1;side+=2)for(int z=-9;z<=9;z+=9){Pillar(parent,r,new Vector3(side*edge,0,z),5,true);Crystal(parent,r,new Vector3(side*edge,5.5f,z),.9f,glow);}
            } else if(theme==3) {
                Primitive(parent,"Sanctuary diamond mosaic",PrimitiveType.Cylinder,new Vector3(0,.018f,0),new Vector3(24,.012f,24),floor);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Crystal(parent,r,new Vector3(Mathf.Sin(a)*edge,2,Mathf.Cos(a)*edge),1.5f,glow);}
                Ring(parent,r,"Sanctuary luminous seal",new Vector3(0,.04f,0),10,.10f,trim,false);
            } else {
                Primitive(parent,"Throne processional carpet",PrimitiveType.Cube,new Vector3(0,.03f,0),new Vector3(7,.018f,27),floor);
                for(int side=-1;side<=1;side+=2)for(int z=-9;z<=9;z+=6){Pillar(parent,r,new Vector3(side*edge,0,z),6,true);Primitive(parent,"Throne suspended gold standard",PrimitiveType.Cube,new Vector3(side*edge,5,z),new Vector3(.15f,3,1.6f),trim);}
            }
            RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,stoneColors[theme]*.32f,.65f);
            PointLight(parent,new Vector3(0,7,3),lights[theme],.8f,22);
        }
    }
}
