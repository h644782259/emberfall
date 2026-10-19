using UnityEngine;
namespace Emberfall
{
    internal sealed class FrostLaneVisual:MonoBehaviour
    {
        private Transform[] shards;private Vector3[] scales;private Material material;private float age,life;private GameSession game;
        internal static void Spawn(Transform parent,GameSession game,Vector3 forward,float width,float length,float life)
        {
            var root=new GameObject("Forward rectangular ice field");root.transform.SetParent(parent,false);root.transform.rotation=Quaternion.LookRotation(forward);
            var fx=root.AddComponent<FrostLaneVisual>();fx.life=life;fx.game=game;fx.material=CombatFx.NewGlow();fx.material.color=new Color(.3f,.8f,1f,.8f);
            fx.shards=new Transform[35];fx.scales=new Vector3[35];
            for(int i=0;i<35;i++)
            {
                var shard=ProceduralVisuals.Create("Raised frost crystal",PrimitiveType.Cube,fx.material).transform;shard.SetParent(root.transform,false);shard.GetComponent<MeshFilter>().sharedMesh=HeroSkillBodyAssets.Spell("Crystal");
                shard.localPosition=new Vector3(((i%5+.5f)/5f-.5f)*width,.3f,((i/5+.5f)/7f-.5f)*length);
                shard.localRotation=Quaternion.Euler(12f,45f+i*17f,14f);fx.scales[i]=new Vector3(.65f,.7f+(i%3)*.15f,.65f);shard.localScale=Vector3.zero;fx.shards[i]=shard;
                var renderer=shard.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
        }
        private void Update()
        {
            if(game==null||game.InputBlocked)return;age+=Time.deltaTime;
            float fade=Mathf.Clamp01((life-age)/.25f);
            for(int i=0;i<shards.Length;i++){float grow=Mathf.Clamp01((age-i/5*.025f)/.16f);shards[i].localScale=scales[i]*grow*fade*(.9f+.1f*Mathf.Sin(age*7+i));}
            if(age>=life)Destroy(gameObject);
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
