using System;
using System.Collections.Generic;

namespace Contoso.LegacyBank.Statements.Api.Models
{
    public sealed class StatementDocument
    {
        public string SchemaVersion { get; set; }
        public Guid JobId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public StatementCustomer Customer { get; set; }
        public StatementAccount Account { get; set; }
        public StatementPeriod Period { get; set; }
        public StatementBalances Balances { get; set; }
        public IList<StatementTransaction> Transactions { get; set; }
    }

    public sealed class StatementCustomer
    {
        public string CustomerNumber { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }

    public sealed class StatementAccount
    {
        public string AccountNumber { get; set; }
        public string CustomerNumber { get; set; }
        public string AccountType { get; set; }
        public string Currency { get; set; }
    }

    public sealed class StatementPeriod
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public sealed class StatementBalances
    {
        public decimal PeriodOpeningBalance { get; set; }
        public decimal PeriodClosingBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal AvailableBalance { get; set; }
    }

    public sealed class StatementTransaction
    {
        public string TransactionId { get; set; }
        public DateTime PostedDate { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal? Balance { get; set; }
        public string Type { get; set; }
    }

    public sealed class AccountCustomer
    {
        public string CustomerNumber { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }

    public sealed class AccountDetails
    {
        public string AccountNumber { get; set; }
        public string CustomerNumber { get; set; }
        public string AccountType { get; set; }
        public string Currency { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal AvailableBalance { get; set; }
    }
}
