namespace Emberfall
{
    public sealed partial class GameUI
    {
        private string BlessingSubtitle(bool mobile)
        {
            string fallback=mobile?"选择一项 · 仅本局生效":"第 "+session.DungeonWave+" 波完成 · 选择一项，仅本局生效";
            return RoomBlessingPreview.Subtitle(session.RoomChainRun,session.RunChoices.CompletedWave,fallback);
        }
    }
}
