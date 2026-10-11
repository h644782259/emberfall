using System;
using UnityEngine;

namespace Emberfall
{
    // Development-only opt-in, reset on every player/domain startup. Never stored in a save.
    public static class BlenderPilotArt
    {
        public static bool Enabled;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayerStartup() { Enabled=false; }
        internal static bool ApplyMaterial(GameObject root)
        {
            Material material=Resources.Load<Material>("BlenderPilot/Pilot_Atlas_Standard");
            if(material==null||material.shader==null||!material.shader.isSupported||material.shader.name!="Standard"||
                !material.HasProperty("_MainTex")||!material.HasProperty("_MetallicGlossMap")||!material.IsKeywordEnabled("_METALLICGLOSSMAP"))return false;
            var albedo=material.GetTexture("_MainTex") as Texture2D;
            var metal=material.GetTexture("_MetallicGlossMap") as Texture2D;
            if(albedo==null||metal==null||albedo.width<2||albedo.height<2||metal.width<2||metal.height<2)return false;
            Renderer[] renderers=root.GetComponentsInChildren<Renderer>(true);bool hasGeometry=false;
            foreach(Renderer renderer in renderers)
            {
                var skinned=renderer as SkinnedMeshRenderer;var filter=renderer.GetComponent<MeshFilter>();
                if(renderer.enabled&&renderer.gameObject.activeInHierarchy&&
                    ((skinned!=null&&skinned.sharedMesh!=null)||(filter!=null&&filter.sharedMesh!=null)))hasGeometry=true;
            }
            if(!hasGeometry)return false;
            foreach(Renderer renderer in renderers)renderer.sharedMaterial=material;
            return true;
        }
        public static GameObject CreateProp(string name, Transform parent, Vector3 position)
        {
            if (!Enabled) return null;
            GameObject source = Resources.Load<GameObject>("BlenderPilot/" + name);
            if (source == null) return null;
            GameObject instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            if(!ApplyMaterial(instance)){instance.SetActive(false);UnityEngine.Object.Destroy(instance);return null;}
            return instance;
        }
    }

    // Sole writer of the imported skeleton. Explicit SampleAnimation means no automatic
    // Animator/Animation clock, root motion, animation events, or gameplay callbacks.
    public sealed class BlenderPilotVisual : MonoBehaviour
    {
        private AnimationClip idle, move, basic, hit, skill;
        private Transform[] sockets;
        // Explicit ownership: Root and lower body [0..9], upper body [10..18].
        private static readonly string[] BoneNames = { "Root", "Pelvis", "Thigh.L", "Shin.L", "Foot.L", "Thigh.R", "Shin.R", "Foot.R", "Tabard.L", "Tabard.R", "Spine", "Head", "Mantle", "UpperArm.L", "Forearm.L", "Hand.L", "UpperArm.R", "Forearm.R", "Hand.R" };
        private static readonly int[] BoneParents = { -1, 0, 1, 2, 3, 1, 5, 6, 1, 1, 1, 10, 10, 10, 13, 14, 10, 16, 17 };
        private Transform[] bones;
        private LocalPose[] basePose;
        private struct LocalPose
        {
            public Vector3 Position, Scale;
            public Quaternion Rotation;
            public LocalPose(Transform bone) { Position=bone.localPosition; Rotation=bone.localRotation; Scale=bone.localScale; }
            public void Apply(Transform bone) { bone.localPosition=Position; bone.localRotation=Rotation; bone.localScale=Scale; }
            public static LocalPose Blend(LocalPose a, LocalPose b, float weight)
            {
                if(weight<=0)return a;
                if(weight>=1)return b;
                return new LocalPose { Position=Vector3.Lerp(a.Position,b.Position,weight), Rotation=Quaternion.Slerp(a.Rotation,b.Rotation,weight), Scale=Vector3.Lerp(a.Scale,b.Scale,weight) };
            }
        }
        private bool BindLayers()
        {
            Transform[] parts=GetComponentsInChildren<Transform>(true);
            bones=new Transform[BoneNames.Length];basePose=new LocalPose[BoneNames.Length];
            for(int i=0;i<BoneNames.Length;i++)
            {
                foreach(Transform part in parts)if(part.name==BoneNames[i])
                { if(bones[i]!=null)return false; bones[i]=part; }
                if(bones[i]==null)return false;
                if(BoneParents[i]>=0 && bones[i].parent!=bones[BoneParents[i]])return false;
            }
            string[] names={ "Anchor_Pommel", "Anchor_Grip", "Anchor_Guard", "Anchor_BladeRoot", "Anchor_Tip" };
            sockets=new Transform[names.Length];
            for(int i=0;i<names.Length;i++)
            {
                foreach(Transform part in parts)if(part.name==names[i])
                { if(sockets[i]!=null)return false; sockets[i]=part; }
                if(sockets[i]==null || sockets[i].parent!=bones[18])return false;
            }
            return true;
        }
        public bool Ready { get; private set; }
        public static BlenderPilotVisual Create(Transform parent)
        {
            if (!BlenderPilotArt.Enabled) return null;
            GameObject source = Resources.Load<GameObject>("BlenderPilot/Vanguard");
            if (source == null) return null;
            GameObject root = Instantiate(source, parent, false);
            root.name = "Blender pilot visual (sampled skeleton)";
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            // Imported components can never compete with the manual sampler.
            foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
            { animator.applyRootMotion = false; animator.enabled = false; }
            foreach (Animation animation in root.GetComponentsInChildren<Animation>(true))
            { animation.playAutomatically = false; animation.enabled = false; }
            if(!BlenderPilotArt.ApplyMaterial(root)){root.SetActive(false);Destroy(root);return null;}
            var view = root.AddComponent<BlenderPilotVisual>();
            foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>("BlenderPilot/Vanguard"))
            {
                if (clip.name.EndsWith("Pilot_Idle", StringComparison.Ordinal)) view.idle = clip;
                else if (clip.name.EndsWith("Pilot_Move", StringComparison.Ordinal)) view.move = clip;
                else if (clip.name.EndsWith("Pilot_Basic", StringComparison.Ordinal)) view.basic = clip;
                else if (clip.name.EndsWith("Pilot_Hit", StringComparison.Ordinal)) view.hit = clip;
                else if (clip.name.EndsWith("Pilot_Skill", StringComparison.Ordinal)) view.skill = clip;
            }
            view.Ready = view.BindLayers() && ValidClip(view.idle) && ValidClip(view.move) && ValidClip(view.basic) && ValidClip(view.hit) && ValidClip(view.skill);
            if(view.Ready)view.Ready=BindingsMove(root,new[]{view.idle,view.move,view.basic,view.hit,view.skill});
            if (!view.Ready) { root.SetActive(false); Destroy(root); return null; }
            root.SetActive(false); // first visible frame must already have a sampled pose
            return view;
        }
        private static bool ValidClip(AnimationClip clip)
        {return clip!=null&&!clip.empty&&clip.length>0&&!float.IsInfinity(clip.length)&&!float.IsNaN(clip.length);}
        private static bool BindingsMove(GameObject root,AnimationClip[] clips)
        {
            // Runtime smoke check for curves targeting this imported hierarchy. Exact
            // Unity import/deformation acceptance remains an Editor/device check.
            Transform[] parts=root.GetComponentsInChildren<Transform>(true);
            var positions=new Vector3[parts.Length];var rotations=new Quaternion[parts.Length];var scales=new Vector3[parts.Length];
            for(int i=0;i<parts.Length;i++){positions[i]=parts[i].localPosition;rotations[i]=parts[i].localRotation;scales[i]=parts[i].localScale;}
            bool valid=true;
            try
            {
                foreach(AnimationClip clip in clips)
                {
                    bool changed=false;
                    for(int sample=0;sample<4;sample++)
                    {
                        for(int i=0;i<parts.Length;i++){parts[i].localPosition=positions[i];parts[i].localRotation=rotations[i];parts[i].localScale=scales[i];}
                        clip.SampleAnimation(root,clip.length*sample*.25f);
                        for(int i=0;i<parts.Length;i++)if(parts[i].localPosition!=positions[i]||parts[i].localRotation!=rotations[i]||parts[i].localScale!=scales[i])changed=true;
                    }
                    valid &= changed;
                }
            }
            catch(Exception){valid=false;}
            finally {for(int i=0;i<parts.Length;i++){parts[i].localPosition=positions[i];parts[i].localRotation=rotations[i];parts[i].localScale=scales[i];}}
            return valid;
        }
        public void Sample(float time, float phase, float speed, bool acting, bool isBasic, float actionProgress, float hurtAge)
        {
            BlenderPilotSample sample=BlenderPilotPosePolicy.Select(time,idle.length,phase,speed,acting,isBasic,actionProgress,hurtAge);
            if(sample.Clip==BlenderPilotClip.Hit || sample.Clip==BlenderPilotClip.Skill)
            {
                AnimationClip fullBody=sample.Clip==BlenderPilotClip.Hit?hit:skill;
                fullBody.SampleAnimation(gameObject,sample.NormalizedTime*fullBody.length);
                return;
            }
            // Use existing preview/world time and accepted gait phase, never a new animation clock.
            var layer=BlenderPilotPosePolicy.Layers(time,idle.length,phase,speed,sample.NormalizedTime);
            idle.SampleAnimation(gameObject,layer.IdleTime*idle.length);
            for(int i=0;i<bones.Length;i++)basePose[i]=new LocalPose(bones[i]);
            move.SampleAnimation(gameObject,layer.MoveTime*move.length);
            float velocity=layer.SpeedWeight;
            for(int i=0;i<bones.Length;i++)
                basePose[i]=LocalPose.Blend(basePose[i],new LocalPose(bones[i]),velocity);
            if(sample.Clip==BlenderPilotClip.Basic)
            {
                basic.SampleAnimation(gameObject,sample.NormalizedTime*basic.length);
                // Contact is already committed at .52: preserve it exactly, then settle only late in recovery.
                float weight=layer.UpperWeight;
                for(int i=10;i<bones.Length;i++)
                    LocalPose.Blend(basePose[i],new LocalPose(bones[i]),weight).Apply(bones[i]);
                for(int i=0;i<10;i++)basePose[i].Apply(bones[i]);
            }
            else for(int i=0;i<bones.Length;i++)basePose[i].Apply(bones[i]);
        }

        public bool Anchor(WeaponVisualAnchor anchor, out Vector3 position)
        {
            int index = (int)anchor - (int)WeaponVisualAnchor.SwordPommel;
            if (index >= 0 && index < sockets.Length && sockets[index] != null)
            { position = sockets[index].position; return true; }
            position = transform.position; return false;
        }
    }
}
