using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Repositories;

namespace BE_QLTiemThuoc.Services
{
    public class LoaiDonViService
    {
        private readonly LoaiDonViRepository _repo;
        public LoaiDonViService(LoaiDonViRepository repo)
        {
            _repo = repo;
        }


        public async Task<(bool Ok, string? Error)> CreateAsync(LoaiDonVi dto)
        {
            if (dto == null) return (false, "Payload is required");
            if (string.IsNullOrWhiteSpace(dto.MaLoaiDonVi)) return (false, "MaLoaiDonVi is required");
            var existing = await _repo.GetByIdAsync(dto.MaLoaiDonVi!);
            if (existing != null) return (false, "MaLoaiDonVi already exists");
            await _repo.CreateAsync(dto);
            return (true, null);
        }

        public async Task<(bool Ok, string? Error)> UpdateAsync(string ma, LoaiDonVi dto)
        {
            if (string.IsNullOrWhiteSpace(ma)) return (false, "Ma is required");
            var existing = await _repo.GetByIdAsync(ma);
            if (existing == null) return (false, "Not found");
            // update fields
            existing.TenLoaiDonVi = dto.TenLoaiDonVi;
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
