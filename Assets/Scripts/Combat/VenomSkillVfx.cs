using UnityEngine;
namespace Emberfall
{
    internal static class VenomSkillVfx
    {
        internal static void Contact(PlayerController owner,Vector3 point,bool consumed)
        {
            if(owner==null||owner.IsDead)return;
            var root=new GameObject(consumed?"Three poison seeds consumed":"Venom arrow contact");
            root.transform.position=point;
            if(CombatVisualLease.Attach(root,consumed?CombatVisualPriority.RealContact:CombatVisualPriority.Decoration)==null)return;
            root.AddComponent<VenomContactVisual>().Initialize(owner,consumed);
        }
    }
    // A body-local sized mark, not an expanding ground radius or damage-area preview.
    internal sealed class VenomContactVisual:MonoBehaviour
    {
        private PlayerController owner;private int epoch;private float age,duration;
        private Material material;private Transform[] pieces;
        internal void Initialize(PlayerController player,bool consumed)
        {
            owner=player;epoch=player.CombatEpoch;duration=consumed?.55f:.45f;
            material=new Material(Resources.Load<Shader>("PhysicalContactSurface"));material.SetColor("_Tint",consumed?new Color(.4f,.7f,.1f):new Color(.18f,.48f,.09f));material.SetFloat("_Liquid",1);var grain=Resources.Load<Texture2D>("WorldArt/GravityBasalt");if(grain!=null)material.SetTexture("_DetailTex",grain);
            SkillBodyMaterialLibrary.Apply(material,SkillBodyMaterialLibrary.Matter.Mud);
            pieces=new Transform[consumed?3:1];
            for(int i=0;i<pieces.Length;i++)
            {
                var part=ProceduralVisuals.Create(consumed?"Consumed poison seed":"Physical venom nick",PrimitiveType.Cube,material);
                part.GetComponent<MeshFilter>().sharedMesh=EnemySkillPhysicalVfx.GelMesh;
                part.transform.SetParent(transform,false);
                part.transform.localPosition=consumed?new Vector3((i-1)*.12f,(i==1?.09f:0),0):Vector3.zero;
                part.transform.localScale=consumed?new Vector3(.08f,.13f,.08f):new Vector3(.1f,.14f,.1f);
                part.transform.localRotation=Quaternion.Euler(0,0,consumed?45:-25);
                part.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                pieces[i]=part.transform;
            }
        }
        private void Update()
        {
            var game=GameSession.Instance;
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||game==null||game.Player!=owner||!game.HasStarted||game.CombatEffectsEnded){Destroy(gameObject);return;}
            if(game.InputBlocked)return;
            age+=Time.deltaTime;if(age>=duration){Destroy(gameObject);return;}
            // Shrinking inward communicates expenditure of three stacks, never outward splash.
            material.SetFloat("_Opacity",Mathf.SmoothStep(0,1,(duration-age)/.18f));
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
