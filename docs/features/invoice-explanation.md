# Invoice explanation MVP

This read-only endpoint uses Qwen3 8B running in Ollama to explain an existing invoice.
It does not change invoices, calculate charges, or initiate payments. No AI SDK or
database migration is required.

## Run

### Browser interface

The page at `http://localhost:8080/llm.html` now provides direct general-purpose
chat through `POST /api/chat`, independent of invoices. Run with
`mvn spring-boot:run "-Dspring-boot.run.profiles=chat"` to use it without SQL Server.
This profile disables billing endpoints and database initialization. Chat accepts
alternating user/assistant messages ending in user, at most 40 messages, 8000
characters per message and 32000 total. It returns `model` and `reply`.
The invoice explanation endpoint below remains available in the default billing profile.

Install Ollama from https://ollama.com and download the model:

```powershell
ollama pull qwen3:8b
```

Ensure Ollama is running (`ollama serve` if it is not already running). Configure
SQL Server as described in the root README, then start the API:

```powershell
mvn spring-boot:run
```

The defaults are `BILLING_LLM_BASE_URL=http://localhost:11434`,
`BILLING_LLM_MODEL=qwen3:8b`, and `BILLING_LLM_TIMEOUT_SECONDS=120`.
Set these environment variables before startup to override them. For a slower or
memory-constrained machine, try `ollama pull qwen3:4b` and set
`$env:BILLING_LLM_MODEL = 'qwen3:4b'`. Model files require disk space plus additional
runtime memory; response speed depends on hardware. Local inference has no API fee.

Use an existing invoice ID from `GET /customers/{id}/invoices`, or create one using
`POST /invoices/generate` with `{"subscriptionId":"..."}`. Then:

```powershell
$invoiceId = 'REPLACE_WITH_EXISTING_INVOICE_UUID'
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/invoices/$invoiceId/explanation"
```

Example response (wording varies):

```json
{
  "invoiceId": "11111111-1111-1111-1111-111111111111",
  "model": "qwen3:8b",
  "explanation": "This open invoice covers your monthly subscription...",
  "aiGenerated": true
}
```

No request body is needed. Unknown invoices return HTTP 404 (`invoice_not_found`).
Connection failures, timeouts, model errors, and empty or truncated generations
return HTTP 503 (`llm_unavailable`). There is no automatic retry or fallback text.

Only stored status, dates, amounts, and line descriptions/quantities/prices are sent
to the configured model server. Customer identifiers and payment credentials are
excluded. Descriptions are still business data; use a trusted model server.
The schema does not store currency on invoices, so the prompt forbids inventing one.
Generated prose may be inaccurate; the existing invoice endpoint remains the source
of truth. Responses are not persisted or cached. Treat explanation text as plain
text when displaying it in a UI.

This MVP inherits the API's existing access controls. Before public deployment,
add authentication, invoice ownership checks, and request limits across the API.

## Verification

```powershell
mvn --batch-mode --no-transfer-progress test
```

Tests use a local fake Ollama HTTP server, requiring no model download. They cover
the request contract, payload minimization, successful explanations, missing invoices,
and upstream failures. To evaluate actual wording and latency, run the example above
with a real local model and compare the explanation against `GET /invoices/{id}`.
