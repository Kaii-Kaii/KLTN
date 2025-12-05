using Microsoft.AspNetCore.Mvc;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Services;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace BE_QLTiemThuoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SimplePaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public SimplePaymentController(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        // POST: api/SimplePayment/Create
        [HttpPost("Create")]
        public async Task<IActionResult> CreatePayment([FromBody] SimplePaymentRequest request)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync<SimplePaymentResponse>(async () =>
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                if (request.Amount < 2000 || request.Amount > 50000000)
                    throw new ArgumentException("Số tiền phải từ 2,000 đến 50,000,000 VND");

                if (string.IsNullOrWhiteSpace(request.ReturnUrl))
                    throw new ArgumentException("returnUrl is required in the request body.");

                if (string.IsNullOrWhiteSpace(request.CancelUrl))
                    throw new ArgumentException("cancelUrl is required in the request body.");

                string clientId = Environment.GetEnvironmentVariable("PayOS__ClientId") ?? _configuration["PayOS:ClientId"] ?? ""; 
                string apiKey = Environment.GetEnvironmentVariable("PayOS__ApiKey") ?? _configuration["PayOS:ApiKey"] ?? ""; 
                string checksumKey = Environment.GetEnvironmentVariable("PayOS__ChecksumKey") ?? _configuration["PayOS:ChecksumKey"] ?? "";

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(checksumKey))
                    throw new Exception("Thiếu cấu hình PayOS");

                // Tạo orderCode (dạng timestamp)
                long orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                DateTime expireAt = DateTime.UtcNow.AddMinutes(3); // hết hiệu lực sau 3 phút

                // TODO: lưu orderCode + expireAt vào DB để kiểm tra sau này
                // SaveOrderToDatabase(orderCode, request.Amount, expireAt, ...);

                var paymentBody = new
                {
                    orderCode = orderCode,
                    amount = (int)request.Amount,
                    description = request.Description ?? "Thanh toan don hang",
                    returnUrl = request.ReturnUrl,
                    cancelUrl = request.CancelUrl
                };

                string signatureData = $"amount={paymentBody.amount}&cancelUrl={paymentBody.cancelUrl}&description={paymentBody.description}&orderCode={paymentBody.orderCode}&returnUrl={paymentBody.returnUrl}";
                string signature = ComputeHmacSha256(signatureData, checksumKey);

                var requestToPayOs = new
                {
                    paymentBody.orderCode,
                    paymentBody.amount,
                    paymentBody.description,
                    paymentBody.returnUrl,
                    paymentBody.cancelUrl,
                    signature
                };

                var jsonRequest = JsonSerializer.Serialize(requestToPayOs);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("https://api-merchant.payos.vn/v2/payment-requests", content);

                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (!httpResponse.IsSuccessStatusCode || json.GetProperty("code").GetString() != "00")
                    throw new Exception("PayOS lỗi: " + responseContent);

                string checkoutUrl = json.GetProperty("data").GetProperty("checkoutUrl").GetString();

                return new SimplePaymentResponse
                {
                    Success = true,
                    OrderCode = orderCode.ToString(),
                    PaymentUrl = checkoutUrl,
                    Amount = request.Amount,
                    Message = "Tạo giao dịch thành công. Giao dịch chỉ có hiệu lực 3 phút."
                };
            });

            return Ok(response);
        }


        // GET: api/SimplePayment/Status/{orderCode}
        [HttpGet("Status/{orderCode}")]
        public async Task<IActionResult> CheckPaymentStatus(string orderCode)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync<PaymentStatusResponse>(async () =>
            {
                if (string.IsNullOrEmpty(orderCode))
                    throw new ArgumentException("OrderCode is required");

                string clientId = Environment.GetEnvironmentVariable("PayOS__ClientId") ?? _configuration["PayOS:ClientId"] ?? "";
                string apiKey = Environment.GetEnvironmentVariable("PayOS__ApiKey") ?? _configuration["PayOS:ApiKey"] ?? "";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var httpResponse = await _httpClient.GetAsync($"https://api-merchant.payos.vn/v2/payment-requests/{orderCode}");
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                var json = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (json.GetProperty("code").GetString() != "00")
                    throw new Exception("PayOS Error: " + json.GetProperty("desc").GetString());

                string status = json.GetProperty("data").GetProperty("status").GetString() ?? "UNKNOWN";
                int amount = json.GetProperty("data").GetProperty("amount").GetInt32();

                return new PaymentStatusResponse
                {
                    OrderCode = orderCode,
                    Status = status,
                    IsPaid = status == "PAID",
                    Amount = amount,
                    Message = status switch
                    {
                        "PAID" => "Thanh toán thành công",
                        "PENDING" => "Đang chờ thanh toán",
                        "CANCELLED" => "Đã hủy",
                        _ => "Trạng thái không xác định"
                    }
                };
            });

            return Ok(response);
        }

        private static string ComputeHmacSha256(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}