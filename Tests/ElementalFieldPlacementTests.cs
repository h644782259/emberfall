using System;
using System.Linq;
using System.Reflection;
using Emberfall;
using UnityEngine;
public static class ElementalFieldPlacementTests
{
    static int checks;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    public static string Run()
    {
        foreach(var element in new[]{ElementalCombatVfx.Element.Fire,ElementalCombatVfx.Element.Lightning,ElementalCombatVfx.Element.Poison})
        {
            typeof(ElementalFieldVisual).GetMethod("ResetCount",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            CombatSight.Wall=.25f;MobileControls.Active=true;EffectPreferences.ReducedEffects=false;Time.time=0;
            var parent=new GameObject("Ground field");var field=ElementalFieldVisual.Spawn(parent.transform,element,3,false);
            var parts=GameObject.All.Where(o=>o.transform.parent==field.transform).ToArray();
            Func<GameObject,bool> visible=o=>o.activeSelf&&(o.GetComponent<LineRenderer>()==null||o.GetComponent<LineRenderer>().enabled);
            int count=parts.Count(visible);
            Check(count>0,"wall-adjacent field must keep visible secondary pieces");
            int priorCalls=CombatSight.FootprintCalls;
            var horizontal=parts.Select(o=>o.GetComponent<LineRenderer>()!=null?o.GetComponent<LineRenderer>().Positions[3]:o.transform.localPosition).ToArray();
            for(int frame=1;frame<=120;frame++)
            {
                Time.time=frame*.071f;field.gameObject.Call("Update");
                Check(parts.Count(visible)==count,"static cover never rotates pieces into a hard visibility toggle");
                foreach(var part in parts.Where(visible))
                {
                    var line=part.GetComponent<LineRenderer>();
                    if(line!=null)
                    {
                        foreach(var point in line.Positions)
                        {var world=part.transform.TransformPoint(point);Check(world.x+line.widthMultiplier*.5f<=CombatSight.Wall+.0001f,"every animated strand point plus width stays on the clear side");}
                    }
                    else Check(part.transform.position.x+part.transform.localScale.x*.5f<=CombatSight.Wall+.0001f,"full bubble extent stays visible-side while it grows");
                }
                if(element==ElementalCombatVfx.Element.Poison)
                    for(int i=0;i<parts.Length;i++)Check(Math.Abs(parts[i].transform.localPosition.x-horizontal[i].x)<.0001f&&Math.Abs(parts[i].transform.localPosition.z-horizontal[i].z)<.0001f,"poison orbit no longer rotates through cover");
            }
            Check(CombatSight.FootprintCalls==priorCalls,"unchanged field does not repeat coverage queries every frame");
            WorldTraversal.Revision++;field.gameObject.Call("Update");Check(CombatSight.FootprintCalls>priorCalls,"known cover revision refreshes stable placement");
            CombatSight.Wall=-.1f;WorldTraversal.Revision++;field.gameObject.Call("Update");Check(parts.Count(visible)==0,"fully obstructed field never invents visible coverage");
            field.gameObject.Call("OnDestroy");
            int bodyQueries=CombatSight.FootprintCalls;
            var aura=ElementalFieldVisual.Spawn(parent.transform,element,.4f,true);
            var auraParts=GameObject.All.Where(o=>o.transform.parent==aura.transform).ToArray();
            Check(auraParts.Length==3&&auraParts.All(visible),"existing on-body aura remains visible independently of ground coverage");
            Time.time+=.4f;aura.gameObject.Call("Update");
            Check(auraParts.All(visible)&&CombatSight.FootprintCalls==bodyQueries,"on-body motion retains its original cover exemption");
            aura.gameObject.Call("OnDestroy");
        }
        return "PASS: "+checks+" production secondary-field placement/animated extent/cache checks (managed, not GPU)";
    }
}
namespace Emberfall
{
    public static class MobileControls{public static bool Active=true;}
    public static class ElementalCombatVfx{public enum Element{Fire,Lightning,Poison}}
    public static class WorldTraversal{public static int Revision;}
    public static class ProceduralVisuals
    {public static GameObject Create(string name,PrimitiveType type,Material material){var obj=new GameObject(name);obj.AddComponent<MeshRenderer>().sharedMaterial=material;return obj;}}
}
