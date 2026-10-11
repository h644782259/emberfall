using UnityEngine;
namespace Emberfall
{
    // Three persistent, allocation-bounded marks. Reads authoritative targeting;
    // never selects a target or changes a pending cast's snapshot.
    public sealed class CombatTargetFeedback : MonoBehaviour
    {
        private GameSession session;
        private Material material;
        private readonly LineRenderer[] marks=new LineRenderer[3];
        private readonly Vector3[] points=new Vector3[65];
        private PlayerController previousOwner;
        private int previousEpoch=-1;
        public void Initialize(GameSession game){session=game;}
        private void LateUpdate()
        {
            var hero=session==null?null:session.Player;
            if(hero==null||hero.IsDead||!session.HasStarted||session.InputBlocked){Hide();return;}
            if(previousOwner!=hero||previousEpoch!=hero.CombatEpoch)
            {Hide();previousOwner=hero;previousEpoch=hero.CombatEpoch;return;}
            Ensure();
            EnemyController pinned=hero.MobilePinnedTarget;
            EnemyController target=pinned!=null?pinned:hero.AimTarget,focus=hero.HeroClass==HeroClass.Summoner?SummonedCompanion.ExplicitFocus(hero):null;
            if(focus==null)focus=hero.FocusTarget;
            Mark(0,Valid(target)?target.transform.position:Vector3.zero,Valid(target),.72f,new Color(.6f,.9f,1f),pinned!=null?3:0);
            Mark(1,Valid(focus)?focus.transform.position:Vector3.zero,Valid(focus),1f,new Color(.45f,1f,.55f),1);
            var charge=hero.GetComponent<SkillChargeController>();
            // TargetPoint is the committed aim snapshot, never today's auto-target.
            Mark(2,charge!=null?charge.TargetPoint:Vector3.zero,charge!=null&&charge.IsCharging,.55f,new Color(1f,.83f,.25f),2);
        }
        private bool Valid(EnemyController enemy)
        {return enemy!=null&&!enemy.IsDead&&enemy.gameObject.activeInHierarchy&&session.Enemies.Contains(enemy);}
        private void Ensure()
        {
            if(material!=null)return;
            material=CombatFx.NewGlow();
            for(int i=0;i<marks.Length;i++)
            {
                var root=new GameObject(i==0?"Automatic aim bracket":i==1?"Companion focus diamond":"Locked charge point");root.transform.SetParent(transform,false);
                var line=root.AddComponent<LineRenderer>();marks[i]=line;line.sharedMaterial=material;line.useWorldSpace=true;
                line.widthMultiplier=.055f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
            }
        }
        private void Mark(int index,Vector3 center,bool show,float radius,Color color,int shape)
        {
            var line=marks[index];line.enabled=show;if(!show)return;
            center.y=.16f;line.startColor=line.endColor=color;
            if(shape==3)
            {
                points[0]=center+new Vector3(-radius,0,-radius);points[1]=center+new Vector3(-radius,0,radius);
                points[2]=center+new Vector3(radius,0,radius);points[3]=center+new Vector3(radius,0,-radius);points[4]=points[0];
                line.positionCount=5;for(int i=0;i<5;i++)line.SetPosition(i,points[i]);
            }
            else if(shape==1)
            {
                points[0]=center+Vector3.forward*radius;points[1]=center+Vector3.right*radius;
                points[2]=center-Vector3.forward*radius;points[3]=center-Vector3.right*radius;points[4]=points[0];
                line.positionCount=5;for(int i=0;i<5;i++)line.SetPosition(i,points[i]);
            }
            else
            {
                int count=shape==0?25:65;float arc=shape==0?Mathf.PI*1.5f:Mathf.PI*2;
                line.positionCount=count;
                for(int i=0;i<count;i++){float a=arc*i/(count-1);points[i]=center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;line.SetPosition(i,points[i]);}
                // Distinct unclosed automatic bracket vs a closed fixed golden cast mark.
            }
        }
        private void Hide(){foreach(var line in marks)if(line!=null)line.enabled=false;}
        private void OnDisable(){Hide();}
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
