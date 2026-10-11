using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        private static void BuildRoomPortalShell(Transform parent,WorldResources resources,Vector3 localCenter)
        {
            WorldPortal3DVisual.Create(parent,resources,localCenter,3.8f,false,"Locked forward room travel passage");
            BuildPortalFocus(parent,resources,localCenter);
        }
        public static GameObject MakeRoomExitPortal(Transform world,Vector3 position)
        {
            WorldPortal3DVisual gate=null;
            foreach(var candidate in world.GetComponentsInChildren<WorldPortal3DVisual>(true))
                if((candidate.InteractionCenter-position).sqrMagnitude<.0025f){gate=candidate;break;}
            if(gate==null)
            {
                WorldResources resources=world.GetComponentInParent<WorldResources>();
                gate=WorldPortal3DVisual.Create(world,resources,world.InverseTransformPoint(position),3.8f,false,"Forward room travel passage");
            }
            var marker=new GameObject("Unlocked forward travel medium");marker.transform.SetParent(world,false);marker.transform.position=position;
            marker.AddComponent<WorldPortalUnlockVisual>().Bind(gate);
            Label(marker.transform,"Continue through doorway","前往下一间\n靠近传送口交互",position+Vector3.up*4.7f,.065f,new Color(.54f,.76f,.65f),false);
            return marker;
        }
    }
}
