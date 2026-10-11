namespace Emberfall
{
    public static class CostumeLayers
    {
        public static int UpgradeStage(int rank) { return rank>=10?3:rank>=7?2:rank>=3?1:0; }
        // Only detachable outer pieces; body, limbs, cloth simulation and head identity remain intact.
        public static bool IsBaseOuter(string name)
        {
            switch(name)
            {
                case "Cuirass": case "Overlapping Armor": case "Chest Crest": case "Armor Tasset": case "Pauldrons":
                case "Layered Robe": case "Embroidered Stole": case "Robe Hem": case "Arcane Brooch": case "Long robe front panel":
                case "Leather Vest": case "Vest Buckle": case "Cross Body Strap": case "Single leather shoulder": case "Diagonal ranger sash":
                case "Leaf ritual mantle": case "Bound spirit totem": case "Totem moonstone": return true;
                default: return false;
            }
        }
    }
}
