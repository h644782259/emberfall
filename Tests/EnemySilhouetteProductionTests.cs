// Actual Enemy and anchor factories, shared strict decoder, F2 loader, managed Unity TRS only.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using Emberfall;
class EnemySilhouetteProductionTests
{
    static int checks;
    static void C(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static void Reset(){typeof(EnemySilhouetteArt).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);Resources.Override.Clear();Resources.Reads.Clear();}
    static MeshFilter[] Filters(Transform root)=>root.GetComponentsInChildren<MeshFilter>(false).Where(f=>f.GetComponent<Renderer>().enabled).ToArray();
    static object Geometry(string name,Transform root)=>new{name,origin=new[]{root.position.x,root.position.y,root.position.z},parts=Filters(root).Select(f=>new{name=f.gameObject.name,mesh=f.sharedMesh.name,color=new[]{f.GetComponent<Renderer>().sharedMaterial.color.r,f.GetComponent<Renderer>().sharedMaterial.color.g,f.GetComponent<Renderer>().sharedMaterial.color.b},vertices=f.sharedMesh.vertices.Select(v=>{var w=f.transform.TransformPoint(v);return new[]{w.x,w.y,w.z};}).ToArray(),triangles=f.sharedMesh.triangles}).ToArray()};
    static CombatModel Enemy(EnemyKind kind,bool boss=false){var root=new GameObject("Enemy");root.AddComponent<EnemyController>();return CombatModel.Enemy(root.transform,kind,boss);}
    static bool Changed(MeshFilter filter)=>filter.sharedMesh.name.StartsWith("Enemy silhouette / ");
    static void Main(string[] args)
    {
        string output=args[0],repo=args[1];var cases=new List<object>();Reset();
        foreach(var kind in new[]{EnemyKind.Slime,EnemyKind.Goblin,EnemyKind.Wisp,EnemyKind.Guardian})foreach(bool boss in kind==EnemyKind.Guardian?new[]{false,true}:new[]{false})
        {
            var model=Enemy(kind,boss);UnityEngine.Object.Flush();var filters=Filters(model.transform);int authored=filters.Count(Changed);
            C(authored==(kind==EnemyKind.Goblin||kind==EnemyKind.Guardian?4:0),"actual enemy factory installs exactly four F2 pieces "+kind);
            var meshes=filters.Select(f=>f.sharedMesh).ToArray();EnemySilhouetteArt.ApplyEnemy(model);C(meshes.SequenceEqual(filters.Select(f=>f.sharedMesh)),"shared mesh cache and repeat application stable");
            cases.Add(Geometry(kind+(boss?"Boss":""),model.transform));
            Console.WriteLine("BUDGET "+kind+" boss="+boss+" parts="+filters.Length+" triangles="+filters.Sum(f=>f.sharedMesh.triangles.Length/3)+" sharedMaterials="+filters.Select(f=>f.GetComponent<Renderer>().sharedMaterial).Distinct().Count()+" F2="+authored);
        }
        foreach(var kind in new[]{EnemyKind.Goblin,EnemyKind.Guardian})
        {
            EnemySilhouetteArt.Enabled=false;var model=Enemy(kind);var filters=Filters(model.transform);var positions=filters.Select(f=>f.transform.localPosition).ToArray();var rotations=filters.Select(f=>f.transform.localRotation).ToArray();var scales=filters.Select(f=>f.transform.localScale).ToArray();var materials=filters.Select(f=>f.GetComponent<Renderer>().sharedMaterial).ToArray();
            C(!filters.Any(Changed),"disabled F2 retains complete original presentation");EnemySilhouetteArt.Enabled=true;EnemySilhouetteArt.ApplyEnemy(model);
            C(positions.SequenceEqual(filters.Select(f=>f.transform.localPosition))&&rotations.SequenceEqual(filters.Select(f=>f.transform.localRotation))&&scales.SequenceEqual(filters.Select(f=>f.transform.localScale)),"actual adapter never changes original rig or attachments");
            C(materials.SequenceEqual(filters.Select(f=>f.GetComponent<Renderer>().sharedMaterial)),"actual adapter retains original material palette");
            foreach(var filter in filters.Where(Changed))
            {
                string key=EnemySilhouetteArt.Key(filter.gameObject.name,false);var bytes=File.ReadAllBytes(Path.Combine(repo,"Assets/Resources/EnemySilhouettes/"+key+".bytes"));
                foreach(int fault in new[]{0,1,2})
                {
                    Reset();byte[] value=fault==0?null:fault==1?new byte[16]:(byte[])bytes.Clone();if(fault==2)Array.Copy(BitConverter.GetBytes(.7f),0,value,12,4);Resources.Override["EnemySilhouettes/"+key]=value;
                    var fallback=Enemy(kind);var target=Filters(fallback.transform).First(f=>f.gameObject.name==filter.gameObject.name);C(!Changed(target),"missing/corrupt/outside-envelope retains original "+key);
                    EnemySilhouetteArt.ApplyEnemy(fallback);C(Resources.Reads["EnemySilhouettes/"+key]==1,"failed resource cached "+key);
                }
            }
        }
        Reset();foreach(var file in Directory.GetFiles(Path.Combine(repo,"Assets/Resources/EnemySilhouettes"),"*.bytes")){var mesh=AuthoredActorMeshes.Decode(File.ReadAllBytes(file),file);C(mesh!=null&&mesh.triangles.Length<=1536,"real resource decoded within 512 triangle budget");}
        foreach(bool enabled in new[]{false,true})
        {
            EnemySilhouetteArt.Enabled=enabled;var host=new GameObject("Encounter");var encounter=host.AddComponent<LargeExpeditionBoss>();var anchors=encounter.BuildAnchorFixture();var props=anchors.GetComponentsInChildren<DestructibleProp>(false);
            C(props.Length==3&&encounter.State.Mask==7,"actual anchor count and live mask preserved");C(props.All(p=>p.Level==7&&p.Radius==.55f),"actual anchor level and interaction radius unchanged");
            C(props.All(p=>Math.Abs(p.transform.position.magnitude-2.6f)<.0001f),"actual anchor placement radius unchanged");
            C(Filters(anchors.transform).Count(Changed)==(enabled?15:0),"actual CreateAnchors installs all fifteen F2 pieces");
            cases.Add(Geometry(enabled?"Anchor-after":"Anchor-before",props[0].transform));
            if(enabled){var rigRoot=new GameObject("Rig");rigRoot.transform.SetParent(host.transform,false);var rig=rigRoot.AddComponent<LargeBossRig>();rig.Build((color,surface)=>new Material(Shader.Find("Standard")){color=color});cases.Add(Geometry("LargeAstrolabe",rigRoot.transform));}
        }
        foreach(string key in new[]{"AnchorPlinth","AnchorCrystal","AnchorClaw"})foreach(int fault in new[]{0,1,2})
        {
            Reset();var bytes=File.ReadAllBytes(Path.Combine(repo,"Assets/Resources/EnemySilhouettes/"+key+".bytes"));byte[] value=fault==0?null:fault==1?new byte[16]:(byte[])bytes.Clone();if(fault==2)Array.Copy(BitConverter.GetBytes(.7f),0,value,12,4);Resources.Override["EnemySilhouettes/"+key]=value;
            var encounter=new GameObject("Fallback encounter").AddComponent<LargeExpeditionBoss>();var anchors=encounter.BuildAnchorFixture();var targets=Filters(anchors.transform).Where(f=>EnemySilhouetteArt.Key(f.gameObject.name,true)==key).ToArray();
            C(targets.Length>0&&targets.All(f=>!Changed(f)),"actual anchors retain original missing/corrupt/oversized "+key);
            C(Resources.Reads["EnemySilhouettes/"+key]==1,"three anchors share failed resource cache "+key);
            C(encounter.State.Mask==7&&anchors.GetComponentsInChildren<DestructibleProp>(false).Length==3,"visual failure preserves live anchor state");
        }
        File.WriteAllText(Path.Combine(output,"f2-assemblies.json"),JsonSerializer.Serialize(cases));
        Console.WriteLine("PASS: "+checks+" production F2 factory/decoder/cache/fallback/anchor-state assertions; managed engine boundaries only");
    }
}
