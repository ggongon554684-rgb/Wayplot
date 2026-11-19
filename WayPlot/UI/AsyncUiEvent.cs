namespace WayPlot.UI;

internal static class AsyncUiEvent
{
    public static async void Run(Control owner, Func<Task> action, string failureMessage)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            AppLog.Error(failureMessage, ex);
            if (!owner.IsDisposed)
                MessageBox.Show(owner, failureMessage, "WayPlot CRM", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
