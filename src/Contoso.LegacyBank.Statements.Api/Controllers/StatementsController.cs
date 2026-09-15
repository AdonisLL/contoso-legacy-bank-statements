using System;
using System.Net;
using System.Web.Http;
using Contoso.LegacyBank.Statements.Api.Infrastructure;
using Contoso.LegacyBank.Statements.Api.Models;
using Contoso.LegacyBank.Statements.Api.Services;

namespace Contoso.LegacyBank.Statements.Api.Controllers
{
    [RoutePrefix("api/statements")]
    public sealed class StatementsController : ApiController
    {
        private readonly IStatementService service;

        public StatementsController() : this(CompositionRoot.StatementService) { }

        public StatementsController(IStatementService service)
        {
            this.service = service;
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post(CreateStatementRequest request)
        {
            try
            {
                var response = service.Create(request);
                return Content(HttpStatusCode.Accepted, response);
            }
            catch (StatementValidationException exception)
            {
                return Content(
                    HttpStatusCode.BadRequest,
                    new ErrorResponse { Error = "validation_failed", Detail = exception.Message });
            }
            catch (StatementNotFoundException exception)
            {
                return Content(
                    HttpStatusCode.NotFound,
                    new ErrorResponse { Error = "not_found", Detail = exception.Message });
            }
            catch (AccountDependencyException exception)
            {
                return Content(
                    HttpStatusCode.BadGateway,
                    new ErrorResponse
                    {
                        Error = "accounts_service_error",
                        Detail = exception.Message
                    });
            }
            catch (Exception)
            {
                return Content(
                    HttpStatusCode.InternalServerError,
                    new ErrorResponse
                    {
                        Error = "statement_creation_failed",
                        Detail = "The statement request could not be persisted."
                    });
            }
        }

        [HttpGet]
        [Route("{id:guid}")]
        public IHttpActionResult Get(Guid id)
        {
            try
            {
                return Ok(service.GetStatus(id));
            }
            catch (StatementNotFoundException exception)
            {
                return Content(
                    HttpStatusCode.NotFound,
                    new ErrorResponse { Error = "not_found", Detail = exception.Message });
            }
            catch (Exception)
            {
                return Content(
                    HttpStatusCode.InternalServerError,
                    new ErrorResponse
                    {
                        Error = "status_read_failed",
                        Detail = "The statement status could not be read."
                    });
            }
        }
    }
}
