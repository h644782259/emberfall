using System;
using Emberfall;

public static class TouchScrollGestureTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }

    public static string Run()
    {
        checks = 0;
        var s = new TouchScrollGesture();
        Check(s.Begin(1, 50, 100, 20, 200, 9), "first finger owns gesture");
        Check(!s.Begin(2, 0, 0, 0, 200, 9), "second finger cannot steal ownership");
        Check(!s.Advance(1, 50, 100, false, false) && s.Position == 20, "stationary touch stays a tap");
        Check(!s.Advance(1, 54, 105, false, false) && s.Position == 20, "small two-axis jitter does not scroll");
        Check(!s.Advance(2, 900, -900, true, false) && s.Finger == 1 && s.Position == 20, "other finger release cannot scroll or end owner");
        Check(!s.Advance(2, 900, -900, false, true) && s.Finger == 1, "other finger cancel cannot end owner");
        Check(s.Advance(1, 50, 70, false, false) && s.Position == 50, "vertical drag follows Y displacement");
        Check(s.Advance(1, 50, -900, false, false) && s.Position == 200, "release outside viewport still uses captured owner and bottom clamp");
        Check(s.Advance(1, 50, 900, false, false) && s.Position == 0, "top clamp");
        Check(s.Advance(1, 50, 900, true, false) && s.Finger == -1000 && !s.Dragging, "vertical release stays consumed and releases owner");
        Check(!s.Advance(1, 50, 900, true, false), "duplicate release does nothing");

        s.Begin(3, 100, 100, 40, 200, 9);
        Check(s.Advance(3, 110, 100, false, false) && s.Position == 40, "horizontal swipe cancels tap without scrolling vertically");
        Check(s.Advance(3, 100, 100, false, false) && s.Position == 40, "returning to original button cannot restore a tap");
        Check(s.Advance(3, 100, 100, true, false), "horizontal swipe release remains consumed over its original button");

        s.Begin(4, 100, 100, 40, 200, 9);
        Check(s.Advance(4, 107, 93, false, false) && s.Position == 47, "diagonal movement crosses radial threshold even when each axis is below nine");
        Check(s.Advance(4, 100, 100, true, false) && s.Position == 40, "diagonal return release is consumed");
        s.Begin(5, 100, 100, 40, 200, 10);
        Check(s.Advance(5, 106, 108, true, false) && s.Position == 32, "exact radial threshold on release consumes without an earlier move event");

        s.Begin(6, 0, 0, 0, 0, 9);
        Check(!s.Advance(6, 0, 0, true, false) && s.Finger == -1000, "stationary tap remains clickable with unscrollable content");
        s.Begin(7, 0, 0, 0, 0, 9);
        Check(s.Advance(7, 30, 0, true, false) && s.Position == 0, "horizontal swipe cancels click even with no vertical overflow");

        s.Begin(8, 0, 0, 25, 200, 9);
        s.Advance(8, 20, 0, false, false);
        Check(s.Advance(8, 20, 0, false, true) && s.Finger == -1000 && !s.Dragging, "cancelled horizontal drag preserves suppression and releases owner");
        Check(!s.Advance(8, 0, 0, true, false), "late release after cancellation cannot act");
        s.Begin(9, 0, 0, 25, 200, 9);
        Check(!s.Advance(9, 0, 0, false, true) && s.Finger == -1000, "cancel before dragging releases without inventing a drag");

        // GameUI calls Cancel when a panel changes or input is suspended. Model
        // the gesture boundary here; this does not claim to execute IMGUI events.
        s.Begin(10, 30, 60, 25, 200, 9);
        s.Advance(10, 60, 20, false, false);
        s.Cancel();
        Check(s.Finger == -1000 && !s.Dragging, "panel reset drops old capture");
        Check(!s.Advance(10, 100, 100, true, false), "old-panel release cannot update reset gesture");
        Check(s.Begin(11, 4, 8, 90, 200, 9) && s.Position == 90, "new panel starts with its own scroll position");
        Check(!s.Advance(10, -900, -900, false, false) && s.Finger == 11 && s.Position == 90, "stale old-panel movement cannot steal new owner");
        Check(!s.Advance(11, 4, 8, true, false), "fresh panel tap does not inherit drag suppression");
        s.Cancel(); s.Cancel();
        Check(s.Begin(11, 0, 0, 0, 200, 18), "reused finger identity can begin after repeated reset");
        Check(!s.Advance(11, 12, 12, false, false), "scaled threshold rejects sub-threshold diagonal jitter");
        Check(s.Advance(11, 14, 14, true, false), "scaled diagonal threshold preserves density-independent cancellation");
        return checks + " touch scroll 2D cancellation, vertical motion, release, ownership and panel-reset assertions passed";
    }
}
