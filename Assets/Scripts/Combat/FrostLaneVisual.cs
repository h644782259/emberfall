using UnityEngine;
namespace Emberfall
{
    internal sealed class FrostLaneVisual:MonoBehaviour
    {
        private Transform[] shards;private Vector3[] scales;private Material material;private float age,life;private GameSession game;private ElementalistSpellVisual mist;
        internal static void Spawn(Transform parent,GameSession game,Vector3 forward,float width,float length,float life)
        {
            var root=new GameObject("Forward rectangular ice field");root.transform.SetParent(parent,false);root.transform.rotation=Quaternion.LookRotation(forward);
            var fx=root.AddComponent<FrostLaneVisual>();fx.life=life;fx.game=game;fx.material=ElementalistSpellVisual.IceMaterial();
            fx.mist=new ElementalistSpellVisual(root.transform,ElementalistSpellVisual.Kind.Frost,width*.5f);
            fx.shards=new Transform[35];fx.scales=new Vector3[35];
            for(int i=0;i<35;i++)
            {
                var shard=ProceduralVisuals.Create("Raised frost crystal",PrimitiveType.Cube,fx.material).transform;shard.SetParent(root.transform,false);shard.GetComponent<MeshFilter>().sharedMesh=HeroSkillBodyAssets.Spell("Crystal");
                shard.localPosition=new Vector3(((i%5+.5f)/5f-.5f)*width,.3f,((i/5+.5f)/7f-.5f)*length);
                shard.localRotation=Quaternion.Euler(12f,45f+i*17f,14f);fx.scales[i]=new Vector3(.55f+(i%3)*.15f,1.1f+(i%4)*.4f,.65f);shard.localScale=Vector3.zero;fx.shards[i]=shard;
                var renderer=shard.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
        }
        private void Update()
        {
            if(game==null||game.InputBlocked)return;age+=Time.deltaTime;
            Draw(age);
            if(age>=life)Destroy(gameObject);
        }
        private void Draw(float time)
        {
            float fade=Mathf.Clamp01((life-time)/.25f);
            material.SetFloat("_Opacity",fade);material.SetFloat("_Phase",time);mist?.Draw(time,fade);
            for(int i=0;i<shards.Length;i++){float grow=Mathf.Clamp01((time-i/5*.025f)/.16f);shards[i].localScale=scales[i]*grow*fade*(.9f+.1f*Mathf.Sin(time*7+i));}
        }
        private void OnDestroy(){mist?.Dispose();if(material!=null)Destroy(material);}
    }
}
