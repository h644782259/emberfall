using UnityEngine;
namespace Emberfall
{
    public sealed partial class PlayerController
    {
        private Renderer[] ghostRenderers;
        private Material[][] ghostOriginals;
        private Material ghostMaterial;
        internal void BeginGhost()
        {
            if(ghostMaterial!=null)return;
            guardTime=burnStrideTime=0;
            model.transform.localRotation=Quaternion.identity;
            ghostMaterial=new Material(Shader.Find("Sprites/Default")){color=new Color(.45f,.85f,1f,.34f)};
            ghostRenderers=model.GetComponentsInChildren<Renderer>();ghostOriginals=new Material[ghostRenderers.Length][];
            for(int i=0;i<ghostRenderers.Length;i++)
            {ghostOriginals[i]=ghostRenderers[i].sharedMaterials;var mats=new Material[ghostOriginals[i].Length];for(int j=0;j<mats.Length;j++)mats[j]=ghostMaterial;ghostRenderers[i].sharedMaterials=mats;}
        }
        internal void EndGhost()
        {
            if(ghostRenderers!=null)for(int i=0;i<ghostRenderers.Length;i++)if(ghostRenderers[i]!=null)ghostRenderers[i].sharedMaterials=ghostOriginals[i];
            if(ghostMaterial!=null)Destroy(ghostMaterial);ghostMaterial=null;ghostRenderers=null;ghostOriginals=null;
            if(model!=null){model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;}
        }
        private void AdvanceGhost()
        {
            if(session.Paused||session.BackgroundPaused||session.SceneLoading)return;
            if(Input.GetKeyDown(KeyCode.R)){session.ReviveInDungeon();return;}
            Vector2 input=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
#if EMBERFALL_VISUAL_VALIDATION
            if(VisualValidationPlayer.PerformanceRun)input=VisualValidationPlayer.PerformanceMove;
#endif
            Vector3 move=AdventureCamera.CameraRelativeMovement(input,Camera.main==null?null:Camera.main.transform);
            if(MobileControls.Active)move+=new Vector3(MobileControls.Move.x,0,MobileControls.Move.y);
            move=Vector3.ClampMagnitude(move,1);float dt=Mathf.Min(.05f,Time.unscaledDeltaTime);
            transform.position=WorldTraversal.Move(transform.position,move*stats.MoveSpeed*dt,.45f);
            if(move.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(move),dt*10);
            model.SetLocomotion(Vector3.zero,dt,stats.MoveSpeed,false);model.Animate(0,0,false);
            model.transform.localPosition=Vector3.up*(.35f+.06f*Mathf.Sin(Time.unscaledTime*2));
        }
    }
}
