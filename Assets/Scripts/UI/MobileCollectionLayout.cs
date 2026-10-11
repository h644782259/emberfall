using System;
namespace Emberfall
{
    /// <summary>Touch-unit geometry shared by mobile inventory and reward content.</summary>
    public static class MobileCollectionLayout
    {
        public const float ActionHeight = 48, ChestHeight = 176, ScoreHeight = 84;
        public static bool SideBySideInventory(float width) { return width >= 800; }
        public static MobilePanelLayout.Area Split(float width, float y, int index, int count, float height = ActionHeight)
        {
            if (count < 1 || count > 4 || index < 0 || index >= count || float.IsNaN(width) || float.IsInfinity(width) || width < count * 48 + (count - 1) * 8)
                throw new ArgumentOutOfRangeException(nameof(width));
            float size = (width - (count - 1) * 8) / count;
            return new MobilePanelLayout.Area(index * (size + 8), y, size, height);
        }
        public static MobilePanelLayout.Area ChestCard(float width, int index)
        { return Split(width, 0, index, 3, ChestHeight); }
        public static MobilePanelLayout.Area ChestAction(float width, int index)
        {
            var card = ChestCard(width, index);
            return new MobilePanelLayout.Area(card.X + 8, card.YMax - ActionHeight - 8, card.Width - 16, ActionHeight);
        }
        public static MobilePanelLayout.Area ScoreCard(float width, int index)
        { return Split(width, 0, index, 2, ScoreHeight); }
    }
}
