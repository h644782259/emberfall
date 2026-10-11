using UnityEngine;
namespace Emberfall
{
    // Actual melee contact owns this wake. Solid swords, fractured ground and
    // gravity-driven chips provide the silhouette without rings or cutting ribbons.
    internal sealed class BlenderSkillVfx : MonoBehaviour
    {
        private sealed class Part
        {
            internal Transform Transform;internal Renderer Renderer;
            internal Vector3 At,Scale,Position,Velocity;internal float Phase,SimulatedAge;
            internal int Motion,Surface;
        }
        private static Material material;
        private readonly System.Collections.Generic.List<Part> parts=new System.Collections.Generic.List<Part>();
        private MaterialPropertyBlock block;
        private readonly System.Collections.Generic.List<Mesh> anchoredBodies=new System.Collections.Generic.List<Mesh>();
        private PlayerController owner;private GameSession session;private int epoch;private float age,range;private bool shock;
        private const float Life=1.15f;
        private void Awake(){block=new MaterialPropertyBlock();}
        internal static bool TryPlay(PlayerController hero,float range,bool groundShock)
        {
            if(hero==null||hero.IsDead||float.IsNaN(range)||float.IsInfinity(range)||range<=0)return false;
            Mesh sword=HeroSkillBodyAssets.Spell("Sword"),ground=HeroSkillBodyAssets.GroundBreak,rock=HeroSkillBodyAssets.Rock;
            if(sword==null||ground==null||rock==null)return false;
            if(material==null)
            {
                var shader=Resources.Load<Shader>("HeroSkillBody");if(shader==null||!shader.isSupported)return false;
                material=new Material(shader);var texture=Resources.Load<Texture2D>("WorldArt/GravityBasalt");if(texture!=null)material.SetTexture("_BodyTex",texture);SkillBodyMaterialLibrary.Bind(material);
            }
            // A complete sword swing needs room. Near walls the clipped Filled
            // presentation retains the same contact point without crossing cover.
            float footprint=(groundShock?4.8f:3.4f)*range;
            if(!CombatSight.VisualFootprint(hero.transform.position,hero.transform.position,footprint))return false;
            var root=new GameObject(groundShock?"Solid ground-shock contact":"Forged whirlwind swords");
            root.transform.position=hero.transform.position;root.transform.rotation=Quaternion.LookRotation(hero.transform.forward);
            if(CombatVisualLease.Attach(root,CombatVisualPriority.ActionBody)==null)return true;
            var fx=root.AddComponent<BlenderSkillVfx>();fx.owner=hero;fx.session=GameSession.Instance;fx.epoch=hero.CombatEpoch;fx.range=range;fx.shock=groundShock;
            int cap=EffectPreferences.ReducedEffects?5:Application.isMobilePlatform?8:12;
            if(!groundShock)
            {
                for(int i=0;i<2;i++)fx.Add(sword,Vector3.up*(.85f+i*.08f),new Vector3(.9f,Mathf.Min(1.85f/range,1.5f),1.5f),0,i*Mathf.PI,1);
            }
            else
            {
                fx.Add(ground,new Vector3(0,.04f,2.5f),new Vector3(2.8f,1.4f,2f),1,0,0);
                fx.Add(sword,new Vector3(0,.08f,1.4f),new Vector3(1.1f,Mathf.Min(1.85f/range,1.8f),1.5f),3,0,1);
                fx.Add(ground,new Vector3(.25f,.06f,.75f),new Vector3(1.5f,1.1f,1.2f),1,1,0);
            }
            for(int i=0;fx.parts.Count<cap;i++)
                fx.Add(rock,groundShock?new Vector3((i%2==0?1:-1)*.15f,.12f,1.4f+i*.15f):Vector3.up*.3f,
                    new Vector3(.09f+(i%3)*.025f,.1f,.12f),2,i*2.399963f,0);
            fx.Sample();return true;
        }
        private void Add(Mesh mesh,Vector3 at,Vector3 scale,int motion,float phase,int surface)
        {
            bool anchored=motion==1;
            if(anchored){mesh=AnchoredImpactMesh.Create(mesh,transform,at*range,scale*range,Quaternion.identity);anchoredBodies.Add(mesh);}
            var child=new GameObject(surface==1?"Solid forged blade":"Textured ground fragment");child.transform.SetParent(transform,false);
            child.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=EffectPreferences.ReducedEffects?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;
            Vector3 radial=new Vector3(Mathf.Cos(phase),0,Mathf.Sin(phase));
            parts.Add(new Part{Transform=child.transform,Renderer=renderer,At=anchored?Vector3.zero:at*range,Scale=anchored?Vector3.one:scale*range,Motion=motion,Phase=phase,Surface=surface,
                Position=transform.TransformPoint(at*range),Velocity=transform.TransformDirection(radial*(1.5f+(phase%3)*.3f)+Vector3.up*(2.7f+(phase%2)*.4f))});
        }
        private void Update()
        {
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||session==null||GameSession.Instance!=session||session.Player!=owner||!session.HasStarted||session.CombatEffectsEnded){Retire();return;}
            if(session.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=Life){Retire();return;}Sample();
        }
        private void Sample()
        {
            foreach(var part in parts)
            {
                Vector3 at=part.At;Quaternion rotation=Quaternion.identity;float opacity=Mathf.Clamp01((Life-age)/.38f);
                if(part.Motion==0)
                {
                    float angle=part.Phase+Mathf.Min(age,.52f)*6f;
                    Vector3 radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));at+=radial*.75f*range;
                    rotation=Quaternion.Euler(74,angle*Mathf.Rad2Deg,12);opacity*=Mathf.Clamp01((.62f-age)/.22f);
                }
                else if(part.Motion==2)
                {
                    PhysicalSkillMotion.Advance(ref part.Position,ref part.Velocity,.065f*range,Mathf.Max(0,age-part.SimulatedAge),.28f,.62f);
                    part.SimulatedAge=age;at=transform.InverseTransformPoint(part.Position);
                    rotation=Quaternion.Euler(age*118+part.Phase*17,age*93,age*71);
                }
                else if(part.Motion==3){rotation=Quaternion.identity;opacity*=Mathf.Clamp01((.72f-age)/.3f);}
                else opacity*=Mathf.Clamp01((.85f-age)/.35f);
                part.Transform.localPosition=at;part.Transform.localScale=part.Scale;part.Transform.localRotation=rotation;
                float deadline=part.Motion==0?.62f:part.Motion==3?.72f:part.Motion==1?.85f:Life;
                opacity=Mathf.Clamp01((deadline-age)/.075f);
                block.SetColor("_Tint",part.Surface==1?new Color(.57f,.54f,.48f):new Color(.42f,.34f,.25f));
                block.SetFloat("_Surface",part.Surface);block.SetFloat("_Opacity",opacity);block.SetFloat("_Phase",age);
                block.SetFloat("_Heat",Mathf.Clamp01(1-age/.15f)*.4f);part.Renderer.SetPropertyBlock(block);
            }
        }
        private void OnDestroy(){foreach(var mesh in anchoredBodies)if(mesh!=null)Destroy(mesh);anchoredBodies.Clear();}
        private void Retire(){gameObject.SetActive(false);Destroy(gameObject);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){if(material!=null)Destroy(material);material=null;}
    }
}
