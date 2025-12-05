using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Repositories;

namespace BE_QLTiemThuoc.Services
{
    public class LoaiThuocService
    {
        private readonly LoaiThuocRepository _repo;
        public LoaiThuocService(LoaiThuocRepository repo)
        {
            _repo = repo;
        }

        public Task<List<LoaiThuoc>> GetAllAsync() => _repo.GetAllAsync();

        public Task<LoaiThuoc?> GetByIdAsync(string ma) => _repo.GetByIdAsync(ma);

        public Task<List<LoaiThuoc>> GetByNhomAsync(string maNhom) => _repo.GetByNhomAsync(maNhom);

        public async Task<(bool Ok, string? Error)> CreateAsync(LoaiThuoc dto)
        {
            if (dto == null) return (false, "Payload is required");
            if (string.IsNullOrWhiteSpace(dto.MaLoaiThuoc)) return (false, "MaLoaiThuoc is required");
            var existing = await _repo.GetByIdAsync(dto.MaLoaiThuoc);
            if (existing != null) return (false, "MaLoaiThuoc already exists");
            await _repo.CreateAsync(dto);
            return (true, null);
        }

        public async Task<(bool Ok, string? Error)> UpdateAsync(string ma, LoaiThuoc dto)
        {
            if (string.IsNullOrWhiteSpace(ma)) return (false, "Ma is required");
            var existing = await _repo.GetByIdAsync(ma);
            if (existing == null) return (false, "Not found");
            existing.TenLoaiThuoc = dto.TenLoaiThuoc;
            existing.MaNhomLoai = dto.MaNhomLoai;
            await _repo.UpdateAsync(existing);
            return (true, null);
        }

        public async Task<(bool Ok, string? Error)> DeleteAsync(string ma)
        {
            if (string.IsNullOrWhiteSpace(ma)) return (false, "Ma is required");
            var existing = await _repo.GetByIdAsync(ma);
            if (existing == null) return (false, "Not found");
            await _repo.DeleteAsync(existing);
            return (true, null);
        }
    }
}
