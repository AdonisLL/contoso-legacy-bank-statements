using System;
using System.Collections.Generic;
using Contoso.LegacyBank.Statements.Api.Models;

namespace Contoso.LegacyBank.Statements.Api.Services
{
    public interface IAccountGateway
    {
        AccountCustomer GetCustomer(string customerNumber);
        AccountDetails GetAccount(string customerNumber, string accountNumber);
        IList<StatementTransaction> GetTransactions(string accountNumber, DateTime fromDate, DateTime toDate);
    }

    public interface IStatementJobRepository
    {
        void Create(StatementDocument document);
        StatementStatusResponse GetStatus(Guid jobId);
    }

    public interface IStatementService
    {
        CreateStatementResponse Create(CreateStatementRequest request);
        StatementStatusResponse GetStatus(Guid jobId);
    }

    public interface IAppLogger
    {
        void Info(string message);
        void Error(string message, Exception exception);
    }

    public sealed class StatementValidationException : Exception
    {
        public StatementValidationException(string message) : base(message) { }
    }

    public sealed class StatementNotFoundException : Exception
    {
        public StatementNotFoundException(string message) : base(message) { }
    }

    public sealed class AccountDependencyException : Exception
    {
        public AccountDependencyException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
