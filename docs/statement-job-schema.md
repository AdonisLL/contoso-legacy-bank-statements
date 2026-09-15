# Statement job schema 1.0

The API writes one UTF-8 JSON file named `{jobId}.json` atomically into
`Pending`. The document worker can render a statement without calling WCF.

```json
{
  "schemaVersion": "1.0",
  "jobId": "52e30b8a-9fd8-44a2-ac10-f3ae01eb2c0b",
  "createdUtc": "2026-09-15T21:00:00Z",
  "customer": {
    "customerNumber": "C100",
    "fullName": "Ada Lovelace",
    "email": "ada@example.test",
    "address": "1 Main Street"
  },
  "account": {
    "accountNumber": "A100",
    "customerNumber": "C100",
    "accountType": "Checking",
    "currency": "USD"
  },
  "period": {
    "fromDate": "2026-08-01T00:00:00",
    "toDate": "2026-08-31T00:00:00"
  },
  "balances": {
    "periodOpeningBalance": 1000.00,
    "periodClosingBalance": 1125.00,
    "currentBalance": 1125.00,
    "availableBalance": 1100.00
  },
  "transactions": [
    {
      "transactionId": "T100",
      "postedDate": "2026-08-15T12:30:00Z",
      "description": "Deposit",
      "amount": 125.00,
      "balance": 1125.00,
      "type": "Credit"
    }
  ]
}
```

Workers move the input through `Processing`. They write a result document with
`pdfPath` to `Completed`, or a result document with `error` to `Failed`.
Directory moves must stay on the same volume to remain atomic.
