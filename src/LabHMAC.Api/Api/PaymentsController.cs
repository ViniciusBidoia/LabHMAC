using LabHMAC.Api.Api;
using Microsoft.AspNetCore.Mvc;

namespace LabHMAC.Api.Api;

/// <summary>
/// Thin controller for the payment validation endpoint.
/// All HMAC verification is handled by <see cref="HmacValidationFilter"/> — the controller
/// only returns the success response after the filter has confirmed request integrity.
/// </summary>
[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    /// <summary>
    /// Validates the HMAC SHA-256 signature of the incoming payment request.
    /// If the <see cref="HmacValidationFilter"/> allows execution to reach this action,
    /// the request is guaranteed to be integral.
    /// </summary>
    [HttpPost("validate")]
    [ServiceFilter(typeof(HmacValidationFilter))]
    public IActionResult Validate()
    {
        return Ok(new { status = "valid", message = "Request integrity verified." });
    }
}
