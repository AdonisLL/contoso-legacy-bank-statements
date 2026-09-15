using System;
using System.Collections.Generic;
using Contoso.LegacyBank.Statements.Api.Models;
using Contoso.LegacyBank.Statements.Api.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contoso.LegacyBank.Statements.Tests
{
    [TestClass]
    public sealed class StatementServiceTests
    {
        [TestMethod]
        public void CreateBuildsVersionedWorkerDocument()
        {
            var repository = new MemoryRepository();
            var gateway = ValidGateway();
            var service = new StatementService(
                gateway,
                repository,
                new NullLogger(),
                366,
                () => new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc));

            var result = service.Create(ValidRequest());

            Assert.AreEqual("pending", result.Status);
            Assert.AreEqual("1.0", repository.Created.SchemaVersion);
            Assert.AreEqual("C100", repository.Created.Customer.CustomerNumber);
            Assert.AreEqual("A100", repository.Created.Account.AccountNumber);
            Assert.AreEqual(90m, repository.Created.Balances.PeriodOpeningBalance);
            Assert.AreEqual(100m, repository.Created.Balances.PeriodClosingBalance);
            Assert.AreEqual(2, repository.Created.Transactions.Count);
            Assert.AreEqual("T1", repository.Created.Transactions[0].TransactionId);
        }

        [TestMethod]
        [ExpectedException(typeof(StatementValidationException))]
        public void CreateRejectsAccountNotOwnedByCustomer()
        {
            var gateway = ValidGateway();
            gateway.Account.CustomerNumber = "OTHER";
            var service = NewService(gateway);
            service.Create(ValidRequest());
        }

        [TestMethod]
        [ExpectedException(typeof(StatementValidationException))]
        public void CreateRejectsInvalidPeriod()
        {
            var request = ValidRequest();
            request.FromDate = new DateTime(2026, 9, 10);
            request.ToDate = new DateTime(2026, 9, 1);
            NewService(ValidGateway()).Create(request);
        }

        private static StatementService NewService(IAccountGateway gateway)
        {
            return new StatementService(
                gateway,
                new MemoryRepository(),
                new NullLogger(),
                366,
                () => new DateTime(2026, 9, 15));
        }

        private static CreateStatementRequest ValidRequest()
        {
            return new CreateStatementRequest
            {
                CustomerNumber = "C100",
                AccountNumber = "A100",
                FromDate = new DateTime(2026, 9, 1),
                ToDate = new DateTime(2026, 9, 15)
            };
        }

        private static FakeAccountGateway ValidGateway()
        {
            return new FakeAccountGateway
            {
                Customer = new AccountCustomer
                {
                    CustomerNumber = "C100",
                    FullName = "Ada Lovelace"
                },
                Account = new AccountDetails
                {
                    AccountNumber = "A100",
                    CustomerNumber = "C100",
                    AccountType = "Checking",
                    Currency = "USD",
                    CurrentBalance = 100m,
                    AvailableBalance = 95m
                },
                Transactions = new List<StatementTransaction>
                {
                    new StatementTransaction
                    {
                        TransactionId = "T2",
                        PostedDate = new DateTime(2026, 9, 2),
                        Amount = -5m
                    },
                    new StatementTransaction
                    {
                        TransactionId = "T1",
                        PostedDate = new DateTime(2026, 9, 1),
                        Amount = 15m
                    }
                }
            };
        }
    }
}
