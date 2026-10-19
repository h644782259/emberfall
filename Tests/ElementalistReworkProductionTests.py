"""Elementalist swapped skills, forward geometry and production gravity regression."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using Emberfall;
class Program{static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}static void Main(){
var h=HeroClass.Arcanist;var load=GameBalance.DefaultLoadout(h);
C(GameBalance.SkillName(h,load[2])=="虚空漩涡"&&GameBalance.SkillRankRequiredLevel(load[2],1)==6,"vortex now key 3 level 6");
C(GameBalance.SkillName(h,load[6])=="重力开关"&&GameBalance.SkillRankRequiredLevel(load[6],1)==20,"switch now former vortex slot level 20");
C(GameBalance.SkillName(h,6)=="上升重力","pressure replaces ice lightning");C(GameBalance.SkillName(HeroClass.Vanguard,2)=="剑刃风暴"&&GameBalance.SkillName(HeroClass.Ranger,7)=="幻影连射","other classes unchanged");
for(int rank=1;rank<=3;rank++){
C(GameBalance.SkillName(h,9)=="流星雨"&&GameBalance.SkillEnergyCost(h,9)==0,"rain retains ultimate zero energy");
C(MeteorShowerRules.Contains(17,0,rank)&&MeteorShowerRules.Contains(-3,0,rank),"rain covers large front and small rear");
C(!MeteorShowerRules.Contains(-MeteorShowerRules.Rear(rank)-.1f,0,rank)&&!MeteorShowerRules.Contains(-1,MeteorShowerRules.HalfWidth(rank,true)+.1f,rank),"rear rain bounded independently");
C(MeteorShowerRules.Count(rank)>=14&&MeteorShowerRules.Duration(rank)>6,"sustained rain");

float l=ElementalistGeometry.FrostLength(rank),w=ElementalistGeometry.FrostWidth(rank);C(l>w*1.5f,"long front strip");
C(ElementalistGeometry.InRectangle(l*.5f-.1f,0,l*.5f,w*.5f),"far front hit");C(!ElementalistGeometry.InRectangle(-l*.5f-.1f,0,l*.5f,w*.5f),"behind caster excluded");C(!ElementalistGeometry.InRectangle(0,w*.5f+.1f,l*.5f,w*.5f),"side excluded");
C(SkillDamageBudgets.AdvancedSteps(h,2,rank)==9+(rank-1)*2&&SkillDamageBudgets.AdvancedScale(h,2)>0,"vortex keeps damage schedule");C(!SkillCastingPolicy.Channeled(h,6)&&!SkillCastingPolicy.Channeled(h,7)&&!SkillCastingPolicy.Channeled(h,2),"detached casts");
}
foreach(int fps in new[]{15,30,60,120}){
var m=new EnemyImpactMotion();m.Launch(3.2f);float max=0;bool landed=false;for(int i=0;i<fps*2;i++){landed|=m.Advance(1f/fps);max=Math.Max(max,m.Height);}C(max>3.1f&&landed&&!m.Airborne&&m.Height==0,"lift falls back to ground");m.Launch(3.2f);m.Advance(.3f);m.PressDown();C(m.Velocity<=-2,"pressure forces airborne monster down");m.Advance(2);C(!m.Airborne,"suppressed monster reaches floor");}
Console.WriteLine("PASS "+n+" elementalist slots, geometry, detachment and gravity assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['MeteorShowerRules','ElementalistGeometry','SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Assets/Scripts/Combat/EnemyImpactMotion.cs',root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs',root/'Assets/Scripts/Combat/MobileSkillPolicy.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
