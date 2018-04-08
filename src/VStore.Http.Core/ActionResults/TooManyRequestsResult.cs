using System;

using Microsoft.AspNetCore.Mvc;

namespace NuClear.VStore.Http.Core.ActionResults
{
    public sealed class TooManyRequestsResult : StatusCodeResult
    {
        public TooManyRequestsResult(TimeSpan retryAfter) : base(429)
        {
            RetryAfter = retryAfter;
        }

        public TimeSpan RetryAfter { get; }

        public override void ExecuteResult(ActionContext context)
        {
            base.ExecuteResult(context);
            context.HttpContext.Response.Headers["Retry-After"] = RetryAfter.TotalSeconds.ToString("0");
        }
    }
}