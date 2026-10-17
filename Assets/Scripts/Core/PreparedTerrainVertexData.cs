using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Experimental: bake immutable decoration's terrain field while loading.
    // Positions/normals/collision are unchanged; the existing vertex shader consumes
    // this optional stream before wind. Do not use on moving or skinned meshes.
    internal static class PreparedTerrainVertexData
    {
        internal static bool Requested => System.Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW")=="performance-terrain-precompute-ab";
        internal static bool Enabled => Requested || (Application.isMobilePlatform &&
            System.Environment.GetEnvironmentVariable("EMBERFALL_ENABLE_TERRAIN_PRECOMPUTE") == "1");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Shader.SetGlobalFloat("_EmberPreparedTerrain", Enabled && !Requested ? 1 : 0);
        }
        internal static bool TryBake(Mesh mesh,Transform transform,Material[] materials)
        {
            if(mesh==null||!mesh.isReadable||transform==null||materials==null||materials.Length==0)return false;
            foreach(var material in materials)
                if(material==null||material.shader==null||material.shader.name!="Emberfall/Weathered World"||
                    !material.HasProperty("_TerrainFollow")||material.GetFloat("_TerrainFollow")==0)return false;
            // Preserve any authored secondary stream rather than overwrite it.
            var existing=new List<Vector4>();mesh.GetUVs(2,existing);if(existing.Count!=0)return false;
            var vertices=mesh.vertices;var data=new List<Vector4>(vertices.Length);
            foreach(var vertex in vertices)
            {
                var p=transform.TransformPoint(vertex);var field=HeightGradient(p);
                data.Add(new Vector4(field.x,field.y,field.z,-12345));
            }
            mesh.SetUVs(2,data);return true;
        }
        private static Vector3 Hill(float x,float z,float cx,float cz,float radius,float height)
        {
            float dx=x-cx,dz=z-cz,inv=1/(radius*radius),q=Mathf.Clamp01(1-(dx*dx+dz*dz)*inv);
            float slope=-6*height*q*q*inv;
            return new Vector3(height*q*q*q,slope*dx,slope*dz);
        }
        internal static Vector3 HeightGradient(Vector3 p)
        {
            if(!WorldTerrain.Enabled)return Vector3.zero;
            float x=p.x,z=p.z;
            if(WorldTerrain.Dungeon)
            {
                if(Mathf.Abs(Mathf.Abs(x)-3)<.021f||Mathf.Abs(Mathf.Abs(x)-6)<.021f||
                    Mathf.Abs(Mathf.Abs(z)-3.3f)<.021f||Mathf.Abs(Mathf.Abs(z)-5.3f)<.021f)
                    return new Vector3(WorldTerrain.Height(p),(WorldTerrain.Height(p+Vector3.right*.02f)-WorldTerrain.Height(p-Vector3.right*.02f))/.04f,
                        (WorldTerrain.Height(p+Vector3.forward*.02f)-WorldTerrain.Height(p-Vector3.forward*.02f))/.04f);
                float r=WorldTerrain.Radius,t=Mathf.Clamp01((Mathf.Abs(x)-3)/3),u=Mathf.Clamp01((Mathf.Abs(z)-3.3f)/2);
                float side=t*t*(3-2*t),crossing=u*u*(3-2*u);
                float sideSlope=2*t*(1-t)*Mathf.Sign(x),crossingSlope=3*u*(1-u)*Mathf.Sign(z);
                var field=Hill(x,z,-r*.52f,-r*.38f,r*.42f,2.4f)+Hill(x,z,r*.48f,r*.42f,r*.40f,2.1f)+
                    Hill(x,z,-r*.46f,r*.50f,r*.34f,1.4f)+Hill(x,z,r*.55f,-r*.40f,r*.35f,1.3f);
                return new Vector3(field.x*side*crossing,field.y*side*crossing+field.x*sideSlope*crossing,
                    field.z*side*crossing+field.x*side*crossingSlope);
            }
            return Hill(x,z,-13,-11,7,2)+Hill(x,z,16,1,5.5f,.85f)+Hill(x,z,-17,13,5,1.4f)+Hill(x,z,-20,-2,5,1.1f)+
                Hill(x,z,-24,-18,7.5f,3.1f)+Hill(x,z,23,13,8,2.6f)+Hill(x,z,-9,25,7,2.8f)+
                Hill(x,z,11,26,6.5f,1.8f)+Hill(x,z,27,-15,5,1.9f);
        }
    }
}
