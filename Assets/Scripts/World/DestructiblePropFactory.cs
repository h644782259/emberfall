using UnityEngine;
namespace Emberfall
{
    public static class DestructiblePropFactory
    {
        // Use only on optional paths/decorative pockets. Never place on a mandatory
        // bridge, entrance, objective or enemy spawn, or register as an enemy.
        public static DestructibleProp Create(Transform parent,Vector3 position,DestructibleKind kind,int level,
            bool blocksPath=false,PropRecovery recovery=PropRecovery.Energy)
        {
            float radius=kind==DestructibleKind.Rubble?.7f:kind==DestructibleKind.Crate?.52f:.4f;
            if(parent==null||!parent.gameObject.activeInHierarchy||!DestructibleProp.CanPlace(position,radius))return null;
            GameObject root=new GameObject("Breakable "+kind);root.transform.SetParent(parent,false);root.transform.position=position;
            WorldResources resources=parent.GetComponentInParent<WorldResources>();
            if(resources==null)resources=root.AddComponent<WorldResources>();
            var model=new GameObject("Intact prop").transform;model.SetParent(root.transform,false);
            Color tint=kind==DestructibleKind.Crate?new Color(.37f,.23f,.13f):kind==DestructibleKind.Pot?new Color(.49f,.27f,.18f):new Color(.35f,.4f,.43f);
            Material body=resources.Material(tint,false,kind==DestructibleKind.Crate?VisualSurface.Wood:VisualSurface.Stone);
            Material trim=resources.Material(new Color(.56f,.42f,.23f),false,VisualSurface.Metal);
            bool authored=(kind==DestructibleKind.Pot||kind==DestructibleKind.Rubble) &&
                BlenderSceneryArt.Create(kind==DestructibleKind.Pot?"WayfarerPot":"FracturedRubble",model,Vector3.zero,resources)!=null;
            if(authored) { /* Intact model remains owned by the same destruction controller. */ }
            else if(kind==DestructibleKind.Crate)
            {
                GameObject pilot=BlenderSceneryArt.CreatePilotProp("SupplyCrate",model,Vector3.zero);
                if(pilot==null)
                {
                Part(model,"Weathered crate",PrimitiveType.Cube,new Vector3(0,.52f,0),new Vector3(.86f,1.04f,.84f),body);
                for(int side=-1;side<=1;side+=2)
                {
                    Part(model,"Crate metal band",PrimitiveType.Cube,new Vector3(side*.31f,.53f,.44f),new Vector3(.085f,1.07f,.045f),trim);
                    Part(model,"Crate side brace",PrimitiveType.Cube,new Vector3(side*.44f,.52f,0),new Vector3(.055f,.1f,.84f),trim);
                }
                Part(model,"Crate lid seam",PrimitiveType.Cube,new Vector3(0,.94f,.44f),new Vector3(.85f,.045f,.04f),trim);
            }
            }
            else if(kind==DestructibleKind.Pot)
            {
                Part(model,"Ceramic body",PrimitiveType.Sphere,new Vector3(0,.43f,0),new Vector3(.68f,.78f,.68f),body);
                Part(model,"Ceramic neck",PrimitiveType.Cylinder,new Vector3(0,.85f,0),new Vector3(.31f,.11f,.31f),body);
                Part(model,"Pot rim",PrimitiveType.Cylinder,new Vector3(0,.97f,0),new Vector3(.42f,.025f,.42f),trim);
                Part(model,"Pot opening",PrimitiveType.Cylinder,new Vector3(0,.998f,0),new Vector3(.28f,.004f,.28f),resources.Material(new Color(.12f,.09f,.075f)));
            }
            else
            {
                for(int i=0;i<3;i++)
                {
                    var rock=Part(model,"Loose rubble",PrimitiveType.Sphere,new Vector3((i-1)*.3f,.27f+(i%2)*.22f,(i%2)*.16f),new Vector3(.75f,.65f,.66f),body);
                    rock.GetComponent<MeshFilter>().sharedMesh=ProceduralVisuals.WeatheredRock;rock.transform.localRotation=Quaternion.Euler(i*14f,i*53f,i*7f);
                }
            }
            var prop=root.AddComponent<DestructibleProp>();prop.Initialize(kind,level,radius,blocksPath,recovery,model,body);return prop;
        }
        private static GameObject Part(Transform parent,string name,PrimitiveType shape,Vector3 at,Vector3 size,Material material)
        {
            GameObject obj=ProceduralVisuals.Create(name,shape,material);obj.transform.SetParent(parent,false);obj.transform.localPosition=at;obj.transform.localScale=size;return obj;
        }
    }
}
