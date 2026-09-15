using System;
using System.IO;
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
                var json = JsonConvert.SerializeObject(document, serializerSettings);
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
                var path = Path.Combine(root, directory, jobId + ".json");
                if (!File.Exists(path))
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
                        Error = ReadString(json, "error", "Error", "errorMessage", "ErrorMessage"),
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
