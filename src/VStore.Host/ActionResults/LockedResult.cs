using Microsoft.AspNetCore.Mvc;

namespace NuClear.VStore.Host.ActionResults
{
    public sealed class LockedResult : ContentResult
    {
        public LockedResult(string message)
        {
            StatusCode = 423;
            Content = message;
        }
    }
}
