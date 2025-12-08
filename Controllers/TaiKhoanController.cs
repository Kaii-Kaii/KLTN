using Microsoft.AspNetCore.Mvc;
using BE_QLTiemThuoc.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.DTOs;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BE_QLTiemThuoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaiKhoanController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public TaiKhoanController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Không còn dùng SMTP nữa, giữ lại để khỏi lỗi nhưng không dùng
        private (string Host, int Port, string Username, string Password, string FromEmail) GetSmtpConfig()
        {
            var username = Environment.GetEnvironmentVariable("EmailSettings__SmtpUsername")
                           ?? _configuration["EmailSettings:SmtpUsername"];

            var password = Environment.GetEnvironmentVariable("EmailSettings__SmtpPassword")
                           ?? _configuration["EmailSettings:SmtpPassword"];

            var fromEmail = Environment.GetEnvironmentVariable("EmailSettings__From")
                            ?? _configuration["EmailSettings:From"]
                            ?? username;

            return ("", 0, username!, password!, fromEmail!);
        }

        // ===== API =====
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var existingUser = await _context.TaiKhoans
                    .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap);
                if (existingUser != null)
                    return BadRequest("Tên đăng nhập đã tồn tại.");

                // Sinh token
                var tokenBytes = new byte[32];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                    rng.GetBytes(tokenBytes);

                string emailToken = Convert.ToBase64String(tokenBytes);

                var newAccount = new TaiKhoan
                {
                    MaTK = GenerateAccountCode(),
                    TenDangNhap = request.TenDangNhap,
                    MatKhau = request.MatKhau,
                    EMAIL = request.Email,
                    ISEMAILCONFIRMED = 0,
                    EMAILCONFIRMATIONTOKEN = emailToken,
                };

                _context.TaiKhoans.Add(newAccount);
                await _context.SaveChangesAsync();

                var baseUrl = Environment.GetEnvironmentVariable("App__BaseUrl")
                              ?? _configuration["App:BaseUrl"]
                              ?? $"{Request.Scheme}://{Request.Host.Value}";

                string confirmationLink =
                    $"{baseUrl.TrimEnd('/')}/api/TaiKhoan/ConfirmEmail?token={Uri.EscapeDataString(emailToken)}";

                await SendConfirmationEmail(newAccount.EMAIL!, confirmationLink);

                return Ok(new RegisterResponse
                {
                    Message = "Tạo tài khoản thành công. Vui lòng kiểm tra email để xác thực."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        // ❤️❤️ SỬ DỤNG SENDGRID TẠI ĐÂY ❤️❤️
        private async Task SendConfirmationEmail(string toEmail, string confirmationLink)
        {
            var smtp = GetSmtpConfig();
            var apiKey = smtp.Password; // API KEY SendGrid lưu ở đây

            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(smtp.FromEmail, "Medion");
            var to = new EmailAddress(toEmail);

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color:#03A9F4;'>Xác thực tài khoản Medion</h2>
                    <p>Chào bạn,</p>
                    <p>Vui lòng xác thực tài khoản bằng cách click vào link bên dưới:</p>
                    <p><a href='{confirmationLink}' target='_blank'>{confirmationLink}</a></p>
                </div>";

            var msg = MailHelper.CreateSingleEmail(from, to, "Xác thực tài khoản", "", body);

            await client.SendEmailAsync(msg);
        }

        // ========== OTP ==========
        [HttpPost("SendOtp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap && u.EMAIL == request.Email);

            if (user == null)
                return NotFound("Không tìm thấy tài khoản.");

            var otp = new Random().Next(100000, 999999);

            user.OTP = otp;
            await _context.SaveChangesAsync();

            // Gửi OTP qua SendGrid
            var smtp = GetSmtpConfig();
            var apiKey = smtp.Password;

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(smtp.FromEmail, "Medion");
            var to = new EmailAddress(user.EMAIL!);

            string body = $@"
                <div style='font-family: Arial;'>
                    <h2 style='color:#17a2b8;'>Đặt lại mật khẩu</h2>
                    <p>Mã OTP của bạn:</p>
                    <h1 style='color:#17a2b8;'>{otp}</h1>
                </div>";

            var msg = MailHelper.CreateSingleEmail(from, to, "Mã OTP đặt lại mật khẩu", "", body);
            await client.SendEmailAsync(msg);

            return Ok(new SendOtpResponse { Message = "OTP đã được gửi." });
        }

        // ========= RESET PASSWORD ==========
        // Không đổi gì trong logic này
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap && u.EMAIL == request.Email);

            if (user == null)
                return NotFound("Không tìm thấy tài khoản.");

            if (user.OTP != request.Otp)
                return BadRequest("OTP không đúng.");

            user.MatKhau = request.MatKhauMoi;
            user.OTP = null;
            await _context.SaveChangesAsync();

            return Ok(new ResetPasswordResponse { Message = "Đổi mật khẩu thành công." });
        }

        // ========= CONFIRM EMAIL ==========
        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
                return Content("Token không hợp lệ", "text/html");

            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(u => u.EMAILCONFIRMATIONTOKEN == token);

            if (user == null)
                return Content("Token sai hoặc đã xác thực", "text/html");

            user.ISEMAILCONFIRMED = 1;
            user.EMAILCONFIRMATIONTOKEN = null;
            await _context.SaveChangesAsync();

            return Content("<h2>Xác thực thành công!</h2>", "text/html");
        }

        // ===== HELPER =====
        private string GenerateAccountCode()
        {
            var last = _context.TaiKhoans.OrderByDescending(x => x.MaTK).FirstOrDefault();
            int num = int.Parse((last?.MaTK ?? "TK0000").Substring(2)) + 1;
            return "TK" + num.ToString("D4");
        }

        private string GenerateKhachHangCode()
        {
            var last = _context.KhachHangs.OrderByDescending(x => x.MAKH).FirstOrDefault();
            int num = int.Parse((last?.MAKH ?? "KH0000").Substring(2)) + 1;
            return "KH" + num.ToString("D4");
        }
    }
}
