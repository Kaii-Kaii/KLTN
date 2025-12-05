namespace BE_QLTiemThuoc.Model
{
    public class SimplePaymentRequest
    {
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
    }

    public class SimplePaymentResponse
    {
        public bool Success { get; set; }
        public string? PaymentUrl { get; set; }
        public string? OrderCode { get; set; }
        public string? InvoiceId { get; set; }
        public string? Message { get; set; }
        public decimal Amount { get; set; }
    }

    public class PaymentStatusResponse
    {
        public string OrderCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public int Amount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? OrderCode { get; set; }
        public string? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public DateTime PaymentTime { get; set; }
        public string? Message { get; set; }
    }
}