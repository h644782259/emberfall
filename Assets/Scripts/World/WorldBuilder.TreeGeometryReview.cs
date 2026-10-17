using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        static readonly Dictionary<MeshFilter,Mesh> reviewOriginalTrees=new Dictionary<MeshFilter,Mesh>();
        static readonly Dictionary<Mesh,Mesh> reviewCompactTrees=new Dictionary<Mesh,Mesh>();
        internal static int TreeReviewFilters => reviewOriginalTrees.Count;
        internal static void ApplyTreeGeometryReview(bool enabled)
        {
            if(!enabled){RestoreTreeGeometryReview();return;}
            foreach(var pair in naturalTrees)
            {
                if(reviewCompactTrees.ContainsKey(pair.Value[0]))continue;
                var compact=CreateNaturalTreeMeshes(pair.Key,true);
                for(int i=0;i<2;i++){compact[i].name=pair.Value[i].name+" (compact review)";reviewCompactTrees.Add(pair.Value[i],compact[i]);}
            }
            foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if(filter.sharedMesh!=null&&reviewCompactTrees.TryGetValue(filter.sharedMesh,out var compact))
                {reviewOriginalTrees.Add(filter,filter.sharedMesh);filter.sharedMesh=compact;}
        }
        internal static void RestoreTreeGeometryReview()
        {
            foreach(var pair in reviewOriginalTrees)if(pair.Key!=null)pair.Key.sharedMesh=pair.Value;
            reviewOriginalTrees.Clear();
        }
        internal static void DisposeTreeGeometryReview()
        {
            RestoreTreeGeometryReview();
            foreach(var mesh in reviewCompactTrees.Values)if(mesh!=null)
            {if(Application.isPlaying)Object.Destroy(mesh);else Object.DestroyImmediate(mesh);}
            reviewCompactTrees.Clear();
        }
    }
}
