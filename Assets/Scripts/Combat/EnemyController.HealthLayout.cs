using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class EnemyController
    {
        private static readonly List<Rect> healthScreenBoxes=new List<Rect>(48);
        private static int healthLayoutFrame=-1;
        private static Camera healthLayoutCamera;
        internal static bool OverlapsHealthLabel(Rect box,Camera camera)
        {
            LayoutHealthLabels(GameSession.Instance,camera);
            foreach(var occupied in healthScreenBoxes)if(box.Overlaps(occupied))return true;
            return false;
        }
        private static void LayoutHealthLabels(GameSession game,Camera camera)
        {
            if(game==null||camera==null)return;
            if(healthLayoutFrame==Time.frameCount&&healthLayoutCamera==camera)return;
            healthLayoutFrame=Time.frameCount;healthLayoutCamera=camera;healthScreenBoxes.Clear();
            foreach(var enemy in game.Enemies)
            {
                if(enemy==null||enemy.healthRoot==null||!enemy.gameObject.activeInHierarchy||enemy.IsDead||!(enemy.aggro||enemy.Health<enemy.MaxHealth||enemy.IsBoss))continue;
                float elevation=enemy.IsBoss?4.2f*enemy.BodyScale:enemy.Kind==EnemyKind.Slime?1.45f:2.6f;
                Vector3 anchor=enemy.transform.TransformPoint(Vector3.up*elevation),point=camera.WorldToScreenPoint(anchor);
                if(point.z<=0||point.x<0||point.x>camera.pixelWidth||point.y<0||point.y>camera.pixelHeight)continue;
                float width=enemy.IsBoss?2.16f*enemy.BodyScale:1.11f;
                float pixels=Mathf.Abs(camera.WorldToScreenPoint(anchor+camera.transform.right*width).x-point.x);
                float height=Mathf.Max(5,Mathf.Abs(camera.WorldToScreenPoint(anchor+camera.transform.up*.14f).y-point.y));
                Rect chosen=default(Rect);bool placed=false;
                for(int lane=0;lane<10;lane++)
                {
                    chosen=new Rect(point.x-pixels*.5f-3,point.y-height*.5f-2+lane*(height+5),pixels+6,height+4);
                    bool overlaps=false;
                    foreach(var occupied in healthScreenBoxes)if(chosen.Overlaps(occupied)){overlaps=true;break;}
                    if(!overlaps){placed=true;break;}
                }
                // A compact vertical stack keeps neighboring health bars distinct.
                enemy.healthRoot.gameObject.SetActive(placed);
                if(!placed)continue;
                enemy.healthRoot.position=camera.ScreenToWorldPoint(new Vector3(chosen.center.x,chosen.center.y,point.z));
                enemy.healthRoot.rotation=camera.transform.rotation;healthScreenBoxes.Add(chosen);
            }
        }
    }
}
