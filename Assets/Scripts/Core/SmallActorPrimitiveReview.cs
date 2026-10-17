using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Explicit frozen-scene experiment only. No normal-launch LOD policy.
    internal static class SmallActorPrimitiveReview
    {
        static readonly Dictionary<MeshFilter,Mesh> originals=new Dictionary<MeshFilter,Mesh>();
        static Mesh sphere,capsule;
        internal static int ChangedFilters => originals.Count;
        internal static long SavedTriangles {get;private set;}
        internal static void Apply(Camera camera,bool enabled)
        {
            Restore();
            if(!enabled||camera==null)return;
            var originalSphere=ProceduralVisuals.Shape(PrimitiveType.Sphere);
            var originalCapsule=ProceduralVisuals.Shape(PrimitiveType.Capsule);
            if(sphere==null)sphere=ProceduralVisuals.Build(VisualMeshRecipes.RoundBody(false,12,8),"Small actor sphere review");
            if(capsule==null)capsule=ProceduralVisuals.Build(VisualMeshRecipes.RoundBody(true,12,8),"Small actor capsule review");
            var planes=GeometryUtility.CalculateFrustumPlanes(camera);
            foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var mesh=filter.sharedMesh;
                if(mesh!=originalSphere&&mesh!=originalCapsule)continue;
                if(filter.GetComponentInParent<CombatModel>()==null)continue;
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null||!renderer.enabled||!GeometryUtility.TestPlanesAABB(planes,renderer.bounds))continue;
                if(FrameReviewProbe.ProjectedDiameter(camera,renderer.bounds)>16)continue;
                var replacement=mesh==originalSphere?sphere:capsule;
                originals.Add(filter,mesh);filter.sharedMesh=replacement;
                SavedTriangles+=(mesh.GetIndexCount(0)-replacement.GetIndexCount(0))/3;
            }
        }
        internal static void Restore()
        {
            foreach(var pair in originals)if(pair.Key!=null)pair.Key.sharedMesh=pair.Value;
            originals.Clear();SavedTriangles=0;
        }
        internal static void Dispose()
        {
            Restore();
            foreach(var mesh in new[]{sphere,capsule})if(mesh!=null)
            {if(Application.isPlaying)Object.Destroy(mesh);else Object.DestroyImmediate(mesh);}
            sphere=capsule=null;
        }
    }
}
