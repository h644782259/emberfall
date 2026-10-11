using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        // Inspect completed meshes so post-creation rotations and all nested
        // scene transforms are reflected in the exact support triangles.
        private static void RegisterGroundSurfaceSupport(Transform root)
        {
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                string name=filter.gameObject.name.ToLowerInvariant();
                bool floor=name.Contains("floor")||name.Contains("court")||name.Contains("rug")||name.Contains("road")||name.Contains("path")||name.Contains("paving")||name.Contains("mosaic")||name.Contains("inset")||name.Contains("threshold")||name.Contains("plank")||name.Contains("crossing")||name.Contains("nave")||name.Contains("transept")||name.Contains("altar approach")||name.Contains("playable surface")||name.Contains("footprint")||name.Contains("trail")||name.Contains("clearing");
                if(!floor)continue;
                Mesh mesh=filter.sharedMesh;var renderer=filter.GetComponent<Renderer>();
                if(mesh==null||!mesh.isReadable||renderer==null||!renderer.enabled)continue;
                Vector3[] local=mesh.vertices,points=new Vector3[local.Length];float low=float.PositiveInfinity,high=float.NegativeInfinity;
                for(int i=0;i<points.Length;i++){points[i]=filter.transform.TransformPoint(local[i]);low=Mathf.Min(low,points[i].y);high=Mathf.Max(high,points[i].y);}
                // Furniture, roofs, galleries and tall silhouettes are solids,
                // never thin walkable floor merely because their name matches.
                if(high-low>.12f||high>.13f||low<-.12f)continue;
                Material material=renderer.sharedMaterial;bool terrain=material!=null&&material.HasProperty("_TerrainFollow")&&material.GetFloat("_TerrainFollow")>.5f;
                int[] triangles=mesh.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector3 a=points[triangles[i]],b=points[triangles[i+1]],c=points[triangles[i+2]];
                    Vector3 normal=Vector3.Cross(b-a,c-a);
                    if(normal.y<=.0000001f||normal.y<normal.magnitude*.8f)continue;
                    WorldTraversal.AddSurfaceTriangle(a,b,c,terrain);
                }
            }
        }
    }
}
