using System;
using System.IO;
using Emberfall;

public static class EquipmentLookupTests
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }

    public static string Run(string directory)
    {
        checks = 0;
        string root = Path.Combine(directory, "equipment-lookup-" + Guid.NewGuid().ToString("N"));
        var service = new ProgressionService(root);
        foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        {
            Check(service.CreateNewSlot(hero), "isolated class fixture");
            service.Profile.level = 50;
            Check(!service.HasMechanic(EquipmentMechanic.None) && !service.HasMechanic((EquipmentMechanic)(-1)) &&
                !service.HasMechanic((EquipmentMechanic)int.MaxValue), "unknown mechanics remain rejected");
            foreach (EquipmentMechanic mechanic in Enum.GetValues(typeof(EquipmentMechanic)))
            {
                if (mechanic == EquipmentMechanic.None) continue;
                var item = new ItemData { id = "lookup-" + mechanic, name = "Lookup fixture", slot = BuildCatalog.MechanicSlot(mechanic),
                    rarity = Rarity.Epic, level = 1, mechanic = mechanic, attack = 10, defense = 5, health = 10 };
                service.Profile.inventory.Add(item);
                if (item.slot == ItemSlot.Weapon) service.Profile.weaponId = item.id;
                else service.Profile.relicId = item.id;
                Check(object.ReferenceEquals(service.Equipped(item.slot), item), "equipped read resolves actual inventory object");
                Check(service.HasMechanic(mechanic) == (hero == BuildCatalog.MechanicClass(mechanic)), "mechanic preserves class and slot gate");
                ItemSlot actual = item.slot;
                item.slot = ItemSlot.Armor;
                Check(service.Equipped(actual) == null && !service.HasMechanic(mechanic), "mismatched slot never grants a mechanism");
                item.slot = actual;
                service.Profile.inventory.Remove(item);
                Check(service.Equipped(actual) == null && !service.HasMechanic(mechanic), "removed item is not retained by a stale cache");
            }
        }
        Check(service.CreateNewSlot(HeroClass.Summoner), "allocation fixture");
        var resonance = new ItemData { id = "allocation-resonance", name = "Lookup fixture", slot = ItemSlot.Relic,
            rarity = Rarity.Epic, level = 1, mechanic = EquipmentMechanic.TwinSummonResonance, health = 10 };
        service.Profile.inventory.Add(resonance); service.Profile.relicId = resonance.id;
        service.Profile.inventory.Insert(0, null);
        Check(service.HasMechanic(EquipmentMechanic.TwinSummonResonance), "null inventory rows are safely skipped");
        // Warm the methods before measuring only these managed read operations.
        for (int i = 0; i < 256; i++) { service.HasMechanic(EquipmentMechanic.TwinSummonResonance); service.Equipped(ItemSlot.Relic); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        bool found = true;
        for (int i = 0; i < 16384; i++)
            found &= service.HasMechanic(EquipmentMechanic.TwinSummonResonance) && object.ReferenceEquals(service.Equipped(ItemSlot.Relic), resonance);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(found, "repeated reads preserve object and mechanic identity");
        Check(allocated == 0, "managed lookup loop allocated " + allocated + " bytes; this is not a Unity frame measurement");
        Check(service.LoadSlot(service.CurrentSlotId), "reload original durable fixture");
        Check(!service.HasMechanic(EquipmentMechanic.TwinSummonResonance) && !object.ReferenceEquals(service.Equipped(ItemSlot.Relic), resonance),
            "profile replacement immediately changes lookup source");
        Check(service.Equipped((ItemSlot)999) == null, "unsupported slot is rejected");
        return checks + " equipment identity/eligibility checks; managed 16,384-read allocation probe passed (not Unity profiling)";
    }
}
