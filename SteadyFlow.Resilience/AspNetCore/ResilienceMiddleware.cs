using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace SteadyFlow.Resilience.AspNetCore
{
    public class ResilienceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ResiliencePipeline _pipeline;

        public ResilienceMiddleware(RequestDelegate next, ResiliencePipeline pipeline)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Func<Task> action = () => _next(context);

            var pipelineFunc = _pipeline.Build(action);
            await pipelineFunc();
        }
    }
}
