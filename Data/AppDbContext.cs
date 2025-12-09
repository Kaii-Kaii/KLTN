using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Model.Kho;
using BE_QLTiemThuoc.Model.Ban; // Added namespace for DanhGiaThuoc and BinhLuan
using BE_QLTiemThuoc.Model.Chat;


namespace BE_QLTiemThuoc.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<Thuoc> Thuoc { get; set; } // Add this DbSet for Thuoc
        public DbSet<NhaCungCap> NhaCungCaps { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<LoaiThuoc> LoaiThuoc { get; set; }
        public DbSet<LoaiDonVi> LoaiDonVi { get; set; }
        public DbSet<NhomLoai> NhomLoai { get; set; }
        public DbSet<GiaThuoc> GiaThuocs { get; set; }
        public DbSet<PhieuNhap> PhieuNhaps { get; set; } 
        public DbSet<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; }
        public DbSet<TonKho> TonKhos { get; set; }
    // Sales / Invoice
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<LichSuThanhToan> LichSuThanhToans { get; set; }
        
        public DbSet<LieuDung> LieuDungs { get; set; }
        public DbSet<PhieuXuLyHoanHuy> PhieuXuLyHoanHuys { get; set; }
        public DbSet<ChiTietPhieuXuLy> ChiTietPhieuXuLys { get; set; }

        public DbSet<DanhGiaThuoc> DanhGiaThuocs { get; set; } // New DbSet
        public DbSet<BinhLuan> BinhLuans { get; set; } // Comments

        public DbSet<PhieuHuy> PhieuHuys { get; set; }
        public DbSet<ChiTietPhieuHuy> ChiTietPhieuHuys { get; set; }

        // Chat
        public DbSet<CuocTroChuyen> CuocTroChuyens { get; set; }
        public DbSet<TinNhan> TinNhans { get; set; }

        // Ma (Mã Code)
        public DbSet<Ma> Mas { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaiKhoan>().ToTable("TaiKhoan");
            modelBuilder.Entity<KhachHang>().ToTable("KhachHang");   
            modelBuilder.Entity<Thuoc>().ToTable("Thuoc");
            modelBuilder.Entity<NhaCungCap>().ToTable("NhaCungCap");
            modelBuilder.Entity<NhanVien>().ToTable("NhanVien");
            modelBuilder.Entity<LoaiThuoc>().ToTable("LoaiThuoc");
            modelBuilder.Entity<LoaiDonVi>().ToTable("LoaiDonVi");
            modelBuilder.Entity<NhomLoai>().ToTable("NhomLoai");
            modelBuilder.Entity<GiaThuoc>().ToTable("GIATHUOC");
            modelBuilder.Entity<PhieuNhap>().ToTable("PhieuNhap");
            modelBuilder.Entity<ChiTietPhieuNhap>().ToTable("ChiTietPhieuNhap");
            modelBuilder.Entity<TonKho>().ToTable("TON_KHO");
            modelBuilder.Entity<HoaDon>().ToTable("HoaDon");
            modelBuilder.Entity<HoaDon>().HasKey(h => h.MaHD);
            modelBuilder.Entity<ChiTietHoaDon>().ToTable("ChiTietHoaDon");
            modelBuilder.Entity<ChiTietHoaDon>().HasKey(ct => ct.MaCTHD);
            modelBuilder.Entity<LieuDung>().ToTable("LieuDung");
            modelBuilder.Entity<ChiTietPhieuXuLy>().ToTable("ChiTietPhieuXuLy");

            modelBuilder.Entity<DanhGiaThuoc>().ToTable("DanhGiaThuoc");
            modelBuilder.Entity<DanhGiaThuoc>().HasKey(d => d.MaDanhGia);
            modelBuilder.Entity<DanhGiaThuoc>().HasIndex(d => new { d.MaKH, d.MaThuoc }).IsUnique();

            modelBuilder.Entity<PhieuHuy>().ToTable("PhieuHuy");
            modelBuilder.Entity<ChiTietPhieuHuy>().ToTable("ChiTietPhieuHuy");

            // Explicit keys
            modelBuilder.Entity<PhieuHuy>().HasKey(p => p.MaPH);
            modelBuilder.Entity<ChiTietPhieuHuy>().HasKey(c => c.MaCTPH);

            // Ensure EF uses ChiTietPhieuHuy.MaPH as FK to PhieuHuy.MaPH (avoid shadow FK 'PhieuHuyMaPH')
            modelBuilder.Entity<ChiTietPhieuHuy>()
                .HasOne<PhieuHuy>()
                .WithMany(p => p.ChiTietPhieuHuys)
                .HasForeignKey(c => c.MaPH)
                .HasPrincipalKey(p => p.MaPH);


            // BinhLuan mapping
            modelBuilder.Entity<BinhLuan>().ToTable("BinhLuan");
            modelBuilder.Entity<BinhLuan>().HasKey(b => b.MaBL);
            modelBuilder.Entity<BinhLuan>()
                .HasOne(b => b.Parent)
                .WithMany(p => p.Replies)
                .HasForeignKey(b => b.TraLoiChoBinhLuan)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<BinhLuan>().HasIndex(b => b.MaThuoc); // for FE query by product
            // Unique reply per parent (filtered index for non-null FK)
            modelBuilder.Entity<BinhLuan>().HasIndex(b => b.TraLoiChoBinhLuan).IsUnique().HasFilter("[TraLoiChoBinhLuan] IS NOT NULL");

            // Chat mapping
            modelBuilder.Entity<CuocTroChuyen>().ToTable("CuocTroChuyen");
            modelBuilder.Entity<CuocTroChuyen>().HasKey(c => c.MaCuocTroChuyen);
            modelBuilder.Entity<CuocTroChuyen>()
                .HasOne<KhachHang>()
                .WithMany()
                .HasForeignKey(c => c.MaKH)
                .HasPrincipalKey(k => k.MAKH);

            modelBuilder.Entity<TinNhan>().ToTable("TinNhan");
            modelBuilder.Entity<TinNhan>().HasKey(t => t.MaTN);
            modelBuilder.Entity<TinNhan>()
                .HasOne(t => t.CuocTroChuyen)
                .WithMany(c => c.TinNhans)
                .HasForeignKey(t => t.MaCuocTroChuyen)
                .HasPrincipalKey(c => c.MaCuocTroChuyen);

            modelBuilder.Entity<TinNhan>()
                .HasOne<NhanVien>()
                .WithMany()
                .HasForeignKey(t => t.MaNV)
                .HasPrincipalKey(n => n.MaNV);

            modelBuilder.Entity<TinNhan>().Property(t => t.ThoiGian).HasColumnType("datetime2");
            modelBuilder.Entity<TinNhan>().HasIndex(t => new { t.MaCuocTroChuyen, t.ThoiGian });

            // Ma (Mã Code)
            modelBuilder.Entity<Ma>().ToTable("ma");
        }
    }

}
