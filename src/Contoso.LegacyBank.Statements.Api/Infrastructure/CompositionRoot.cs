using System;
using System.Configuration;
using Contoso.LegacyBank.Statements.Api.Services;

namespace Contoso.LegacyBank.Statements.Api.Infrastructure
{
    public static class CompositionRoot
    {
        private static readonly Lazy<IStatementService> Service =
            new Lazy<IStatementService>(BuildService, true);

        public static IStatementService StatementService
        {
            get { return Service.Value; }
        }

        private static IStatementService BuildService()
        {
            var root = ConfigurationManager.AppSettings["StatementDocumentsRoot"];
            if (string.IsNullOrWhiteSpace(root))
            {
                root = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ContosoLegacyBank",
                    "Documents");
            }

            var accountServiceUrl = ConfigurationManager.AppSettings["AccountServiceUrl"];
            int maximumPeriodDays;
            if (!int.TryParse(
                ConfigurationManager.AppSettings["StatementMaximumPeriodDays"],
                out maximumPeriodDays))
            {
                maximumPeriodDays = 366;
            }

            var logger = new AppLogger(root);
            return new StatementService(
                new AccountServiceClient(accountServiceUrl, logger),
                new FileStatementJobRepository(root, logger),
                logger,
                maximumPeriodDays,
                () => DateTime.UtcNow);
        }
    }
}
