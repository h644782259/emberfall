using System;
using Emberfall;
public static class MobileCollectionLayoutTests
{
    private static int checks;
    private static void Check(bool condition, string description) { checks++; if (!condition) throw new Exception(description); }
    private static void Inside(MobilePanelLayout.Area child, float width, float height)
    { Check(child.X >= 0 && child.Y >= 0 && child.XMax <= width + .01f && child.YMax <= height + .01f, "Contained mobile content"); }
    public static string Run()
    {
        checks = 0;
        float[][] devices = { new[] { 568f,320f,163f }, new[] { 1334f,750f,326f }, new[] { 2208f,1080f,401f }, new[] { 2250f,1125f,458f }, new[] { 2340f,1080f,460f }, new[] { 2048f,1536f,264f }, new[] { 2388f,1668f,264f }, new[] { 2732f,2048f,264f }, new[] { 1280f,720f,0f } };
        foreach (var device in devices)
        {
            var controls = new MobileControlLayout(device[0],device[1],device[2]);
            var panel = new MobilePanelLayout(controls.Width,controls.Height);
            Check(panel.Body.Height >= 188 && panel.TabbedBody.Height >= 136, "Compact scroll viewports retain their minimum readable height");
            Check(!panel.Body.Overlaps(panel.Footer) && !panel.TabbedBody.Overlaps(panel.Tabs), "Fixed controls never overlap scrolling content");
            for (int i = 0; i < 4; i++)
            {
                var tab = panel.Tab(i,4); Inside(tab,panel.Width,panel.Height);
                Check(tab.Width >= 100 && tab.Height >= 44, "Four inventory tabs remain finger-sized");
            }
            for (int count = 2; count <= 3; count++)
                for (int i = 0; i < count; i++)
                {
                    var action = panel.FooterButton(i,count); Inside(action,panel.Width,panel.Height);
                    Check(action.Width >= 150 && action.Height == 48, "Inventory/reward fixed actions are 48 touch units tall");
                    for (int j = i+1; j < count; j++) Check(!action.Overlaps(panel.FooterButton(j,count)), "Adjacent fixed actions do not overlap");
                }
            float chestWidth = panel.Body.Width-18;
            for (int i = 0; i < 3; i++)
            {
                MobilePanelLayout.Area card = MobileCollectionLayout.ChestCard(chestWidth,i), action = MobileCollectionLayout.ChestAction(chestWidth,i);
                Inside(card,chestWidth,panel.Body.Height); Inside(action,chestWidth,panel.Body.Height);
                Check(action.Height==48 && action.Width>=140, "All three unopened choices retain equal-sized large actions");
                Check(action.X>=card.X && action.XMax<=card.XMax && action.Y>=card.Y && action.YMax<=card.YMax, "Chest action remains inside its own card");
                for(int j=i+1;j<3;j++) Check(!card.Overlaps(MobileCollectionLayout.ChestCard(chestWidth,j)), "Three equal-odds card hitboxes stay separate");
            }
            bool wide=MobileCollectionLayout.SideBySideInventory(panel.Width);
            float detailWidth=(wide?panel.Right.Width:panel.Body.Width)-34;
            MobilePanelLayout.Area current=MobileCollectionLayout.ScoreCard(detailWidth,0),candidate=MobileCollectionLayout.ScoreCard(detailWidth,1);
            Check(current.Width>=180 && candidate.Width>=180 && !current.Overlaps(candidate), "Current/candidate scores retain independent readable columns");
            Check(current.Height==84 && candidate.Height==84, "Score labels, large number and delta have dedicated vertical space");
            MobilePanelLayout.Area lockButton=MobileCollectionLayout.Split(detailWidth,0,0,2),saleButton=MobileCollectionLayout.Split(detailWidth,0,1,2);
            Check(lockButton.Height==48 && !lockButton.Overlaps(saleButton), "Lock and sale have separate full touch targets");
            if(wide)Check(panel.Right.Width>=400 && !panel.Left.Overlaps(panel.Right),"Tablet list and detail columns have sufficient width");
        }
        Check(!MobileCollectionLayout.SideBySideInventory(568) && !MobileCollectionLayout.SideBySideInventory(799) && MobileCollectionLayout.SideBySideInventory(800), "Compact navigation switches at the production breakpoint");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,20f})
        { bool threw=false;try{MobileCollectionLayout.Split(invalid,0,0,2);}catch(ArgumentOutOfRangeException){threw=true;}Check(threw,"Invalid content width cannot create unsafe targets"); }
        return checks + " mobile inventory/reward geometry assertions passed";
    }
}
