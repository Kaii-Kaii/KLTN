using System.Collections.Generic;
using System.Threading.Tasks;
using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Repositories;
using BE_QLTiemThuoc.Dto;
using System.Linq;

namespace BE_QLTiemThuoc.Services
{
    

    public class NhomLoaiService
    {
        private readonly NhomLoaiRepository _repo;

        public NhomLoaiService(NhomLoaiRepository repo)
        {
            _repo = repo;
        }

       
        public async Task<List<LoaiThuoc>> GetLoaiByNhomAsync(string maNhom)
        {
            return await _repo.GetLoaiByNhomAsync(maNhom);
        }

        public Task<NhomLoai?> GetByIdAsync(string maNhom)
        {
            return _repo.GetByIdAsync(maNhom);
        }

        public async Task<List<GroupWithLoaiDto>> GetGroupsWithLoaiAsync()
        {
            var groups = await _repo.GetAllAsync();
            var result = new List<GroupWithLoaiDto>();

            foreach (var g in groups)
            {
                var loai = await _repo.GetLoaiByNhomAsync(g.MaNhomLoai ?? string.Empty);
                var dto = new GroupWithLoaiDto
                {
                    MaNhomLoai = g.MaNhomLoai,
                    TenNhomLoai = g.TenNhomLoai,
                    Loai = loai.Select(l => new LoaiItemDto { MaLoaiThuoc = l.MaLoaiThuoc, TenLoaiThuoc = l.TenLoaiThuoc }).ToList()
                };
                result.Add(dto);
            }

            return result;
        }

        public async Task<(bool Ok, string? Error)> CreateAsync(NhomLoai dto)
        {
            if (dto == null) return (false, "Payload required");
            if (string.IsNullOrWhiteSpace(dto.MaNhomLoai)) return (false, "MaNhomLoai is required");
            var existing = await _repo.GetByIdAsync(dto.MaNhomLoai!);
            if (existing != null) return (false, "MaNhomLoai already exists");
            await _repo.AddAsync(dto);
            return (true, null);
        }

        public async Task<(bool Ok, string? Error)> UpdateAsync(string maNhom, NhomLoai dto)
        {
            if (string.IsNullOrWhiteSpace(maNhom)) return (false, "MaNhomLoai is required");
            var existing = await _repo.GetByIdAsync(maNhom);
            if (existing == null) return (false, "Not found");
            existing.TenNhomLoai = dto.TenNhomLoai;
            await _repo.UpdateAsync(existing);
            return (true, null);
        }

        public async Task<(bool Ok, string? Error)> DeleteAsync(string maNhom)
        {
            if (string.IsNullOrWhiteSpace(maNhom)) return (false, "MaNhomLoai is required");
            var existing = await _repo.GetByIdAsync(maNhom);
            if (existing == null) return (false, "Not found");
            await _repo.DeleteAsync(maNhom);
            return (true, null);
        }
    }
}
