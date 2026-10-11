using System;

namespace Emberfall
{
    // One stable identity for the production skill, its copy and its photographed
    // icon. Names, descriptions, costs and hit geometry remain gameplay authority.
    public enum SkillPrimaryBody
    {
        ForgedBlade, DustStorm, WeaponMastery, GuardArmor, ChargeWarrior,
        LifeBanner, GroundFault, JudgmentSword, IceCrystal, Meteor,
        Lightning, LightningChain, FrostStride, IceLightning, GravityCore,
        ArrowVolley, MechanicalTrap, ArrowRain, RangerVault, PoisonVines,
        WindWard, SpiritImpulse, Wolf, TimberTurret, SpiritSpear, Ember,
        ContractMastery, Treant
    }

    public readonly struct SkillIdentityRecipe
    {
        public readonly HeroClass Hero;
        public readonly int Slot;
        public readonly SkillPrimaryBody Primary;
        public readonly float PhotographSeconds;
        public readonly bool IncludeCaster;
        internal SkillIdentityRecipe(HeroClass hero, int slot, SkillPrimaryBody primary, float seconds, bool caster)
        { Hero = hero; Slot = slot; Primary = primary; PhotographSeconds = seconds; IncludeCaster = caster; }
        public string Id { get { return SkillIdentity.Id(Hero, Slot); } }
        public string Name { get { return GameBalance.SkillName(Hero, Slot); } }
        public string Description { get { return GameBalance.SkillDescription(Hero, Slot); } }
        public string IconResourceKey { get { return SkillIdentity.IconResourceKey(Hero, Slot); } }
    }

    public static class SkillIdentity
    {
        private static readonly string[] families = { "vanguard", "arcanist", "ranger", "summoner" };
        private static readonly SkillPrimaryBody[,] bodies = {
            { SkillPrimaryBody.ForgedBlade, SkillPrimaryBody.ForgedBlade, SkillPrimaryBody.DustStorm, SkillPrimaryBody.WeaponMastery,
              SkillPrimaryBody.GuardArmor, SkillPrimaryBody.ChargeWarrior, SkillPrimaryBody.LifeBanner, SkillPrimaryBody.GroundFault,
              SkillPrimaryBody.GuardArmor, SkillPrimaryBody.JudgmentSword },
            { SkillPrimaryBody.IceCrystal, SkillPrimaryBody.Meteor, SkillPrimaryBody.Lightning, SkillPrimaryBody.WeaponMastery,
              SkillPrimaryBody.LightningChain, SkillPrimaryBody.FrostStride, SkillPrimaryBody.IceLightning, SkillPrimaryBody.GravityCore,
              SkillPrimaryBody.Lightning, SkillPrimaryBody.Meteor },
            { SkillPrimaryBody.ArrowVolley, SkillPrimaryBody.MechanicalTrap, SkillPrimaryBody.ArrowRain, SkillPrimaryBody.WeaponMastery,
              SkillPrimaryBody.RangerVault, SkillPrimaryBody.PoisonVines, SkillPrimaryBody.ArrowVolley, SkillPrimaryBody.ArrowVolley,
              SkillPrimaryBody.WindWard, SkillPrimaryBody.ArrowRain },
            { SkillPrimaryBody.SpiritImpulse, SkillPrimaryBody.PoisonVines, SkillPrimaryBody.Wolf, SkillPrimaryBody.ContractMastery,
              SkillPrimaryBody.TimberTurret, SkillPrimaryBody.SpiritSpear, SkillPrimaryBody.Ember, SkillPrimaryBody.GravityCore,
              SkillPrimaryBody.SpiritImpulse, SkillPrimaryBody.Treant }
        };
        // Sampling time is editorial only. It never schedules damage, animation,
        // target acquisition or cooldown. The fixture advances normal Unity frames.
        private static readonly float[,] photograph = {
            { .10f, .10f, .30f, 0, .15f, .17f, 1.04f, .12f, .15f, .19f },
            { .12f, .47f, .245f, 0, .09f, .20f, .66f, .30f, .13f, .40f },
            { .045f, .12f, .12f, 0, .29f, .30f, .045f, .055f, .27f, .40f },
            { .12f, .28f, .18f, 0, .18f, .05f, .18f, .28f, .13f, .18f }
        };

        private static void Validate(HeroClass hero, int slot)
        {
            if ((int)hero < 0 || (int)hero >= families.Length) throw new ArgumentOutOfRangeException(nameof(hero));
            if (slot < 0 || slot >= GameBalance.SkillCount) throw new ArgumentOutOfRangeException(nameof(slot));
        }
        public static string Id(HeroClass hero, int slot)
        { Validate(hero, slot); return families[(int)hero] + "/" + slot.ToString("D2"); }
        public static string IconResourceKey(HeroClass hero, int slot)
        { Validate(hero, slot); return "skill-entities-" + families[(int)hero] + "/" + slot.ToString("D2"); }
        public static SkillIdentityRecipe Describe(HeroClass hero, int slot)
        {
            Validate(hero, slot);
            bool caster = slot == 3 || hero == HeroClass.Vanguard && (slot == 4 || slot == 5 || slot == 8)
                || hero == HeroClass.Arcanist && slot == 5 || hero == HeroClass.Ranger && (slot == 4 || slot == 8);
            return new SkillIdentityRecipe(hero, slot, bodies[(int)hero, slot], photograph[(int)hero, slot], caster);
        }
    }
}
