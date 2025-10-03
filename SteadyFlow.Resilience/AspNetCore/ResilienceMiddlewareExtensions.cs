using Microsoft.AspNetCore.Builder;
using System;

namespace SteadyFlow.Resilience.AspNetCore
{
    public static class ResilienceMiddlewareExtensions
    {
        public static IApplicationBuilder UseResiliencePipeline(
           this IApplicationBuilder builder,
           Action<ResilienceOptions> configure)
        {
            var options = new ResilienceOptions();
            configure(options);

            var pipeline = new ResiliencePipeline(options);
            return builder.UseMiddleware<ResilienceMiddleware>(pipeline);
        }
    }
}
