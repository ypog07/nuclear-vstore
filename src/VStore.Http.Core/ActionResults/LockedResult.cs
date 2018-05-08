using Microsoft.AspNetCore.Mvc;

namespace NuClear.VStore.Http.Core.ActionResults
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
