using UnityEngine;
namespace Emberfall
{
    public sealed partial class AdventureCamera
    {
        private Camera viewCamera;


        private void UpdateVisibility()
        {
            var game=GameSession.Instance;
            if(viewCamera==null)viewCamera=GetComponent<Camera>();
            if(viewCamera!=null)
            {
                viewCamera.ResetProjectionMatrix();
                if(MobileControls.Active&&game!=null&&game.HasStarted)
                {
                    var layout=MobileControls.Layout;var health=layout.PlayerHealth;var safe=MobileControls.SafeArea;
                    // Anchor the feet just above the health bar, independent of zoom and pitch.
                    if(game.Player!=null)
                    {
                        float x=(safe.x+(health.X+health.Width*.5f)*layout.Scale)/Screen.width;
                        float y=(safe.y+safe.height-(health.Y-14f)*layout.Scale)/Screen.height;
                        Vector3 feet=viewCamera.WorldToViewportPoint(game.Player.transform.position);
                        if(feet.z>viewCamera.nearClipPlane)
                        {
                            Matrix4x4 projection=viewCamera.projectionMatrix;
                            projection.m02+=2f*(feet.x-x);
                            projection.m12+=2f*(feet.y-y);
                            viewCamera.projectionMatrix=projection;
                        }
                    }
                }
            }
            if(game==null||game.Player==null||!game.HasStarted||game.IsDead){CameraOcclusionSurface.RestoreAll();return;}
            var target=game.Player.AimTarget;
            bool protectTarget=target!=null&&!target.IsDead&&target.gameObject.activeInHierarchy&&CombatFx.Flat(target.transform.position-game.Player.transform.position).sqrMagnitude<=18*18;
            CameraOcclusionSurface.Advance(transform.position,game.Player.transform.position+Vector3.up*1.35f,
                game.Player.transform.position+Vector3.up*.18f,protectTarget?target.transform.position+Vector3.up:Vector3.zero,protectTarget,Time.unscaledDeltaTime);
        }
        private void RestoreVisibility(){CameraOcclusionSurface.RestoreAll();if(viewCamera!=null)viewCamera.ResetProjectionMatrix();}
    }
}
