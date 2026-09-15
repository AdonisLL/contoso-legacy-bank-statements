using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Contoso.LegacyBank.Statements.Api.Services;

namespace Contoso.LegacyBank.Statements.Api.Infrastructure
{
    public sealed class AppLogger : IAppLogger
    {
        private readonly string logPath;
        private static readonly object Sync = new object();

        public AppLogger(string documentsRoot)
        {
            var logDirectory = Path.Combine(documentsRoot, "Logs");
            Directory.CreateDirectory(logDirectory);
            logPath = Path.Combine(logDirectory, "statements.log");
        }

        public void Info(string message)
        {
            Write("INFO", message, null);
        }

        public void Error(string message, Exception exception)
        {
            Write("ERROR", message, exception);
        }

        private void Write(string level, string message, Exception exception)
        {
            var line = string.Format(
                CultureInfo.InvariantCulture,
                "{0:O} [{1}] {2}{3}",
                DateTime.UtcNow,
                level,
                message,
                exception == null ? string.Empty : " | " + exception);

            Trace.WriteLine(line);
            try
            {
                lock (Sync)
                {
                    File.AppendAllText(logPath, line + Environment.NewLine);
                }
            }
            catch (IOException)
            {
                Trace.WriteLine("Unable to write statements log file.");
            }
            catch (UnauthorizedAccessException)
            {
                Trace.WriteLine("Access denied writing statements log file.");
            }
        }
    }
}
