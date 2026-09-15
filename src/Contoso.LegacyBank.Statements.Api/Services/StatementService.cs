using System;
using System.Collections.Generic;
using System.Linq;
using Contoso.LegacyBank.Statements.Api.Models;

namespace Contoso.LegacyBank.Statements.Api.Services
{
    public sealed class StatementService : IStatementService
    {
        private readonly IAccountGateway accountGateway;
        private readonly IStatementJobRepository repository;
        private readonly IAppLogger logger;
        private readonly int maximumPeriodDays;
        private readonly Func<DateTime> utcNow;

        public StatementService(
            IAccountGateway accountGateway,
            IStatementJobRepository repository,
            IAppLogger logger,
            int maximumPeriodDays,
            Func<DateTime> utcNow)
        {
            this.accountGateway = accountGateway;
            this.repository = repository;
            this.logger = logger;
            this.maximumPeriodDays = maximumPeriodDays;
            this.utcNow = utcNow;
        }

        public CreateStatementResponse Create(CreateStatementRequest request)
        {
            ValidateRequest(request);

            var customerNumber = request.CustomerNumber.Trim();
            var accountNumber = request.AccountNumber.Trim();
            var fromDate = request.FromDate.Value.Date;
            var toDate = request.ToDate.Value.Date;

            AccountCustomer customer;
            AccountDetails account;
            IList<StatementTransaction> transactions;
            try
            {
                customer = accountGateway.GetCustomer(customerNumber);
                account = accountGateway.GetAccount(customerNumber, accountNumber);
                if (customer == null)
                {
                    throw new StatementNotFoundException("The customer was not found.");
                }
                if (account == null)
                {
                    throw new StatementNotFoundException("The account was not found.");
                }
                if (!string.Equals(
                    account.CustomerNumber,
                    customerNumber,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new StatementValidationException(
                        "The account does not belong to the supplied customer.");
                }

                transactions = accountGateway.GetTransactions(accountNumber, fromDate, toDate)
                    ?? new List<StatementTransaction>();
            }
            catch (StatementValidationException)
            {
                throw;
            }
            catch (StatementNotFoundException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.Error("The accounts service request failed.", exception);
                throw new AccountDependencyException(
                    "The accounts service is unavailable or returned an invalid response.",
                    exception);
            }

            var orderedTransactions = transactions
                .OrderBy(transaction => transaction.PostedDate)
                .ThenBy(transaction => transaction.TransactionId)
                .ToList();
            var periodActivity = orderedTransactions.Sum(transaction => transaction.Amount);
            var runningBalance = account.CurrentBalance - periodActivity;
            foreach (var transaction in orderedTransactions)
            {
                runningBalance += transaction.Amount;
                transaction.Balance = runningBalance;
            }
            var jobId = Guid.NewGuid();
            var document = new StatementDocument
            {
                SchemaVersion = "1.0",
                JobId = jobId,
                CreatedUtc = utcNow(),
                Customer = new StatementCustomer
                {
                    CustomerNumber = customer.CustomerNumber,
                    FullName = customer.FullName,
                    Email = customer.Email,
                    Address = customer.Address
                },
                Account = new StatementAccount
                {
                    AccountNumber = account.AccountNumber,
                    CustomerNumber = account.CustomerNumber,
                    AccountType = account.AccountType,
                    Currency = account.Currency
                },
                Period = new StatementPeriod
                {
                    FromDate = fromDate,
                    ToDate = toDate
                },
                Balances = new StatementBalances
                {
                    PeriodOpeningBalance = account.CurrentBalance - periodActivity,
                    PeriodClosingBalance = account.CurrentBalance,
                    CurrentBalance = account.CurrentBalance,
                    AvailableBalance = account.AvailableBalance
                },
                Transactions = orderedTransactions
            };

            repository.Create(document);
            logger.Info("Created statement job " + jobId + ".");
            return new CreateStatementResponse
            {
                JobId = jobId,
                Status = "pending",
                StatusUrl = "/api/statements/" + jobId
            };
        }

        public StatementStatusResponse GetStatus(Guid jobId)
        {
            return repository.GetStatus(jobId);
        }

        private void ValidateRequest(CreateStatementRequest request)
        {
            if (request == null)
            {
                throw new StatementValidationException("A request body is required.");
            }
            if (string.IsNullOrWhiteSpace(request.CustomerNumber))
            {
                throw new StatementValidationException("customerNumber is required.");
            }
            if (string.IsNullOrWhiteSpace(request.AccountNumber))
            {
                throw new StatementValidationException("accountNumber is required.");
            }
            if (!request.FromDate.HasValue || !request.ToDate.HasValue)
            {
                throw new StatementValidationException("fromDate and toDate are required.");
            }

            var fromDate = request.FromDate.Value.Date;
            var toDate = request.ToDate.Value.Date;
            if (fromDate > toDate)
            {
                throw new StatementValidationException(
                    "fromDate must be before or equal to toDate.");
            }
            if ((toDate - fromDate).TotalDays + 1 > maximumPeriodDays)
            {
                throw new StatementValidationException(
                    "The requested statement period exceeds " + maximumPeriodDays + " days.");
            }
            if (toDate > utcNow().Date)
            {
                throw new StatementValidationException("toDate cannot be in the future.");
            }
        }
    }
}
