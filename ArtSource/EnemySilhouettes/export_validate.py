"""Production enemy/large-rig/anchor construction plus F2 decoder/fallback invariants.
python3 export_validate.py repo output dotnet
"""
import sys,subprocess,json,os
from pathlib import Path
root=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();dotnet=sys.argv[3];out.mkdir(parents=True,exist_ok=True)
for generated in ['AnchorFixture.cs','TacticalAttachmentArt.cs','EnemySilhouetteArt.cs']:
 (out/'export'/generated).unlink(missing_ok=True) # Generated project extensions only.
subprocess.run([sys.executable,str(root/'ArtSource/TacticalAttachments/export_enemies.py'),str(root),str(out),dotnet],check=True)
p=out/'export'
def member(source,signature):
 a=source.index(signature);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
f=p/'Boss.cs';f.write_text(f.read_text().replace('public class LargeExpeditionBoss:MonoBehaviour{}','public partial class LargeExpeditionBoss:MonoBehaviour{}'))
f=p/'Fixture.cs';s=f.read_text();s=s.replace('public static class Resources {','public static class Resources {public static readonly Dictionary<string,byte[]> Override=new Dictionary<string,byte[]>();public static readonly Dictionary<string,int> Reads=new Dictionary<string,int>();')
s=s.replace('public static T Load<T>(string name) where T:class {','public static T Load<T>(string name) where T:class {Reads[name]=Reads.TryGetValue(name,out var count)?count+1:1;if(Override.TryGetValue(name,out var bytes))return bytes==null?null:new TextAsset{bytes=bytes} as T;')
f.write_text(s)
source=(root/'Assets/Scripts/Combat/LargeExpeditionBoss.cs').read_text()
(p/'AnchorFixture.cs').write_text('''using UnityEngine;namespace Emberfall{
public class WorldResources:MonoBehaviour{internal Material Material(Color c,bool glow,VisualSurface surface)=>new Material(Shader.Find("Standard")){color=c};}
public enum ChapterDifficulty{Normal,Heroic}public enum DestructibleKind{Crate}public enum PropRecovery{None}
public class DestructibleProp:MonoBehaviour{public int Level;public float Radius;public static bool CanPlace(Vector3 at,float radius)=>true;public void Initialize(DestructibleKind kind,int level,float radius,bool a,PropRecovery recovery,Transform model,Material material){Level=level;Radius=radius;}}
public static class WorldTraversal{public static bool HasGroundPath(Vector3 a,Vector3 b,float radius)=>true;}
public static class CombatFx{public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);}
public static class ChapterBossPattern{public static float AnchorOffset(bool heroic,int seed,int phase)=>0;}
public class Profile{public int level=7;}public class Progression{public Profile Profile=new Profile();}public class GameSession{public Progression Progression=new Progression();public GameObject Player=new GameObject("player");}
public class StateBoundary{public int PhaseNumber=2,Mask;public void CommitAnchors(int mask){Mask=mask;}}
public partial class LargeExpeditionBoss{private GameObject anchorRoot;private WorldResources resources;private DestructibleProp[] anchors=new DestructibleProp[3];private bool unprovenAnchorRemoval,chapterConfigured;private float startAngle;private int seed=9;private Vector3 phaseCenter;private ChapterDifficulty chapterDifficulty;private GameSession game=new GameSession();public StateBoundary State=new StateBoundary();
private void ReleaseAnchors(){}public GameObject BuildAnchorFixture(){CreateAnchors();return anchorRoot;}
'''+member(source,'private void CreateAnchors(')+'}}')
(p/'Exporter.cs').write_text((root/'Tests/EnemySilhouetteProductionTests.cs').read_text())
project=p/'Export.csproj';command=[dotnet,'run','--project',str(project),'--',str(out),str(root)]
env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1')
subprocess.run(command,env=env,check=True)
model=p/'Model.cs';original=model.read_text();assert 'EnemySilhouetteArt.ApplyEnemy(model);' in original;model.write_text(original.replace('EnemySilhouetteArt.ApplyEnemy(model);',''))
failed=subprocess.run(command,env=env,capture_output=True,text=True);model.write_text(original)
(out/'missing-enemy-hook.log').write_text(failed.stdout+failed.stderr)
assert failed.returncode and 'actual enemy factory installs exactly four F2 pieces' in failed.stdout+failed.stderr,failed.stdout+failed.stderr
print('PASS compiled missing enemy factory hook rejected')
anchor=p/'AnchorFixture.cs';original=anchor.read_text();assert 'EnemySilhouetteArt.ApplyAnchors(model);' in original;anchor.write_text(original.replace('EnemySilhouetteArt.ApplyAnchors(model);',''))
failed=subprocess.run(command,env=env,capture_output=True,text=True);anchor.write_text(original)
(out/'missing-anchor-hook.log').write_text(failed.stdout+failed.stderr)
assert failed.returncode and 'actual CreateAnchors installs all fifteen F2 pieces' in failed.stdout+failed.stderr,failed.stdout+failed.stderr
print('PASS compiled missing anchor factory hook rejected')
