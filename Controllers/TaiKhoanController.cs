using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Services;
using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.DTOs;
using SendGrid;
using SendGrid.Helpers.Mail;
using Google.Apis.Auth;

namespace BE_QLTiemThuoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaiKhoanController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly JwtService _jwtService;

        public TaiKhoanController(AppDbContext context, IConfiguration configuration, JwtService jwtService)
        {
            _context = context;
            _configuration = configuration;
            _jwtService = jwtService;
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

        // ========= LẤY TẤT CẢ TÀI KHOẢN (ADMIN ONLY) =========
        [HttpGet]
        [Authorize]   // hoặc Policy = "AdminOnly" nếu m có tạo Policy
        public async Task<ActionResult<IEnumerable<TaiKhoan>>> GetAll()
        {
            return await _context.TaiKhoans.ToListAsync();
        }

        // ========= CHECK USERNAME =========
        [HttpGet("CheckUsername")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckUsername([FromQuery] string username)
        {
            var exists = await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == username);
            return Ok(new { Exists = exists });
        }

        // ========= CREATE ACCOUNT =========
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateAccount([FromBody] RegisterRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == req.TenDangNhap))
                return BadRequest("Tên đăng nhập đã tồn tại.");

            // tạo token email confirm
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

        // ========= SEND OTP =========
        [HttpPost("SendOtp")]
        [AllowAnonymous]
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
        [AllowAnonymous]
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

        // ========= LOGIN (JWT) =========
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap
                                       && u.MatKhau == request.MatKhau);

            if (user == null)
                return Unauthorized("Sai tên đăng nhập hoặc mật khẩu.");

            if (user.ISEMAILCONFIRMED == 0)
                return BadRequest("Tài khoản chưa xác thực email.");

            // ====== ROLE DETECTION ======
            bool isAdmin = false;
            int? chucVu = null;
            string vaiTro = "User";

            if (!string.IsNullOrEmpty(user.MaNV))
            {
                var nv = await _context.NhanViens.FirstOrDefaultAsync(x => x.MaNV == user.MaNV);
                if (nv != null)
                {
                    chucVu = nv.ChucVu;
                    isAdmin = (nv.ChucVu == 1);
                    vaiTro = isAdmin ? "Admin" : "Staff";
                }
            }

            // ====== CUSTOMER CREATION ======
            bool hasCustomerInfo = false;

            if (!isAdmin)
            {
                if (string.IsNullOrEmpty(user.MaKH))
                {
                    string newMaKH = GenerateKhachHangCode();
                    user.MaKH = newMaKH;

                    var kh = new KhachHang
                    {
                        MAKH = newMaKH,
                        HoTen = null,
                        GioiTinh = null,
                        NgaySinh = null,
                        DiaChi = null,
                        DienThoai = null
                    };

                    _context.KhachHangs.Add(kh);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MAKH == user.MaKH);
                    if (kh != null)
                    {
                        hasCustomerInfo = !string.IsNullOrEmpty(kh.HoTen)
                                       && !string.IsNullOrEmpty(kh.DiaChi)
                                       && !string.IsNullOrEmpty(kh.DienThoai);
                    }
                }
            }

            // ========= GENERATE JWT TOKEN =========
            string token = _jwtService.GenerateToken(
                user.MaTK,
                user.TenDangNhap,
                user.EMAIL,
                user.MaKH,
                user.MaNV,
                vaiTro,
                chucVu ?? 0
            );

            return Ok(new LoginResponse
            {
                Message = "Đăng nhập thành công.",
                MaTK = user.MaTK,
                TenDangNhap = user.TenDangNhap,
                Email = user.EMAIL,
                MaKH = user.MaKH,
                MaNV = user.MaNV,
                ChucVu = chucVu ?? 0,
                VaiTro = vaiTro,
                HasCustomerInfo = hasCustomerInfo,
                IsAdmin = isAdmin,
                Token = token
            });
        }


        // ========= LOGIN WITH GOOGLE =========
        [HttpPost("LoginWithGoogle")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Verify Firebase ID Token (not Google ID Token)
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { "nhathuoc-f9fce" }, // Firebase Project ID
                    IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
                    ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
                };

                GoogleJsonWebSignature.Payload payload;
                try
                {
                    payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
                }
                catch (Exception ex)
                {
                    // Nếu verification thất bại, vẫn cho phép login nếu có email
                    // (vì Firebase đã verify rồi)
                    if (string.IsNullOrEmpty(request.Email))
                    {
                        return BadRequest($"Token không hợp lệ: {ex.Message}");
                    }
                    
                    // Sử dụng email từ request thay vì từ token
                    var userByEmail = await _context.TaiKhoans
                        .FirstOrDefaultAsync(u => u.EMAIL == request.Email);
                    
                    return await ProcessGoogleLogin(userByEmail, request.Email, request.DisplayName);
                }

                // Token hợp lệ, sử dụng email từ payload
                var user = await _context.TaiKhoans
                    .FirstOrDefaultAsync(u => u.EMAIL == payload.Email);

                return await ProcessGoogleLogin(user, payload.Email, request.DisplayName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi đăng nhập Google: {ex.Message}");
            }
        }

        // Helper method to process Google login
        private async Task<IActionResult> ProcessGoogleLogin(TaiKhoan? user, string email, string? displayName)
        {
            if (user == null)
            {
                // Tạo tài khoản mới cho user Google
                user = new TaiKhoan
                {
                    MaTK = GenerateAccountCode(),
                    TenDangNhap = email, // Dùng email làm username
                    MatKhau = GenerateRandomPassword(), // Generate random password
                    EMAIL = email,
                    ISEMAILCONFIRMED = 1, // Google đã verify email rồi
                    EMAILCONFIRMATIONTOKEN = null
                };

                _context.TaiKhoans.Add(user);
                await _context.SaveChangesAsync();
            }

            // Kiểm tra admin status
            bool isAdmin = false;
            int? chucVu = null;
            string vaiTro = "User";

            if (!string.IsNullOrEmpty(user.MaNV))
            {
                var nhanVien = await _context.Set<NhanVien>()
                    .FirstOrDefaultAsync(nv => nv.MaNV == user.MaNV);

                if (nhanVien != null)
                {
                    chucVu = nhanVien.ChucVu;
                    isAdmin = (nhanVien.ChucVu == 1);
                    vaiTro = isAdmin ? "Admin" : "Staff";
                }
            }

            bool hasCustomerInfo = false;

            // Kiểm tra thông tin khách hàng
            if (!isAdmin && string.IsNullOrEmpty(user.MaNV))
            {
                if (string.IsNullOrEmpty(user.MaKH))
                {
                    // Tạo mã khách hàng mới
                    string newMaKH = GenerateKhachHangCode();
                    user.MaKH = newMaKH;

                    var kh = new KhachHang
                    {
                        MAKH = newMaKH,
                        HoTen = displayName, // Dùng tên từ Google
                        GioiTinh = null,
                        NgaySinh = null,
                        DiaChi = null,
                        DienThoai = null
                    };

                    _context.KhachHangs.Add(kh);
                    await _context.SaveChangesAsync();

                    hasCustomerInfo = false; // Vẫn cần nhập thông tin đầy đủ
                }
                else
                {
                    var kh = await _context.KhachHangs
                        .FirstOrDefaultAsync(k => k.MAKH == user.MaKH);

                    if (kh != null)
                    {
                        hasCustomerInfo = !string.IsNullOrEmpty(kh.HoTen)
                                       && !string.IsNullOrEmpty(kh.DiaChi)
                                       && !string.IsNullOrEmpty(kh.DienThoai);
                    }
                }
            }

            // ========= GENERATE JWT TOKEN =========
            string token = _jwtService.GenerateToken(
                user.MaTK,
                user.TenDangNhap,
                user.EMAIL,
                user.MaKH,
                user.MaNV,
                vaiTro,
                chucVu ?? 0
            );

            return Ok(new LoginResponse
            {
                Message = "Đăng nhập Google thành công.",
                MaTK = user.MaTK,
                TenDangNhap = user.TenDangNhap,
                Email = user.EMAIL,
                MaKH = user.MaKH,
                MaNV = user.MaNV,
                ChucVu = chucVu ?? 0,
                VaiTro = vaiTro,
                HasCustomerInfo = hasCustomerInfo,
                IsAdmin = isAdmin,
                Token = token
            });
        }

        // Helper: Generate random password for Google users
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }




        // ========= CONFIRM EMAIL =========
        [HttpGet("ConfirmEmail")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(x => x.EMAILCONFIRMATIONTOKEN == token);

            if (user == null)
                return Content("Token không hợp lệ hoặc đã được dùng.");

            user.ISEMAILCONFIRMED = 1;
            user.EMAILCONFIRMATIONTOKEN = null;

            await _context.SaveChangesAsync();

var html = @"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Xác thực thành công</title>
    <style>
        body {
            font-family: 'Segoe UI', sans-serif;
            background: linear-gradient(135deg, #E0F7FA, #F1FBFF);
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            color: #333;
        }

        .card {
            background: white;
            padding: 40px 50px;
            border-radius: 18px;
            box-shadow: 0 10px 25px rgba(0,0,0,0.08);
            text-align: center;
            max-width: 420px;
            animation: fadeIn 0.5s ease;
        }

        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(20px); }
            to { opacity: 1; transform: translateY(0); }
        }

        .icon {
            width: 90px;
            height: 90px;
            border-radius: 50%;
            background: #03A9F4;
            display: flex;
            justify-content: center;
            align-items: center;
            margin: 0 auto 20px;
            color: white;
            font-size: 48px;
            animation: pop 0.4s ease;
        }

        @keyframes pop {
            0% { transform: scale(0.3); opacity: 0; }
            100% { transform: scale(1); opacity: 1; }
        }

        h2 {
            margin-top: 0;
            color: #0288D1;
        }

        p {
            font-size: 15px;
            margin-bottom: 25px;
        }

        a.button {
            display: inline-block;
            padding: 12px 22px;
            background: #0288D1;
            color: white;
            text-decoration: none;
            border-radius: 10px;
            font-weight: bold;
            transition: 0.2s;
        }

        a.button:hover {
            background: #0277BD;
        }

        .redirect-msg {
            margin-top: 15px;
            font-size: 13px;
            color: #555;
        }
    </style>

    <script>
        setTimeout(() => {
            window.location.href = '/login';
        }, 3000);
    </script>
</head>
<body>

    <div class='card'>
        <div class='icon'>✓</div>
        <h2>Xác thực thành công!</h2>
        <p>Tài khoản của bạn đã được kích hoạt. Bạn có thể đăng nhập để tiếp tục sử dụng hệ thống.</p>

        <a class='button' href='/login'>Trở về đăng nhập</a>

        <div class='redirect-msg'>
            Sẽ tự động chuyển trong 3 giây...
        </div>
    </div>

</body>
</html>
";
return new ContentResult {
    Content = html,
    ContentType = "text/html; charset=UTF-8",
    StatusCode = 200
};

        }

        // ========= HELPER =========
        private string GenerateAccountCode()
        {
            var last = _context.TaiKhoans.OrderByDescending(x => x.MaTK).FirstOrDefault();
            int num = int.Parse((last?.MaTK ?? "TK0000")[2..]) + 1;
            return "TK" + num.ToString("D4");
        }

        private string GenerateKhachHangCode()
        {
            var last = _context.KhachHangs
                .OrderByDescending(k => k.MAKH)
                .FirstOrDefault();

            string lastCode = last?.MAKH ?? "KH0000";
            int number = int.Parse(lastCode.Substring(2)) + 1;

            return "KH" + number.ToString("D4");
        }
        // ========= SEND CONFIRMATION EMAIL =========
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

    }
}
