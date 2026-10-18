using UnityEngine;
namespace Emberfall
{
    // Visual landmark only: no collider or change to the capture footprint.
    public sealed class CaptureFlagVisual : MonoBehaviour
    {
        private Mesh cloth;
        private Material fabric;
        private readonly Vector3[] vertices=new Vector3[14];
        public static CaptureFlagVisual Create(Transform parent,Vector3 worldPosition)
        {
            var root=new GameObject("Capture point planted flag");root.transform.SetParent(parent,true);
            worldPosition.y=WorldTerrain.Height(worldPosition)+.04f;root.transform.position=worldPosition;
            var flag=root.AddComponent<CaptureFlagVisual>();var resources=root.AddComponent<WorldResources>();
            var metal=resources.Material(new Color(.55f,.40f,.18f),false,VisualSurface.Metal);
            flag.Part("Flag footing",PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(.46f,.12f,.46f),metal);
            flag.Part("Flag pole",PrimitiveType.Cylinder,new Vector3(0,1.65f,0),new Vector3(.09f,1.65f,.09f),metal);
            flag.Part("Flag finial",PrimitiveType.Sphere,new Vector3(0,3.35f,0),Vector3.one*.20f,resources.Material(new Color(1,.8f,.3f),true));
            flag.fabric=resources.Material(new Color(.15f,.85f,.72f),true);
            var banner=new GameObject("Two-sided capture banner");banner.transform.SetParent(root.transform,false);
            flag.cloth=new Mesh{name="Capture banner cloth"};flag.cloth.MarkDynamic();resources.Own(flag.cloth);
            var triangles=new int[72];for(int i=0;i<6;i++)
            {int a=i*2,b=a+1,c=a+2,d=a+3,j=i*12;int[] face={a,c,b,b,c,d,b,c,a,d,c,b};for(int k=0;k<12;k++)triangles[j+k]=face[k];}
            flag.UpdateCloth();flag.cloth.triangles=triangles;flag.cloth.RecalculateNormals();
            banner.AddComponent<MeshFilter>().sharedMesh=flag.cloth;var renderer=banner.AddComponent<MeshRenderer>();renderer.sharedMaterial=flag.fabric;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            return flag;
        }
        private void Part(string label,PrimitiveType shape,Vector3 position,Vector3 size,Material material)
        {
            // Visual-only geometry must not request collider types stripped from player builds.
            var part=ProceduralVisuals.Create(label,shape,material);part.transform.SetParent(transform,false);part.transform.localPosition=position;part.transform.localScale=size;
        }
        public void SetState(Color color){if(fabric==null)return;color.a=1;fabric.color=color;if(fabric.HasProperty("_EmissionColor"))fabric.SetColor("_EmissionColor",color*.35f);}
        private void LateUpdate(){UpdateCloth();}
        private void UpdateCloth()
        {
            if(cloth==null)return;
            for(int i=0;i<7;i++){float t=i/6f;float wave=EffectPreferences.ReducedEffects?0:Mathf.Sin(Time.time*2.5f-t*5)*.13f*t;
                vertices[i*2]=new Vector3(.06f+1.4f*t,3.12f-.12f*t,wave);
                vertices[i*2+1]=new Vector3(.06f+1.4f*t,2.2f+.2f*t,wave);}
            cloth.vertices=vertices;cloth.RecalculateBounds();
        }
    }
}
