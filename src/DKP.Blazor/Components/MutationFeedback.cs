using Radzen;

namespace DKP.Blazor.Components;

internal static class MutationFeedback
{
    public static async Task RefreshAsync(Func<Task> refresh, NotificationService notifications)
    {
        try
        {
            await refresh();
        }
        catch (Exception)
        {
            notifications.Notify(NotificationSeverity.Warning, "Saved; refresh failed",
                "Your change was saved, but the latest data could not be loaded. Reload the page before making another change; do not repeat the saved operation.");
        }
    }
}
