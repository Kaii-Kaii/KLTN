// using Microsoft.AspNetCore.Mvc;
// using BE_QLTiemThuoc.Data;
// using Microsoft.EntityFrameworkCore;
// using System.Net.Mail;
// using System.Net;
// using BE_QLTiemThuoc.Model;
// using BE_QLTiemThuoc.DTOs;

// namespace BE_QLTiemThuoc.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class TaiKhoanController : ControllerBase
//     {
//         private readonly AppDbContext _context;

//         public TaiKhoanController(AppDbContext context)
//         {
//             _context = context;
//         }

//         [HttpGet]
//         public async Task<ActionResult<IEnumerable<TaiKhoan>>> GetAll()
//         {
//             return await _context.TaiKhoans.ToListAsync();
//         }
//  // ========= LOAD CONFIG SENDGRID =========
//         private (string FromEmail, string ApiKey) GetEmailConfig()
//         {
//             var from = Environment.GetEnvironmentVariable("EmailSettings__From")
//                         ?? _configuration["EmailSettings:From"];

//             var apiKey = Environment.GetEnvironmentVariable("EmailSettings__SmtpPassword")
//                          ?? _configuration["EmailSettings:SmtpPassword"];

//             if (string.IsNullOrEmpty(apiKey))
//                 throw new Exception("Thiếu SendGrid API key");

//             return (from!, apiKey!);
//         }

//         // [HttpGet("CheckUsername")]
//         // public async Task<IActionResult> CheckUsername([FromQuery] string username)
//         // {
//         //     if (string.IsNullOrWhiteSpace(username))
//         //         return BadRequest("Thiếu tên đăng nhập.");

//         //     var exists = await _context.TaiKhoans.AnyAsync(u => u.TenDangNhap == username);
//         //     // Trả về true nếu đã tồn tại, false nếu chưa
//         //     return Ok(new CheckUsernameResponse { Exists = exists });
//         // }

//                 // ========= CHECK USERNAME =========
//         [HttpGet("CheckUsername")]
//         public async Task<IActionResult> CheckUsername([FromQuery] string username)
//         {
//             var exists = await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == username);
//             return Ok(new { Exists = exists });
//         }

//         // [HttpPost]
//         // public async Task<IActionResult> CreateAccount([FromBody] RegisterRequest request)
//         // {
//         //     try
//         //     {
//         //         if (!ModelState.IsValid)
//         //         {
//         //             return BadRequest(ModelState);
//         //         }

//         //         // Kiểm tra tên đăng nhập đã tồn tại
//         //         var existingUser = await _context.TaiKhoans
//         //             .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap);
//         //         if (existingUser != null)
//         //         {
//         //             return BadRequest("Tên đăng nhập đã tồn tại.");
//         //         }

//         //         // Sinh token xác thực email
//         //         var tokenBytes = new byte[32];
//         //         using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
//         //         {
//         //             rng.GetBytes(tokenBytes);
//         //         }
//         //         string emailToken = Convert.ToBase64String(tokenBytes);

//         //         // Tạo đối tượng TaiKhoan từ DTO
//         //         var newAccount = new TaiKhoan
//         //         {
//         //             MaTK = GenerateAccountCode(),
//         //             TenDangNhap = request.TenDangNhap,
//         //             MatKhau = request.MatKhau,
//         //             EMAIL = request.Email,
//         //             ISEMAILCONFIRMED = 0,
//         //             EMAILCONFIRMATIONTOKEN = emailToken,
//         //             OTP = null,
//         //             KhachHang = null
//         //         };

//         //         _context.TaiKhoans.Add(newAccount);
//         //         await _context.SaveChangesAsync();

//         //         // Gửi email xác thực
//         //         string confirmationLink = $"https://localhost:7283/api/TaiKhoan/ConfirmEmail?token={Uri.EscapeDataString(emailToken)}";
//         //         await SendConfirmationEmail(newAccount.EMAIL, confirmationLink);

//         //         return Ok(new RegisterResponse { Message = "Tạo tài khoản thành công. Vui lòng kiểm tra email để xác thực." });
//         //     }
//         //     catch (Exception ex)
//         //     {
//         //         return StatusCode(500, new
//         //         {
//         //             message = "Đã xảy ra lỗi phía server.",
//         //             error = ex.Message
//         //         });
//         //     }
//         // }

// // ========= CREATE ACCOUNT =========
//         [HttpPost]
//         public async Task<IActionResult> CreateAccount([FromBody] RegisterRequest req)
//         {
//             if (!ModelState.IsValid)
//                 return BadRequest(ModelState);

//             if (await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == req.TenDangNhap))
//                 return BadRequest("Tên đăng nhập đã tồn tại.");

//             // tạo token
//             var bytes = new byte[32];
//             using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
//                 rng.GetBytes(bytes);

//             string token = Convert.ToBase64String(bytes);

//             var tk = new TaiKhoan
//             {
//                 MaTK = GenerateAccountCode(),
//                 TenDangNhap = req.TenDangNhap,
//                 MatKhau = req.MatKhau,
//                 EMAIL = req.Email,
//                 ISEMAILCONFIRMED = 0,
//                 EMAILCONFIRMATIONTOKEN = token
//             };

//             _context.TaiKhoans.Add(tk);
//             await _context.SaveChangesAsync();

//             // Auto detect domain (local hoặc Render)
//             string baseUrl = $"{Request.Scheme}://{Request.Host}";
//             string link = $"{baseUrl}/api/TaiKhoan/ConfirmEmail?token={Uri.EscapeDataString(token)}";

//             await SendConfirmationEmail(req.Email!, link);

//             return Ok(new { Message = "Tạo tài khoản thành công." });
//         }



//         // private async Task SendConfirmationEmail(string toEmail, string confirmationLink)
//         // {
//         //     var smtp = new SmtpClient("smtp.gmail.com") // Thêm host ở đây
//         //     {
//         //         Credentials = new NetworkCredential("chaytue0203@gmail.com", "svyx qaob bihf vaac"),
//         //         EnableSsl = true,
//         //         Port = 587
//         //     };
//         //     var mail = new MailMessage("khangtuong040@gmail.com", toEmail)
//         //     {
//         //         Subject = "Xác thực tài khoản",
//         //         Body = $"Vui lòng xác thực tài khoản bằng cách click vào link: {confirmationLink}"
//         //     };
//         //     await smtp.SendMailAsync(mail);
//         // }


// // ========= GỬI EMAIL XÁC MINH =========
//         private async Task SendConfirmationEmail(string toEmail, string link)
//         {
//             var cfg = GetEmailConfig();
//             var client = new SendGridClient(cfg.ApiKey);

//             var from = new EmailAddress(cfg.FromEmail, "Medion");
//             var to = new EmailAddress(toEmail);

//             string html = $@"
//                 <div style='font-family:Arial'>
//                     <h2 style='color:#03A9F4'>Xác thực tài khoản Medion</h2>
//                     <p>Nhấn để xác thực:</p>
//                     <a href='{link}' target='_blank'>{link}</a>
//                 </div>";

//             var msg = MailHelper.CreateSingleEmail(from, to, "Xác thực tài khoản", "", html);

//             await client.SendEmailAsync(msg);
//         }

// //         [HttpGet("ConfirmEmail")]
// //         public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
// //         {
// //             if (string.IsNullOrEmpty(token))
// //                 return Content("<h2 style='color:#03A9F4;text-align:center;margin-top:40px'>Token không hợp lệ.</h2>", "text/html; charset=utf-8");

// //             var user = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.EMAILCONFIRMATIONTOKEN == token);
// //             if (user == null)
// //                 return Content("<h2 style='color:#03A9F4;text-align:center;margin-top:40px'>Token không hợp lệ hoặc đã xác thực.</h2>", "text/html; charset=utf-8");

// //             user.ISEMAILCONFIRMED = 1;
// //             user.EMAILCONFIRMATIONTOKEN = null;
// //             await _context.SaveChangesAsync();

// //             string html = @"
// // <body style='background:#f5f6fa;'>
// //   <div style='max-width:400px;margin:60px auto;padding:32px 24px;background:#fff;border-radius:12px;box-shadow:0 2px 12px #0001;text-align:center'>
// //     <svg width='60' height='60' viewBox='0 0 24 24' fill='none' style='margin-bottom:16px'>
// //       <circle cx='12' cy='12' r='12' fill='#03A9F4'/>
// //       <path d='M7 13l3 3 7-7' stroke='#fff' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/>
// //     </svg>
// //     <h2 style='color:#03A9F4'>Đã xác thực email thành công!</h2>
// //   </div>
// // </body>
// // ";
// //             return Content(html, "text/html; charset=utf-8");
// //         }

//   // ========= CONFIRM EMAIL =========
//         [HttpGet("ConfirmEmail")]
//         public async Task<IActionResult> ConfirmEmail(string token)
//         {
//             var user = await _context.TaiKhoans
//                 .FirstOrDefaultAsync(x => x.EMAILCONFIRMATIONTOKEN == token);

//             if (user == null)
//                 return Content("Token không hợp lệ hoặc đã được dùng.");

//             user.ISEMAILCONFIRMED = 1;
//             user.EMAILCONFIRMATIONTOKEN = null;

//             await _context.SaveChangesAsync();

//             return Content("<h2 style='color:#03A9F4'>Xác thực thành công!</h2>", "text/html");
//         }


//         // private string GenerateAccountCode()
//         // {
//         //     var lastAccount = _context.TaiKhoans
//         //         .OrderByDescending(t => t.MaTK)
//         //         .FirstOrDefault();

//         //     string lastCode = lastAccount?.MaTK ?? "TK0000";
//         //     int number = int.Parse(lastCode.Substring(2)) + 1;
//         //     return "TK" + number.ToString("D4");
//         // }

//     private string GenerateAccountCode()
//         {
//             var last = _context.TaiKhoans.OrderByDescending(x => x.MaTK).FirstOrDefault();
//             int num = int.Parse((last?.MaTK ?? "TK0000")[2..]) + 1;
//             return "TK" + num.ToString("D4");
//         }


//         // [HttpPost("Login")]
//         // public async Task<IActionResult> Login([FromBody] LoginRequest request)
//         // {
//         //     if (!ModelState.IsValid)
//         //     {
//         //         return BadRequest(ModelState);
//         //     }

//         //     var user = await _context.TaiKhoans
//         //         .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap && u.MatKhau == request.MatKhau);

//         //     if (user == null)
//         //         return Unauthorized("Sai tên đăng nhập hoặc mật khẩu.");

//         //     if (user.ISEMAILCONFIRMED == 0)
//         //         return BadRequest("Tài khoản chưa xác thực email.");

//         //     // Kiểm tra vai trò: Nếu có MaNV và ChucVu = 1 thì là Admin
//         //     bool isAdmin = false;
//         //     int chucVu = 0;
//         //     string vaiTro = "User";

//         //     if (!string.IsNullOrEmpty(user.MaNV))
//         //     {
//         //         // Lấy thông tin nhân viên để kiểm tra ChucVu
//         //         var nhanVien = await _context.Set<NhanVien>()
//         //             .FirstOrDefaultAsync(nv => nv.MaNV == user.MaNV);
                
//         //         if (nhanVien != null)
//         //         {
//         //             chucVu = nhanVien.ChucVu ?? 0;
//         //             isAdmin = (chucVu == 1); // Chỉ ChucVu = 1 là Admin
//         //             vaiTro = isAdmin ? "Admin" : "Staff";
//         //         }
//         //     }

//         //     bool hasCustomerInfo = false;

//         //     // Nếu là Admin hoặc Staff (có MaNV) - không cần thông tin khách hàng
//         //     if (!string.IsNullOrEmpty(user.MaNV))
//         //     {
//         //         hasCustomerInfo = true; // Staff/Admin không cần nhập thông tin
//         //     }
//         //     else
//         //     {
//         //         // Nếu là User (không có MaNV) và chưa có MaKH, tự động tạo mã khách hàng
//         //         if (string.IsNullOrEmpty(user.MaKH))
//         //         {
//         //             string newMaKH = GenerateKhachHangCode();

//         //             // Gán MaKH vào TaiKhoan
//         //             user.MaKH = newMaKH;
//         //             _context.TaiKhoans.Update(user);

//         //             // Tạo bản ghi KhachHang mới với thông tin rỗng
//         //             var newKhachHang = new KhachHang
//         //             {
//         //                 MAKH = newMaKH,
//         //                 HoTen = null,
//         //                 GioiTinh = null,
//         //                 NgaySinh = null,
//         //                 DiaChi = null,
//         //                 DienThoai = null
//         //             };

//         //             _context.KhachHangs.Add(newKhachHang);

//         //             // Lưu cả MaKH vào bảng TaiKhoan và bản ghi KhachHang mới
//         //             await _context.SaveChangesAsync();

//         //             // Chưa điền thông tin nên hasCustomerInfo = false
//         //             hasCustomerInfo = false;
//         //         }
//         //         else
//         //         {
//         //             // Đã có MaKH, kiểm tra xem đã điền đầy đủ thông tin chưa
//         //             var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MAKH == user.MaKH);
//         //             if (khachHang != null)
//         //             {
//         //                 // Kiểm tra các trường bắt buộc: HoTen, DienThoai, DiaChi
//         //                 hasCustomerInfo = !string.IsNullOrEmpty(khachHang.HoTen)
//         //                                && !string.IsNullOrEmpty(khachHang.DienThoai)
//         //                                && !string.IsNullOrEmpty(khachHang.DiaChi);
//         //             }
//         //             else
//         //             {
//         //                 hasCustomerInfo = false;
//         //             }
//         //         }
//         //     }

//         //     return Ok(new LoginResponse
//         //     {
//         //         Message = "Đăng nhập thành công",
//         //         MaTK = user.MaTK,
//         //         TenDangNhap = user.TenDangNhap,
//         //         Email = user.EMAIL,
//         //         MaKH = user.MaKH,
//         //         MaNV = user.MaNV,
//         //         ChucVu = chucVu,
//         //         VaiTro = vaiTro,
//         //         HasCustomerInfo = hasCustomerInfo,
//         //         IsAdmin = isAdmin
//         //     });
//         // }
//          // ========= LOGIN =========
//         [HttpPost("Login")]
//         public async Task<IActionResult> Login([FromBody] LoginRequest request)
//         {
//             if (!ModelState.IsValid)
//                 return BadRequest(ModelState);

//             var user = await _context.TaiKhoans
//                 .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap
//                                        && u.MatKhau == request.MatKhau);

//             if (user == null)
//                 return Unauthorized("Sai tên đăng nhập hoặc mật khẩu.");

//             if (user.ISEMAILCONFIRMED == 0)
//                 return BadRequest("Tài khoản chưa xác thực email.");

//             bool isAdmin = !string.IsNullOrEmpty(user.MaNV);
//             string vaiTro = isAdmin ? "Admin" : "User";
//             bool hasCustomerInfo = false;

//             // Nếu là KH bình thường → kiểm tra MaKH
//             if (!isAdmin)
//             {
//                 if (string.IsNullOrEmpty(user.MaKH))
//                 {
//                     // Lần đầu đăng nhập → tạo mã khách hàng mới
//                     string newMaKH = GenerateKhachHangCode();
//                     user.MaKH = newMaKH;

//                     var kh = new KhachHang
//                     {
//                         MAKH = newMaKH,
//                         HoTen = null,
//                         GioiTinh = null,
//                         NgaySinh = null,
//                         DiaChi = null,
//                         DienThoai = null
//                     };

//                     _context.KhachHangs.Add(kh);

//                     await _context.SaveChangesAsync();

//                     hasCustomerInfo = false;
//                 }
//                 else
//                 {
//                     var kh = await _context.KhachHangs
//                         .FirstOrDefaultAsync(k => k.MAKH == user.MaKH);

//                     if (kh != null)
//                     {
//                         hasCustomerInfo = !string.IsNullOrEmpty(kh.HoTen)
//                                        && !string.IsNullOrEmpty(kh.DiaChi)
//                                        && !string.IsNullOrEmpty(kh.DienThoai);
//                     }
//                 }
//             }

//             return Ok(new LoginResponse
//             {
//                 Message = "Đăng nhập thành công.",
//                 MaTK = user.MaTK,
//                 TenDangNhap = user.TenDangNhap,
//                 Email = user.EMAIL,
//                 MaKH = user.MaKH,        // 🔥 FE CẦN CÁI NÀY
//                 MaNV = user.MaNV,
//                 VaiTro = vaiTro,
//                 HasCustomerInfo = hasCustomerInfo,
//                 IsAdmin = isAdmin
//             });
//         }

//         // private string GenerateKhachHangCode()
//         // {
//         //     var lastKhachHang = _context.KhachHangs
//         //         .OrderByDescending(k => k.MAKH)
//         //         .FirstOrDefault();

//         //     string lastCode = lastKhachHang?.MAKH ?? "KH0000";
//         //     int number = int.Parse(lastCode.Substring(2)) + 1;
//         //     return "KH" + number.ToString("D4");
//         // }
//           private string GenerateKhachHangCode()
//         {
//             var last = _context.KhachHangs
//                 .OrderByDescending(k => k.MAKH)
//                 .FirstOrDefault();

//             string lastCode = last?.MAKH ?? "KH0000";
//             int number = int.Parse(lastCode.Substring(2)) + 1;

//             return "KH" + number.ToString("D4");
//         }

// //         // Gửi OTP về email khi quên mật khẩu
// //         [HttpPost("SendOtp")]
// //         public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
// //         {
// //             if (!ModelState.IsValid)
// //             {
// //                 return BadRequest(ModelState);
// //             }

// //             var user = await _context.TaiKhoans
// //                 .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap && u.EMAIL == request.Email);

// //             if (user == null)
// //                 return NotFound("Không tìm thấy tài khoản với tên đăng nhập và email này.");

// //             // Sinh OTP ngẫu nhiên 6 số
// //             var rng = new Random();
// //             int otp = rng.Next(100000, 999999);

// //             user.OTP = otp;
// //             await _context.SaveChangesAsync();

// //             // Gửi OTP về email
// //             var smtp = new SmtpClient("smtp.gmail.com")
// //             {
// //                 Credentials = new NetworkCredential("chaytue0203@gmail.com", "svyx qaob bihf vaac"),
// //                 EnableSsl = true,
// //                 Port = 587
// //             };

// //             var mail = new MailMessage("khangtuong040@gmail.com", user.EMAIL)
// //             {
// //                 Subject = "Mã OTP đặt lại mật khẩu - Medion",
// //                 Body = $@"
// //                     <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
// //                         <h2 style='color: #17a2b8;'>Đặt lại mật khẩu</h2>
// //                         <p>Xin chào <strong>{user.TenDangNhap}</strong>,</p>
// //                         <p>Bạn đã yêu cầu đặt lại mật khẩu. Mã OTP của bạn là:</p>
// //                         <div style='background: #f8f9fa; padding: 20px; text-align: center; margin: 20px 0;'>
// //                             <h1 style='color: #17a2b8; margin: 0; font-size: 36px; letter-spacing: 5px;'>{otp}</h1>
// //                         </div>
// // <p>Mã OTP có hiệu lực trong 5 phút.</p>
// //                         <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
// //                         <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
// //                         <p style='color: #6c757d; font-size: 12px;'>Đây là email tự động, vui lòng không trả lời.</p>
// //                     </div>
// //                 ",
// //                 IsBodyHtml = true
// //             };

// //             await smtp.SendMailAsync(mail);

// //             return Ok(new SendOtpResponse { Message = "OTP đã được gửi về email của bạn." });
// //         }

//  // ========= SEND OTP =========
//         [HttpPost("SendOtp")]
//         public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
//         {
//             var user = await _context.TaiKhoans
//                 .FirstOrDefaultAsync(x => x.TenDangNhap == req.TenDangNhap && x.EMAIL == req.Email);

//             if (user == null)
//                 return NotFound("Không tìm thấy tài khoản.");

//             int otp = new Random().Next(100000, 999999);
//             user.OTP = otp;

//             await _context.SaveChangesAsync();

//             var cfg = GetEmailConfig();
//             var client = new SendGridClient(cfg.ApiKey);

//             var from = new EmailAddress(cfg.FromEmail, "Medion");
//             var to = new EmailAddress(req.Email);

//             string html = $@"
//                 <div style='font-family:Arial'>
//                     <h2 style='color:#17a2b8'>OTP đặt lại mật khẩu</h2>
//                     <h1>{otp}</h1>
//                 </div>";

//             var msg = MailHelper.CreateSingleEmail(from, to, "Mã OTP đặt lại mật khẩu", "", html);
//             await client.SendEmailAsync(msg);

//             return Ok(new { Message = "OTP đã được gửi." });
//         }


//         // [HttpPost("ResetPassword")]
//         // public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
//         // {
//         //     if (!ModelState.IsValid)
//         //     {
//         //         return BadRequest(ModelState);
//         //     }

//         //     var user = await _context.TaiKhoans
//         //         .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap && u.EMAIL == request.Email);

//         //     if (user == null)
//         //         return NotFound("Không tìm thấy tài khoản.");

//         //     if (user.OTP == null || user.OTP != request.Otp)
//         //         return BadRequest("OTP không đúng hoặc đã hết hạn.");

//         //     user.MatKhau = request.MatKhauMoi;
//         //     user.OTP = null; // Xóa OTP sau khi đổi mật khẩu thành công
//         //     await _context.SaveChangesAsync();

//         //     return Ok(new ResetPasswordResponse { Message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });
//         // }

// // ========= RESET PASSWORD =========
//         [HttpPost("ResetPassword")]
//         public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
//         {
//             var user = await _context.TaiKhoans
//                 .FirstOrDefaultAsync(x => x.TenDangNhap == req.TenDangNhap && x.EMAIL == req.Email);

//             if (user == null)
//                 return NotFound("Không tìm thấy tài khoản.");

//             if (user.OTP != req.Otp)
//                 return BadRequest("OTP không đúng.");

//             user.MatKhau = req.MatKhauMoi;
//             user.OTP = null;

//             await _context.SaveChangesAsync();

//             return Ok(new { Message = "Đổi mật khẩu thành công." });
//         }


//         [HttpPost("reset-password/{maNV}")]
//         public async Task<IActionResult> ResetPasswordByAdmin(string maNV, [FromBody] ResetPasswordByAdminRequest request)
//         {
//             try
//             {
//                 if (string.IsNullOrWhiteSpace(maNV))
//                     return BadRequest("Mã nhân viên không hợp lệ.");

//                 if (string.IsNullOrWhiteSpace(request.NewPassword))
//                     return BadRequest("Mật khẩu mới không được để trống.");

//                 // Tìm nhân viên
//                 var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.MaNV == maNV);
//                 if (nhanVien == null)
//                     return NotFound("Không tìm thấy nhân viên.");

//                 // Tìm tài khoản liên kết theo MaNV
//                 var taiKhoan = await _context.TaiKhoans
//                     .FirstOrDefaultAsync(t => t.MaNV == maNV);

//                 if (taiKhoan == null)
//                     return NotFound("Không tìm thấy tài khoản cho nhân viên này.");

//                 // Cập nhật mật khẩu
//                 taiKhoan.MatKhau = request.NewPassword;
//                 _context.TaiKhoans.Update(taiKhoan);
//                 await _context.SaveChangesAsync();

//                 return Ok(new { message = "Reset mật khẩu thành công.", newPassword = request.NewPassword });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { message = "Lỗi khi reset mật khẩu.", error = ex.Message });
//             }
//         }

//     }
// }
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Services;
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

            // Check admin status based on ChucVu from NhanVien table
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
                    // ChucVu === 1 means Admin, anything else is Staff
                    isAdmin = (nhanVien.ChucVu == 1);
                    vaiTro = isAdmin ? "Admin" : "Staff";
                }
            }
            
            bool hasCustomerInfo = false;

            // Nếu là KH bình thường → kiểm tra MaKH
            if (!isAdmin && string.IsNullOrEmpty(user.MaNV))
            {
                if (string.IsNullOrEmpty(user.MaKH))
                {
                    // Lần đầu đăng nhập → tạo mã khách hàng mới
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

                    hasCustomerInfo = false;
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
                IsAdmin = isAdmin
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
        private string GenerateKhachHangCode()
        {
            var last = _context.KhachHangs
                .OrderByDescending(k => k.MAKH)
                .FirstOrDefault();

            string lastCode = last?.MAKH ?? "KH0000";
            int number = int.Parse(lastCode.Substring(2)) + 1;

            return "KH" + number.ToString("D4");
        }

    }
}
