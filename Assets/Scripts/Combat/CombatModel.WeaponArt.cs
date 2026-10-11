using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private void ApplyWeaponArt(ItemData weapon)
        {
            Transform rig=swordRig!=null?swordRig:staffRig!=null?staffRig:bowRig;
            if(rig==null)return;
            foreach(var filter in rig.GetComponentsInChildren<MeshFilter>(true))WeaponModules.Restore(filter);
            if(!WeaponModules.Enabled)return;
            ApplyWeaponFashionArt();
            if(swordRig!=null)
            {
                // All four source pieces must load together. Other gear retains its tier blade/guard;
                // small repeated ornaments are simplified below without moving their attachment points.
                if(weapon==null||PilotStarterCompatible(weapon,ItemSlot.Weapon))
                {
                    Mesh blade=WeaponModules.Load("StarBlade"),guard=WeaponModules.Load("StarGuard"),grip=WeaponModules.Load("StarGrip"),pommel=WeaponModules.Load("StarPommel");
                    if(blade==null||guard==null||grip==null||pommel==null)return;
                    Transform root=weapon==null?swordRig:equipmentWeapon;if(root==null)return;
                    foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    {
                        string name=filter.transform.name;
                        if(name=="Forged Sword"||name=="Tiered blade")WeaponModules.Set(filter,blade,new Vector3(weapon==null?.18f:.205f,weaponStructure.SwordTip-weaponStructure.SwordRoot,weapon==null?.065f:.075f));
                        else if(name=="Crossguard"||name=="Swept guard")WeaponModules.Set(filter,guard);
                        else if(name=="Wrapped Hilt"||name=="Wrapped grip")WeaponModules.Set(filter,grip);
                        else if(name=="Gold Pommel"||name=="Faceted pommel")WeaponModules.Set(filter,pommel);
                    }
                }
            }
            foreach(var filter in rig.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.sharedMesh!=null&&filter.sharedMesh.name.StartsWith("Blender weapon Star"))continue;
                string name=filter.transform.name,key=null;
                if(name=="Bow Limb"||name=="Layered bow limb"||name=="Bow horn")key="BowJoiner";
                else if(name=="Staff"||name=="Inlaid staff")key="StaffShaft";
                else if(name=="Staff Gold"||name=="Staff collar")key="StaffShaft";
                else if(name=="Faceted pommel"||name=="Blade rune"||name=="Forging mark"||name=="Bow focus"||name=="Bow tip"||name=="Floating shard"||name=="Staff rune"||name=="Staff forging mark"||name=="Bow forging mark"||name=="Weapon aura shard")key="Facet";
                bool half=false;
                if(name=="Blade spine"||name=="Guard wing"||name=="Blade fang"||name=="Bow grip"||name=="Bow Grip"||name=="Arrow rest"||name=="Arrow Shaft"||name=="Arrow Fletching"){key="StaffShaft";half=true;}
                if(key!=null){Vector3 scale=filter.transform.localScale;if(half)scale.y*=.5f;WeaponModules.Set(filter,WeaponModules.Load(key),scale);}

            }
        }
        private void ApplyWeaponFashionArt()
        {
            if(fashionWeapon==null)return;
            foreach(var filter in fashionWeapon.GetComponentsInChildren<MeshFilter>(true))
            {
                WeaponModules.Restore(filter);
                if(filter.sharedMesh!=null&&filter.sharedMesh.name=="Shared costume Mechanical")WeaponModules.Set(filter,WeaponModules.Load("MechanicalHoop"));
            }
        }
    }
}
