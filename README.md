# LabHMAC

Projeto didatico em .NET 10 que demonstra como proteger a integridade de requisicoes HTTP com HMAC SHA-256, simulando o fluxo de uma maquininha de adquirencia chamando uma API de pagamentos.

---

## Como o HMAC SHA-256 funciona

```
Segredo compartilhado
			 |
			 v
+-------------+    +---------------+    +----------------+    +----------------+
| UTF-8 bytes |    |  UTF-8 bytes  |    |  HMAC-SHA256   |    |   Hex string   |
| (secret key)|--->|  (body JSON)  |--->|   (32 bytes)   |--->|  (64 chars)    |
+-------------+    +---------------+    +----------------+    +----------------+
			 |
Enviada no header X-Hmac-Signature
```

1. Codificar a chave secreta como bytes UTF-8.
2. Codificar o corpo da requisicao como bytes UTF-8.
3. Computar HMAC-SHA256(keyBytes, payloadBytes) para gerar 32 bytes.
4. Converter para hex lowercase e obter string de 64 caracteres.
5. Comparar com CryptographicOperations.FixedTimeEquals (timing-safe).

O servidor refaz os passos 1-4 com o mesmo segredo e compara o resultado com o header recebido. Se qualquer byte do corpo for alterado, o hash muda completamente.

---

## Estrutura do projeto

```
LabHMAC/
|-- src/
|   |-- LabHMAC.Api/
|   |   |-- Domain/
|   |   |   |-- IHmacService.cs
|   |   |   |-- HmacValidationResult.cs
|   |   |   \-- PaymentRequest.cs
|   |   |-- Application/
|   |   |   \-- HmacService.cs
|   |   |-- Api/
|   |   |   |-- HmacValidationFilter.cs
|   |   |   \-- PaymentsController.cs
|   |   \-- Program.cs
|   \-- LabHMAC.Simulator/
|       \-- Program.cs
\-- tests/
		\-- LabHMAC.Tests/
				|-- Unit/
				|   |-- HmacServiceTests.cs
				|   \-- HmacValidationResultTests.cs
				\-- Integration/
						\-- PaymentsEndpointTests.cs
```

---

## Pre-requisitos

- .NET 10 SDK
- Terminal (PowerShell, bash, etc.)

Verificar versao:

```bash
dotnet --version
```

Esperado: 10.x.x

---

## Endpoint

| Metodo | Rota | Header obrigatorio |
|---|---|---|
| POST | /api/payments/validate | X-Hmac-Signature: <hex> |

Body (application/json):

```json
{
	"transactionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
	"amount": 49.90,
	"merchantId": "MERCHANT-001",
	"timestamp": "2026-04-12T10:00:00+00:00"
}
```

---

## Respostas esperadas

| Cenario | Status | Body |
|---|---|---|
| Assinatura valida | 200 | {"status":"valid","message":"Request integrity verified."} |
| Corpo adulterado ou chave errada | 401 | {"status":"invalid","message":"Signature mismatch. ..."} |
| Header ausente | 400 | {"status":"error","message":"X-Hmac-Signature header is missing."} |
| Header em formato invalido | 400 | {"status":"error","message":"X-Hmac-Signature header value is not a valid hex string. ..."} |

---

## Configuracao da chave secreta

A chave e lida de [src/LabHMAC.Api/appsettings.json](src/LabHMAC.Api/appsettings.json):

```json
{
	"HMAC": {
		"SecretKey": "minha-chave-secreta-super-segura"
	}
}
```

Ou via variavel de ambiente:

```powershell
# PowerShell
$env:HMAC__SecretKey = "outra-chave"
```

```bash
# bash
export HMAC__SecretKey="outra-chave"
```

Importante: a chave no simulador em [src/LabHMAC.Simulator/Program.cs](src/LabHMAC.Simulator/Program.cs) deve ser identica a da API.

---

## Passo a passo para executar e testar

### 1. Restaurar dependencias

Na raiz do repositorio:

```bash
dotnet restore
```

### 2. Executar testes automatizados

```bash
dotnet test
```

Cobertura atual:

- Unit: [tests/LabHMAC.Tests/Unit/HmacServiceTests.cs](tests/LabHMAC.Tests/Unit/HmacServiceTests.cs)
- Unit: [tests/LabHMAC.Tests/Unit/HmacValidationResultTests.cs](tests/LabHMAC.Tests/Unit/HmacValidationResultTests.cs)
- Integration: [tests/LabHMAC.Tests/Integration/PaymentsEndpointTests.cs](tests/LabHMAC.Tests/Integration/PaymentsEndpointTests.cs)

Para output detalhado:

```bash
dotnet test --logger "console;verbosity=detailed"
```

### 3. Rodar a API

```bash
dotnet run --project src/LabHMAC.Api
```

URLs padrao:

- http://localhost:5024
- https://localhost:7074

### 4. Teste manual com curl

Gerar assinatura no PowerShell:

```powershell
$body = '{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":49.90,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}'
$key  = [System.Text.Encoding]::UTF8.GetBytes("minha-chave-secreta-super-segura")
$msg  = [System.Text.Encoding]::UTF8.GetBytes($body)
$hmac = [System.Security.Cryptography.HMACSHA256]::new($key)
[System.BitConverter]::ToString($hmac.ComputeHash($msg)).Replace("-","").ToLower()
```

Cenario 1 - requisicao valida (200):

```bash
curl -s -X POST http://localhost:5024/api/payments/validate \
	-H "Content-Type: application/json" \
	-H "X-Hmac-Signature: <SIGNATURE>" \
	-d '{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":49.90,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}'
```

Cenario 2 - corpo adulterado (401):

```bash
curl -s -X POST http://localhost:5024/api/payments/validate \
	-H "Content-Type: application/json" \
	-H "X-Hmac-Signature: <SIGNATURE>" \
	-d '{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":99999.99,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}'
```

Cenario 3 - header ausente (400):

```bash
curl -s -X POST http://localhost:5024/api/payments/validate \
	-H "Content-Type: application/json" \
	-d '{"transactionId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","amount":49.90,"merchantId":"MERCHANT-001","timestamp":"2026-04-12T10:00:00+00:00"}'
```

### 5. Rodar o simulador (maquininha)

Em um terminal, suba a API na porta esperada pelo simulador:

```bash
dotnet run --project src/LabHMAC.Api --urls http://localhost:5000
```

Em outro terminal, execute o simulador:

```bash
dotnet run --project src/LabHMAC.Simulator
```

O simulador executa automaticamente 3 cenarios:

1. Assinatura valida (200)
2. Corpo adulterado (401)
3. Header ausente (400)

---

## Tecnologias usadas

- .NET 10 / ASP.NET Core 10
- System.Security.Cryptography.HMACSHA256
- CryptographicOperations.FixedTimeEquals
- IAsyncActionFilter (pipeline MVC)
- xUnit + WebApplicationFactory
