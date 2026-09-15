using System;
using System.IO;
using Contoso.LegacyBank.Statements.Api.Models;
using Contoso.LegacyBank.Statements.Api.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Contoso.LegacyBank.Statements.Tests
{
    [TestClass]
    public sealed class FileStatementJobRepositoryTests
    {
        private string root;

        [TestInitialize]
        public void Initialize()
        {
            root = Path.Combine(
                Environment.CurrentDirectory,
                "TestDocuments",
                Guid.NewGuid().ToString("N"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void CreateAtomicallyWritesPendingJson()
        {
            var repository = new FileStatementJobRepository(root, new NullLogger());
            var document = MinimalDocument();

            repository.Create(document);

            var path = Path.Combine(root, "Pending", document.JobId + ".json");
            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual("1.0", JObject.Parse(File.ReadAllText(path))["schemaVersion"]);
            Assert.AreEqual(0, Directory.GetFiles(Path.Combine(root, "Pending"), "*.tmp").Length);
            Assert.AreEqual("pending", repository.GetStatus(document.JobId).Status);
        }

        [TestMethod]
        public void GetStatusReadsCompletedResult()
        {
            var repository = new FileStatementJobRepository(root, new NullLogger());
            var id = Guid.NewGuid();
            File.WriteAllText(
                Path.Combine(root, "Completed", id + ".json"),
                "{\"jobId\":\"" + id + "\",\"pdfPath\":\"C:\\\\statements\\\\x.pdf\"}");

            var status = repository.GetStatus(id);

            Assert.AreEqual("completed", status.Status);
            Assert.AreEqual(@"C:\statements\x.pdf", status.PdfPath);
        }

        [TestMethod]
        public void GetStatusReadsFailedResult()
        {
            var repository = new FileStatementJobRepository(root, new NullLogger());
            var id = Guid.NewGuid();
            File.WriteAllText(
                Path.Combine(root, "Failed", id + ".json"),
                "{\"error\":\"PDF rendering failed\"}");

            var status = repository.GetStatus(id);

            Assert.AreEqual("failed", status.Status);
            Assert.AreEqual("PDF rendering failed", status.Error);
        }

        private static StatementDocument MinimalDocument()
        {
            return new StatementDocument
            {
                SchemaVersion = "1.0",
                JobId = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow,
                Customer = new StatementCustomer(),
                Account = new StatementAccount(),
                Period = new StatementPeriod(),
                Balances = new StatementBalances(),
                Transactions = new StatementTransaction[0]
            };
        }
    }
}
