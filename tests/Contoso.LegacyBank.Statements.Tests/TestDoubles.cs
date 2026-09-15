using System;
using System.Collections.Generic;
using Contoso.LegacyBank.Statements.Api.Models;
using Contoso.LegacyBank.Statements.Api.Services;

namespace Contoso.LegacyBank.Statements.Tests
{
    internal sealed class FakeAccountGateway : IAccountGateway
    {
        public AccountCustomer Customer { get; set; }
        public AccountDetails Account { get; set; }
        public IList<StatementTransaction> Transactions { get; set; }

        public AccountCustomer GetCustomer(string customerNumber) { return Customer; }
        public AccountDetails GetAccount(string accountNumber) { return Account; }
        public IList<StatementTransaction> GetTransactions(
            string accountNumber,
            DateTime fromDate,
            DateTime toDate) { return Transactions; }
    }

    internal sealed class MemoryRepository : IStatementJobRepository
    {
        public StatementDocument Created { get; private set; }
        public StatementStatusResponse Status { get; set; }

        public void Create(StatementDocument document) { Created = document; }
        public StatementStatusResponse GetStatus(Guid jobId) { return Status; }
    }

    internal sealed class NullLogger : IAppLogger
    {
        public void Info(string message) { }
        public void Error(string message, Exception exception) { }
    }

    internal sealed class FakeStatementService : IStatementService
    {
        public CreateStatementResponse CreateResponse { get; set; }
        public StatementStatusResponse StatusResponse { get; set; }

        public CreateStatementResponse Create(CreateStatementRequest request)
        {
            return CreateResponse;
        }

        public StatementStatusResponse GetStatus(Guid jobId)
        {
            return StatusResponse;
        }
    }
}
