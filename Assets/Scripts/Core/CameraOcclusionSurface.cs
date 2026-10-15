using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Emberfall
{
    // Explicit world-only registration. Telegraphs, effects and actor materials
    // are never collected or modified by this system.
    public sealed class CameraOcclusionSurface : MonoBehaviour
    {
        private static readonly List<CameraOcclusionSurface> surfaces=new List<CameraOcclusionSurface>(256);
        private static int fadedCount;
        private Renderer visual;
        private readonly Dictionary<Renderer,Bounds> geometryBounds=new Dictionary<Renderer,Bounds>();
        private Renderer[] hierarchy;
        private Material[][] hierarchyOriginals;
        private Renderer[] fadeRenderers;
        private bool externalChange;
        private readonly Dictionary<Material,Material> hierarchyFades=new Dictionary<Material,Material>();

        private Material fade; // First owned single-renderer slot; kept for group state reporting.
        private float alpha=1;
        private BuildingOcclusionGroup group;
        private bool requested,heroRequested,targetRequested;
        private static readonly HashSet<BuildingOcclusionGroup> hitGroups=new HashSet<BuildingOcclusionGroup>(),admittedGroups=new HashSet<BuildingOcclusionGroup>();
        public static int LastOccluders {get;private set;}
        public static int LastHeroOccluders {get;private set;}
        public static int LastTargetOccluders {get;private set;}
        public static void Mark(GameObject root)
        {if(root!=null&&root.GetComponent<CameraOcclusionSurface>()==null)root.AddComponent<CameraOcclusionSurface>();}
        // One bounded registry entry for a complete authored prop. Shared bark/leaf
        // materials are cloned once per prop, with all-or-none material admission.
        public static void MarkHierarchy(GameObject root)
        {
            if(root==null)return;
            var surface=root.GetComponent<CameraOcclusionSurface>();
            if(surface==null)surface=root.AddComponent<CameraOcclusionSurface>();
            surface.Restore();surface.hierarchy=root.GetComponentsInChildren<Renderer>();
            surface.visual=surface.hierarchy.Length==0?null:surface.hierarchy[0];
            if(surface.visual!=null&&root.activeInHierarchy&&surface.enabled&&surfaces.Count<CameraVisibilityRules.MaximumSurfaces&&!surfaces.Contains(surface))surfaces.Add(surface);
        }
        internal void AssignGroup(BuildingOcclusionGroup value){group=value;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry(){surfaces.Clear();hitGroups.Clear();admittedGroups.Clear();fadedCount=0;LastOccluders=0;LastHeroOccluders=0;LastTargetOccluders=0;}
        private void OnEnable()
        {visual=hierarchy!=null&&hierarchy.Length>0?hierarchy[0]:GetComponent<Renderer>();if(visual!=null&&surfaces.Count<CameraVisibilityRules.MaximumSurfaces&&!surfaces.Contains(this))surfaces.Add(this);}
        public static float Nearest(Vector3 point)
        {
            float nearest=float.PositiveInfinity;
            foreach(var surface in surfaces)
                if(surface!=null&&surface.visual!=null&&(surface.hierarchy!=null||surface.visual.enabled))
                {
                    if(surface.hierarchy!=null){foreach(var renderer in surface.hierarchy)if(renderer!=null&&renderer.enabled)nearest=Mathf.Min(nearest,Vector3.Distance(point,surface.GeometryBounds(renderer).ClosestPoint(point)));}
                    else {Vector3 closest=surface.GeometryBounds(surface.visual).ClosestPoint(point);nearest=Mathf.Min(nearest,Vector3.Distance(point,closest));}
                }
            return nearest;
        }
        public static void Advance(Vector3 camera,Vector3 hero,float deltaTime)
        {Advance(camera,hero,hero,hero,false,deltaTime);}
        public static void Advance(Vector3 camera,Vector3 torso,Vector3 feet,Vector3 target,bool protectTarget,float deltaTime)
        {
            LastOccluders=0;LastHeroOccluders=0;LastTargetOccluders=0;hitGroups.Clear();admittedGroups.Clear();
            foreach(var surface in surfaces)
            {
                if(surface==null)continue;
                surface.externalChange=surface.hierarchyFades.Count>0&&!surface.BindingsIntact();
                if(surface.externalChange)surface.Restore();
                if(surface.visual==null)continue;
                Bounds bounds=surface.GeometryBounds(surface.visual);bounds.Expand(.7f);
                surface.heroRequested=surface.hierarchy!=null?(surface.HierarchyProtects(camera,torso)||surface.HierarchyProtects(camera,feet)):surface.visual.enabled&&(Protects(bounds,camera,torso)||Protects(bounds,camera,feet));
                surface.targetRequested=surface.hierarchy!=null?protectTarget&&surface.HierarchyProtects(camera,target):surface.visual.enabled&&protectTarget&&Protects(bounds,camera,target);
                surface.requested=surface.heroRequested||surface.targetRequested;
                if(surface.heroRequested)LastHeroOccluders++;
                if(surface.targetRequested)LastTargetOccluders++;
                if(surface.requested)LastOccluders++;
                if(surface.requested&&surface.group!=null)hitGroups.Add(surface.group);
            }
            // Reserve whole groups first. Never show half a roof because the material cap ran out.
            int available=CameraVisibilityRules.MaximumFaded-fadedCount;
            foreach(var group in hitGroups)
            {
                int needed=0;bool complete=true;
                foreach(var surface in surfaces)if(surface!=null&&surface.group==group)
                {if(!surface.CanFade){complete=false;break;}needed+=surface.RequiredFadeSlots;}
                if(complete&&CameraVisibilityRules.ReserveGroup(ref available,needed))admittedGroups.Add(group);
                else if(!complete)foreach(var surface in surfaces)if(surface!=null&&surface.group==group)surface.Restore();
            }
            foreach(var surface in surfaces)
            {
                if(surface==null||surface.visual==null||surface.group==null)continue;
                bool hit=admittedGroups.Contains(surface.group);
                surface.SetFade(hit,deltaTime);surface.group.SetOccluded(hit||surface.fade!=null||surface.hierarchyFades.Count>0);
            }
            foreach(var surface in surfaces)
            {
                if(surface==null||surface.visual==null||surface.group!=null)continue;
                surface.SetFade(surface.requested,deltaTime);
            }
        }
        private Bounds GeometryBounds(Renderer renderer)
        {
            Bounds cached;if(geometryBounds.TryGetValue(renderer,out cached))return cached;
            var filter=renderer.GetComponent<MeshFilter>();
            if(filter==null||filter.sharedMesh==null)return renderer.bounds;
            Bounds local=filter.sharedMesh.bounds;Vector3 min=local.min,max=local.max;
            Material material=renderer.sharedMaterial;
            bool terrain=material!=null&&material.HasProperty("_TerrainFollow")&&material.GetFloat("_TerrainFollow")>.5f;
            Bounds result=new Bounds();
            for(int corner=0;corner<8;corner++)
            {
                Vector3 point=renderer.transform.TransformPoint(new Vector3((corner&1)==0?min.x:max.x,(corner&2)==0?min.y:max.y,(corner&4)==0?min.z:max.z));
                if(terrain)point.y+=WorldTerrain.Height(point);
                if(corner==0)result=new Bounds(point,Vector3.zero);else result.Encapsulate(point);
            }
            // Midpoint terrain may be higher than the corners on a broad canopy.
            Vector3 center=renderer.transform.TransformPoint(local.center);
            if(terrain){center.y+=WorldTerrain.Height(center);result.Encapsulate(center);}
            geometryBounds.Add(renderer,result);return result;
        }
        private bool HierarchyProtects(Vector3 camera,Vector3 point)
        {foreach(var renderer in hierarchy)if(renderer!=null&&renderer.enabled){Bounds bounds=GeometryBounds(renderer);bounds.Expand(.7f);if(Protects(bounds,camera,point))return true;}return false;}
        private Renderer[] Renderers {get{return hierarchy??new[]{visual};}}
        // Negative means one slot is unsupported: never admit a partial tree or building.
        private int RequiredFadeSlots
        {
            get
            {
                if(hierarchyFades.Count>0)return BindingsIntact()?0:-1;
                var materials=new HashSet<Material>();
                foreach(var renderer in Renderers)
                {
                    if(renderer==null)continue;
                    var slots=renderer.sharedMaterials;if(slots.Length==0)return -1;
                    foreach(var source in slots)
                    {if(source==null||!source.HasProperty("_Color"))return -1;materials.Add(source);}
                }
                return materials.Count;
            }
        }
        private bool CanFade {get{return visual!=null&&!externalChange&&RequiredFadeSlots>=0;}}
        private bool BindingsIntact()
        {
            for(int i=0;i<fadeRenderers.Length;i++)
            {
                var renderer=fadeRenderers[i];if(renderer==null)return false;
                var current=renderer.sharedMaterials;var saved=hierarchyOriginals[i];
                if(current.Length!=saved.Length)return false;
                for(int j=0;j<saved.Length;j++)
                {
                    Material owned;
                    if(saved[j]==null||!saved[j].HasProperty("_Color")||!hierarchyFades.TryGetValue(saved[j],out owned)||current[j]!=owned)return false;
                }
            }
            return true;
        }
        private static bool Protects(Bounds bounds,Vector3 camera,Vector3 point)
        {
            Vector3 min=bounds.min,max=bounds.max;
            return CameraVisibilityRules.ProtectsBox(camera.x,camera.y,camera.z,point.x,point.y,point.z,
                min.x,min.y,min.z,max.x,max.y,max.z);
        }
        private void SetFade(bool occluded,float delta)
        {
            occluded=occluded&&CanFade;
            if(occluded&&hierarchyFades.Count==0)
            {
                int needed=RequiredFadeSlots;
                if(needed<=0||needed>CameraVisibilityRules.MaximumFaded-fadedCount)return;
                fadeRenderers=Renderers;hierarchyOriginals=new Material[fadeRenderers.Length][];
                // Snapshot all slots before assigning any clone. Deduplicate across the
                // complete logical surface, including repeated slots on the same renderer.
                for(int i=0;i<fadeRenderers.Length;i++)
                {
                    var renderer=fadeRenderers[i];if(renderer==null){hierarchyOriginals[i]=new Material[0];continue;}
                    var slots=renderer.sharedMaterials;hierarchyOriginals[i]=slots;
                    foreach(var source in slots)if(!hierarchyFades.ContainsKey(source))
                    {
                        Material owned=new Material(source){name=hierarchy==null?"Camera fade (owned)":"Camera hierarchy fade (owned)"};
                        if(owned.HasProperty("_OcclusionFade")){owned.EnableKeyword("EMBER_WORLD_FADE");owned.renderQueue=2450;}
                        else
                        {
                            owned.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);owned.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                            owned.SetInt("_ZWrite",0);if(owned.HasProperty("_Mode"))owned.SetFloat("_Mode",2);
                            owned.DisableKeyword("_ALPHATEST_ON");owned.DisableKeyword("_ALPHAPREMULTIPLY_ON");owned.EnableKeyword("_ALPHABLEND_ON");owned.renderQueue=3000;
                        }
                        hierarchyFades.Add(source,owned);
                        if(hierarchy==null&&fade==null)fade=owned;
                    }
                }
                fadedCount+=hierarchyFades.Count;
                for(int i=0;i<fadeRenderers.Length;i++)if(fadeRenderers[i]!=null)
                {
                    var saved=hierarchyOriginals[i];var slots=new Material[saved.Length];
                    for(int j=0;j<saved.Length;j++)slots[j]=hierarchyFades[saved[j]];
                    fadeRenderers[i].sharedMaterials=slots;
                }
            }
            alpha=CameraVisibilityRules.FadeStep(alpha,occluded,delta);
            foreach(var pair in hierarchyFades)if(pair.Key!=null&&pair.Value!=null)
            {Color color=pair.Key.color;color.a*=alpha;pair.Value.color=color;if(pair.Value.HasProperty("_OcclusionFade"))pair.Value.SetFloat("_OcclusionFade",alpha);}
            if(!occluded&&alpha>.995f)Restore();
        }
        private void Restore()
        {
            if(hierarchyFades.Count>0)
            {
                for(int i=0;i<fadeRenderers.Length;i++)
                {
                    var renderer=fadeRenderers[i];if(renderer==null)continue;
                    var current=renderer.sharedMaterials;var saved=hierarchyOriginals[i];bool changed=false;
                    for(int j=0;j<current.Length&&j<saved.Length;j++)
                    {
                        Material owned;
                        // External replacements and resized material arrays belong to
                        // their caller; restore only slots still holding our exact clone.
                        if(!ReferenceEquals(saved[j],null)&&hierarchyFades.TryGetValue(saved[j],out owned)&&current[j]==owned)
                        {current[j]=saved[j];changed=true;}
                    }
                    if(changed)renderer.sharedMaterials=current;
                }
                fadedCount=Mathf.Max(0,fadedCount-hierarchyFades.Count);
                foreach(var owned in hierarchyFades.Values)if(owned!=null)Destroy(owned);
                hierarchyFades.Clear();hierarchyOriginals=null;fadeRenderers=null;
            }
            fade=null;alpha=1;
        }
        public static void RestoreAll(){foreach(var surface in surfaces)if(surface!=null){surface.Restore();if(surface.group!=null)surface.group.SetOccluded(false);}LastOccluders=0;LastHeroOccluders=0;LastTargetOccluders=0;}
        private void OnDisable(){surfaces.Remove(this);Restore();}
        private void OnDestroy(){surfaces.Remove(this);Restore();}
    }
}
