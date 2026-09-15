using System;
using System.IO;
using System.Linq;
using Contoso.LegacyBank.Statements.Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Contoso.LegacyBank.Statements.Api.Services
{
    public sealed class FileStatementJobRepository : IStatementJobRepository
    {
        private static readonly string[] StatusDirectories =
            { "Pending", "Processing", "Completed", "Failed" };

        private readonly string root;
        private readonly IAppLogger logger;
        private readonly JsonSerializerSettings serializerSettings;

        public FileStatementJobRepository(string root, IAppLogger logger)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("A documents root is required.", "root");
            }

            this.root = Path.GetFullPath(root);
            this.logger = logger;
            serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };

            foreach (var directory in StatusDirectories)
            {
                Directory.CreateDirectory(Path.Combine(this.root, directory));
            }
        }

        public void Create(StatementDocument document)
        {
            var pendingDirectory = Path.Combine(root, "Pending");
            var destination = Path.Combine(pendingDirectory, document.JobId + ".json");
            var temporary = Path.Combine(
                pendingDirectory,
                "." + document.JobId + "." + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                var job = new
                {
                    schemaVersion = 1,
                    jobId = document.JobId,
                    customer = document.Customer.FullName,
                    account = document.Account.AccountNumber,
                    fromDate = document.Period.FromDate,
                    toDate = document.Period.ToDate,
                    openingBalance = document.Balances.PeriodOpeningBalance,
                    closingBalance = document.Balances.PeriodClosingBalance,
                    transactions = document.Transactions.Select(transaction => new
                    {
                        date = transaction.PostedDate,
                        description = transaction.Description,
                        amount = transaction.Amount,
                        balance = transaction.Balance
                    })
                };
                var json = JsonConvert.SerializeObject(job, serializerSettings);
                using (var stream = new FileStream(
                    temporary,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.WriteThrough))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                File.Move(temporary, destination);
            }
            catch (Exception exception)
            {
                TryDelete(temporary);
                logger.Error("Unable to persist statement job " + document.JobId + ".", exception);
                throw;
            }
        }

        public StatementStatusResponse GetStatus(Guid jobId)
        {
            foreach (var directory in StatusDirectories)
            {
                var path = FindStatusPath(directory, jobId);
                if (path == null)
                {
                    continue;
                }

                try
                {
                    var json = JObject.Parse(File.ReadAllText(path));
                    return new StatementStatusResponse
                    {
                        JobId = jobId,
                        Status = MapStatus(directory, json),
                        PdfPath = ReadString(json, "pdfPath", "PdfPath", "outputPath", "OutputPath"),
                        Error = ReadString(json, "error", "Error", "errorMessage", "ErrorMessage", "message", "Message"),
                        UpdatedUtc = File.GetLastWriteTimeUtc(path)
                    };
                }
                catch (Exception exception)
                {
                    logger.Error("Unable to read statement status " + jobId + ".", exception);
                    return new StatementStatusResponse
                    {
                        JobId = jobId,
                        Status = "failed",
                        Error = "The statement status file is invalid.",
                        UpdatedUtc = File.GetLastWriteTimeUtc(path)
                    };
                }
            }

            throw new StatementNotFoundException("Statement job " + jobId + " was not found.");
        }

        private string FindStatusPath(string directory, Guid jobId)
        {
            var statusDirectory = Path.Combine(root, directory);
            if (directory.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                var completion = Path.Combine(statusDirectory, jobId + ".completion.json");
                return File.Exists(completion) ? completion : null;
            }
            if (directory.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                return Directory.GetFiles(statusDirectory, jobId + ".*.failure.json")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }

            var job = Path.Combine(statusDirectory, jobId + ".json");
            return File.Exists(job) ? job : null;
        }

        private static string MapStatus(string directory, JObject json)
        {
            if (directory.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                return "completed";
            }
            if (directory.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                return "failed";
            }

            var explicitStatus = ReadString(json, "status", "Status");
            return string.Equals(explicitStatus, "failed", StringComparison.OrdinalIgnoreCase)
                ? "failed"
                : "pending";
        }

        private static string ReadString(JObject json, params string[] names)
        {
            foreach (var name in names)
            {
                var token = json.GetValue(name, StringComparison.OrdinalIgnoreCase);
                if (token != null && token.Type != JTokenType.Null)
                {
                    return token.Type == JTokenType.String
                        ? token.Value<string>()
                        : token.ToString();
                }
            }
            return null;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Preserve the original persistence failure.
            }
        }
    }
}
