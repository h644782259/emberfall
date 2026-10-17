using UnityEngine;

namespace Emberfall
{
    // Large, camera-readable shapes supplement the smaller particle details.
    internal sealed class ElementalFieldVisual : MonoBehaviour
    {
        private static int activeCount;
        private bool counted;
        private SkillFlowVolume flowVolume;
        private float flowAge;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCount(){activeCount=0;if(fieldFlame!=null)Destroy(fieldFlame);if(fieldLightning!=null)Destroy(fieldLightning);fieldFlame=fieldLightning=null;}
        private ElementalCombatVfx.Element element;
        private float radius;
        private bool onBody;
        private Transform[] bodies;
        private Renderer[] bodyRenderers;
        private MaterialPropertyBlock surfaceBlock;
        private static Mesh fieldFlame,fieldLightning;
        private Mesh bodyMesh;
        private Material material;
        private Vector3[] placements;
        private float[] placementScales;
        private bool[] placed;
        private Vector3 placementPosition,placementScale;
        private Quaternion placementRotation;
        private int placementRevision=-1;
        private bool placementReady;
        private System.Func<float,float,float,bool> clearPlacement;
        private float bodyHorizontalRadius;
        private bool ClearPlacement(float x,float z,float extent)
        {
            Vector3 point=transform.TransformPoint(new Vector3(x,0,z));
            float scale=Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z));
            if(!onBody&&(element==ElementalCombatVfx.Element.Fire||element==ElementalCombatVfx.Element.Poison)&&!GroundSkillSurface.AttachedFootprintDry(point,extent*scale))return false;
            return CombatSight.Area(transform.position,point)&&CombatSight.VisualFootprint(point,point,extent*scale);
        }

        public static ElementalFieldVisual Spawn(Transform parent, ElementalCombatVfx.Element type, float size, bool body,CombatVisualPriority priority=CombatVisualPriority.SustainedBackground)
        {
            if(activeCount>=(MobileControls.Active?12:20))return null;
            GameObject obj = new GameObject(type + " Silhouette");
            obj.transform.SetParent(parent, false);
            if(CombatVisualLease.Attach(obj,priority)==null)return null;
            ElementalFieldVisual effect = obj.AddComponent<ElementalFieldVisual>();
            effect.Initialize(type, size, body);
            return effect;
        }

        private void Initialize(ElementalCombatVfx.Element type, float size, bool body,CombatVisualPriority priority=CombatVisualPriority.SustainedBackground)
        {
            if(!Application.isMobilePlatform&&Camera.main!=null)Camera.main.depthTextureMode|=DepthTextureMode.Depth;
            activeCount++;counted=true;
            element = type;
            radius = size;
            onBody = body;
            if(type==ElementalCombatVfx.Element.Fire)
            {
                flowVolume=new SkillFlowVolume(transform,SkillFlowVolume.Kind.Fire,size,onBody?1.1f:1.25f,new Color(1,.35f,.04f),!onBody,airy:!onBody);
                flowVolume.Draw(0,.35f);return;
            }
            int count = onBody ? 3 : MobileControls.Active ? 5 : 8;
            if(EffectPreferences.ReducedEffects)count=onBody?2:3;
            if(!onBody&&type==ElementalCombatVfx.Element.Poison)count=EffectPreferences.ReducedEffects?1:2;
            placements=new Vector3[count];placementScales=new float[count];placed=new bool[count];clearPlacement=ClearPlacement;
            if(fieldFlame==null)fieldFlame=AuthoredSpellBases.Load("Flame");
            if(fieldLightning==null)fieldLightning=AuthoredSpellBases.Load("Lightning");
            bodyMesh=type==ElementalCombatVfx.Element.Fire?fieldFlame:type==ElementalCombatVfx.Element.Lightning?fieldLightning:type==ElementalCombatVfx.Element.Poison&&!onBody?FilledSkillVfx.PoisonRoot:AuthoredSpellBases.Load(type==ElementalCombatVfx.Element.Poison?"Vine":"Crystal");
            if(bodyMesh==null)bodyMesh=ProceduralVisuals.WeatheredRock;
            if(bodyMesh!=null)
            {
                bodyHorizontalRadius=GroundSkillSurface.HorizontalRadius(bodyMesh);
                var shader=Resources.Load<Shader>("FilledSpell");
                material=new Material(shader!=null?shader:Shader.Find("Sprites/Default"));
                SkillBodyMaterialLibrary.Bind(material);
                if(type==ElementalCombatVfx.Element.Poison)SkillBodyMaterialLibrary.Apply(material,SkillBodyMaterialLibrary.Matter.Vine);
                else if(type==ElementalCombatVfx.Element.Ice)SkillBodyMaterialLibrary.Apply(material,SkillBodyMaterialLibrary.Matter.Ice);
                surfaceBlock=new MaterialPropertyBlock();bodies=new Transform[count];bodyRenderers=new Renderer[count];
                for(int i=0;i<count;i++)
                {
                    var piece=new GameObject(type==ElementalCombatVfx.Element.Fire?"Filled field flame":"Filled field lightning");
                    piece.transform.SetParent(transform,false);piece.AddComponent<MeshFilter>().sharedMesh=bodyMesh;
                    var renderer=piece.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;MobileSceneDepth.Track(renderer);
                    bool solid=type==ElementalCombatVfx.Element.Ice||type==ElementalCombatVfx.Element.Poison;
                    renderer.shadowCastingMode=solid&&!EffectPreferences.ReducedEffects?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=solid;
                    bodies[i]=piece.transform;bodyRenderers[i]=renderer;
                }
            }
            RefreshPlacement();
            Draw(Time.time); // Initial visible-side placement is applied before the first render.
        }

        private void RefreshPlacement()
        {
            if(onBody)return;
            if(placementReady&&placementRevision==WorldTraversal.Revision&&placementPosition.Equals(transform.position)&&
                placementRotation.Equals(transform.rotation)&&placementScale.Equals(transform.lossyScale))return;
            placementReady=true;placementRevision=WorldTraversal.Revision;placementPosition=transform.position;
            placementRotation=transform.rotation;placementScale=transform.lossyScale;
            float extent=element==ElementalCombatVfx.Element.Poison?.45f:element==ElementalCombatVfx.Element.Fire?.44f:.45f;
            for(int i=0;i<placements.Length;i++)
            {
                // Retain a still-valid envelope when unrelated cover changes or opens.
                if(placed[i]&&ClearPlacement(placements[i].x,placements[i].z,extent*placementScales[i]))continue;
                float angle=i*2.39996f,distance=radius*(.28f+(i%4)*.19f),x,z,fit;
                placed[i]=FilledVfxPlacement.TryPlace(Mathf.Cos(angle)*distance,Mathf.Sin(angle)*distance,radius,extent,clearPlacement,out x,out z,out fit);
                Vector3 contact=transform.TransformPoint(new Vector3(x,0,z));
                contact.y=WorldTraversal.SurfaceHeight(contact,.04f);
                placements[i]=transform.InverseTransformPoint(contact);placementScales[i]=fit;
                bodies[i].gameObject.SetActive(placed[i]);
            }
        }
        private void Update(){if(Time.deltaTime<=0||GameSession.Instance!=null&&GameSession.Instance.InputBlocked)return;if(flowVolume!=null){flowAge+=Time.deltaTime;flowVolume.Draw(flowAge,onBody?.6f:1);return;}RefreshPlacement();Draw(Time.time);}
        private void Draw(float time)
        {
            int count = bodies.Length;
            for (int i = 0; i < count; i++)
            {
                if(!onBody&&!placed[i])continue;
                float angle=i*2.39996f+time*.45f;
                Vector3 origin=onBody?new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius*.75f:placements[i];
                float fit=onBody?1:placementScales[i];
                if(bodies!=null)
                {
                    bool fire=element==ElementalCombatVfx.Element.Fire,poison=element==ElementalCombatVfx.Element.Poison;
                    float beat=.5f+.5f*Mathf.Sin(time*(fire?6:24)+i*1.7f);
                    float height=(onBody?.65f:fire?1.35f:1.55f)*(fire?.65f+beat*.35f:1);
                    // Ground shapes pivot at their roots, not the bbox center.
                    // Bent branches can extend beyond an off-center half-width.
                    float horizontal=onBody?Mathf.Max(bodyMesh.bounds.extents.x,bodyMesh.bounds.extents.z):bodyHorizontalRadius;
                    float radial=(fire?.28f:.3f)/Mathf.Max(.01f,horizontal)*fit;
                    float vertical=height/Mathf.Max(.01f,bodyMesh.bounds.size.y);
                    bodies[i].localScale=new Vector3(radial,vertical,radial);
                    bodies[i].localPosition=origin+Vector3.up*((onBody?-.42f:.006f)-bodyMesh.bounds.min.y*vertical);
                    bodies[i].localRotation=Quaternion.Euler(0,i*137.5f,0);
                    Color color=fire?new Color(1,.38f,.07f,onBody?.4f:.62f):poison?new Color(.24f,.33f,.12f,.85f):new Color(.25f,.63f,1,(onBody?.36f:.6f)*(.35f+beat*.65f));
                    surfaceBlock.SetColor("_Color",color);surfaceBlock.SetFloat("_Sculpted",1);
                    surfaceBlock.SetColor("_Tint",color);
                    surfaceBlock.SetFloat("_Element",fire?1:poison?4:element==ElementalCombatVfx.Element.Ice?2:3);surfaceBlock.SetFloat("_Seed",i*.37f);
                    surfaceBlock.SetFloat("_Opacity",1);surfaceBlock.SetFloat("_Progress",0);surfaceBlock.SetFloat("_Style",0);
                    bodyRenderers[i].SetPropertyBlock(surfaceBlock);
                }

            }
        }

        private void Release(){if(!counted)return;counted=false;activeCount=Mathf.Max(0,activeCount-1);}
        private void OnDisable(){Release();}
        private void OnEnable(){if(!gameObject.activeInHierarchy||material==null&&flowVolume==null||counted)return;if(activeCount>=(MobileControls.Active?12:20)){gameObject.SetActive(false);return;}activeCount++;counted=true;flowAge=0;}
        private void OnDestroy() { flowVolume?.Dispose();Release();if (material != null) Destroy(material); }
    }
}
