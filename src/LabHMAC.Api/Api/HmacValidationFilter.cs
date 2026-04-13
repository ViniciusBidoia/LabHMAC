using LabHMAC.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LabHMAC.Api.Api;

/// <summary>
/// Action filter that performs HMAC SHA-256 validation before the controller action executes.
/// </summary>
/// <remarks>
/// <b>How it works</b>:
/// <list type="number">
///   <item>Reads the raw request body (buffering was enabled in middleware).</item>
///   <item>Extracts the <c>X-Hmac-Signature</c> header value.</item>
///   <item>Delegates to <see cref="IHmacService.Validate"/> for cryptographic verification.</item>
///   <item>If validation fails, short-circuits the pipeline with the appropriate HTTP status code.</item>
/// </list>
/// This filter is applied to endpoints that require HMAC validation, keeping the controller
/// free of signature-checking logic (Single Responsibility Principle).
/// </remarks>
public class HmacValidationFilter : IAsyncActionFilter
{
    private readonly IHmacService _hmacService;

    public HmacValidationFilter(IHmacService hmacService)
    {
        _hmacService = hmacService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Read the raw body — EnableBuffering() was called in middleware so the stream is seekable.
        context.HttpContext.Request.Body.Position = 0;
        using var reader = new StreamReader(context.HttpContext.Request.Body, leaveOpen: true);
        string rawBody = await reader.ReadToEndAsync();
        context.HttpContext.Request.Body.Position = 0;

        // Extract the X-Hmac-Signature header.
        string? receivedSignature = context.HttpContext.Request.Headers["X-Hmac-Signature"].FirstOrDefault();

        // Delegate to the domain service for validation.
        HmacValidationResult result = _hmacService.Validate(rawBody, receivedSignature);

        if (result.IsValid)
        {
            // Signature is valid — proceed to the controller action.
            await next();
            return;
        }

        // Determine the appropriate HTTP status code based on the failure type.
        int statusCode = result.Message.Contains("missing", StringComparison.OrdinalIgnoreCase)
                         || result.Message.Contains("not a valid hex", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status401Unauthorized;

        string status = statusCode == StatusCodes.Status400BadRequest ? "error" : "invalid";

        context.Result = new JsonResult(new { status, message = result.Message })
        {
            StatusCode = statusCode
        };
    }
}
