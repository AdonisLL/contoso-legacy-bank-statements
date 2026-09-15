using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Contoso.LegacyBank.Statements.Api.App_Start;
using Contoso.LegacyBank.Statements.Api.Controllers;
using Contoso.LegacyBank.Statements.Api.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Contoso.LegacyBank.Statements.Tests
{
    [TestClass]
    public sealed class StatementsApiIntegrationTests
    {
        [TestMethod]
        public async Task RoutesPostAndGetThroughWebApiPipeline()
        {
            var id = Guid.NewGuid();
            var service = new FakeStatementService
            {
                CreateResponse = new CreateStatementResponse
                {
                    JobId = id,
                    Status = "pending",
                    StatusUrl = "/api/statements/" + id
                },
                StatusResponse = new StatementStatusResponse
                {
                    JobId = id,
                    Status = "completed",
                    PdfPath = @"C:\statements\statement.pdf"
                }
            };

            using (var configuration = new HttpConfiguration())
            {
                WebApiConfig.Register(configuration);
                configuration.DependencyResolver = new ControllerResolver(service);
                using (var server = new HttpServer(configuration))
                using (var client = new HttpClient(server))
                {
                    client.BaseAddress = new Uri("http://localhost/");
                    var post = await client.PostAsJsonAsync(
                        "api/statements",
                        new CreateStatementRequest
                        {
                            CustomerNumber = "C100",
                            AccountNumber = "A100",
                            FromDate = new DateTime(2026, 9, 1),
                            ToDate = new DateTime(2026, 9, 15)
                        });
                    Assert.AreEqual(HttpStatusCode.Accepted, post.StatusCode);

                    var get = await client.GetAsync("api/statements/" + id);
                    Assert.AreEqual(HttpStatusCode.OK, get.StatusCode);
                    var body = JsonConvert.DeserializeObject<StatementStatusResponse>(
                        await get.Content.ReadAsStringAsync());
                    Assert.AreEqual("completed", body.Status);
                }
            }
        }

        private sealed class ControllerResolver : System.Web.Http.Dependencies.IDependencyResolver
        {
            private readonly FakeStatementService service;

            public ControllerResolver(FakeStatementService service)
            {
                this.service = service;
            }

            public object GetService(Type serviceType)
            {
                return serviceType == typeof(StatementsController)
                    ? new StatementsController(service)
                    : null;
            }

            public System.Collections.Generic.IEnumerable<object> GetServices(Type serviceType)
            {
                return new object[0];
            }

            public System.Web.Http.Dependencies.IDependencyScope BeginScope() { return this; }
            public void Dispose() { }
        }
    }
}
