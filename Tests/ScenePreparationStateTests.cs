using System;
using Emberfall;
public static class ScenePreparationStateTests
{
    private static int checks;
    private static void Check(bool condition, string text) { checks++; if (!condition) throw new Exception(text); }
    public static string Run()
    {
        var load = new ScenePreparationState();
        Check(!load.Active, "idle does not block play");
        load.Begin("camp"); Check(load.Active && load.Progress == 0, "entry closes action before work");
        load.Report(.7f, "terrain"); load.Report(.2f, "textures"); Check(load.Progress == .7f, "nested tasks cannot regress progress");
        load.Report(4, "complete work"); Check(load.Active && load.Progress < 1, "reported progress cannot release action");
        load.Fail("asset missing"); load.Report(.9f, "other task"); load.Complete();
        Check(load.Active && load.Error == "asset missing" && load.Progress < 1, "failure keeps the action gate closed");
        load.Cancel(); Check(!load.Active && load.Error == null && load.Progress == 0, "return to title cancels failure");
        load.Begin("dungeon"); load.Report(-2, "start"); Check(load.Progress == 0, "negative progress clamps");
        load.Report(.99f, "GPU"); Check(load.Active, "last upload still blocks input");
        load.Complete(); Check(!load.Active && load.Progress == 1, "only successful completion releases input");
        load.Report(.4f, "late callback"); Check(load.Progress == 1, "late callbacks cannot regress a completed load");
        return "PASS " + checks + " preparation progress/action gate checks";
    }
}
