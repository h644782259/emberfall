using UnityEngine;
using UnityEngine.Rendering;
namespace Emberfall
{
    // A single bounded volume, driven by its producer's clock and lifetime.
    // It never owns combat timing, target selection, or damage.
    internal sealed class SkillFlowVolume
    {
        internal enum Kind { Fire, Frost, Poison, Spirit, Wind }
        private static Material shared;
        private static Texture3D flowNoise;
        internal static Texture3D FlowNoise
        {
            get
            {
                if(flowNoise!=null)return flowNoise;
                // Every volume consumer samples .r; other colour channels carry no data.
                const int size=32;var pixels=new byte[size*size*size];uint state=1729;
                for(int i=0;i<pixels.Length;i++)
                {
                    state^=state<<13;state^=state>>17;state^=state<<5;
                    pixels[i]=(byte)(state>>24);
                }
                flowNoise=new Texture3D(size,size,size,TextureFormat.R8,false){name="Shared skill flow noise",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave};
                flowNoise.SetPixelData(pixels,0);flowNoise.Apply(false,true);return flowNoise;
            }
        }
        private readonly GameObject root;
        private readonly Renderer surface;
        private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){if(shared!=null)Object.Destroy(shared);shared=null;if(flowNoise!=null)Object.Destroy(flowNoise);flowNoise=null;}
        internal SkillFlowVolume(Transform parent,Kind kind,float radius,float height,Color tint,bool grounded=true,bool airy=false)
        {
            if(shared==null){shared=new Material(Resources.Load<Shader>("SkillFlowVolume"));shared.SetTexture("_FlowNoise",FlowNoise);}
            root=new GameObject("Continuous skill volume / "+kind);root.transform.SetParent(parent,false);
            Vector3 dimensions=new Vector3(radius*2,height,radius*2),at=Vector3.zero;
            var shape=ProceduralVisuals.Shape(PrimitiveType.Cube);
            if(grounded&&!GroundSkillSurface.TryPlaceAttached(parent,shape,ref at,ref dimensions,Quaternion.identity,radius))
            {root.SetActive(false);return;}
            Vector3 support=parent.TransformPoint(at);support.y=WorldTraversal.SurfaceHeight(support,.02f);
            at.y=grounded?parent.InverseTransformPoint(support).y+dimensions.y*.5f:0;
            root.transform.localPosition=at;root.transform.localScale=dimensions;
            root.AddComponent<MeshFilter>().sharedMesh=shape;
            surface=root.AddComponent<MeshRenderer>();surface.sharedMaterial=shared;MobileSceneDepth.Track(surface);
            surface.shadowCastingMode=ShadowCastingMode.Off;surface.receiveShadows=false;
            properties.SetFloat("_Kind",(float)kind);properties.SetColor("_Tint",tint);
            // Large impact fields must reveal the floor and actors through the flame.
            properties.SetFloat("_Density",airy?.28f:1);
            properties.SetFloat("_AlphaLimit",airy?.24f:1);
            properties.SetFloat("_Sparsity",airy?.9f:0);
            properties.SetFloat("_Steps",EffectPreferences.ReducedEffects||Application.isMobilePlatform?6:10);
            Draw(0,0);
            if(!Application.isMobilePlatform&&Camera.main!=null)Camera.main.depthTextureMode|=DepthTextureMode.Depth;
        }
        internal void Draw(float age,float opacity)
        {
            if(surface==null)return;
            properties.SetFloat("_Clock",age);properties.SetFloat("_Opacity",Mathf.Clamp01(opacity));
            surface.SetPropertyBlock(properties);surface.enabled=opacity>.001f;
        }
        internal void Dispose(){if(root!=null){root.SetActive(false);Object.Destroy(root);}}
    }
}
