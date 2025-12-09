using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Repositories;

namespace BE_QLTiemThuoc.Services
{
    public class MaService
    {
        private readonly MaRepository _repo;

        public MaService(MaRepository repo)
        {
            _repo = repo;
        }

        public async Task<Ma?> GetByCodeAsync(string code)
        {
            return await _repo.GetByCodeAsync(code);
        }

        public async Task<List<Ma>> SearchByCodeAsync(string code)
        {
            return await _repo.SearchByCodeAsync(code);
        }

        public async Task<Ma?> GetByMaAsync(string maCode)
        {
            return await _repo.GetByMaAsync(maCode);
        }

        public async Task<List<Ma>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }
    }
}
