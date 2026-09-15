# Statements API

The classic ASP.NET Web API 2 application listens on `http://localhost:8091`.

## Create a statement job

`POST /api/statements`

```json
{
  "customerNumber": "C100",
  "accountNumber": "A100",
  "fromDate": "2026-08-01",
  "toDate": "2026-08-31"
}
```

Successful requests return HTTP `202 Accepted`:

```json
{
  "jobId": "52e30b8a-9fd8-44a2-ac10-f3ae01eb2c0b",
  "status": "pending",
  "statusUrl": "/api/statements/52e30b8a-9fd8-44a2-ac10-f3ae01eb2c0b"
}
```

The API returns `400` for invalid ownership or periods, `404` for an unknown
customer/account, `502` when the accounts WCF dependency fails, and `500` when
the job cannot be stored.

## Read status

`GET /api/statements/{jobId}` returns:

```json
{
  "jobId": "52e30b8a-9fd8-44a2-ac10-f3ae01eb2c0b",
  "status": "completed",
  "pdfPath": "C:\\Statements\\52e30b8a.pdf",
  "updatedUtc": "2026-09-15T21:00:00Z"
}
```

Status is `pending` while a job is in `Pending` or `Processing`, `completed`
when its result is in `Completed`, and `failed` when its result is in `Failed`.
