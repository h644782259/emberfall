using UnityEngine;

namespace Emberfall
{
    // Three fixed, clearly warned rows. Holes and vertical clearance are shared
    // by presentation and damage; nothing tracks the player after release.
    internal sealed class BossLayeredStrikes : MonoBehaviour
    {
        internal const int Rows=3, Columns=26;
        internal const float RowSpacing=6f, RowDelay=1.25f, WarningSeconds=1.5f, ActiveSeconds=.28f;
        internal const float GapWidth=5f, HalfDepth=.38f, JumpClearance=.6f;
        private EnemyController boss;
        private GameSession game;
        private PlayerController player;
        private int epoch;
        private Vector3 origin,forward,right;
        private float age,damage;
        private int geometryRevision=-1;
        private readonly int[] renderedPhase=new int[Rows];
        private readonly Transform[,] tiles=new Transform[Rows,Columns];
        private readonly bool[,] clear=new bool[Rows,Columns];
        private readonly bool[] hit=new bool[Rows];
        private readonly Renderer[,] renderers=new Renderer[Rows,Columns];
        private MaterialPropertyBlock block;
        internal static float GapCenter(int row){return (row-1)*3f;}
        internal static bool InGap(float lateral,int row,float bodyRadius)
        {return Mathf.Abs(lateral-GapCenter(row))<GapWidth*.5f-bodyRadius;}
        internal static BossLayeredStrikes Spawn(EnemyController boss,GameSession game,Vector3 forward,float damage)
        {
            var root=new GameObject("Boss / three staggered breach rows");
            var wave=root.AddComponent<BossLayeredStrikes>();
            wave.boss=boss;wave.game=game;wave.player=game.Player;wave.epoch=game.Player.CombatEpoch;
            wave.origin=boss.transform.position;wave.forward=CombatFx.Flat(forward).normalized;
            wave.right=Vector3.Cross(Vector3.up,wave.forward);wave.damage=damage;wave.block=new MaterialPropertyBlock();
            var resources=root.AddComponent<WorldResources>();
            var material=resources.Material(new Color(.55f,.18f,.055f),false,VisualSurface.Stone);
            SkillBodyMaterialLibrary.Apply(material,SkillBodyMaterialLibrary.Matter.Rock);
            for(int row=0;row<Rows;row++)for(int col=0;col<Columns;col++)
            {
                float lateral=col-(Columns-1)*.5f;
                if(Mathf.Abs(lateral-GapCenter(row))<GapWidth*.5f)continue;
                Vector3 at=wave.origin+wave.forward*(5f+row*RowSpacing)+wave.right*lateral;
                var tile=EnemySkillPhysicalVfx.Body(root.transform,"Warned fracture",material,false);
                tile.position=WorldTerrain.Ground(at,.04f);tile.rotation=Quaternion.LookRotation(wave.forward);
                tile.localScale=new Vector3(1f,.08f,HalfDepth*2);
                var renderer=tile.GetComponent<Renderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                wave.tiles[row,col]=tile;wave.renderers[row,col]=renderer;
            }
            game.Notify("断层冲击 · 从缺口穿过，或跳跃越过低矮横条");
            return wave;
        }
        private void Update()
        {
            if(game==null||boss==null||boss.IsDead||!boss.isActiveAndEnabled||game.Player!=player||player==null||player.IsDead||player.CombatEpoch!=epoch||!game.HasStarted||game.CombatEnded||game.CombatEffectsEnded)
            {Destroy(gameObject);return;}
            if(game.InputBlocked)return;
            float previousAge=age;age+=Time.deltaTime;
            bool refreshGeometry=geometryRevision!=WorldTraversal.Revision;geometryRevision=WorldTraversal.Revision;
            for(int row=0;row<Rows;row++)
            {
                float start=WarningSeconds+row*RowDelay;
                bool active=age>=start&&previousAge<start+ActiveSeconds,visible=age<start+ActiveSeconds;
                int phase=!visible?3:active?2:1;bool refreshVisual=renderedPhase[row]!=phase;renderedPhase[row]=phase;
                for(int col=0;col<Columns;col++)
                {
                    var tile=tiles[row,col];if(tile==null)continue;
                    Vector3 at=tile.position;
                    if(refreshGeometry)clear[row,col]=WorldTraversal.HasClearSweepCapsule(at-right*.5f,at+right*.5f,HalfDepth);
                    if(!refreshVisual&&!refreshGeometry)continue;
                    tile.gameObject.SetActive(visible&&clear[row,col]);if(!visible||!clear[row,col])continue;
                    tile.position=WorldTerrain.Ground(at,active?.25f:.04f);
                    tile.localScale=new Vector3(1f,active?.5f:.08f,HalfDepth*2);
                    block.Clear();block.SetColor("_Color",active?new Color(1f,.32f,.06f):new Color(.65f,.3f,.09f));
                    block.SetColor("_Tint",active?new Color(1f,.32f,.06f):new Color(.65f,.3f,.09f));
                    block.SetFloat("_Heat",active?1f:.45f+.35f*Mathf.Clamp01(age/start));
                    renderers[row,col].SetPropertyBlock(block);
                }
                if(!active||hit[row])continue;
                Vector3 offset=CombatFx.Flat(player.transform.position-origin);
                float lateral=Vector3.Dot(offset,right),along=Vector3.Dot(offset,forward);
                float elevation=player.transform.position.y-WorldTraversal.SurfaceHeight(player.transform.position,.45f);
                if(elevation>JumpClearance||InGap(lateral,row,.46f)||Mathf.Abs(along-(5f+row*RowSpacing))>HalfDepth+.46f)continue;
                for(int col=0;col<Columns;col++)
                    if(clear[row,col]&&Mathf.Abs(lateral-(col-(Columns-1)*.5f))<=.5f+.46f)
                    {hit[row]=true;player.TakeDamageFrom(damage,boss.DisplayName+" · 断层冲击",origin);break;}
            }
            if(age>WarningSeconds+(Rows-1)*RowDelay+ActiveSeconds+.2f)Destroy(gameObject);
        }
    }
}
