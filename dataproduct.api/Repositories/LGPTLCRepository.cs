using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class LGPTLCRepository : ILGPTLCRepository
    {
        private readonly ProductFormContext _context;

        public LGPTLCRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<List<LG_NKVHPT_DuLieuAuto>> GetAutoDataAsync(int idLoCao, DateTime ngayVanHanh, Guid? idPhieu = null)
        {
            var tuThoiGian = ngayVanHanh.Date.AddHours(8);
            var denThoiGian = ngayVanHanh.Date.AddDays(1).AddHours(7).AddMinutes(59).AddSeconds(59);

            List<int>? existingHours = null;
            if (idPhieu.HasValue)
            {
                existingHours = await _context.LG_NKVHPT_ChiTiet
                    .AsNoTracking()
                    .Where(x => x.IDPhieu == idPhieu.Value)
                    .Select(x => x.ThoiGian.Hour)
                    .ToListAsync();
            }

            var query = _context.LG_NKVHPT_DuLieu
                .AsNoTracking()
                .Where(x =>
                    x.ID_LoCao == idLoCao &&
                    x.ThoiGian >= tuThoiGian &&
                    x.ThoiGian <= denThoiGian);

            if (existingHours != null && existingHours.Count > 0)
                query = query.Where(x => !existingHours.Contains(x.ThoiGian.Hour));

            return await query
                .OrderBy(x => x.ThoiGian)
                .Select(x => new LG_NKVHPT_DuLieuAuto
                {
                    Id = x.ID,
                    ThoiGian = x.ThoiGian,
                    IdCa = x.ThoiGian.Hour >= 8 && x.ThoiGian.Hour < 20 ? 1 : 2,
                    TrongLuongBonPhunThoi1_Auto = x.TrongLuongBonPhunThoi1,
                    TrongLuongBonPhunThoi2_Auto = x.TrongLuongBonPhunThoi2,
                    TrongLuongBonPhunThoi3_Auto = x.TrongLuongBonPhunThoi3,
                    YeuCauTuLoCao_Auto = x.YeuCauTuLoCao,
                    LuongThanPhunThucTe_Auto = x.LuongThanPhunThucTe,
                    LuyKeLuongPhunThanTrongCa_Auto = x.LuyKeLuongPhunThanTrongCa,
                    SoLuongSungPhun_Auto = x.SoLuongSungPhun
                })
                .ToListAsync();
        }

        public async Task<List<LGPTLCChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu)
        {
            return await _context.LG_NKVHPT_ChiTiet
                .AsNoTracking()
                .Where(x => x.IDPhieu == idPhieu)
                .OrderBy(x => x.ThoiGian)
                .Select(x => new LGPTLCChiTietDto
                {
                    Id = x.ID,
                    IdPhieu = x.IDPhieu,
                    SoPhieu = x.SoPhieu,
                    IdLoCao = x.ID_LoCao,
                    NgayVanHanh = x.NgayVanHanh,
                    IdCa = x.IDCa,
                    Kip = x.Kip,
                    ThoiGian = x.ThoiGian,
                    NhietDoSiloBotThan1_Auto = x.NhietDoSiloBotThan1_Auto,
                    NhietDoSiloBotThan1_Manual = x.NhietDoSiloBotThan1_Manual,
                    NhietDoSiloBotThan2_Auto = x.NhietDoSiloBotThan2_Auto,
                    NhietDoSiloBotThan2_Manual = x.NhietDoSiloBotThan2_Manual,
                    NhietDoBonPhunThoi1_Auto = x.NhietDoBonPhunThoi1_Auto,
                    NhietDoBonPhunThoi1_Manual = x.NhietDoBonPhunThoi1_Manual,
                    NhietDoBonPhunThoi2_Auto = x.NhietDoBonPhunThoi2_Auto,
                    NhietDoBonPhunThoi2_Manual = x.NhietDoBonPhunThoi2_Manual,
                    NhietDoBonPhunThoi3_Auto = x.NhietDoBonPhunThoi3_Auto,
                    NhietDoBonPhunThoi3_Manual = x.NhietDoBonPhunThoi3_Manual,
                    DongDienMayNghien_Auto = x.DongDienMayNghien_Auto,
                    DongDienMayNghien_Manual = x.DongDienMayNghien_Manual,
                    DongDienQuatGioNguoc_Auto = x.DongDienQuatGioNguoc_Auto,
                    DongDienQuatGioNguoc_Manual = x.DongDienQuatGioNguoc_Manual,
                    NhietDoDauVaoMayNghien_Auto = x.NhietDoDauVaoMayNghien_Auto,
                    NhietDoDauVaoMayNghien_Manual = x.NhietDoDauVaoMayNghien_Manual,
                    NhietDoDauRaMayNghien_Auto = x.NhietDoDauRaMayNghien_Auto,
                    NhietDoDauRaMayNghien_Manual = x.NhietDoDauRaMayNghien_Manual,
                    NhietDoKhoangLo_Auto = x.NhietDoKhoangLo_Auto,
                    NhietDoKhoangLo_Manual = x.NhietDoKhoangLo_Manual,
                    MucLieuSiloBotThan1_Auto = x.MucLieuSiloBotThan1_Auto,
                    MucLieuSiloBotThan1_Manual = x.MucLieuSiloBotThan1_Manual,
                    MucLieuSiloBotThan2_Auto = x.MucLieuSiloBotThan2_Auto,
                    MucLieuSiloBotThan2_Manual = x.MucLieuSiloBotThan2_Manual,
                    MucLieuSiloThanTho_Auto = x.MucLieuSiloThanTho_Auto,
                    MucLieuSiloThanTho_Manual = x.MucLieuSiloThanTho_Manual,
                    TrongLuongBonPhunThoi1_Auto = x.TrongLuongBonPhunThoi1_Auto,
                    TrongLuongBonPhunThoi1_Manual = x.TrongLuongBonPhunThoi1_Manual,
                    TrongLuongBonPhunThoi2_Auto = x.TrongLuongBonPhunThoi2_Auto,
                    TrongLuongBonPhunThoi2_Manual = x.TrongLuongBonPhunThoi2_Manual,
                    TrongLuongBonPhunThoi3_Auto = x.TrongLuongBonPhunThoi3_Auto,
                    TrongLuongBonPhunThoi3_Manual = x.TrongLuongBonPhunThoi3_Manual,
                    ApLucKhiThan_Auto = x.ApLucKhiThan_Auto,
                    ApLucKhiThan_Manual = x.ApLucKhiThan_Manual,
                    ApLucBonKhiN2_Auto = x.ApLucBonKhiN2_Auto,
                    ApLucBonKhiN2_Manual = x.ApLucBonKhiN2_Manual,
                    NhietDoTramDauBoiTron_Auto = x.NhietDoTramDauBoiTron_Auto,
                    NhietDoTramDauBoiTron_Manual = x.NhietDoTramDauBoiTron_Manual,
                    NhietDoStatoDongCoMayNghien_Auto = x.NhietDoStatoDongCoMayNghien_Auto,
                    NhietDoStatoDongCoMayNghien_Manual = x.NhietDoStatoDongCoMayNghien_Manual,
                    NhietDoTrucDongCoMayNghien_Auto = x.NhietDoTrucDongCoMayNghien_Auto,
                    NhietDoTrucDongCoMayNghien_Manual = x.NhietDoTrucDongCoMayNghien_Manual,
                    ApLucTrucNghien_Auto = x.ApLucTrucNghien_Auto,
                    ApLucTrucNghien_Manual = x.ApLucTrucNghien_Manual,
                    YeuCauTuLoCao_Auto = x.YeuCauTuLoCao_Auto,
                    YeuCauTuLoCao_Manual = x.YeuCauTuLoCao_Manual,
                    LuongThanPhunThucTe_Auto = x.LuongThanPhunThucTe_Auto,
                    LuongThanPhunThucTe_Manual = x.LuongThanPhunThucTe_Manual,
                    LuyKeLuongPhunThanTrongCa_Auto = x.LuyKeLuongPhunThanTrongCa_Auto,
                    LuyKeLuongPhunThanTrongCa_Manual = x.LuyKeLuongPhunThanTrongCa_Manual,
                    SoLuongSungPhun_Auto = x.SoLuongSungPhun_Auto,
                    SoLuongSungPhun_Manual = x.SoLuongSungPhun_Manual,
                    ApLucGioLanhLoCao_Auto = x.ApLucGioLanhLoCao_Auto,
                    ApLucGioLanhLoCao_Manual = x.ApLucGioLanhLoCao_Manual,
                    SanLuongNghien = x.SanLuongNghien,
                    SanLuongPhun = x.SanLuongPhun,
                    GhiChu = x.GhiChu,
                    TinhTrangSanXuat = x.TinhTrangSanXuat,
                    SapXepSanXuat = x.SapXepSanXuat,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task DeleteChiTietByPhieuAsync(Guid idPhieu)
        {
            await _context.LG_NKVHPT_ChiTiet
                .Where(x => x.IDPhieu == idPhieu)
                .ExecuteDeleteAsync();
        }

        public async Task AddChiTietRangeAsync(List<LG_NKVHPT_ChiTiet> entities)
        {
            if (entities.Count == 0) return;
            await _context.LG_NKVHPT_ChiTiet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateManualValuesAsync(UpdateLGPTLCManualDto dto)
        {
            if (dto.Items.Count == 0) return;

            var ids = dto.Items.Select(x => x.Id).ToList();
            var rows = await _context.LG_NKVHPT_ChiTiet
                .Where(x => x.IDPhieu == dto.IdPhieu && ids.Contains(x.ID))
                .ToListAsync();

            var lookup = rows.ToDictionary(r => r.ID);

            foreach (var item in dto.Items)
            {
                if (!lookup.TryGetValue(item.Id, out var row)) continue;

                row.NhietDoSiloBotThan1_Manual = item.NhietDoSiloBotThan1_Manual;
                row.NhietDoSiloBotThan2_Manual = item.NhietDoSiloBotThan2_Manual;
                row.NhietDoBonPhunThoi1_Manual = item.NhietDoBonPhunThoi1_Manual;
                row.NhietDoBonPhunThoi2_Manual = item.NhietDoBonPhunThoi2_Manual;
                row.NhietDoBonPhunThoi3_Manual = item.NhietDoBonPhunThoi3_Manual;
                row.DongDienMayNghien_Manual = item.DongDienMayNghien_Manual;
                row.DongDienQuatGioNguoc_Manual = item.DongDienQuatGioNguoc_Manual;
                row.NhietDoDauVaoMayNghien_Manual = item.NhietDoDauVaoMayNghien_Manual;
                row.NhietDoDauRaMayNghien_Manual = item.NhietDoDauRaMayNghien_Manual;
                row.NhietDoKhoangLo_Manual = item.NhietDoKhoangLo_Manual;
                row.MucLieuSiloBotThan1_Manual = item.MucLieuSiloBotThan1_Manual;
                row.MucLieuSiloBotThan2_Manual = item.MucLieuSiloBotThan2_Manual;
                row.MucLieuSiloThanTho_Manual = item.MucLieuSiloThanTho_Manual;
                row.TrongLuongBonPhunThoi1_Manual = item.TrongLuongBonPhunThoi1_Manual;
                row.TrongLuongBonPhunThoi2_Manual = item.TrongLuongBonPhunThoi2_Manual;
                row.TrongLuongBonPhunThoi3_Manual = item.TrongLuongBonPhunThoi3_Manual;
                row.ApLucKhiThan_Manual = item.ApLucKhiThan_Manual;
                row.ApLucBonKhiN2_Manual = item.ApLucBonKhiN2_Manual;
                row.NhietDoTramDauBoiTron_Manual = item.NhietDoTramDauBoiTron_Manual;
                row.NhietDoStatoDongCoMayNghien_Manual = item.NhietDoStatoDongCoMayNghien_Manual;
                row.NhietDoTrucDongCoMayNghien_Manual = item.NhietDoTrucDongCoMayNghien_Manual;
                row.ApLucTrucNghien_Manual = item.ApLucTrucNghien_Manual;
                row.YeuCauTuLoCao_Manual = item.YeuCauTuLoCao_Manual;
                row.LuongThanPhunThucTe_Manual = item.LuongThanPhunThucTe_Manual;
                row.LuyKeLuongPhunThanTrongCa_Manual = item.LuyKeLuongPhunThanTrongCa_Manual;
                row.SoLuongSungPhun_Manual = item.SoLuongSungPhun_Manual;
                row.ApLucGioLanhLoCao_Manual = item.ApLucGioLanhLoCao_Manual;
                row.GhiChu = item.GhiChu;
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdatePhieuSummaryAsync(Guid idPhieu, UpdateLGPTLCSummaryDto dto)
        {
            await _context.LG_NKVHPT_ChiTiet
                .Where(x => x.IDPhieu == idPhieu)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.SanLuongNghien, dto.SanLuongNghien)
                    .SetProperty(x => x.SanLuongPhun, dto.SanLuongPhun)
                    .SetProperty(x => x.TinhTrangSanXuat, dto.TinhTrangSanXuat)
                    .SetProperty(x => x.SapXepSanXuat, dto.SapXepSanXuat));
        }

        public async Task<BmPhieu?> GetPhieuByIdAsync(Guid idPhieu)
        {
            return await _context.BmPhieus
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Idphieu == idPhieu);
        }
    }
}
