using UnityEngine;
namespace Emberfall
{
    internal sealed class PhysicalSkillVfx:MonoBehaviour
    {
        private static Material stone,liquid;
        private static bool assetsReady;
        private GameSession session;private PlayerController player;private int epoch;
        private Transform[] pieces;private Renderer[] surfaces;private Vector3[] velocity,spin;
        private float[] extent;private int[] supportRevision;private Vector3[] scales;private MaterialPropertyBlock block;
        private float age,life;private bool slime;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){if(stone!=null)Destroy(stone);if(liquid!=null)Destroy(liquid);stone=liquid=null;assetsReady=false;}
        private static void StoneMaterial()
        {
            if(stone!=null)return;
            stone=new Material(Resources.Load<Shader>("HeroSkillBody")){name="Natural fractured stone"};
            SkillBodyMaterialLibrary.Apply(stone,SkillBodyMaterialLibrary.Matter.Rock);
        }
        private static void LiquidMaterial()
        {
            if(liquid!=null)return;
            StoneMaterial();liquid=new Material(stone){name="Wet granular contact material"};
            SkillBodyMaterialLibrary.Apply(liquid,SkillBodyMaterialLibrary.Matter.Mud);
        }
        // Run behind the scene-transition gate. Each procedural body and material
        // is cached on a separate frame; no temporary combat owner is required.
        internal static System.Collections.IEnumerator PrewarmAssets()
        {
            if(assetsReady)yield break;
            var gel=EnemySkillPhysicalVfx.GelMesh;yield return null;
            for(int variant=0;variant<3;variant++){var crust=EnemySkillPhysicalVfx.CrustMesh(variant);yield return null;}
            var rock=AuthoredProjectileMeshes.Load("MeteorRock")??ProceduralVisuals.WeatheredRock;yield return null;
            StoneMaterial();yield return null;
            LiquidMaterial();yield return null;
            assetsReady=true;
        }
        internal static GameObject Impact(Vector3 at,Vector3 incoming,Color tint,float radius=1,bool slime=false,CombatVisualPriority priority=CombatVisualPriority.RealContact)
        {
            var game=GameSession.Instance;if(game==null||game.Player==null||game.CombatEffectsEnded)return null;
            var root=new GameObject(slime?"Liquid contact droplets":"Material contact debris");root.transform.position=at;
            if(CombatVisualLease.Attach(root,priority)==null)return null;
            StoneMaterial();LiquidMaterial();
            var fx=root.AddComponent<PhysicalSkillVfx>();fx.session=game;fx.player=game.Player;fx.epoch=game.Player.CombatEpoch;fx.slime=slime;
            fx.life=slime?1.2f:1.8f;fx.block=new MaterialPropertyBlock();
            int count=EffectPreferences.ReducedEffects?4:priority>=CombatVisualPriority.RealContact?8:Mathf.Max(4,Mathf.RoundToInt(8*GraphicsQualityRuntime.Current.DecorativeEffectsScale));
            fx.pieces=new Transform[count];fx.surfaces=new Renderer[count];fx.velocity=new Vector3[count];fx.spin=new Vector3[count];fx.extent=new float[count];fx.scales=new Vector3[count];fx.supportRevision=new int[count];
            var rock=slime?EnemySkillPhysicalVfx.GelMesh:(AuthoredProjectileMeshes.Load("MeteorRock")??ProceduralVisuals.WeatheredRock);
            Vector3 rebound=CombatFx.Flat(incoming).normalized;
            for(int i=0;i<count;i++)
            {
                float angle=i*2.399963f;Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));float width=Mathf.Clamp(radius*.1f,.045f,.24f)*(1+(i%3)*.3f);
                var part=new GameObject(slime?"Viscous droplet":"Fractured material");part.transform.SetParent(root.transform,false);part.AddComponent<MeshFilter>().sharedMesh=rock;
                var renderer=part.AddComponent<MeshRenderer>();renderer.sharedMaterial=slime?liquid:stone;renderer.shadowCastingMode=EffectPreferences.ReducedEffects?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;
                fx.pieces[i]=part.transform;fx.surfaces[i]=renderer;fx.extent[i]=width*.7f;fx.scales[i]=new Vector3(width,width*(slime?1.2f:.7f),width*.85f);part.transform.localScale=fx.scales[i];
                fx.supportRevision[i]=-1;
                part.transform.position=at+radial*width+Vector3.up*width;
                fx.velocity[i]=(radial*.8f-rebound*.3f)*(1.2f+Mathf.Min(radius,3)*.6f)+Vector3.up*(1.1f+(i%3)*.65f);
                fx.spin[i]=new Vector3(61+i*23,95-i*7,73+i*11);part.transform.localRotation=Quaternion.Euler(fx.spin[i]);
                fx.block.SetColor("_Tint",Color.Lerp(tint,slime?new Color(.2f,.52f,.07f):new Color(.27f,.2f,.14f),slime?.22f:.7f));fx.block.SetFloat("_Opacity",1);renderer.SetPropertyBlock(fx.block);
            }
            return root;
        }
        private void LateUpdate()
        {
            if(session==null||session!=GameSession.Instance||player==null||player!=session.Player||epoch!=player.CombatEpoch||session.CombatEffectsEnded){Destroy(gameObject);return;}
            if(session.InputBlocked||Time.deltaTime<=0)return;age+=Time.deltaTime;if(age>=life){Destroy(gameObject);return;}
            for(int i=0;i<pieces.Length;i++)
            {
                Vector3 point=pieces[i].position;bool contact=PhysicalSkillMotion.AdvanceSupported(ref point,ref velocity[i],ref supportRevision[i],extent[i],Time.deltaTime,slime?.05f:.28f,slime?.35f:.68f);pieces[i].position=point;
                if(velocity[i].sqrMagnitude>.025f)pieces[i].Rotate(spin[i]*Time.deltaTime,Space.World);
                if(slime&&contact)pieces[i].localScale=Vector3.Lerp(pieces[i].localScale,new Vector3(scales[i].x*1.65f,scales[i].y*.38f,scales[i].z*1.65f),1-Mathf.Exp(-18*Time.deltaTime));
                surfaces[i].GetPropertyBlock(block);block.SetFloat("_Opacity",Mathf.SmoothStep(0,1,(life-age)/.35f));surfaces[i].SetPropertyBlock(block);
            }
        }
    }
}
