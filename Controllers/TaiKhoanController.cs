using Microsoft.AspNetCore.Mvc;
using BE_QLTiemThuoc.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.DTOs;

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

        // ====== HÀM DÙNG CHUNG ĐỌC SMTP CONFIG ======
        private (string Host, int Port, string Username, string Password, string FromEmail) GetSmtpConfig()
        {
            var host = Environment.GetEnvironmentVariable("EmailSettings__SmtpHost")
                       ?? _configuration["EmailSettings:SmtpHost"]
                       ?? "smtp.gmail.com";

            var portStr = Environment.GetEnvironmentVariable("EmailSettings__SmtpPort")
                          ?? _configuration["EmailSettings:SmtpPort"]
                          ?? "587";

            var username = Environment.GetEnvironmentVariable("EmailSettings__SmtpUsername")
                           ?? _configuration["EmailSettings:SmtpUsername"];

            var password = Environment.GetEnvironmentVariable("EmailSettings__SmtpPassword")
                           ?? _configuration["EmailSettings:SmtpPassword"];

            // From có thể cấu hình, nếu không thì mặc định = username (Gmail yêu cầu vậy)
            var fromEmail = Environment.GetEnvironmentVariable("EmailSettings__From")
                            ?? _configuration["EmailSettings:From"]
                            ?? username;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("SMTP credentials are not configured.");
            }

            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                fromEmail = username!;
            }

            int port = int.TryParse(portStr, out var p) ? p : 587;

            return (host, port, username!, password!, fromEmail!);
        }

        // ====== API ======

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaiKhoan>>> GetAll()
        {
            return await _context.TaiKhoans.ToListAsync();
        }

        [HttpGet("CheckUsername")]
        public async Task<IActionResult> CheckUsername([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Thiếu tên đăng nhập.");

            var exists = await _context.TaiKhoans.AnyAsync(u => u.TenDangNhap == username);
            return Ok(new CheckUsernameResponse { Exists = exists });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingUser = await _context.TaiKhoans
                    .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap);
                if (existingUser != null)
                {
                    return BadRequest("Tên đăng nhập đã tồn tại.");
                }

                // Sinh token xác thực email
                var tokenBytes = new byte[32];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(tokenBytes);
                }
                string emailToken = Convert.ToBase64String(tokenBytes);

                var newAccount = new TaiKhoan
                {
                    MaTK = GenerateAccountCode(),
                    TenDangNhap = request.TenDangNhap,
                    MatKhau = request.MatKhau,
                    EMAIL = request.Email,
                    ISEMAILCONFIRMED = 0,
                    EMAILCONFIRMATIONTOKEN = emailToken,
                    OTP = null,
                    KhachHang = null
                };

                _context.TaiKhoans.Add(newAccount);
                await _context.SaveChangesAsync();

                // Base URL: lấy từ env trước, rồi mới fallback
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
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi phía server.",
                    error = ex.Message
                });
            }
        }

        private async Task SendConfirmationEmail(string toEmail, string confirmationLink)
        {
            var smtpConfig = GetSmtpConfig();

            using var smtp = new SmtpClient(smtpConfig.Host)
            {
                Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password),
                EnableSsl = true,
                Port = smtpConfig.Port,
                Timeout = 10000
            };

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color:#03A9F4;'>Xác thực tài khoản Medion</h2>
                    <p>Chào bạn,</p>
                    <p>Vui lòng xác thực tài khoản bằng cách click vào link bên dưới:</p>
                    <p><a href='{confirmationLink}' style='color:#03A9F4;' target='_blank'>{confirmationLink}</a></p>
                    <p>Nếu bạn không đăng ký tài khoản, vui lòng bỏ qua email này.</p>
                </div>";

            using var mail = new MailMessage(smtpConfig.FromEmail, toEmail)
            {
                Subject = "Xác thực tài khoản",
                Body = body,
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(mail);
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
                return Content("<h2 style='color:#03A9F4;text-align:center;margin-top:40px'>Token không hợp lệ.</h2>", "text/html; charset=utf-8");

            var user = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.EMAILCONFIRMATIONTOKEN == token);
            if (user == null)
                return Content("<h2 style='color:#03A9F4;text-align:center;margin-top:40px'>Token không hợp lệ hoặc đã xác thực.</h2>", "text/html; charset=utf-8");

            user.ISEMAILCONFIRMED = 1;
            user.EMAILCONFIRMATIONTOKEN = null;
            await _context.SaveChangesAsync();

            string html = @"
<body style='background:#f5f6fa;'>
  <div style='max-width:400px;margin:60px auto;padding:32px 24px;background:#fff;border-radius:12px;box-shadow:0 2px 12px #0001;text-align:center'>
    <svg width='60' height='60' viewBox='0 0 24 24' fill='none' style='margin-bottom:16px'>
      <circle cx='12' cy='12' r='12' fill='#03A9F4'/>
      <path d='M7 13l3 3 7-7' stroke='#fff' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/>
    </svg>
    <h2 style='color:#03A9F4'>Đã xác thực email thành công!</h2>
  </div>
</body>
";
            return Content(html, "text/html; charset=utf-8");
        }

        private string GenerateAccountCode()
        {
            var lastAccount = _context.TaiKhoans
                .OrderByDescending(t => t.MaTK)
                .FirstOrDefault();

            string lastCode = lastAccount?.MaTK ?? "TK0000";
            int number = int.Parse(lastCode.Substring(2)) + 1;
            return "TK" + number.ToString("D4");
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap && u.MatKhau == request.MatKhau);

            if (user == null)
                return Unauthorized("Sai tên đăng nhập hoặc mật khẩu.");

            if (user.ISEMAILCONFIRMED == 0)
                return BadRequest("Tài khoản chưa xác thực email.");

            bool isAdmin = !string.IsNullOrEmpty(user.MaNV);
            bool hasCustomerInfo = false;
            string vaiTro = "User";

            if (isAdmin)
            {
                vaiTro = "Admin";
                hasCustomerInfo = true;
            }
            else
            {
                if (string.IsNullOrEmpty(user.MaKH))
                {
                    string newMaKH = GenerateKhachHangCode();

                    user.MaKH = newMaKH;
                    _context.TaiKhoans.Update(user);

                    var newKhachHang = new KhachHang
                    {
                        MAKH = newMaKH,
                        HoTen = null,
                        GioiTinh = null,
                        NgaySinh = null,
                        DiaChi = null,
                        DienThoai = null
                    };

                    _context.KhachHangs.Add(newKhachHang);

                    await _context.SaveChangesAsync();

                    hasCustomerInfo = false;
                }
                else
                {
                    var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MAKH == user.MaKH);
                    if (khachHang != null)
                    {
                        hasCustomerInfo = !string.IsNullOrEmpty(khachHang.HoTen)
                                       && !string.IsNullOrEmpty(khachHang.DienThoai)
                                       && !string.IsNullOrEmpty(khachHang.DiaChi);
                    }
                    else
                    {
                        hasCustomerInfo = false;
                    }
                }
            }

            return Ok(new LoginResponse
            {
                Message = "Đăng nhập thành công",
                MaTK = user.MaTK,
                TenDangNhap = user.TenDangNhap,
                Email = user.EMAIL,
                MaKH = user.MaKH,
                MaNV = user.MaNV,
                VaiTro = vaiTro,
                HasCustomerInfo = hasCustomerInfo,
                IsAdmin = isAdmin
            });
        }

        private string GenerateKhachHangCode()
        {
            var lastKhachHang = _context.KhachHangs
                .OrderByDescending(k => k.MAKH)
                .FirstOrDefault();

            string lastCode = lastKhachHang?.MAKH ?? "KH0000";
            int number = int.Parse(lastCode.Substring(2)) + 1;
            return "KH" + number.ToString("D4");
        }

        [HttpPost("SendOtp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap && u.EMAIL == request.Email);

            if (user == null)
                return NotFound("Không tìm thấy tài khoản với tên đăng nhập và email này.");

            var rng = new Random();
            int otp = rng.Next(100000, 999999);

            user.OTP = otp;
            await _context.SaveChangesAsync();

            var smtpConfig = GetSmtpConfig();

            using var smtp = new SmtpClient(smtpConfig.Host)
            {
                Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password),
                EnableSsl = true,
                Port = smtpConfig.Port,
                Timeout = 10000
            };

            using var mail = new MailMessage(smtpConfig.FromEmail, user.EMAIL!)
            {
                Subject = "Mã OTP đặt lại mật khẩu - Medion",
                Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #17a2b8;'>Đặt lại mật khẩu</h2>
                        <p>Xin chào <strong>{user.TenDangNhap}</strong>,</p>
                        <p>Bạn đã yêu cầu đặt lại mật khẩu. Mã OTP của bạn là:</p>
                        <div style='background: #f8f9fa; padding: 20px; text-align: center; margin: 20px 0;'>
                            <h1 style='color: #17a2b8; margin: 0; font-size: 36px; letter-spacing: 5px;'>{otp}</h1>
                        </div>
                        <p>Mã OTP có hiệu lực trong 5 phút.</p>
                        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #6c757d; font-size: 12px;'>Đây là email tự động, vui lòng không trả lời.</p>
                    </div>
                ",
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(mail);

            return Ok(new SendOtpResponse { Message = "OTP đã được gửi về email của bạn." });
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap && u.EMAIL == request.Email);

            if (user == null)
                return NotFound("Không tìm thấy tài khoản.");

            if (user.OTP == null || user.OTP != request.Otp)
                return BadRequest("OTP không đúng hoặc đã hết hạn.");

            user.MatKhau = request.MatKhauMoi;
            user.OTP = null;
            await _context.SaveChangesAsync();

            return Ok(new ResetPasswordResponse { Message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });
        }

        [HttpPost("reset-password/{maNV}")]
        public async Task<IActionResult> ResetPasswordByAdmin(string maNV, [FromBody] ResetPasswordByAdminRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(maNV))
                    return BadRequest("Mã nhân viên không hợp lệ.");

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                    return BadRequest("Mật khẩu mới không được để trống.");

                var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.MaNV == maNV);
                if (nhanVien == null)
                    return NotFound("Không tìm thấy nhân viên.");

                var taiKhoan = await _context.TaiKhoans
                    .FirstOrDefaultAsync(t => t.TenDangNhap == nhanVien.MaNV);

                if (taiKhoan == null)
                    return NotFound("Không tìm thấy tài khoản cho nhân viên này.");

                taiKhoan.MatKhau = request.NewPassword;
                _context.TaiKhoans.Update(taiKhoan);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Reset mật khẩu thành công.", newPassword = request.NewPassword });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi reset mật khẩu.", error = ex.Message });
            }
        }
    }
}
