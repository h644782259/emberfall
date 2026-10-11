using UnityEngine;
namespace Emberfall
{
    // Cosmetic after-current of a confirmed chain hit; never applies damage or control.
    internal sealed class ChainElectrifiedVisual : MonoBehaviour
    {
        private const float Duration=2.2f;
        private EnemyController target;
        private PlayerController owner;
        private GameSession session;
        private int epoch,seed;
        private float age,radius=.66f;
        private Material volumeMaterial;
        private Transform[] branches;private Renderer[] branchRenderers;private MaterialPropertyBlock block;
        private int dischargeTick=-1;
        internal static void Attach(PlayerController source,EnemyController enemy)
        {
            if(source==null||enemy==null||enemy.IsDead)return;
            var existing=enemy.GetComponentInChildren<ChainElectrifiedVisual>();
            if(existing!=null&&existing.owner==source&&existing.epoch==source.CombatEpoch)
            {existing.age=0;return;}
            var root=new GameObject("Chain lightning body current");
            root.transform.SetParent(enemy.transform,false);
            if(CombatVisualLease.Attach(root,CombatVisualPriority.SustainedBackground)==null)return;
            var visual=root.AddComponent<ChainElectrifiedVisual>();
            visual.target=enemy;visual.owner=source;visual.session=GameSession.Instance;
            visual.epoch=source.CombatEpoch;
#if UNITY_6000_6_OR_NEWER
            visual.seed=enemy.GetEntityId().GetHashCode()&1023;
#else
            visual.seed=enemy.GetHashCode()&1023;
#endif
            var model=enemy.GetComponentInChildren<CombatModel>();
            Transform anchor;Vector3 flank,other;
            if(model!=null&&model.TryStatusAttachment(out anchor,out flank,out other)&&anchor!=null)
            {root.transform.SetParent(anchor,false);visual.radius=Mathf.Max(.66f,Mathf.Abs(flank.x)*.94f);}
            else root.transform.localPosition=Vector3.up*.8f;
            int count=EffectPreferences.ReducedEffects?2:3;
            visual.branches=new Transform[count];visual.branchRenderers=new Renderer[count];visual.block=new MaterialPropertyBlock();
            visual.volumeMaterial=VolumetricDischarge.Surface;
            for(int i=0;i<count;i++)
            {
                var branch=new GameObject("Branching electric volume");branch.transform.SetParent(root.transform,false);
                branch.AddComponent<MeshFilter>().sharedMesh=VolumetricDischarge.Body(i+visual.seed);
                visual.branchRenderers[i]=branch.AddComponent<MeshRenderer>();visual.branchRenderers[i].sharedMaterial=visual.volumeMaterial;
                visual.branchRenderers[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                branch.transform.localScale=new Vector3(.8f,1.3f,.8f);visual.branches[i]=branch.transform;
            }
            visual.Draw();
        }
        private void LateUpdate()
        {
            if(target==null||target.IsDead||!target.gameObject.activeInHierarchy||owner==null||owner.IsDead||
                owner.CombatEpoch!=epoch||session==null||GameSession.Instance!=session||session.Player!=owner||
                !session.HasStarted||session.CombatEnded||session.ModeFinished)
            {Retire();return;}
            if(session.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=Duration){Retire();return;}Draw();
        }
        private void Draw()
        {
            int tick=Mathf.FloorToInt(age/.11f);float fade=Mathf.Clamp01((Duration-age)/.4f);
            for(int i=0;i<branches.Length;i++)
            {
                if(tick!=dischargeTick)
                {
                    float side=i*Mathf.PI*2/branches.Length+tick*.71f;
                    branches[i].localPosition=new Vector3(Mathf.Cos(side)*radius*.8f,-.55f,Mathf.Sin(side)*radius*.8f);
                    branches[i].localRotation=Quaternion.Euler(0,side*Mathf.Rad2Deg,0);
                    branches[i].GetComponent<MeshFilter>().sharedMesh=VolumetricDischarge.Body(seed+i+tick);
                }
                float strikeAge=age-tick*.11f;
                block.SetColor("_Tint",new Color(.08f,.52f,1));block.SetFloat("_Opacity",fade*(strikeAge<.05f?1:.15f));block.SetFloat("_Phase",age);branchRenderers[i].SetPropertyBlock(block);
            }
            dischargeTick=tick;
        }

        private void Retire(){gameObject.SetActive(false);Destroy(gameObject);}

    }
}
