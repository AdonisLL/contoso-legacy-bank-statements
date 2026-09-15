using System;
using System.ComponentModel.DataAnnotations;

namespace Contoso.LegacyBank.Statements.Api.Models
{
    public sealed class CreateStatementRequest
    {
        [Required]
        public string CustomerNumber { get; set; }

        [Required]
        public string AccountNumber { get; set; }

        [Required]
        public DateTime? FromDate { get; set; }

        [Required]
        public DateTime? ToDate { get; set; }
    }

    public sealed class CreateStatementResponse
    {
        public Guid JobId { get; set; }
        public string Status { get; set; }
        public string StatusUrl { get; set; }
    }

    public sealed class StatementStatusResponse
    {
        public Guid JobId { get; set; }
        public string Status { get; set; }
        public string PdfPath { get; set; }
        public string Error { get; set; }
        public DateTime? UpdatedUtc { get; set; }
    }

    public sealed class ErrorResponse
    {
        public string Error { get; set; }
        public string Detail { get; set; }
    }
}
