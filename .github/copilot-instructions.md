# Copilot instructions

- This repository is a C# .NET Framework 4.8 classic ASP.NET Web API 2 service.
- Preserve compatibility with Visual Studio MSBuild and IIS Express.
- Do not replace the accounts `BasicHttpBinding` WCF dependency with REST.
- Keep statement jobs backward compatible and version `schemaVersion` when
  changing their JSON shape.
- Statement creation must gather all WCF data before atomically publishing a
  job into `Pending`; the document worker must never need WCF.
- Never log customer details, account numbers, transaction data, or full JSON
  documents. Log job IDs, operation names, and actionable failures only.
- Add or update MSTest coverage for behavior changes.
