using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Linq;
using Contoso.LegacyBank.Statements.Api.Models;

namespace Contoso.LegacyBank.Statements.Api.Services
{
    [ServiceContract(Namespace = AccountServiceClient.ContractNamespace)]
    internal interface IAccountServiceSoap
    {
        [OperationContract(
            Action = AccountServiceClient.ContractNamespace + "IAccountService/GetCustomer",
            ReplyAction = "*")]
        Message GetCustomer(Message request);

        [OperationContract(
            Action = AccountServiceClient.ContractNamespace + "IAccountService/GetAccount",
            ReplyAction = "*")]
        Message GetAccount(Message request);

        [OperationContract(
            Action = AccountServiceClient.ContractNamespace + "IAccountService/GetTransactions",
            ReplyAction = "*")]
        Message GetTransactions(Message request);
    }

    // Generated-proxy style WCF client. Raw Message results keep the client tolerant of
    // data-contract CLR namespaces while preserving the BasicHttpBinding SOAP contract.
    public sealed class AccountServiceClient : IAccountGateway
    {
        internal const string ContractNamespace = "http://tempuri.org/";
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
                MaxReceivedMessageSize = 4 * 1024 * 1024,
                ReaderQuotas = XmlDictionaryReaderQuotas.Max
            };
            factory = new ChannelFactory<IAccountServiceSoap>(
                binding,
                new EndpointAddress(endpointUrl));
            this.logger = logger;
        }

        public AccountCustomer GetCustomer(string customerNumber)
        {
            var result = Invoke(
                "GetCustomer",
                channel => channel.GetCustomer(CreateRequest(
                    "GetCustomer",
                    new KeyValuePair<string, object>("customerNumber", customerNumber))));
            if (IsNilOrEmpty(result))
            {
                return null;
            }

            return new AccountCustomer
            {
                CustomerNumber = Read(result, "CustomerNumber", "customerNumber", "Number"),
                FullName = ReadFullName(result),
                Email = Read(result, "Email", "EmailAddress"),
                Address = Read(result, "Address", "MailingAddress")
            };
        }

        public AccountDetails GetAccount(string accountNumber)
        {
            var result = Invoke(
                "GetAccount",
                channel => channel.GetAccount(CreateRequest(
                    "GetAccount",
                    new KeyValuePair<string, object>("accountNumber", accountNumber))));
            if (IsNilOrEmpty(result))
            {
                return null;
            }

            return new AccountDetails
            {
                AccountNumber = Read(result, "AccountNumber", "accountNumber", "Number"),
                CustomerNumber = Read(result, "CustomerNumber", "customerNumber"),
                AccountType = Read(result, "AccountType", "Type"),
                Currency = Read(result, "Currency", "CurrencyCode") ?? "USD",
                CurrentBalance = ReadDecimal(result, "CurrentBalance", "Balance"),
                AvailableBalance = ReadDecimal(result, "AvailableBalance", "CurrentBalance", "Balance")
            };
        }

        public IList<StatementTransaction> GetTransactions(
            string accountNumber,
            DateTime fromDate,
            DateTime toDate)
        {
            var result = Invoke(
                "GetTransactions",
                channel => channel.GetTransactions(CreateRequest(
                    "GetTransactions",
                    new KeyValuePair<string, object>("accountNumber", accountNumber),
                    new KeyValuePair<string, object>("fromDate", fromDate),
                    new KeyValuePair<string, object>("toDate", toDate))));
            if (IsNilOrEmpty(result))
            {
                return new List<StatementTransaction>();
            }

            var elements = result
                .Descendants()
                .Where(element =>
                    element.Elements().Any(child =>
                        child.Name.LocalName.Equals("Amount", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return elements.Select(element => new StatementTransaction
            {
                TransactionId = Read(element, "TransactionId", "Id", "Reference"),
                PostedDate = ReadDate(element, "PostedDate", "TransactionDate", "Date"),
                Description = Read(element, "Description", "Narrative", "Memo"),
                Amount = ReadDecimal(element, "Amount"),
                Balance = ReadNullableDecimal(element, "Balance", "RunningBalance"),
                Type = Read(element, "Type", "TransactionType")
            }).ToList();
        }

        private XElement Invoke(string operation, Func<IAccountServiceSoap, Message> action)
        {
            IClientChannel channel = null;
            try
            {
                var proxy = factory.CreateChannel();
                channel = (IClientChannel)proxy;
                using (var response = action(proxy))
                {
                    var document = XDocument.Load(response.GetReaderAtBodyContents());
                    var fault = document.Descendants()
                        .FirstOrDefault(element => element.Name.LocalName == "Fault");
                    if (fault != null)
                    {
                        throw new FaultException(fault.Value);
                    }

                    return document.Descendants()
                        .FirstOrDefault(element =>
                            element.Name.LocalName.Equals(
                                operation + "Result",
                                StringComparison.OrdinalIgnoreCase))
                        ?? document.Root;
                }
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

        private static Message CreateRequest(
            string operation,
            params KeyValuePair<string, object>[] arguments)
        {
            var body = new XElement(XName.Get(operation, ContractNamespace));
            foreach (var argument in arguments)
            {
                body.Add(new XElement(
                    XName.Get(argument.Key, ContractNamespace),
                    FormatValue(argument.Value)));
            }

            return Message.CreateMessage(
                MessageVersion.Soap11,
                ContractNamespace + "IAccountService/" + operation,
                new XElementBodyWriter(body));
        }

        private static object FormatValue(object value)
        {
            var date = value as DateTime?;
            return date.HasValue
                ? date.Value.ToString("O", CultureInfo.InvariantCulture)
                : value;
        }

        private static bool IsNilOrEmpty(XElement element)
        {
            if (element == null)
            {
                return true;
            }
            var nil = element.Attributes()
                .FirstOrDefault(attribute => attribute.Name.LocalName == "nil");
            return (nil != null && nil.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                || (!element.HasElements && string.IsNullOrWhiteSpace(element.Value));
        }

        private static string ReadFullName(XElement element)
        {
            var fullName = Read(element, "FullName", "Name");
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }
            return string.Join(
                " ",
                new[] { Read(element, "FirstName"), Read(element, "LastName") }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        private static string Read(XElement element, params string[] names)
        {
            foreach (var name in names)
            {
                var match = element.DescendantsAndSelf()
                    .FirstOrDefault(candidate =>
                        candidate.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (match != null && !string.IsNullOrWhiteSpace(match.Value))
                {
                    return match.Value.Trim();
                }
            }
            return null;
        }

        private static decimal ReadDecimal(XElement element, params string[] names)
        {
            return ReadNullableDecimal(element, names) ?? 0m;
        }

        private static decimal? ReadNullableDecimal(XElement element, params string[] names)
        {
            decimal value;
            return decimal.TryParse(
                Read(element, names),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out value)
                ? value
                : (decimal?)null;
        }

        private static DateTime ReadDate(XElement element, params string[] names)
        {
            DateTime value;
            return DateTime.TryParse(
                Read(element, names),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out value)
                ? value
                : DateTime.MinValue;
        }

        private sealed class XElementBodyWriter : BodyWriter
        {
            private readonly XElement body;

            public XElementBodyWriter(XElement body) : base(true)
            {
                this.body = body;
            }

            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                body.WriteTo(writer);
            }
        }
    }
}
