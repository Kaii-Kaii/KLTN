-- Tạo bảng PhieuHuy (updated schema)
CREATE TABLE PhieuHuy (
    MaPH VARCHAR(20) PRIMARY KEY,
    NgayHuy DATETIME NOT NULL DEFAULT GETDATE(),
    MaNV VARCHAR(10) NOT NULL,
    LoaiHuy bit, -- 0: KHO (hủy từ kho), 1: HOADON (hủy từ hóa đơn)
    MaHD VARCHAR(20) NULL, -- Mã hóa đơn (nếu hủy từ hóa đơn)
    TongMatHangHuy INT,
    TongSoLuongHuy INT,
    TongTienHuy DECIMAL(18,2), -- tổng tiền huỷ bình thường
    TongTienKho DECIMAL(18,2), -- tổng tiền huỷ nếu vào kho
    TongTien DECIMAL(18,2), -- tổng (TongTienHuy + TongTienKho)
    GhiChu NVARCHAR(1000),

    CONSTRAINT FK_PhieuHuy_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MANV),
    CONSTRAINT FK_PhieuHuy_HoaDon FOREIGN KEY (MaHD) REFERENCES HoaDon(MaHD)
);

-- Tạo bảng ChiTietPhieuHuy (updated schema)
CREATE TABLE ChiTietPhieuHuy (
    MaCTPH VARCHAR(20) PRIMARY KEY,
    MaPH VARCHAR(20) NOT NULL,
    MaLo VARCHAR(20) NOT NULL,
    SoLuongHuy INT,
    DonGia DECIMAL(18,2),
    ThanhTien DECIMAL(18,2),
    LyDoHuy NVARCHAR(500) NOT NULL,
    GhiChu NVARCHAR(1000),
    LoaiHuy bit, -- 1: huỷ vào kho, 0: huỷ bình thường

    CONSTRAINT FK_ChiTietPhieuHuy_PhieuHuy FOREIGN KEY (MaPH) REFERENCES PhieuHuy(MaPH),
    CONSTRAINT FK_ChiTietPhieuHuy_TonKho FOREIGN KEY (MaLo) REFERENCES TON_KHO(MaLo)
);

-- Indexes
CREATE INDEX IX_PhieuHuy_NgayHuy ON PhieuHuy(NgayHuy);
CREATE INDEX IX_PhieuHuy_MaNV ON PhieuHuy(MaNV);
CREATE INDEX IX_PhieuHuy_LoaiHuy ON PhieuHuy(LoaiHuy);
CREATE INDEX IX_PhieuHuy_MaHD ON PhieuHuy(MaHD);
CREATE INDEX IX_ChiTietPhieuHuy_MaLo ON ChiTietPhieuHuy(MaLo);