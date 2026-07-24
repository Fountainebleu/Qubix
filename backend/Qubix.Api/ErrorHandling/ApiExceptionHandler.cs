using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubix.Core.Common;
using Qubix.Core.Exceptions;

namespace Qubix.Api.ErrorHandling;

internal sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IProblemDetailsService problemDetailsService,
    ApiProblemDetailsFactory problemDetailsFactory) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = MapException(httpContext, exception);
        httpContext.Response.StatusCode =
            problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        LogException(httpContext, exception, problemDetails);

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });

        return true;
    }

    private ProblemDetails MapException(
        HttpContext httpContext,
        Exception exception)
    {
        return exception switch
        {
            RequestValidationException validationException =>
                problemDetailsFactory.CreateValidation(
                    httpContext,
                    validationException.Errors,
                    validationException.Message),
            ArgumentException argumentException =>
                problemDetailsFactory.CreateValidation(
                    httpContext,
                    CreateArgumentErrors(argumentException)),
            AuthenticationRequiredException authenticationException =>
                problemDetailsFactory.Create(
                    httpContext,
                    StatusCodes.Status401Unauthorized,
                    authenticationException.Message),
            AccessDeniedException accessDeniedException =>
                problemDetailsFactory.Create(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    accessDeniedException.Message),
            UnauthorizedAccessException unauthorizedAccessException =>
                problemDetailsFactory.Create(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    unauthorizedAccessException.Message),
            NotFoundException notFoundException =>
                problemDetailsFactory.Create(
                    httpContext,
                    StatusCodes.Status404NotFound,
                    notFoundException.Message),
            KeyNotFoundException keyNotFoundException =>
                problemDetailsFactory.Create(
                    httpContext,
                    StatusCodes.Status404NotFound,
                    keyNotFoundException.Message),
            ConflictException conflictException =>
                problemDetailsFactory.Create(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    conflictException.Message),
            DomainException domainException =>
                problemDetailsFactory.Create(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    domainException.Message),
            DbUpdateConcurrencyException =>
                problemDetailsFactory.Create(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "The resource was changed by another request."),
            BadHttpRequestException =>
                problemDetailsFactory.Create(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "The request could not be parsed."),
            _ => problemDetailsFactory.Create(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.")
        };
    }

    private static IReadOnlyDictionary<string, string[]> CreateArgumentErrors(
        ArgumentException exception)
    {
        return new Dictionary<string, string[]>
        {
            [exception.ParamName ?? "request"] = [exception.Message]
        };
    }

    private void LogException(
        HttpContext httpContext,
        Exception exception,
        ProblemDetails problemDetails)
    {
        var statusCode =
            problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        var code = problemDetails.Extensions["code"];

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled error for {Method} {Path}. Status: {StatusCode}, Code: {Code}, TraceId: {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode,
                code,
                httpContext.TraceIdentifier);
            return;
        }

        logger.LogWarning(
            "Request {Method} {Path} failed. Status: {StatusCode}, Code: {Code}, TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            statusCode,
            code,
            httpContext.TraceIdentifier);
    }
}
