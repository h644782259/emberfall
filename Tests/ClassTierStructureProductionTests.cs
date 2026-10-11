using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using Emberfall;
public static class ClassTierStructureProductionTests
{
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static readonly string[] TierNames={"Scholar split side lining","Raised scholar collar","Split scholar rear stole","Arch scholar star ray","Scholar orbit yoke","Arch scholar under-robe hem","Hunter quiver harness","Hunter quiver retaining hoop","Hunter offside utility flap","Hunter layered hip tab","Master hunter raised left guard","Master hunter quiver rail","Split contract shawl tail","Hanging contract tablet","Divided contract shoulder fan","Contract crown outer fork","Elder contract crown branch","Elder contract split clasp"};
 static MeshFilter[] Parts(CombatModel m)=>m.GetComponentsInChildren<MeshFilter>(false).Where(f=>TierNames.Contains(f.transform.name)).ToArray();
 static string Signature(CombatModel m)=>string.Join(";",Parts(m).Select(f=>f.transform.name+":"+string.Join("/",f.sharedMesh.vertices.Select(v=>m.transform.InverseTransformPoint(f.transform.TransformPoint(v)).ToString()))).OrderBy(x=>x));
 public static void Main()
 {
  foreach(var hero in new[]{HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner})
  {
   var signatures=new HashSet<string>();
   for(int tier=1;tier<=4;tier++)
   {
    string baseline=null;
    foreach(var rarity in new[]{Rarity.Common,Rarity.Rare,Rarity.Epic,Rarity.Legendary})foreach(int upgrade in new[]{0,3,7,10})foreach(var fashion in new[]{Rarity.Common,Rarity.Legendary})
    {
     var host=new GameObject("tier fixture");var model=CombatModel.Hero(host.transform,hero);
     var weapon=new ItemData{id="w",slot=ItemSlot.Weapon,level=(tier-1)*25+1,rarity=rarity,upgradeLevel=upgrade};var armor=new ItemData{id="a",slot=ItemSlot.Armor,level=weapon.level,rarity=rarity,upgradeLevel=upgrade};
     model.ApplyEquipment(weapon,armor,null);model.ApplyFashion(new FashionData{id="wing",slot=FashionSlot.Wings,rarity=fashion},new FashionData{id="weapon",slot=FashionSlot.Weapon,rarity=fashion});UnityEngine.Object.Flush();
     var parts=Parts(model);Check(parts.Length>0,"tier construction produces actual mesh geometry");string signature=Signature(model);if(baseline==null)baseline=signature;else Check(signature==baseline,"tier construction is independent of rarity and upgrade");
     foreach(var part in parts)
     {
      var vertices=part.sharedMesh.vertices.Select(v=>model.transform.InverseTransformPoint(part.transform.TransformPoint(v))).ToArray();
      Check(vertices.Length>=3&&part.sharedMesh.triangles.Length>=3,"tier part has real recipe topology");
      Check(vertices.All(v=>!(Math.Abs(v.x)<.19f&&v.y>1.75f&&v.y<2.22f&&v.z>.12f)),"tier geometry preserves face opening");
      Check(vertices.All(v=>!(Math.Abs(v.x)<.245f&&v.y>.65f&&v.y<1.045f&&v.z>.39f)),"tier geometry preserves B05 relic reservation");
     }
     Check(model.GetComponentsInChildren<MeshFilter>(false).Length<250,"tier construction remains bounded with maximum fashion");
     var owned=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Equipped contract crown forks").ToArray();
     if(hero==HeroClass.Summoner&&tier>=3)Check(owned.Length==1&&owned[0].parent.name=="Neck","tier crown follows real head joint");
     model.ApplyEquipment(null,null,null);Check(owned.All(t=>!t.gameObject.activeSelf),"unequip immediately retires detached tier crown");UnityEngine.Object.Flush();
     Check(!model.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Equipped contract crown forks"),"unequip destroys detached crown hierarchy");
     UnityEngine.Object.Destroy(host);UnityEngine.Object.Flush();Check(!UnityEngine.Object.All.OfType<Material>().Any(m=>!m.destroyed),"tier swap releases owned materials");
    }
    Check(signatures.Add(baseline),"each level tier has distinct actual structural topology");
   }
  }
  Console.WriteLine("PASS: "+checks+" actual class-tier construction/reservation/lifecycle checks; managed static geometry, not dynamic Unity rendering");
 }
}
