using dataproduct.api.DTOs;
using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace dataproduct.api.Repositories
{
    public interface IBKNguyenLieuRepository
    {
        Task<IEnumerable<BkNguyenLieu>> GetAllAsync(DateOnly? NgaySX, int? Ca, string? Kip);
        Task<BkNguyenLieu?> GetByIdAsync(int id);
        Task AddAsync(BkNguyenLieu entity);
        Task UpdateAsync(BkNguyenLieu entity);
        Task DeleteAsync(int id);
    }
    public interface IPhieuRepository
    {
        Task<IEnumerable<BmPhieu>> GetAllAsync(string? MaBM, int? NguoiTaoID);
        Task<BmPhieu?> GetByIdAsync(Guid id);
        Task<BmPhieu> AddAsync([FromBody] JsonElement formData);
        Task UpdateAsync(BmPhieu entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);

        Task<bool> CheckExistsAsync(string maBm, DateOnly ngaySX, int ca, int? scope, int? mayduc);
        Task<(IEnumerable<SearchPhieuResponseModel> Data, int TotalCount)> SearchWithPagingAsync(SearchPhieuRequest request);
    }
    public interface IBMPheDuyetRepository
    {
        Task<IEnumerable<BmPheDuyet>> GetAllAsync(int? NguoiDuyetID, int? isCheckDuyet);
        Task<BmPheDuyet> GetByIdAsync(int? id);
        Task<IEnumerable<BmPheDuyet>?> GetByIdPhieuAsync(Guid id);
        Task AddAsync(BmPheDuyet entity);
        Task UpdateAsync(BmPheDuyet entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task AddListAsync(List<BmPheDuyet> pheDuyetList, Guid idphieu);
        Task<bool> UpdateTinhTrangAsync(Guid phieuId, int nguoiDuyetId, int tinhTrang);
    }
    public interface IBKPhoiThepRepository
    {
        Task<IEnumerable<BkPhoiThep>> GetAllAsync(DateOnly? NgaySX, DateOnly? TuNgay, DateOnly? DenNgay, int? Ca, string? Kip, int? LoaiPhoi, int? MayDuc);
        Task<BkPhoiThep?> GetByIdAsync(int id);
        Task AddAsync(BkPhoiThep entity);
        Task UpdateAsync(BkPhoiThep entity);
        Task<bool> UpdateStDaChuyenAsync(int id, int stDaChuyen);
        Task<int> UpdateStDaChuyenRangeAsync(List<BKPhoiThepStUpdate> items);
        Task<int?> RevokeStDaChuyenAsync(int id, int soThuHoi);
        Task<int> RevokeStDaChuyenRangeAsync(List<BKPhoiThepStRevoke> items);
        Task DeleteAsync(int id);
    }
    public interface IDLNMHRC2Repository
    {
        Task<IEnumerable<DLNM_HRC2>> GetAllAsync(DateTime? NgaySX, int? Ca, string? BieuMau, int? Scope);
        Task<DLNM_HRC2?> GetByIdAsync(long id);
        Task<HRC2DetailByReportNoModel?> GetByReportNoAsync(int reportNo);
        Task<HRC2GroupedByReportNoModel?> GetByReportNoGroupedAsync(int reportNo);
        Task<HRC2GroupedByReportNoModel?> GetByMeThoiGroupedAsync(string meThoi);
        Task<HRC2GroupedByReportNoModel?> GetByIdGroupedAsync(int id);
        Task AddAsync(DLNM_HRC2 entity);
        Task UpdateAsync(DLNM_HRC2 entity);
        Task DeleteAsync(long id);
        Task<(IEnumerable<DLNM_HRC2> Data, int TotalCount)> SearchWithPagingAsync(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, string? searchText, int page, int pageSize);
        Task<bool> ChuyenMeThoiAsync(ChuyenMeThoiRequest request);
        Task<IEnumerable<FilterSTD_NXTResponse>> GetHRC2GroupedByMaterialAsync(DateTime ngaySX, int ca);
    }
    public interface IHeaderKeyRepository
    {
        Task<IEnumerable<Header_Key>> GetAllAsync();
        Task<Header_Key?> GetByIdAsync(int id);
        Task AddAsync(Header_Key entity);
        Task UpdateAsync(Header_Key entity);
        Task DeleteAsync(int id);
        Task<(IEnumerable<HeaderKey_ResponseModel> Data, int TotalCount)> SearchWithPagingAsync(string? searchKey, string? LoaiPhieu, int page, int pageSize);
        Task<(IEnumerable<HeaderKeyMapping_ResponseModel> Data, int TotalCount)> SearchMappingsWithPagingAsync(
            string? searchKey,
            string? LoaiPhieu,
            string? TrangThai,
            bool? IsUsedNXT,
            DateTime? FromDate,
            DateTime? ToDate,
            string? SortThuTu,
            int page,
            int pageSize
        );
        Task<bool> ExistsByTenHienThiAsync(string tenHienThi, int? excludeId = null);
        Task<bool> IsInUseAsync(int id);
    }
    public interface IHeaderMappingRepository
    {
        Task<Header_Mapping?> GetByIdAsync(int id);
        Task<Header_Mapping> AddAsync(Header_Mapping entity);
        Task UpdateAsync(Header_Mapping entity);
        Task DeleteAsync(int id);
        Task DeleteByHeaderKeyAsync(int headerKeyId);
        Task<bool> ExistsAsync(int phuLieuId, int headerKeyId, int? excludeId = null);
    }
    public interface ISTD_NXT_HRC2Repository
    {
        Task<STD_NXT_HRC2_UpsertResponse> UpsertAsync(STD_NXT_HRC2_UpsertDto entity);
        Task InitializeHRC2_STD_NXTAsync(BmPhieu phieu);
        Task<STD_NXT_HRC2_GetDetailResponse> GetByPhieuIdAsync(Guid phieuId);
        Task<bool> PhanBoAsync(STD_NXT_HRC2_PhanBoDto entity);
        // Task<STD_NXT_HRC2_GetDetailResponse> GetByIdAsync(Guid idPhieu);
        // Task<STD_NXT_HRC2_GetDetailResponse> FilterAsync(DateTime ngaySX, int ca);
    }
    public interface ICtdPhoiNongRepository
    {
        Task<IEnumerable<CtdPhoiNong>> GetAllAsync(DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me);
        Task<CtdPhoiNong?> GetByIdAsync(int id);
        Task<IEnumerable<CtdPhoiNong>> GetByPhieuIdAsync(Guid phieuId);
        Task AddAsync(CtdPhoiNong entity);
        Task AddListAsync(List<CtdPhoiNong> entities);
        Task UpdateAsync(CtdPhoiNong entity);
        Task<int> UpdateStatusRangeAsync(List<CtdPhoiNongStatusUpdate> items);
        Task<int> UpdateStatusDone(DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me, int? status);
        Task DeleteAsync(int id);
        Task<(int Created, int Updated)> UpsertListAsync(List<CtdPhoiNong> entities);
    }
    //  Begin NM CTD 
    public interface ICtdBMDucCTDRepository
    {
        Task<List<SanLuongPhoiDto>> GetSanLuongPhoiAsync(string ca, string kip, DateTime ngaySX);
        Task<List<PhoinhapkhoDto>> GetPhoiNhapKhoAsync(string ca, string kip, DateTime ngaySX, int mayduc);
        Task<List<SanLuongPhoiDto>> GetSanLuongPhoiAsync(int ca, string kip, DateTime ngaySX, int? mayDuc = null, Guid? idPhieu = null);
        Task<List<BM_SanLuongPhoi>> InsertSanLuongPhoiAsync(List<BM_SanLuongPhoi> entity);
        Task DeleteByPhieuAsync(Guid idPhieu);

    }
    //  End NM CTD 
    public interface IBmQuyenXlRepository
    {
        Task<IEnumerable<BmQuyenXl>> GetAllAsync(int? idTaiKhoan, string? maBm, string? maKhuVuc);
        Task<BmQuyenXl?> GetByIdAsync(int id);
        Task AddAsync(BmQuyenXl entity);
        Task UpdateAsync(BmQuyenXl entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<IEnumerable<BmQuyenXl>> GetByTaiKhoanIdAsync(int idTaiKhoan);
        Task<bool> CheckDuplicateAsync(int? idTaiKhoan, string? maBm, string? maKhuVuc, int? excludeId = null);
    }

    //  Begin láy thông tin người và chữ ký 

    public interface IPheDuyetRepository
    {
        Task<List<BmPheDuyet>> GetBmPheDuyetByPhieuIdAsync(Guid phieuId);
        Task<List<TaiKhoan>> GetTaiKhoanByIdsAsync(List<int> ids);
        Task<List<PhongBan>> GetAllPhongBanAsync();
        Task<List<ViTri>> GetAllViTriAsync();
    }

    // End
    public interface ISiloRepository
    {
        Task<IEnumerable<Silo>> GetAllAsync();
        Task<Silo?> GetByIdAsync(int id);
        Task AddAsync(Silo entity);
        Task UpdateAsync(Silo entity);
        Task DeleteAsync(int id);
        Task<(IEnumerable<Silo> Data, int TotalCount)> SearchMappingsWithPagingAsync(
            string? searchKey,
            int? scope,
            bool? tinhTrang,
            string? BieuMau,
            int? nhaMay,
            int page,
            int pageSize
        );
        Task<bool> ExistsByTenSiloAsync(string tenSilo, int nhaMay, string bieuMau, int? scope, int? excludeId = null);
        Task<List<SiloWithPhuLieuNmDto>> GetValidSilosAsync(
                List<int> phuLieuNMIds,
                DateTime ngaySX
            );
        Task<List<int>> GetPhuLieuNMInSiloAsync(
            int siloId,
            DateTime ngaySX
        );
        Task<bool> IsSiloContainPhuLieuNMAsync(
            int siloId,
            int phuLieuNMId,
            DateTime ngaySX
        );
        // MapSiloPhuLieuNM methods
        Task<List<MapSiloPhuLieuNMResponse>> GetMappingsBySiloIdAsync(int siloId);
        Task<MapSiloPhuLieuNM?> GetMappingByIdAsync(int id);
        Task<MapSiloPhuLieuNM> AddMappingAsync(MapSiloPhuLieuNM entity);
        Task UpdateMappingAsync(MapSiloPhuLieuNM entity);
        Task DeleteMappingAsync(int id);
        Task<List<PhuLieu_NM>> GetAllPhuLieuNMAsync();
        Task<(IEnumerable<PhuLieu_NM> Data, int TotalCount)> SearchPhuLieuNMWithPagingAsync(
            string? searchKey,
            int page,
            int pageSize
        );
        Task<List<DTOs.SiloByHeaderKeyDto>> GetSilosByHeaderKeyAsync(
            int headerKeyId,
            DateTime ngaySX,
            int nhaMay,
            string? bieuMau
        );
    }
}
