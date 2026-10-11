using UnityEngine;
namespace Emberfall
{
 /// <summary>Combat-time pulses share their damage footprint and warning clock.</summary>
 public sealed class ArenaHazards:MonoBehaviour
 {
  private GameSession session;private int mode;private float age;private Material material;
  private readonly LineRenderer[] rings=new LineRenderer[3],clocks=new LineRenderer[2],glyphs=new LineRenderer[2],holdSegments=new LineRenderer[24];
  private readonly Vector3[] centers=new Vector3[3],outline=new Vector3[48];private readonly int[] hitCycle={-1,-1};
  private LineRenderer holdFlag;
  public void Initialize(GameSession owner,int selectedMode)
  {
   session=owner;mode=selectedMode;material=ThreatVisualStyle.Material();
   centers[0]=mode==0?new Vector3(-8,0,0):mode==1?new Vector3(-5,0,-5):new Vector3(-5,0,3);
   centers[1]=mode==0?new Vector3(8,0,0):mode==1?new Vector3(5,0,5):new Vector3(5,0,-3);
   centers[2]=Vector3.zero;
   for(int i=0;i<2;i++)
   {
    rings[i]=Line("Environmental damage boundary",true,.1f);clocks[i]=Line("Environmental windup clock",false,.09f);
    glyphs[i]=Line(mode==0?"Thorn source glyph":mode==1?"Heat source glyph":"Eclipse source glyph",false,.10f);
    rings[i].enabled=clocks[i].enabled=glyphs[i].enabled=false;
    // Thorn fork, heat zigzag and eclipse diamond distinguish source without color swapping.
    Vector3[] shape=mode==0?new[]{new Vector3(-.35f,0,.15f),Vector3.zero,new Vector3(-.28f,0,-.28f),Vector3.zero,new Vector3(.3f,0,-.2f),Vector3.zero,new Vector3(.35f,0,.3f)}:
     mode==1?new[]{new Vector3(-.4f,0,-.2f),new Vector3(-.15f,0,.25f),new Vector3(.1f,0,-.2f),new Vector3(.35f,0,.25f)}:
     new[]{new Vector3(0,0,.38f),new Vector3(.3f,0,0),new Vector3(0,0,-.38f),new Vector3(-.3f,0,0),new Vector3(0,0,.38f)};
    glyphs[i].positionCount=shape.Length;for(int p=0;p<shape.Length;p++)glyphs[i].SetPosition(p,centers[i]+shape[p]+Vector3.up*.2f);
   }
   rings[2]=Line("Hold objective boundary 3.2m",true,.07f);Circle(rings[2],Vector3.zero,ExpeditionModeState.HoldPointRadius);rings[2].enabled=mode==0;rings[2].startColor=rings[2].endColor=new Color(.3f,1,.62f,.6f);
   if(mode==0)
   {
    for(int i=0;i<24;i++)
    {
     holdSegments[i]=Line("Hold captured segment",false,.11f);holdSegments[i].positionCount=4;
     for(int p=0;p<4;p++){float a=(i+(p/3f)*.72f)*Mathf.PI*2/24;holdSegments[i].SetPosition(p,new Vector3(Mathf.Cos(a)*(ExpeditionModeState.HoldPointRadius-.22f),.16f,Mathf.Sin(a)*(ExpeditionModeState.HoldPointRadius-.22f)));}
     holdSegments[i].enabled=false;
    }
    holdFlag=Line("Hold point flag",false,.07f);holdFlag.positionCount=5;holdFlag.startColor=holdFlag.endColor=new Color(.3f,1,.62f,.6f);
    holdFlag.SetPositions(new[]{new Vector3(0,.12f,0),new Vector3(0,1.35f,0),new Vector3(.55f,1.2f,0),new Vector3(0,1.05f,0),new Vector3(0,1.35f,0)});
   }
  }
  private LineRenderer Line(string name,bool loop,float width)
  {
   var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();
   line.useWorldSpace=true;line.loop=loop;line.widthMultiplier=width;line.sharedMaterial=material;
   line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.sortingOrder=120;
   line.startColor=line.endColor=ThreatVisualStyle.Danger;return line;
  }
  private void Circle(LineRenderer line,Vector3 center,float radius)
  {line.positionCount=48;for(int p=0;p<48;p++){float a=p*Mathf.PI*2/48;line.SetPosition(p,center+new Vector3(Mathf.Cos(a)*radius,.16f,Mathf.Sin(a)*radius));}}
  public void Advance(float dt)
  {
   if(session==null||session.Player==null||session.InputBlocked||float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;age+=dt;
   if(mode==0&&session.ModeRun!=null)
   {
    HoldPointState state=session.ModeRun.HoldState;
    Color objective=state==HoldPointState.Contested?new Color(1,.65f,.15f,.9f):state==HoldPointState.Capturing?new Color(.2f,1,.82f,.85f):new Color(.3f,1,.62f,.6f);
    rings[2].startColor=rings[2].endColor=objective;holdFlag.startColor=holdFlag.endColor=objective;
    int complete=ArenaPulseRules.HoldSegments(session.ModeRun.ObjectiveProgress);
    for(int i=0;i<holdSegments.Length;i++){holdSegments[i].enabled=i<complete;holdSegments[i].startColor=holdSegments[i].endColor=objective;}
   }
   for(int i=0;i<2;i++)
   {
    float clock=age+i*2.7f;int cycle=ArenaPulseRules.Cycle(clock);float phase=ArenaPulseRules.Phase(clock);
    bool warning=ArenaPulseRules.Warning(phase),active=ArenaPulseRules.Active(phase);
    rings[i].enabled=clocks[i].enabled=glyphs[i].enabled=warning;
    if(!warning)continue;
    CombatSight.FillAreaBoundary(outline,centers[i]+Vector3.up*.16f,ArenaPulseRules.Radius);rings[i].positionCount=outline.Length;rings[i].SetPositions(outline);
    float progress=ArenaPulseRules.Progress(phase);int count=Mathf.Max(2,Mathf.CeilToInt(progress*32)+1);clocks[i].positionCount=count;
    for(int p=0;p<count;p++){float a=2*Mathf.PI*progress*p/(count-1);clocks[i].SetPosition(p,centers[i]+new Vector3(Mathf.Sin(a)*.72f,.2f,Mathf.Cos(a)*.72f));}
    rings[i].widthMultiplier=active?.16f:.1f;
    if(active&&hitCycle[i]!=cycle&&ArenaPulseRules.Contains(CombatFx.Flat(session.Player.transform.position-centers[i]).sqrMagnitude,CombatSight.Area(centers[i],session.Player.transform.position)))
    {hitCycle[i]=cycle;session.Player.TakeDamageFrom(Mathf.Max(3,session.Player.MaxHealth*.065f),mode==0?"林庭荆棘脉冲":mode==1?"烬河热涌":"蚀星脉冲");}
   }
  }
  private void OnDestroy(){if(material!=null)Destroy(material);}
 }
}
