using Microsoft.AspNetCore.Mvc;
using BE_QLTiemThuoc.Data;
using Microsoft.EntityFrameworkCore;
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

        // ========= LOAD CONFIG SENDGRID =========
        private (string FromEmail, string ApiKey) GetEmailConfig()
        {
            var from = Environment.GetEnvironmentVariable("EmailSettings__From")
                        ?? _configuration["EmailSettings:From"];

            var apiKey = Environment.GetEnvironmentVariable("EmailSettings__SmtpPassword")
                         ?? _configuration["EmailSettings:SmtpPassword"];

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Thiếu SendGrid API key");

            return (from!, apiKey!);
        }

        // ========= API LẤY TẤT CẢ =========
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaiKhoan>>> GetAll()
        {
            return await _context.TaiKhoans.ToListAsync();
        }

        // ========= CHECK USERNAME =========
        [HttpGet("CheckUsername")]
        public async Task<IActionResult> CheckUsername([FromQuery] string username)
        {
            var exists = await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == username);
            return Ok(new { Exists = exists });
        }

        // ========= CREATE ACCOUNT =========
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] RegisterRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == req.TenDangNhap))
                return BadRequest("Tên đăng nhập đã tồn tại.");

            // tạo token
            var bytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                rng.GetBytes(bytes);

            string token = Convert.ToBase64String(bytes);

            var tk = new TaiKhoan
            {
                MaTK = GenerateAccountCode(),
                TenDangNhap = req.TenDangNhap,
                MatKhau = req.MatKhau,
                EMAIL = req.Email,
                ISEMAILCONFIRMED = 0,
                EMAILCONFIRMATIONTOKEN = token
            };

            _context.TaiKhoans.Add(tk);
            await _context.SaveChangesAsync();

            // Auto detect domain (local hoặc Render)
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string link = $"{baseUrl}/api/TaiKhoan/ConfirmEmail?token={Uri.EscapeDataString(token)}";

            await SendConfirmationEmail(req.Email!, link);

            return Ok(new { Message = "Tạo tài khoản thành công." });
        }

        // ========= GỬI EMAIL XÁC MINH =========
        private async Task SendConfirmationEmail(string toEmail, string link)
        {
            var cfg = GetEmailConfig();
            var client = new SendGridClient(cfg.ApiKey);

            var from = new EmailAddress(cfg.FromEmail, "Medion");
            var to = new EmailAddress(toEmail);

            string html = $@"
                <div style='font-family:Arial'>
                    <h2 style='color:#03A9F4'>Xác thực tài khoản Medion</h2>
                    <p>Nhấn để xác thực:</p>
                    <a href='{link}' target='_blank'>{link}</a>
                </div>";

            var msg = MailHelper.CreateSingleEmail(from, to, "Xác thực tài khoản", "", html);

            await client.SendEmailAsync(msg);
        }

        // ========= SEND OTP =========
        [HttpPost("SendOtp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
        {
            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(x => x.TenDangNhap == req.TenDangNhap && x.EMAIL == req.Email);

            if (user == null)
                return NotFound("Không tìm thấy tài khoản.");

            int otp = new Random().Next(100000, 999999);
            user.OTP = otp;

            await _context.SaveChangesAsync();

            var cfg = GetEmailConfig();
            var client = new SendGridClient(cfg.ApiKey);

            var from = new EmailAddress(cfg.FromEmail, "Medion");
            var to = new EmailAddress(req.Email);

            string html = $@"
                <div style='font-family:Arial'>
                    <h2 style='color:#17a2b8'>OTP đặt lại mật khẩu</h2>
                    <h1>{otp}</h1>
                </div>";

            var msg = MailHelper.CreateSingleEmail(from, to, "Mã OTP đặt lại mật khẩu", "", html);
            await client.SendEmailAsync(msg);

            return Ok(new { Message = "OTP đã được gửi." });
        }

        // ========= RESET PASSWORD =========
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(x => x.TenDangNhap == req.TenDangNhap && x.EMAIL == req.Email);

            if (user == null)
                return NotFound("Không tìm thấy tài khoản.");

            if (user.OTP != req.Otp)
                return BadRequest("OTP không đúng.");

            user.MatKhau = req.MatKhauMoi;
            user.OTP = null;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đổi mật khẩu thành công." });
        }

        // ========= LOGIN =========
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(x => x.TenDangNhap == req.TenDangNhap && x.MatKhau == req.MatKhau);

            if (user == null)
                return Unauthorized("Sai tài khoản hoặc mật khẩu.");

            if (user.ISEMAILCONFIRMED == 0)
                return BadRequest("Tài khoản chưa xác thực email.");

            return Ok(new
            {
                Message = "Đăng nhập thành công.",
                MaTK = user.MaTK,
                TenDangNhap = user.TenDangNhap,
                Email = user.EMAIL,
                IsAdmin = !string.IsNullOrEmpty(user.MaNV)
            });
        }

        // ========= CONFIRM EMAIL =========
        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(x => x.EMAILCONFIRMATIONTOKEN == token);

            if (user == null)
                return Content("Token không hợp lệ hoặc đã được dùng.");

            user.ISEMAILCONFIRMED = 1;
            user.EMAILCONFIRMATIONTOKEN = null;

            await _context.SaveChangesAsync();

            return Content("<h2 style='color:#03A9F4'>Xác thực thành công!</h2>", "text/html");
        }

        // ========= HELPER =========
        private string GenerateAccountCode()
        {
            var last = _context.TaiKhoans.OrderByDescending(x => x.MaTK).FirstOrDefault();
            int num = int.Parse((last?.MaTK ?? "TK0000")[2..]) + 1;
            return "TK" + num.ToString("D4");
        }
    }
}
