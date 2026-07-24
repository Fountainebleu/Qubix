namespace Qubix.Api.ErrorHandling;

internal static class ErrorHandlingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApiStatusCodePages(
        this IApplicationBuilder app)
    {
        app.UseStatusCodePages(async statusCodeContext =>
        {
            var httpContext = statusCodeContext.HttpContext;
            var problemDetailsFactory = httpContext.RequestServices
                .GetRequiredService<ApiProblemDetailsFactory>();
            var problemDetailsService = httpContext.RequestServices
                .GetRequiredService<IProblemDetailsService>();
            var problemDetails = problemDetailsFactory.Create(
                httpContext,
                httpContext.Response.StatusCode);

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });
        });

        return app;
    }
}
