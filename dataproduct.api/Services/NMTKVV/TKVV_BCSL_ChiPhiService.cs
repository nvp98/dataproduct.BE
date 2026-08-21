using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Repositories.NMTKVV;

namespace dataproduct.api.Services.NMTKVV
{
    public class TKVV_BCSL_ChiPhiService
    {
        private readonly ITKVV_BCSL_ChiPhiRepository _repo;

        public TKVV_BCSL_ChiPhiService(ITKVV_BCSL_ChiPhiRepository repo)
        {
            _repo = repo;
        }

        public Task<List<TKVVGiaTriNVLAutoDto>> GetGiaTriNVLAutoAsync(
            DateTime ngay, int ca, int scope, string maBM)
            => _repo.GetGiaTriNVLAutoAsync(ngay, ca, scope.ToString(), maBM);

        public Task<List<TKVVDuLieuCanDto>> GetDuLieuCanAsync(
            DateTime ngay, int ca, string maBM, string loaiDuLieu, int scope)
            => _repo.GetDuLieuCanAsync(ngay, ca, maBM, loaiDuLieu, scope);
    }
}
