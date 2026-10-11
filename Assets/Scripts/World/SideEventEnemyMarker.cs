using UnityEngine;
namespace Emberfall
{
    // Bound to the two registered enemies, not to the destructible source crystal.
    public sealed class SideEventEnemyMarker:MonoBehaviour
    {
        private EnemyController enemy;
        private GameSession session;
        private TextMesh label;
        private int shown=-1;
        public static void Attach(EnemyController enemy,GameSession session)
        {
            if(enemy==null||session==null)return;
            var existing=enemy.GetComponentInChildren<SideEventEnemyMarker>(true);
            if(existing!=null&&existing.isActiveAndEnabled)return;
            var root=new GameObject("Crystal-linked enemy marker");root.transform.SetParent(enemy.transform,false);
            root.transform.localPosition=new Vector3(0,2.95f,0);
            var marker=root.AddComponent<SideEventEnemyMarker>();marker.enemy=enemy;marker.session=session;
            marker.label=root.AddComponent<TextMesh>();marker.label.fontSize=48;marker.label.characterSize=.055f;
            marker.label.anchor=TextAnchor.MiddleCenter;marker.label.alignment=TextAlignment.Center;
            marker.label.color=new Color(.60f,.94f,1f);marker.Refresh();
            if(root.activeSelf&&marker.label!=null)root.AddComponent<WorldLabelPresentation>().Initialize(marker.label,2);
        }
        private void LateUpdate(){Refresh();}
        private void Refresh()
        {
            if(session==null||enemy==null||!session.IsSideEventEnemy(enemy))
            {gameObject.SetActive(false);Destroy(gameObject);return;}
            int remaining=session.SideEventEnemiesRemaining;
            if(shown==remaining||label==null)return;
            shown=remaining;label.text=remaining==2?"晶核守卫 2/2":"晶核守卫 1/2";GameFont.Apply(label);
        }
        private void OnDisable(){if(label!=null){var visual=label.GetComponent<Renderer>();if(visual!=null)visual.enabled=false;}}
    }
}
