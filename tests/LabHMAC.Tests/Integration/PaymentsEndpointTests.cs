using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LabHMAC.Tests.Integration;

/// <summary>
/// Integration tests for POST /api/payments/validate.
/// Uses <see cref="WebApplicationFactory{TEntryPoint}"/> to spin up the full HTTP pipeline in-memory.
/// </summary>
public class PaymentsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string SecretKey = "minha-chave-secreta-super-segura";
    private const string Endpoint = "/api/payments/validate";

    public PaymentsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ──────────────────────────────────────────────────
    // T015 [US1] — Valid signed request → 200 OK
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task ValidSignedRequest_Returns200WithValidStatus()
    {
        string body = """{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":49.90,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}""";
        string signature = ComputeHmac(body);

        var request = CreateRequest(body, signature);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"valid\"", json);
        Assert.Contains("\"message\":\"Request integrity verified.\"", json);
    }

    // ──────────────────────────────────────────────────
    // T017 [US2] — Tampered request → 401 Unauthorized
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task TamperedRequest_Returns401WithInvalidStatus()
    {
        string originalBody = """{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":49.90,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}""";
        string tamperedBody = """{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":99.99,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}""";
        string signature = ComputeHmac(originalBody); // signed with original, sent with tampered

        var request = CreateRequest(tamperedBody, signature);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"invalid\"", json);
        Assert.Contains("Signature mismatch", json);
    }

    // ──────────────────────────────────────────────────
    // T020 [US3] — Missing header → 400 Bad Request
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task MissingSignatureHeader_Returns400WithErrorStatus()
    {
        string body = """{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":49.90,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}""";

        var request = CreateRequest(body, signature: null);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"error\"", json);
        Assert.Contains("X-Hmac-Signature header is missing.", json);
    }

    // ──────────────────────────────────────────────────
    // T021 [US3] — Malformed signature → 400 Bad Request
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task MalformedSignature_Returns400WithFormatMessage()
    {
        string body = """{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":49.90,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}""";

        var request = CreateRequest(body, signature: "not-valid-hex");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"error\"", json);
        Assert.Contains("not a valid hex string", json);
    }

    // ──────────────────────────────────────────────────
    // Helper methods
    // ──────────────────────────────────────────────────

    private static string ComputeHmac(string payload)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(SecretKey);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        byte[] hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static HttpRequestMessage CreateRequest(string body, string? signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        if (signature is not null)
        {
            request.Headers.Add("X-Hmac-Signature", signature);
        }

        return request;
    }
}
