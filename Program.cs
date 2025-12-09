using BE_QLTiemThuoc.Data;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Repositories;
using BE_QLTiemThuoc.Services;
using DotNetEnv;

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
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("_cors");
app.UseAuthorization();

app.MapControllers();
app.Run();
