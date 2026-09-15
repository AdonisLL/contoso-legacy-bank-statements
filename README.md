# Contoso Legacy Bank Statements

Classic ASP.NET Web API 2 service targeting .NET Framework 4.8. It validates a
statement request against the accounts WCF service, gathers all rendering data,
and atomically publishes a versioned JSON job for the document worker.

## Prerequisites

- Windows with .NET Framework 4.8 developer pack
- Visual Studio 2022 Build Tools with MSBuild and Web build tools
- IIS Express
- Accounts WCF service at `http://localhost:8090/AccountService`

## Build and test

```powershell
$msbuild = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
& $msbuild .\Contoso.LegacyBank.Statements.sln /t:Restore
& $msbuild .\Contoso.LegacyBank.Statements.sln /m /p:Configuration=Release
dotnet test .\tests\Contoso.LegacyBank.Statements.Tests\Contoso.LegacyBank.Statements.Tests.csproj --no-build --configuration Release
```

## Run

Start the accounts service first, then launch this web project from Visual
Studio or IIS Express:

```powershell
& "${env:ProgramFiles}\IIS Express\iisexpress.exe" /path:"$PWD\src\Contoso.LegacyBank.Statements.Api" /port:8091
```

The API is available at `http://localhost:8091/api/statements`.

## Configuration

Settings are in `src\Contoso.LegacyBank.Statements.Api\Web.config`:

- `AccountServiceUrl`: accounts `BasicHttpBinding` endpoint.
- `StatementDocumentsRoot`: job root. When blank it defaults to
  `%LOCALAPPDATA%\ContosoLegacyBank\Documents`.
- `StatementMaximumPeriodDays`: inclusive maximum statement period, default
  `366`.

The service creates `Pending`, `Processing`, `Completed`, `Failed`, and `Logs`
under the job root. See [API documentation](docs/api.md) and the
[job schema](docs/statement-job-schema.md).
