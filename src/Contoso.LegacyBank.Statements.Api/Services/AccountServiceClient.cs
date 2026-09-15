using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Contoso.LegacyBank.Statements.Api.Models;

namespace Contoso.LegacyBank.Statements.Api.Services
{
    [ServiceContract(Name = "IAccountService", Namespace = AccountServiceClient.ContractNamespace)]
    internal interface IAccountServiceSoap
    {
        [OperationContract]
        [FaultContract(typeof(AccountFaultDto))]
        CustomerDto GetCustomer(string customerNumber);

        [OperationContract]
        [FaultContract(typeof(AccountFaultDto))]
        IList<AccountDto> GetAccounts(string customerNumber);

        [OperationContract]
        [FaultContract(typeof(AccountFaultDto))]
        IList<TransactionDto> GetTransactions(string accountNumber, DateTime fromDate, DateTime toDate);
    }

    public sealed class AccountServiceClient : IAccountGateway
    {
        internal const string ContractNamespace = "urn:contoso:legacy-bank:accounts:v1";
        private readonly ChannelFactory<IAccountServiceSoap> factory;
        private readonly IAppLogger logger;

        public AccountServiceClient(string endpointUrl, IAppLogger logger)
        {
            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                throw new ArgumentException("The account service URL is required.", "endpointUrl");
            }

            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                OpenTimeout = TimeSpan.FromSeconds(10),
                CloseTimeout = TimeSpan.FromSeconds(10),
                SendTimeout = TimeSpan.FromSeconds(30),
                ReceiveTimeout = TimeSpan.FromSeconds(30),
                MaxReceivedMessageSize = 4 * 1024 * 1024
            };
            factory = new ChannelFactory<IAccountServiceSoap>(binding, new EndpointAddress(endpointUrl));
            this.logger = logger;
        }

        public AccountCustomer GetCustomer(string customerNumber)
        {
            var customer = Invoke("GetCustomer", channel => channel.GetCustomer(customerNumber));
            if (customer == null)
            {
                return null;
            }

            return new AccountCustomer
            {
                CustomerNumber = customer.CustomerNumber,
                FullName = string.Join(
                    " ",
                    new[] { customer.FirstName, customer.LastName }
                        .Where(value => !string.IsNullOrWhiteSpace(value))),
                Email = customer.Email
            };
        }

        public AccountDetails GetAccount(string customerNumber, string accountNumber)
        {
            var accounts = Invoke("GetAccounts", channel => channel.GetAccounts(customerNumber))
                ?? new List<AccountDto>();
            var account = accounts.FirstOrDefault(value =>
                string.Equals(value.AccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase));
            if (account == null)
            {
                return null;
            }

            return new AccountDetails
            {
                AccountNumber = account.AccountNumber,
                CustomerNumber = account.CustomerNumber,
                AccountType = account.AccountType,
                Currency = account.CurrencyCode,
                CurrentBalance = account.Balance,
                AvailableBalance = account.Balance
            };
        }

        public IList<StatementTransaction> GetTransactions(
            string accountNumber,
            DateTime fromDate,
            DateTime toDate)
        {
            var transactions = Invoke(
                "GetTransactions",
                channel => channel.GetTransactions(accountNumber, fromDate, toDate))
                ?? new List<TransactionDto>();

            return transactions.Select(transaction => new StatementTransaction
            {
                TransactionId = transaction.ExternalId,
                PostedDate = transaction.PostedUtc,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Type = transaction.TransactionType
            }).ToList();
        }

        private T Invoke<T>(string operation, Func<IAccountServiceSoap, T> action)
        {
            IClientChannel channel = null;
            try
            {
                var proxy = factory.CreateChannel();
                channel = (IClientChannel)proxy;
                return action(proxy);
            }
            catch (Exception exception)
            {
                if (channel != null)
                {
                    channel.Abort();
                }
                logger.Error("WCF account operation " + operation + " failed.", exception);
                throw;
            }
            finally
            {
                if (channel != null && channel.State != CommunicationState.Faulted)
                {
                    try
                    {
                        channel.Close();
                    }
                    catch
                    {
                        channel.Abort();
                    }
                }
            }
        }
    }

    [DataContract(Name = "CustomerDto", Namespace = AccountServiceClient.ContractNamespace)]
    internal sealed class CustomerDto
    {
        [DataMember(Order = 1)] public string CustomerNumber { get; set; }
        [DataMember(Order = 2)] public string FirstName { get; set; }
        [DataMember(Order = 3)] public string LastName { get; set; }
        [DataMember(Order = 4)] public string Email { get; set; }
        [DataMember(Order = 5)] public DateTime CreatedUtc { get; set; }
    }

    [DataContract(Name = "AccountDto", Namespace = AccountServiceClient.ContractNamespace)]
    internal sealed class AccountDto
    {
        [DataMember(Order = 1)] public string AccountNumber { get; set; }
        [DataMember(Order = 2)] public string CustomerNumber { get; set; }
        [DataMember(Order = 3)] public string AccountType { get; set; }
        [DataMember(Order = 4)] public string CurrencyCode { get; set; }
        [DataMember(Order = 5)] public decimal Balance { get; set; }
        [DataMember(Order = 6)] public DateTime OpenedUtc { get; set; }
        [DataMember(Order = 7)] public bool IsActive { get; set; }
    }

    [DataContract(Name = "TransactionDto", Namespace = AccountServiceClient.ContractNamespace)]
    internal sealed class TransactionDto
    {
        [DataMember(Order = 1)] public string ExternalId { get; set; }
        [DataMember(Order = 2)] public string AccountNumber { get; set; }
        [DataMember(Order = 3)] public DateTime PostedUtc { get; set; }
        [DataMember(Order = 4)] public decimal Amount { get; set; }
        [DataMember(Order = 5)] public string Description { get; set; }
        [DataMember(Order = 6)] public string TransactionType { get; set; }
    }

    [DataContract(Name = "AccountFault", Namespace = AccountServiceClient.ContractNamespace)]
    internal sealed class AccountFaultDto
    {
        [DataMember(Order = 1)] public string Code { get; set; }
        [DataMember(Order = 2)] public string Message { get; set; }
        [DataMember(Order = 3)] public string Field { get; set; }
    }
}
