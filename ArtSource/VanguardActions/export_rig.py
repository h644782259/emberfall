"""Export actual production rigid hierarchy using existing managed construction fixture."""
from pathlib import Path
import sys, subprocess, os
root=Path(__file__).resolve().parents[2];out=Path(sys.argv[1]).resolve();dotnet=sys.argv[2]
source=(root/'ArtSource/ActorModules/export_constructions.py').read_text().split('subprocess.run(')[0]
sys.argv=['export',str(root),str(out),dotnet];ns={'__file__':str(root/'ArtSource/ActorModules/export_constructions.py')};exec(source,ns)
p=out/'export'
model=p/'Model.cs';model.write_text(model.read_text().replace('model.ConfigureVanguardArt();',''))
(p/'Exporter.cs').write_text(r'''using System;using System.Linq;using System.Text.Json;using UnityEngine;using Emberfall;
class Exporter {static void Main(){var host=new GameObject("source");var m=CombatModel.Hero(host.transform,HeroClass.Vanguard);var all=m.GetComponentsInChildren<Transform>(true);var parts=all.Select((t,i)=>new{id=i,name=t.name,parent=Array.IndexOf(all,t.parent),p=new[]{t.localPosition.x,t.localPosition.y,t.localPosition.z},q=new[]{t.localRotation.q.X,t.localRotation.q.Y,t.localRotation.q.Z,t.localRotation.q.W},s=new[]{t.localScale.x,t.localScale.y,t.localScale.z},vertices=t.GetComponent<MeshFilter>()?.sharedMesh?.vertices?.Select(v=>new[]{v.x,v.y,v.z}).ToArray(),triangles=t.GetComponent<MeshFilter>()?.sharedMesh?.triangles,visible=t.gameObject.activeInHierarchy&&(t.GetComponent<Renderer>()==null||t.GetComponent<Renderer>().enabled)}).ToArray();System.IO.File.WriteAllText(@"OUTPUT",JsonSerializer.Serialize(parts));}}'''.replace('OUTPUT',str(out/'rig.json')))
subprocess.run([dotnet,'run','--project',str(p/'Export.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli')))
