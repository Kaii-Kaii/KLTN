using BE_QLTiemThuoc.Data;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Repositories;
using BE_QLTiemThuoc.Services;
using DotNetEnv;

// Fix crash do FileSystemWatcher trên Render
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "0");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Load biến môi trường (.env)
Env.Load();

// Tắt reloadOnChange để tránh crash "inotify limit"
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

// Lấy connection string (ưu tiên từ biến môi trường của Render)
var defaultConnection =
    Environment.GetEnvironmentVariable("Default__Connection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Đăng ký DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(defaultConnection));

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("_myAllowSpecificOrigins",
        policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// CLOUDINARY
var cloudinaryCloudName = Environment.GetEnvironmentVariable("Cloudinary__CloudName") ?? builder.Configuration["Cloudinary:CloudName"];
var cloudinaryApiKey = Environment.GetEnvironmentVariable("Cloudinary__ApiKey") ?? builder.Configuration["Cloudinary:ApiKey"];
var cloudinaryApiSecret = Environment.GetEnvironmentVariable("Cloudinary__ApiSecret") ?? builder.Configuration["Cloudinary:ApiSecret"];

var account = new Account(cloudinaryCloudName, cloudinaryApiKey, cloudinaryApiSecret);
builder.Services.AddSingleton(new Cloudinary(account));

// Đăng ký Service & Repository
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

// Controller + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Luôn bật Swagger (Render không có môi trường Development)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("_myAllowSpecificOrigins");
app.UseAuthorization();

app.MapControllers();
app.Run();
