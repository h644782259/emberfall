using UnityEngine;
namespace Emberfall
{
    // The caller supplies the same solid rectangle used by town navigation.
    // Low footing geometry stays opaque; only upper building renderers fade together.
    public sealed class BuildingOcclusionGroup:MonoBehaviour
    {
        private LineRenderer footprint;
        private Material footprintMaterial;
        public static void Configure(Transform root,Vector3 center,Vector2 size)
        {
            if(root==null||root.GetComponent<BuildingOcclusionGroup>()!=null)return;
            var group=root.gameObject.AddComponent<BuildingOcclusionGroup>();
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if(renderer.GetComponent<TextMesh>()!=null||renderer.bounds.max.y<=center.y+.5f)continue;
                CameraOcclusionSurface.Mark(renderer.gameObject);
                renderer.GetComponent<CameraOcclusionSurface>().AssignGroup(group);
            }
            var go=new GameObject("Solid building footprint while faded");go.transform.SetParent(root,false);
            group.footprint=go.AddComponent<LineRenderer>();var line=group.footprint;
            line.useWorldSpace=true;line.loop=true;line.positionCount=4;line.widthMultiplier=.07f;
            float x=size.x*.5f,z=size.y*.5f;
            line.SetPositions(new[]{center+new Vector3(-x,.07f,-z),center+new Vector3(-x,.07f,z),center+new Vector3(x,.07f,z),center+new Vector3(x,.07f,-z)});
            group.footprintMaterial=CombatFx.NewGlow();line.sharedMaterial=group.footprintMaterial;
            line.startColor=line.endColor=new Color(.82f,.8f,.64f,.85f);line.enabled=false;
        }
        internal void SetOccluded(bool value){if(footprint!=null)footprint.enabled=value;}
        private void OnDisable(){SetOccluded(false);}
        private void OnDestroy(){if(footprintMaterial!=null)Destroy(footprintMaterial);}
    }
}
