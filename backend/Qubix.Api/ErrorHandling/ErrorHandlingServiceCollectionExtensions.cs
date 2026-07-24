using Microsoft.AspNetCore.Mvc;

namespace Qubix.Api.ErrorHandling;

internal static class ErrorHandlingServiceCollectionExtensions
{
    public static IServiceCollection AddApiErrorHandling(
        this IServiceCollection services)
    {
        services.AddSingleton<ApiProblemDetailsFactory>();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
                ApiProblemDetailsFactory.EnsureCommonExtensions(
                    context.HttpContext,
                    context.ProblemDetails);
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = actionContext =>
            {
                var errors = actionContext.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors
                            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The supplied value is invalid."
                                : error.ErrorMessage)
                            .Distinct()
                            .ToArray());
                var factory = actionContext.HttpContext.RequestServices
                    .GetRequiredService<ApiProblemDetailsFactory>();
                var problemDetails = factory.CreateValidation(
                    actionContext.HttpContext,
                    errors);

                return new BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        return services;
    }
}
