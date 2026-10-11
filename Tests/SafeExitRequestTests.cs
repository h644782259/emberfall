using System;
using Emberfall;

public static class SafeExitRequestTests
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    public static string Run()
    {
        assertions = 0;
        int writes = 0, leaves = 0;
        var request = new SafeExitRequest();
        Func<bool> preserve = () => { writes++; return true; };
        Func<bool, bool> leave = title =>
        {
            leaves++;
            Check(!request.Confirm(preserve, _ => { leaves++; return true; }), "reentrant confirmation is blocked");
            request.Cancel();
            request.Request(!title);
            Check(request.Open && request.ToTitle == title, "busy request cannot be cancelled or redirected");
            return true;
        };
        request.Request(false);
        request.Cancel();
        Check(!request.Confirm(preserve, leave) && writes == 0 && leaves == 0, "cancel does not save or leave");
        request.Request(true);
        request.Request(false);
        Check(request.ToTitle, "repeated request keeps original destination");
        Check(!request.Confirm(() => { writes++; return false; }, leave) && request.Open && !request.Prepared && leaves == 0,
            "save failure keeps dialog open without leaving");
        Check(request.Confirm(preserve, leave) && !request.Open && !request.Prepared && leaves == 1 && writes == 2,
            "retry after failed save preserves once and leaves");
        Check(!request.Confirm(preserve, leave) && writes == 2 && leaves == 1, "repeated completed confirmation is a no-op");

        // Regression: successful preparation followed by rejected leave must not
        // dismiss the dialog or rotate the backup on its next confirmation.
        request.Request(false);
        int before = writes;
        Check(!request.Confirm(preserve, _ => { leaves++; return false; }) && request.Open && request.Prepared && !request.Busy,
            "rejected leave retains successful save and visible retry state");
        Check(writes == before + 1, "rejected leave prepared exactly once");
        Check(request.Confirm(() => { throw new Exception("must reuse successful preparation"); }, leave) && !request.Open,
            "retry uses prepared progress without another save");
        Check(writes == before + 1, "successful retry did not rotate backup again");

        request.Request(true);
        Check(!request.Confirm(preserve, _ => false), "second rejection fixture prepared");
        request.Cancel();
        Check(!request.Prepared && !request.Open, "cancel invalidates prepared transaction");
        before = writes;
        request.Request(true);
        Check(request.Confirm(preserve, leave) && writes == before + 1, "new request after cancel must preserve new progress");

        request.Request(true);
        try { request.Confirm(() => { throw new InvalidOperationException("save interrupted"); }, leave); }
        catch (InvalidOperationException) { }
        Check(request.Open && !request.Busy && !request.Prepared, "save exception leaves retryable unprepared dialog");
        before = writes;
        try { request.Confirm(preserve, _ => { throw new InvalidOperationException("leave rejected"); }); }
        catch (InvalidOperationException) { }
        Check(request.Open && !request.Busy && request.Prepared && writes == before + 1,
            "leave exception retains successful preparation and visible dialog");
        Check(request.Confirm(() => { throw new Exception("duplicate write after exception"); }, leave),
            "leave exception can retry without duplicate persistence");
        Check(!request.Open && !request.Busy, "accepted retry finishes transaction");

        int actualWrites = 0;
        request.Request(false);
        Check(request.Confirm(() => true, _ => true) && actualWrites == 0, "title-only exit requires no character write");
        return "PASS: " + assertions + " safe-exit preservation, rejection, cancel, exception and repeated-confirm assertions";
    }
}
