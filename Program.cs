using BE_QLTiemThuoc.Data;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Repositories;
using BE_QLTiemThuoc.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

// Disable file watchers entirely (fix for Render)
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "0");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// REMOVE ALL DEFAULT CONFIG SOURCES (this disables file watchers)
builder.Configuration.Sources.Clear();

// Add config WITHOUT reloadOnChange (critical!)
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

// Load .env
Env.Load();

// Read connection string
var defaultConnection =
    Environment.GetEnvironmentVariable("Default__Connection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(defaultConnection));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("_cors", p =>
   p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ========= JWT CONFIGURATION =========
var jwtSecretKey = Environment.GetEnvironmentVariable("Jwt__SecretKey")
    ?? builder.Configuration["Jwt:SecretKey"]
    ?? "YourSuperSecretKeyMustBeAtLeast32CharactersLong!";

var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "BE_QLTiemThuoc";

var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience")
?? builder.Configuration["Jwt:Audience"]
    ?? "BE_QLTiemThuoc_Clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
   ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    // Policy cho Admin (ChucVu = 1)
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("ChucVu", "1"));
    
    // Policy cho Staff (có MaNV)
    options.AddPolicy("StaffOnly", policy =>
        policy.RequireAssertion(context =>
         context.User.HasClaim(c => c.Type == "MaNV" && !string.IsNullOrEmpty(c.Value))));
    
    // Policy cho Admin hoặc Staff
    options.AddPolicy("AdminOrStaff", policy =>
   policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "ChucVu" && c.Value == "1") ||
            context.User.HasClaim(c => c.Type == "MaNV" && !string.IsNullOrEmpty(c.Value))));
});

// Register JWT Service
builder.Services.AddScoped<JwtService>();

// Cloudinary config
var account = new Account(
  Environment.GetEnvironmentVariable("Cloudinary__CloudName") ?? builder.Configuration["Cloudinary:CloudName"],
    Environment.GetEnvironmentVariable("Cloudinary__ApiKey") ?? builder.Configuration["Cloudinary:ApiKey"],
    Environment.GetEnvironmentVariable("Cloudinary__ApiSecret") ?? builder.Configuration["Cloudinary:ApiSecret"]
);

builder.Services.AddSingleton(new Cloudinary(account));

// Register services
builder.Services.AddScoped<NhaCungCapRepository>();
builder.Services.AddScoped<NhaCungCapService>();
builder.Services.AddScoped<KhachHangRepository>();
builder.Services.AddScoped<KhachHangService>();
builder.Services.AddScoped<ImagesService>();
builder.Services.AddScoped<NhomLoaiRepository>();
builder.Services.AddScoped<NhomLoaiService>();
builder.Services.AddScoped<PhieuNhapRepository>();
builder.Services.AddScoped<PhieuNhapService>();
builder.Services.AddScoped<ThuocRepository>();
builder.Services.AddScoped<ThuocService>();
builder.Services.AddScoped<LoaiThuocRepository>();
builder.Services.AddScoped<LoaiThuocService>();
builder.Services.AddScoped<LieuDungRepository>();
builder.Services.AddScoped<LieuDungService>();
builder.Services.AddScoped<PhieuQuyDoiService>();
builder.Services.AddScoped<ThuocViewService>();
builder.Services.AddScoped<NhanVienRepository>();
builder.Services.AddScoped<NhanVienService>();
builder.Services.AddScoped<LoaiDonViRepository>();
builder.Services.AddScoped<LoaiDonViService>();
builder.Services.AddScoped<PhieuHuyService>();
builder.Services.AddScoped<PhieuXuLyHoanHuyRepository>();
builder.Services.AddScoped<PhieuXuLyHoanHuyService>();
builder.Services.AddScoped<DanhGiaThuocRepository>();
builder.Services.AddScoped<DanhGiaThuocService>();
builder.Services.AddScoped<BinhLuanRepository>();
builder.Services.AddScoped<BinhLuanService>();
builder.Services.AddScoped<ChatRepository>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IThongKeService, ThongKeService>();

// (các service khác giữ nguyên)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ========= SWAGGER WITH JWT SUPPORT =========
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "BE_QLTiemThuoc API", 
        Version = "v1",
    Description = "API quản lý tiệm thuốc với JWT Authentication"
    });

// Thêm JWT Authentication vào Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. 
        Nhập 'Bearer' [space] và token của bạn.
 Ví dụ: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        Name = "Authorization",
   In = ParameterLocation.Header,
   Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
     {
            new OpenApiSecurityScheme
            {
          Reference = new OpenApiReference
     {
              Type = ReferenceType.SecurityScheme,
             Id = "Bearer"
              },
 Scheme = "oauth2",
    Name = "Bearer",
    In = ParameterLocation.Header,
          },
         new List<string>()
   }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("_cors");

// ========= IMPORTANT: UseAuthentication MUST come before UseAuthorization =========
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
