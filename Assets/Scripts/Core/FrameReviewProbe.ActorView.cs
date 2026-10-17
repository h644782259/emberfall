using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    internal sealed partial class FrameReviewProbe
    {
        [Serializable] private sealed class ActorViewEstimate
        {
            public string category;
            public int renderers, shadowCasters;
            public long triangles;
        }

        // Snapshot only, outside the timed sample. Bounds/frustum intersection
        // is conservative and does not prove an actual color or shadow draw.
        private static ActorViewEstimate[] CaptureActorViewEstimates(Camera camera)
        {
            if(camera==null)return Array.Empty<ActorViewEstimate>();
            var planes=GeometryUtility.CalculateFrustumPlanes(camera);
            var groups=new Dictionary<string,ActorViewEstimate>();
            foreach(var model in FindObjectsByType<CombatModel>())
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                // Nested models are visited independently.
                if(!renderer.enabled||renderer.GetComponentInParent<CombatModel>()!=model)continue;
                var filter=renderer.GetComponent<MeshFilter>();
                var skin=renderer as SkinnedMeshRenderer;
                var mesh=skin!=null?skin.sharedMesh:filter!=null?filter.sharedMesh:null;
                if(mesh==null)continue;
                bool intersects=GeometryUtility.TestPlanesAABB(planes,renderer.bounds)&&
                    (camera.cullingMask & (1<<renderer.gameObject.layer))!=0;
                string category;
                if(!intersects)category=renderer.isVisible?"outside-main-frustum-visible-to-other-pass":"outside-main-frustum-not-visible";
                else
                {
                    float diameter=ProjectedDiameter(camera,renderer.bounds);
                    category=diameter<=8?"main-frustum-at-most-8px":diameter<=16?"main-frustum-8-to-16px":
                        diameter<=32?"main-frustum-16-to-32px":diameter<=64?"main-frustum-32-to-64px":"main-frustum-over-64px";
                }
                if(!groups.TryGetValue(category,out var estimate))groups.Add(category,estimate=new ActorViewEstimate{category=category});
                estimate.renderers++;
                if(renderer.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.Off)estimate.shadowCasters++;
                for(int submesh=0;submesh<mesh.subMeshCount;submesh++)
                    if(mesh.GetTopology(submesh)==UnityEngine.MeshTopology.Triangles)estimate.triangles+=mesh.GetIndexCount(submesh)/3;
            }
            var result=new List<ActorViewEstimate>(groups.Values);
            result.Sort((a,b)=>string.CompareOrdinal(a.category,b.category));
            return result.ToArray();
        }

        internal static float ProjectedDiameter(Camera camera,Bounds bounds)
        {
            Vector2 low=new Vector2(float.PositiveInfinity,float.PositiveInfinity),high=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            for(int i=0;i<8;i++)
            {
                Vector3 corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                Vector3 pixel=camera.WorldToScreenPoint(corner);
                if(pixel.z<=camera.nearClipPlane)return float.PositiveInfinity;
                low=Vector2.Min(low,pixel);high=Vector2.Max(high,pixel);
            }
            return Mathf.Max(high.x-low.x,high.y-low.y);
        }
    }
}
