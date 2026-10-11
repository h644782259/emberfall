using System;
using System.Linq;
using System.Reflection;
using Emberfall;
using UnityEngine;
public static class ElementalFieldContinuityTests
{
    static int checks;
    static void Check(bool condition,string why){checks++;if(!condition)throw new Exception(why);}
    static Vector3[] Points(GameObject piece)
    {
        var line=piece.GetComponent<LineRenderer>();
        return line!=null?line.Positions.Select(piece.transform.TransformPoint).ToArray():new[]{piece.transform.position,piece.transform.localScale};
    }
    public static string Run()
    {
        foreach(var element in new[]{ElementalCombatVfx.Element.Fire,ElementalCombatVfx.Element.Lightning,ElementalCombatVfx.Element.Poison})
        {
            typeof(ElementalFieldVisual).GetMethod("ResetCount",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            CombatSight.Wall=.25f;MobileControls.Active=true;EffectPreferences.ReducedEffects=false;Time.time=0;
            var field=ElementalFieldVisual.Spawn(new GameObject("Field").transform,element,3,false);
            var pieces=GameObject.All.Where(o=>o.transform.parent==field.transform).ToArray();
            var before=pieces.Select(Points).ToArray();
            // Break/remove nearby cover. Old placements remain safe; global Revision still changes.
            CombatSight.Wall=float.PositiveInfinity;WorldTraversal.Revision++;field.gameObject.Call("Update");
            for(int i=0;i<pieces.Length;i++)
            {
                var after=Points(pieces[i]);
                for(int j=0;j<after.Length;j++)Check((after[j]-before[i][j]).sqrMagnitude<.000001f,
                    "removing cover must not teleport already valid secondary geometry: "+element+" piece "+i);
            }
            // An unrelated revision likewise preserves the rendered output.
            WorldTraversal.Revision++;field.gameObject.Call("Update");
            for(int i=0;i<pieces.Length;i++)for(int j=0;j<before[i].Length;j++)Check((Points(pieces[i])[j]-before[i][j]).sqrMagnitude<.000001f,"unrelated revision retains valid placement");
            int staticQueries=CombatSight.FootprintCalls;
            field.gameObject.Call("Update");
            Check(CombatSight.FootprintCalls==staticQueries,"static update adds no coverage queries");
            // A transform-only change must validate cached local envelopes in their new world positions.
            CombatSight.Wall=.25f;field.transform.localPosition=new Vector3(3,0,0);
            field.gameObject.Call("Update");
            foreach(var piece in pieces)Check(piece.GetComponent<LineRenderer>()!=null?!piece.GetComponent<LineRenderer>().enabled:!piece.activeSelf,"transform change invalidates unsafe cached placement without revision");
            field.transform.localPosition=Vector3.zero;CombatSight.Wall=float.PositiveInfinity;
            field.gameObject.Call("Update");
            Check(pieces.Any(p=>p.GetComponent<LineRenderer>()!=null?p.GetComponent<LineRenderer>().enabled:p.activeSelf),"transform restoration refreshes coverage without revision");
            // Newly blocking cover still invalidates retained positions; no unsafe interpolation.
            CombatSight.Wall=-.1f;WorldTraversal.Revision++;field.gameObject.Call("Update");
            foreach(var piece in pieces)Check(piece.GetComponent<LineRenderer>()!=null?!piece.GetComponent<LineRenderer>().enabled:!piece.activeSelf,"newly blocked pieces are not retained");
            // A previously omitted piece can be admitted after cover clears.
            CombatSight.Wall=float.PositiveInfinity;WorldTraversal.Revision++;field.gameObject.Call("Update");
            Check(pieces.Any(p=>p.GetComponent<LineRenderer>()!=null?p.GetComponent<LineRenderer>().enabled:p.activeSelf),"newly reachable sector can reappear");
            field.gameObject.Call("OnDestroy");
        }
        return "PASS: "+checks+" production secondary-field revision continuity checks (managed geometry, not GPU)";
    }
}
