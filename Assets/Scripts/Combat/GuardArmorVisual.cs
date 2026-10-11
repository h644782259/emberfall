using UnityEngine;
namespace Emberfall
{
    public sealed class GuardArmorVisual:MonoBehaviour
    {
        private EnemyController enemy;private Transform plates,core;private Material material,coreMaterial;private Renderer armor;
        private Transform coreSocket;private Renderer socketDecoration;private bool socketDecorationEnabled;
        private float hitUntil,openBlend,appearanceAge;private bool scraped;
        public static GuardArmorVisual Attach(EnemyController enemy)
        {
            if(enemy==null||enemy.Kind!=EnemyKind.Guardian||enemy.IsBoss)return null;
            var root=new GameObject("Guardian live material armor");root.transform.SetParent(enemy.transform,false);
            var visual=root.AddComponent<GuardArmorVisual>();visual.enemy=enemy;
            visual.material=new Material(Resources.Load<Shader>("HeroSkillBody")){name="Weathered guardian armor"};visual.material.SetFloat("_Surface",1);visual.material.SetColor("_Tint",new Color(.35f,.42f,.46f));
            SkillBodyMaterialLibrary.Bind(visual.material);
            var atlas=Resources.Load<Texture2D>("WorldArt/ActorMaterialAtlas");if(atlas!=null)visual.material.SetTexture("_BodyTex",atlas);
            var body=new GameObject("Layered breast and shoulder plates");body.transform.SetParent(root.transform,false);body.AddComponent<MeshFilter>().sharedMesh=HeroSkillBodyAssets.Armor;
            visual.armor=body.AddComponent<MeshRenderer>();visual.armor.sharedMaterial=visual.material;visual.plates=body.transform;visual.plates.localPosition=Vector3.up*.52f;
            visual.armor.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;visual.armor.receiveShadows=true;
            visual.coreMaterial=new Material(Resources.Load<Shader>("SpellSolidSurface"));visual.coreMaterial.SetFloat("_Surface",2);visual.coreMaterial.SetColor("_Tint",new Color(.7f,.24f,.06f));
            // This is the guardian's warm crystal, not an ice-textured field.
            // Keep its closed crystalline surface and the actual rig socket.
            foreach(var part in enemy.GetComponentsInChildren<Transform>())if(part.name=="Guardian ember crystal")
            {visual.coreSocket=part;visual.socketDecoration=part.GetComponent<Renderer>();visual.socketDecorationEnabled=visual.socketDecoration!=null&&visual.socketDecoration.enabled;break;}
            var core=new GameObject("Exposed guardian crystal heart");core.transform.SetParent(root.transform,false);core.AddComponent<MeshFilter>().sharedMesh=AuthoredSpellBases.Load("Crystal");core.AddComponent<MeshRenderer>().sharedMaterial=visual.coreMaterial;
            visual.core=core.transform;visual.core.localPosition=new Vector3(0,1.43f,.54f);visual.core.localScale=new Vector3(.18f,.44f,.17f);visual.Refresh();return visual;
        }
        public void RecordImpact(bool armorReduced,bool weakpointOpen)
        {
            if(!armorReduced&&!weakpointOpen)return;scraped=armorReduced;hitUntil=Time.time+.16f;
            if(enemy!=null)PhysicalSkillVfx.Impact(enemy.transform.position+enemy.transform.forward*.5f+Vector3.up*1.35f,-enemy.transform.forward,new Color(.7f,.58f,.35f),.4f);
            Refresh();
        }
        private void LateUpdate(){var game=GameSession.Instance;if(Time.deltaTime>0&&(game==null||!game.InputBlocked))Refresh(true);}
        private void Refresh(bool advance=false)
        {
            if(enemy==null||enemy.IsDead||!enemy.isActiveAndEnabled){Hide();return;}
            bool closed=enemy.GuardArmorClosed,flash=Time.time<hitUntil;plates.gameObject.SetActive(true);core.gameObject.SetActive(!closed);
            if(coreSocket!=null){core.position=coreSocket.position+enemy.transform.forward*.045f;core.rotation=coreSocket.rotation;}
            if(socketDecoration!=null)socketDecoration.enabled=socketDecorationEnabled&&closed;
            if(advance){appearanceAge+=Time.deltaTime;openBlend=Mathf.MoveTowards(openBlend,closed?0:1,Time.deltaTime/.12f);}
            // The real weakpoint state remains visible from its first frame;
            // only the rigid plate motion and material arrival are eased.
            plates.localRotation=Quaternion.Euler(-14*openBlend,0,0);
            float coverage=.35f+.65f*Mathf.SmoothStep(0,1,Mathf.Clamp01(appearanceAge/.08f));
            material.SetFloat("_Opacity",coverage);coreMaterial.SetFloat("_Opacity",coverage);
            material.SetColor("_Tint",flash&&scraped?new Color(.7f,.59f,.32f):new Color(.35f,.42f,.46f));
            coreMaterial.SetColor("_Tint",flash&&!scraped?new Color(1,.57f,.16f):new Color(.7f,.24f,.06f));
        }
        private void Hide(){if(plates!=null)plates.gameObject.SetActive(false);if(core!=null)core.gameObject.SetActive(false);if(socketDecoration!=null)socketDecoration.enabled=socketDecorationEnabled;}
        private void OnDisable(){Hide();hitUntil=0;}
        private void OnDestroy(){if(material!=null)Destroy(material);if(coreMaterial!=null)Destroy(coreMaterial);}
    }
}
