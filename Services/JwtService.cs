using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BE_QLTiemThuoc.Services
{
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;

     public JwtService(IConfiguration configuration)
        {
   _secretKey = Environment.GetEnvironmentVariable("Jwt__SecretKey") 
      ?? configuration["Jwt:SecretKey"] 
        ?? throw new ArgumentNullException("JWT SecretKey is not configured");
   _issuer = Environment.GetEnvironmentVariable("Jwt__Issuer") 
      ?? configuration["Jwt:Issuer"] 
        ?? "BE_QLTiemThuoc";
        _audience = Environment.GetEnvironmentVariable("Jwt__Audience") 
         ?? configuration["Jwt:Audience"] 
                ?? "BE_QLTiemThuoc_Clients";
            
  var expMinutesStr = Environment.GetEnvironmentVariable("Jwt__ExpirationMinutes") 
       ?? configuration["Jwt:ExpirationMinutes"] 
    ?? "1440"; // default 24 hours
            _expirationMinutes = int.Parse(expMinutesStr);
        }

public string GenerateToken(string maTK, string tenDangNhap, string? email, string? maKH, string? maNV, string vaiTro, int chucVu)
        {
  var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
      new Claim(JwtRegisteredClaimNames.Sub, maTK),
              new Claim(JwtRegisteredClaimNames.UniqueName, tenDangNhap),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
          new Claim("VaiTro", vaiTro),
                new Claim("ChucVu", chucVu.ToString()),
   new Claim(ClaimTypes.Role, vaiTro)
            };

  // Add optional claims
         if (!string.IsNullOrEmpty(email))
  claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
            
          if (!string.IsNullOrEmpty(maKH))
                claims.Add(new Claim("MaKH", maKH));
            
          if (!string.IsNullOrEmpty(maNV))
        claims.Add(new Claim("MaNV", maNV));

            var token = new JwtSecurityToken(
     issuer: _issuer,
      audience: _audience,
                claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
      signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
   }

        public ClaimsPrincipal? ValidateToken(string token)
        {
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(_secretKey);

       try
          {
        var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
          {
           ValidateIssuerSigningKey = true,
         IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
           ValidIssuer = _issuer,
ValidateAudience = true,
   ValidAudience = _audience,
       ValidateLifetime = true,
           ClockSkew = TimeSpan.Zero
     }, out SecurityToken validatedToken);

          return principal;
   }
            catch
            {
        return null;
  }
   }
  }
}
