using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class ChppWrite05PermissionRegression
{
    public static int Run()
    {
        ChppMatchOrderPermissionGuard.EnsureScope(new[] { "set_matchorder", "manage_youthplayers" });

        var blocked = false;
        try
        {
            ChppMatchOrderPermissionGuard.EnsureScope(new[] { "manage_youthplayers" });
        }
        catch (UnauthorizedAccessException)
        {
            blocked = true;
        }
        if (!blocked) throw new InvalidOperationException("WRITE-05: set_matchorder yokken aktarım engellenmedi.");

        blocked = false;
        try
        {
            ChppMatchOrderPermissionGuard.EnsureScope(Array.Empty<string>());
        }
        catch (UnauthorizedAccessException)
        {
            blocked = true;
        }
        if (!blocked) throw new InvalidOperationException("WRITE-05: boş scope ile aktarım engellenmedi.");

        ChppMatchOrderPermissionGuard.EnsureScope(new[] { "SET_MATCHORDER" });
        Console.WriteLine("CHPP WRITE-05 permission regression: PASS");
        return 0;
    }
}
