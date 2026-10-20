#!/usr/bin/env python3
"""Production Boss/arena dimensions, actual navigation, muzzle and capsule geometry.
Numeric Unity doubles provide arithmetic only; native mesh/render review is separate.
"""
import os, sys, subprocess, tempfile
from pathlib import Path
r=Path(__file__).resolve().parents[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
enemy=(r/'Assets/Scripts/Combat/EnemyController.cs').read_text()
world=(r/'Assets/Scripts/World/WorldBuilder.cs').read_text()
body=member((r/'Assets/Scripts/Combat/PhysicalSkillMotion.cs').read_text(),'        internal static bool BodyContact(')
bolt='\n'.join(member(enemy,key) for key in ['private Vector3 BoltMuzzle(', 'private Vector3 BoltDirection(', 'private float BoltSpeed ']).replace('private ','public ')
nav='\n'.join(member(world,key) for key in ['private static Vector3 NavigationScenePoint(', 'private static Vector2 NavigationSceneSize(', 'private static float NavigationSceneRadius(']).replace('private ','public ')
large=(r/'Assets/Scripts/Combat/LargeExpeditionBoss.cs').read_text()
sweep=(r/'Assets/Scripts/Combat/EnemySkillPhysicalVfx.cs').read_text();sweep=sweep[sweep.index('internal sealed class EnemySweepPhysicalVfx'):]
a=sweep.index('                spanStarts[i]=');b=sweep.index('                bodies[i].gameObject.SetActive',a)
mask=sweep[a:b].replace('bodies.Length','clearSpans.Length')
beam='float radius=>BeamDangerRadius;bool[] clearSpans=new bool[32];Vector3[] spanStarts=new Vector3[32],spanEnds=new Vector3[32];public bool BeamContains(Vector3 point,Vector3 from,Vector3 to){for(int i=0;i<clearSpans.Length;i++){'+mask+'}return Contains(point);}'+member(sweep,'internal bool Contains(')

math=(r/'Tests/DestructibleTraversalTests.cs').read_text();math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):]
fixture=r'''
namespace UnityEngine{public class Transform{public Vector3 position,scale=new Vector3(1,1,1);public Vector3 lossyScale=>scale;public Vector3 TransformPoint(Vector3 p)=>position+new Vector3(p.x*scale.x,p.y*scale.y,p.z*scale.z);}}
namespace Emberfall{
 public enum EnemyKind{Slime,Goblin,Wisp,Guardian}public enum ChapterNode{ForestCourt,Redrock,StarPlatform}
 public class SummonedCompanion{public enum Kind{Wolf,Spirit,Treant,Wisp}public bool IsAlive=true;public float PhysicalBodyHeight;}
 public class BoltFixture{
  public enum AttackType{Fan,Bolt}public AttackType attackType=AttackType.Bolt;public bool IsBoss=true;public object largeBoss;public Transform bossBoltSocket;public Vector3 targetPoint;public SummonedCompanion companionTarget;
  public float BodyScale=>BossScaleRules.EnemyMultiplier(IsBoss,largeBoss!=null);
  BOLT
 }
 public static class NavFixture{NAV}
 public class BeamFixture{public float BodyScale=1;float BeamDangerRadius=>(LargeBossPhaseState.BeamHalfWidth+.4f)*BodyScale;BEAM}
 public static class ContactFixture{BODY}
}
class Program{
 static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}static bool Near(float a,float b)=>Math.Abs(a-b)<.001f;
 static void Flat(float radius=18){WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.SetArenaRadius(radius);WorldTerrain.Configure(ZoneKind.Wilderness,1,radius);}
 static void Main(){
  foreach(EnemyKind kind in Enum.GetValues(typeof(EnemyKind))){
   C(Near(BossScaleRules.EnemyMultiplier(false),1),"nonboss enemy scale unchanged");
   C(Near(BossScaleRules.NavigationRadius(false,false,kind),kind==EnemyKind.Guardian?.44f:.32f),"nonboss navigation unchanged");
  }
  C(Near(BossScaleRules.EnemyMultiplier(true),3f)&&Near(BossScaleRules.EnemyMultiplier(true,true),2.9f),"central Boss model dimensions");
  C(Near(BossScaleRules.NavigationRadius(true,false,EnemyKind.Guardian),2.7f)&&Near(BossScaleRules.ProjectileRadius(true,false),3.15f)&&Near(BossScaleRules.BodyHeight(true,false,EnemyKind.Guardian),11.4f),"ordinary Boss navigation and contact grow together");
  C(Near(BossScaleRules.NavigationRadius(true,true,EnemyKind.Guardian),3.625f)&&Near(BossScaleRules.ProjectileRadius(true,true),3.77f)&&Near(BossScaleRules.BodyHeight(true,true,EnemyKind.Guardian),12.76f),"Large Boss routing and full-height projectile body");
  C(BossScaleRules.SpawnRadius(true)>BossScaleRules.NavigationRadius(true,true,EnemyKind.Guardian),"spawn clearance exceeds full enlarged body");
  foreach(SummonedCompanion.Kind form in Enum.GetValues(typeof(SummonedCompanion.Kind))){float scale=BossScaleRules.CompanionMultiplier(form);C(Near(scale,form==SummonedCompanion.Kind.Treant?1.5f:1),"only ultimate Treant grows");}
  C(Near(BossScaleRules.CompanionNavigation(SummonedCompanion.Kind.Treant),1.05f)&&Near(BossScaleRules.CompanionBodyRadius(SummonedCompanion.Kind.Treant),1.35f),"Treant navigation and projectile capsule match added scale");
  for(int layout=-1;layout<110;layout++)C(ArenaSizeRules.LayoutScale(layout)>=1&&ArenaSizeRules.LayoutScale(layout)<=1.5f,"room scales stay within navigation limits");
  foreach(ExpeditionModeKind mode in Enum.GetValues(typeof(ExpeditionModeKind))){var run=new ExpeditionModeState(mode,9,234);ExpeditionPhasePlan plan;C(run.TryBeginPhase(out plan),"production mode creates actual phase");bool boss=false;foreach(var role in plan.Enemies)boss|=role==ExpeditionEnemyRole.Boss;C(ArenaSizeRules.HasBossEncounter((int)mode+2)==boss,"production role list decides arena expansion");}
  C(Near(ArenaSizeRules.Radius(ZoneKind.Dungeon,0,0),35)&&Near(ArenaSizeRules.Radius(ZoneKind.Dungeon,105,0),26.55f),"world/game radii include enlarged encounters");
  var p=new Vector3(3,1.7f,-8);C((ArenaSizeRules.LocalPoint(105,ArenaSizeRules.Point(105,p))-p).magnitude<.001f,"world point roundtrip leaves height unchanged");
  Flat(35);C(WorldTraversal.IsWalkable(new Vector3(30,0,0),BossScaleRules.NavigationRadius(true,true,EnemyKind.Guardian)),"expanded perimeter accommodates enlarged Boss");C(WorldTraversal.FindPath(new Vector3(-30,0,0),new Vector3(30,0,0),3.625f).Count>0,"fixed navigation grid reaches both enlarged edges");
  WorldTerrain.Configure(ZoneKind.Dungeon,0,35);C(Near(WorldTerrain.Radius,35),"height shader and actual terrain use expanded radius");
  Flat(22.5f);WorldTraversal.BeginSceneBuild(1.25f);C(Near(WorldTraversal.SceneLocalArenaRadius,18),"scene-local placement keeps authored extent");
  var h=WorldTraversal.AddDynamicCircle(new Vector3(8,0,3),.8f);C(!WorldTraversal.IsWalkable(new Vector3(10,0,3.75f),.05f)&&WorldTraversal.IsWalkable(new Vector3(8,0,3),.05f),"navigation center scales exactly once");
  WorldTraversal.SetPhysicalBounds(h,new Vector3(10,.3f,3.75f),new Vector3(1,.6f,1));C(!WorldTraversal.IsPhysicalSpaceClear(new Vector3(10,.3f,3.75f),.05f),"world physical bounds never double-scale");C(WorldTraversal.IsPhysicalSpaceClear(new Vector3(10,1.2f,3.75f),.05f),"finite scaled rock allows flight over its actual top");
  var parent=new Transform{position=new Vector3(2,0,-3),scale=new Vector3(1.875f,1,1.875f)};var local=new Vector3(7,0,-4);var scene=NavFixture.NavigationScenePoint(parent,local);var actual=parent.TransformPoint(local);WorldTraversal.AddDynamicBox(scene,NavFixture.NavigationSceneSize(parent,new Vector2(1.4f,1.4f)));C(!WorldTraversal.IsWalkable(actual,.1f)&&WorldTraversal.IsWalkable(local,.1f),"nested cathedral columns register at actual transformed location");C(Near(NavFixture.NavigationSceneRadius(parent,.22f),.33f),"only nested scale is included before outer registration");
  WorldTraversal.EndSceneBuild();C(Near(WorldTraversal.SceneBuildScale,1),"scene build restores unscaled runtime scope");var anchor=WorldTraversal.AddDynamicCircle(new Vector3(-7,0,3),.55f);C(!WorldTraversal.IsWalkable(new Vector3(-7,0,3),.05f)&&WorldTraversal.IsWalkable(new Vector3(-8.75f,0,3.75f),.05f),"post-build anchors use true world coordinates");
  Flat(22.5f);WorldTraversal.BeginSceneBuild(1.25f);var channel=new[]{new Vector3(-15,0,0),new Vector3(15,0,0)};WorldTraversal.SetRiver(channel,3.2f,new Rect(-3,-3,6,6));WorldTraversal.EndSceneBuild();C(WorldTraversal.IsWalkable(new Vector3(3.4f,0,0),.1f)&&WorldTraversal.IsOpenWater(new Vector3(4.1f,0,0)),"scaled bridge and water agree with geometry");C(WorldTraversal.IsOpenWater(new Vector3(8,0,1.9f))&&WorldTraversal.IsWalkable(new Vector3(8,0,2.4f),.1f),"river width scales once");C(Near(channel[0].x,-15),"registration does not mutate authored river input");
  Flat(22.5f);WorldTraversal.BeginSceneBuild(1.25f);WorldTraversal.AddPlatform(new Vector3(5,0,4),new Vector2(2,2),.8f);WorldTraversal.AddJumpPlatform(new Vector3(-5,0,-4),.7f,.9f);WorldTraversal.EndSceneBuild();C(Near(WorldTraversal.SurfaceHeight(new Vector3(6.25f,0,5),.05f),.8f)&&Near(WorldTraversal.SurfaceHeight(new Vector3(-6.25f,0,-5),.05f),.9f),"platform positions scale but jump heights do not");
  foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))for(int room=0;room<2;room++)for(int seed=0;seed<6;seed++){
   var plan=ChapterRoomGeometry.Plan(node,room,seed);float scale=ArenaSizeRules.LayoutScale(plan.Layout);Flat(ArenaSizeRules.Radius(ZoneKind.Dungeon,plan.Layout,0));WorldTraversal.BeginSceneBuild(scale);ChapterRoomGeometry.Register(plan);WorldTraversal.EndSceneBuild();
   C(Near(plan.Entrance.z,-12*scale)&&Near(plan.Exit.z,14*scale),"actual chapter plan arrival and portal follow geometry");
   float radius=node==ChapterNode.StarPlatform?BossScaleRules.SpawnRadius(true):.65f;Vector3 spawn;C(ChapterRoomGeometry.TrySpawn(plan,0,new System.Collections.Generic.List<Vector3>(),radius,out spawn),"production chapter spawn fits matching enlarged clearance");C(WorldTraversal.IsWalkable(spawn,radius)&&WorldTraversal.CanReach(plan.Entrance,spawn,radius),"actual chapter boss spawn and entrance route pass navigation");
  }
  foreach(bool large in new[]{false,true}){
   var bolt=new BoltFixture{largeBoss=large?new object():null,targetPoint=new Vector3(0,0,24),bossBoltSocket=new Transform{position=new Vector3(0,large?2.1f*2.9f:1.43f*1.48f*3f,0)}};
   Vector3 muzzle=bolt.BoltMuzzle(Vector3.zero,new Vector3(0,0,1)),direction=bolt.BoltDirection(muzzle,new Vector3(0,0,1));C(Near(muzzle.y,bolt.bossBoltSocket.position.y),"Boss projectile starts at actual animated core socket");C(direction.y<0&&Near(direction.magnitude,1),"enlarged Boss aims from chest down to victim body");float fraction;C(ContactFixture.BodyContact(muzzle,muzzle+direction*bolt.BoltSpeed*3,bolt.targetPoint,2.2f,.45f,.32f,out fraction),"3D Boss shot reaches actual player body before expiry");float horizontal=bolt.BoltSpeed*3*CombatFx.Flat(direction).magnitude;C(Near(CombatFx.Flat(muzzle+direction*bolt.BoltSpeed*3-muzzle).magnitude,horizontal),"warning endpoint uses actual horizontal range of 3D velocity");
   bolt.companionTarget=new SummonedCompanion{PhysicalBodyHeight=BossScaleRules.CompanionBodyHeight(SummonedCompanion.Kind.Treant)};direction=bolt.BoltDirection(muzzle,new Vector3(0,0,1));C(ContactFixture.BodyContact(muzzle,muzzle+direction*bolt.BoltSpeed*3,bolt.targetPoint,bolt.companionTarget.PhysicalBodyHeight,1.35f,.32f,out fraction),"elevated Boss shot aims at actual enlarged Treant body");
   float height=BossScaleRules.BodyHeight(true,large,EnemyKind.Guardian),width=BossScaleRules.ProjectileRadius(true,large);C(ContactFixture.BodyContact(new Vector3(-6,height-.5f,0),new Vector3(6,height-.5f,0),Vector3.zero,height,width,.32f,out fraction),"full-height Boss contact accepts high shot at enlarged head");C(!ContactFixture.BodyContact(new Vector3(-6,height+1,0),new Vector3(6,height+1,0),Vector3.zero,height,width,.32f,out fraction),"shot truly above enlarged body remains safe");
  }
  foreach(float scale in new[]{1f,1.45f}){var sweep=new BeamFixture{BodyScale=scale};Flat(22.5f);C(sweep.BeamContains(new Vector3(.94f*scale,0,6),Vector3.zero,new Vector3(0,0,12)),"scaled Boss beam full width accepts damage");C(!sweep.BeamContains(new Vector3(.96f*scale,0,6),Vector3.zero,new Vector3(0,0,12)),"scaled Boss beam safe side outside real width");WorldTraversal.AddCircle(new Vector3(.6f*scale,0,6),.2f);C(!sweep.BeamContains(new Vector3(.6f*scale,0,6),Vector3.zero,new Vector3(0,0,12)),"scaled beam has no hidden cover damage");C(sweep.BeamContains(new Vector3(0,0,10),Vector3.zero,new Vector3(0,0,12)),"scaled beam continues after cover");}
  BossScaleRules.ReviewLegacyDimensions=true;C(BossScaleRules.EnemyMultiplier(true)==1&&ArenaSizeRules.LayoutScale(105)==1&&BossScaleRules.CompanionMultiplier(SummonedCompanion.Kind.Treant)==1,"review-only before comparison restores original dimensions");BossScaleRules.ReviewLegacyDimensions=false;
  Console.WriteLine("PASS "+n+" production Boss/arena/route/contact/core/3D-flight/mode-scope checks");
 }
}
'''.replace('BOLT',bolt).replace('NAV',nav).replace('BODY',body).replace('BEAM',beam)
with tempfile.TemporaryDirectory(prefix='emberfall-boss-arena-scale-') as d:
 p=Path(d);(p/'Math.cs').write_text(math);(p/'Program.cs').write_text('using System;using Emberfall;using UnityEngine;'+fixture)
 sources=['Assets/Scripts/Combat/BossScaleRules.cs','Assets/Scripts/World/ArenaSizeRules.cs','Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/World/ChapterRoomGeometry.cs','Assets/Scripts/World/TacticalRoomGeometry.cs','Assets/Scripts/Core/ExpeditionModeState.cs','Assets/Scripts/Core/LargeBossPhaseState.cs','Assets/Scripts/Core/TierRewardBand.cs']
 for file in sources:(p/Path(file).name).write_text((r/file).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1');dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--configuration','Release'],env=env,check=True)
 controls=[
  ('WorldTraversal.cs','center=ScenePoint(center);radius*=sceneBuildScale;','radius*=sceneBuildScale;','navigation center scales exactly once'),
  ('WorldTraversal.cs','o.PhysicalCenter=center;','o.PhysicalCenter=ScenePoint(center);','world physical bounds never double-scale'),
  ('WorldTraversal.cs','internal static void EndSceneBuild(){sceneBuildScale=1;}','internal static void EndSceneBuild(){}','scene build restores unscaled runtime scope'),
  ('Program.cs','float distance=Mathf.Max(.1f,CombatFx.Flat(targetPoint-muzzle).magnitude);','return flatForward;\n            float distance=Mathf.Max(.1f,CombatFx.Flat(targetPoint-muzzle).magnitude);','enlarged Boss aims from chest down to victim body'),
  ('ArenaSizeRules.cs','return layout==0||layout==1||layout==4||layout==14||layout==105;','return layout>=0&&layout<=4||layout==14||layout==105;','production role list decides arena expansion'),
 ]
 for name,old,new,why in controls:
  source=(p/name).read_text();assert old in source,(name,old);(p/name).write_text(source.replace(old,new))
  built=subprocess.run([dotnet,'build',str(p/'Test.csproj'),'--no-restore','--configuration','Release','-v:q'],env=env,text=True,capture_output=True)
  if built.returncode:raise AssertionError(built.stdout+built.stderr)
  result=subprocess.run([dotnet,str(p/'bin/Release/net8.0/Test.dll')],env=env,text=True,capture_output=True);assert result.returncode and why in result.stderr,result.stdout+result.stderr
  print('PASS compiled negative control: '+why);(p/name).write_text(source)
# Wiring assertions supplement the executed production methods and cannot substitute for native renders.
for text in ['BossAttackPolicy.ChargeHalfWidth*BodyScale','BossAttackPolicy.ChargeSpeed*BodyScale','BossAttackPolicy.SlamRadius*BodyScale','model.transform.localScale *= BodyScale;','CacheBossBoltSocket();','BoltDirection(muzzle,Quaternion.Euler(0,FanAngle(i),0)*attackForward)','float range=BoltSpeed*3f*CombatFx.Flat(flight).magnitude;']:
 assert text in enemy,text
assert 'projectile.direction=forward.normalized;' in (r/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
assert 'candidate.IsBoss?candidate.PhysicalBodyHeight:' in (r/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
assert 'NavigationSceneSize(parent,new Vector2(1.4f,1.4f))' in world
assert 'finally{WorldTraversal.EndSceneBuild();}' in world
assert world.index('finally{WorldTraversal.EndSceneBuild();}')<world.index('if(dungeon)BuildBreakablePockets(root.transform,dungeonLayout);')
assert 'BossSpawnRadius' in (r/'Assets/Scripts/Core/GameSession.cs').read_text()
assert 'BossSpawnRadius' in (r/'Assets/Scripts/Core/GameSession.RoomChain.cs').read_text()
print('PASS source integration for ordinary/Large/Treant scale, actor contact, warning, charge, native socket and runtime registration scope')
