using UnityEngine;
namespace Emberfall
{
    // Project the arch's name into the HUD so portal mist/stone cannot cover its letters.
    // This is a non-interactive sign; the existing proximity trigger owns entry.
    public sealed class PortalNameplate : MonoBehaviour
    {
        private string title;
        private Color accent;
        private GUIStyle style;
        public static void Create(Transform parent,Vector3 entrance,string title,Color accent)
        {
            var root=new GameObject("Portal name · "+title);root.transform.SetParent(parent,false);
            root.transform.position=entrance+Vector3.up*4.4f;
            var sign=root.AddComponent<PortalNameplate>();sign.title=title;sign.accent=accent;
        }
        private void OnGUI()
        {
            var game=GameSession.Instance;var camera=Camera.main;
            if(Event.current.type!=EventType.Repaint||camera==null||game==null||!game.HasStarted||game.InDungeon||game.InputBlocked)return;
            Vector3 point=camera.WorldToScreenPoint(transform.position);
            if(point.z<=camera.nearClipPlane||(transform.position-camera.transform.position).sqrMagnitude>6400)return;
            if(point.x<0||point.x>Screen.width||point.y<0||point.y>Screen.height)return;
            float scale=HudLogicalScale.For(Screen.safeArea.width,Screen.safeArea.height);
            if(style==null)style=new GUIStyle{font=GameFont.Shared,alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            style.fontSize=Mathf.RoundToInt(20*scale);style.normal.textColor=accent;
            Vector2 textSize=style.CalcSize(new GUIContent(title));
            // Place the entire text above the arch crown, with a small screen-space gap.
            Rect caption=new Rect(point.x-(textSize.x+8*scale)*.5f,Screen.height-point.y-textSize.y-6*scale,textSize.x+8*scale,textSize.y);
            Matrix4x4 matrix=GUI.matrix;Color color=GUI.color;int depth=GUI.depth;
            try
            {
                GUI.matrix=Matrix4x4.identity;GUI.depth=10;
                GUI.color=Color.white;
                style.normal.textColor=new Color(.02f,.025f,.04f,.75f);
                GUI.Label(new Rect(caption.x+scale,caption.y+2*scale,caption.width,caption.height),title,style);
                style.normal.textColor=accent;GUI.Label(caption,title,style);
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
        }
    }
}
