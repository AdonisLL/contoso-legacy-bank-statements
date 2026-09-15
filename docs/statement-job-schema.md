# Statement job schema 1

The API writes one UTF-8 JSON file named `{jobId}.json` atomically into
`Pending`. The document worker can render a statement without calling WCF.

```json
{
  "schemaVersion": 1,
  "jobId": "52e30b8a-9fd8-44a2-ac10-f3ae01eb2c0b",
  "customer": "Ada Lovelace",
  "account": "A100",
  "fromDate": "2026-08-01T00:00:00",
  "toDate": "2026-08-31T00:00:00",
  "openingBalance": 1000.00,
  "closingBalance": 1125.00,
  "transactions": [
    {
      "date": "2026-08-15T12:30:00Z",
      "description": "Deposit",
      "amount": 125.00,
      "balance": 1125.00
    }
  ]
}
```

The worker moves the input through `Processing`. It writes
`{jobId}.completion.json` with `pdfPath` to `Completed`. Failures are written
as `{jobId}.{diagnosticId}.failure.json` with a sanitized `message`, and the
original job is preserved beside the metadata. Directory moves must stay on
the same volume to remain atomic.
