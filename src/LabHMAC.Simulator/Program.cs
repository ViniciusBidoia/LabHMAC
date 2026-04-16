using System.Security.Cryptography;
using System.Text;

// ────────────────────────────────────────────────────────────────
// LabHMAC Terminal Simulator
// Simulates a payment terminal (acquirer) calling the backend API.
// Demonstrates three scenarios:
//   1. Valid signed request   → 200 OK
//   2. Tampered request       → 401 Unauthorized
//   3. Missing signature      → 400 Bad Request
// ────────────────────────────────────────────────────────────────

const string baseUrl = "http://localhost:5024";
const string endpoint = "/api/payments/validate";
const string secretKey = "minha-chave-secreta-super-segura";

using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║           LabHMAC — Payment Terminal Simulator           ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.WriteLine();

// ─── Scenario 1: Valid signed request ───────────────────────────
Console.WriteLine("━━━ Scenario 1: Valid Signed Request ━━━");
string body = """{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":49.90,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}""";

Console.WriteLine($"  Body:      {body}");
string signature = ComputeHmacSignature(secretKey, body);
Console.WriteLine($"  Signature: {signature}");

await SendRequest(httpClient, endpoint, body, signature);
Console.WriteLine();

// ─── Scenario 2: Tampered request ───────────────────────────────
Console.WriteLine("━━━ Scenario 2: Tampered Request (body modified after signing) ━━━");
string tamperedBody = """{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":99999.99,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}""";

Console.WriteLine($"  Original signature: {signature}");
Console.WriteLine($"  Tampered body:      {tamperedBody}");

await SendRequest(httpClient, endpoint, tamperedBody, signature);
Console.WriteLine();

// ─── Scenario 3: Missing signature header ───────────────────────
Console.WriteLine("━━━ Scenario 3: Missing Signature Header ━━━");
Console.WriteLine($"  Body: {body}");
Console.WriteLine("  X-Hmac-Signature: (not sent)");

await SendRequest(httpClient, endpoint, body, signatureHeader: null);
Console.WriteLine();

Console.WriteLine("Done. All three scenarios demonstrated.");

// ────────────────────────────────────────────────────────────────
// Helper functions
// ────────────────────────────────────────────────────────────────

static string ComputeHmacSignature(string key, string payload)
{
    // Step 1: Encode the secret key as UTF-8 bytes.
    byte[] keyBytes = Encoding.UTF8.GetBytes(key);
    Console.WriteLine($"  HMAC Step 1 — Key encoded to {keyBytes.Length} bytes.");

    // Step 2: Encode the payload as UTF-8 bytes.
    byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
    Console.WriteLine($"  HMAC Step 2 — Payload encoded to {payloadBytes.Length} bytes.");

    // Step 3: Compute HMAC-SHA256.
    byte[] hash = HMACSHA256.HashData(keyBytes, payloadBytes);
    Console.WriteLine($"  HMAC Step 3 — HMAC-SHA256 computed ({hash.Length} bytes).");

    // Step 4: Convert to lowercase hex string.
    string hex = Convert.ToHexString(hash).ToLowerInvariant();
    Console.WriteLine($"  HMAC Step 4 — Hex signature: {hex}");

    return hex;
}

static async Task SendRequest(HttpClient client, string url, string body, string? signatureHeader)
{
    using var request = new HttpRequestMessage(HttpMethod.Post, url)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    if (signatureHeader is not null)
    {
        request.Headers.Add("X-Hmac-Signature", signatureHeader);
    }

    try
    {
        using var response = await client.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"  → HTTP {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine($"  → Response: {responseBody}");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"  → Connection error: {ex.Message}");
        Console.WriteLine("    Make sure the API is running: dotnet run --project src/LabHMAC.Api");
    }
}
