using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Qubix.Api.ErrorHandling;

internal sealed class ApiProblemDetailsFactory
{
    public ProblemDetails Create(
        HttpContext httpContext,
        int statusCode,
        string? detail = null,
        string? code = null,
        string? title = null)
    {
        var descriptor = GetDescriptor(statusCode);
        var problemDetails = new ProblemDetails
        {
            Type = descriptor.Type,
            Title = title ?? descriptor.Title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        AddCommonExtensions(
            httpContext,
            problemDetails,
            code ?? descriptor.Code);

        return problemDetails;
    }

    public ValidationProblemDetails CreateValidation(
        HttpContext httpContext,
        IReadOnlyDictionary<string, string[]> errors,
        string? detail = null)
    {
        var descriptor = GetDescriptor(StatusCodes.Status400BadRequest);
        var problemDetails = new ValidationProblemDetails(
            errors.ToDictionary(pair => pair.Key, pair => pair.Value))
        {
            Type = descriptor.Type,
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        AddCommonExtensions(
            httpContext,
            problemDetails,
            ApiErrorCodes.Validation);

        return problemDetails;
    }

    public static void EnsureCommonExtensions(
        HttpContext httpContext,
        ProblemDetails problemDetails)
    {
        var statusCode = problemDetails.Status ?? httpContext.Response.StatusCode;
        var descriptor = GetDescriptor(statusCode);

        problemDetails.Type ??= descriptor.Type;
        problemDetails.Title ??= descriptor.Title;
        problemDetails.Status ??= statusCode;
        problemDetails.Instance ??= httpContext.Request.Path;

        if (!problemDetails.Extensions.ContainsKey("code"))
        {
            problemDetails.Extensions["code"] = descriptor.Code;
        }

        if (!problemDetails.Extensions.ContainsKey("traceId"))
        {
            problemDetails.Extensions["traceId"] =
                Activity.Current?.Id ?? httpContext.TraceIdentifier;
        }
    }

    private static void AddCommonExtensions(
        HttpContext httpContext,
        ProblemDetails problemDetails,
        string code)
    {
        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }

    private static ProblemDescriptor GetDescriptor(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => new(
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1",
                "Bad request",
                ApiErrorCodes.Validation),
            StatusCodes.Status401Unauthorized => new(
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.2",
                "Authentication required",
                ApiErrorCodes.AuthenticationRequired),
            StatusCodes.Status403Forbidden => new(
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.4",
                "Access denied",
                ApiErrorCodes.AccessDenied),
            StatusCodes.Status404NotFound => new(
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5",
                "Resource not found",
                ApiErrorCodes.NotFound),
            StatusCodes.Status409Conflict => new(
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10",
                "Conflict",
                ApiErrorCodes.Conflict),
            StatusCodes.Status500InternalServerError => new(
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1",
                "Internal server error",
                ApiErrorCodes.InternalError),
            _ => new(
                "about:blank",
                "Request failed",
                ApiErrorCodes.HttpError)
        };
    }

    private sealed record ProblemDescriptor(
        string Type,
        string Title,
        string Code);
}

internal static class ApiErrorCodes
{
    public const string Validation = "validation_error";
    public const string AuthenticationRequired = "authentication_required";
    public const string AccessDenied = "access_denied";
    public const string NotFound = "resource_not_found";
    public const string Conflict = "conflict";
    public const string InternalError = "internal_error";
    public const string HttpError = "http_error";
}
