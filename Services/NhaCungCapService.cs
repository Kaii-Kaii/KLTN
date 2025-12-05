using System.Collections.Generic;
using System.Threading.Tasks;
using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Repositories;
namespace BE_QLTiemThuoc.Services
{
    // Single-file service implementation (no interface as requested)
    public class NhaCungCapService
    {
        private readonly NhaCungCapRepository _repo;

        public NhaCungCapService(NhaCungCapRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<NhaCungCap>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<NhaCungCap> GetByIdAsync(string id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) throw new Exception("Không tìm thấy nhà cung cấp.");
            return item;
        }

        public async Task<NhaCungCap> CreateAsync(NhaCungCap ncc)
        {
            // Business rules: ensure MaNCC not exists
            var existing = await _repo.GetByIdAsync(ncc.MaNCC);
            if (existing != null) throw new Exception("Mã nhà cung cấp đã tồn tại.");
            await _repo.CreateAsync(ncc);
            return ncc;
        }

        public async Task<NhaCungCap> UpdateAsync(string id, NhaCungCap ncc)
        {
            if (id != ncc.MaNCC) throw new Exception("Mã nhà cung cấp không khớp.");
            await _repo.UpdateAsync(ncc);
            return ncc;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                return await _repo.DeleteAsync(id);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 547) // Foreign key constraint violation
            {
                throw new Exception("Nhà cung cấp đang được sử dụng.");
            }
        }
    }
}
