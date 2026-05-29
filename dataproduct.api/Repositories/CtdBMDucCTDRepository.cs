using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using static dataproduct.api.DTOs.CTD_Dto.PhoinhapkhoDto;

namespace dataproduct.api.Repositories
{
    public class CtdBMDucCTDRepository : ICtdBMDucCTDRepository
    {
        private readonly ProductFormContext _context;

        public CtdBMDucCTDRepository(ProductFormContext context)
        {
            _context = context;
        }
        public async Task<List<SanLuongPhoiDto>> GetSanLuongPhoiAsync(string ca, string kip, DateTime ngaySX)
        {
            return await _context.Database
             .SqlQueryRaw<SanLuongPhoiDto>(
                 "EXEC sp_CTD_GetSanLuongPhoi_ByKipNgay @p_Kip, @p_NgaySX, @p_Ca",
                 new SqlParameter("@p_Kip", kip),
                 new SqlParameter("@p_NgaySX", ngaySX),
                 new SqlParameter("@p_Ca", ca)
             )
         .ToListAsync();
        }
        public async Task<List<PhoinhapkhoNhanPhoiDto>> GetPhoiNhapKhoAsync(string ca, string kip, DateTime ngaySX, int mayduc)
        {
            var result = await _context.Database
             .SqlQueryRaw<PhoinhapkhoNhanPhoiDto>(
                 "EXEC sp_CTD_GetPhoiNhapKho_ByKipNgay @p_Kip, @p_NgaySX, @p_Ca,@p_MayDuc",
                 new SqlParameter("@p_Kip", kip),
                 new SqlParameter("@p_NgaySX", ngaySX),
                 new SqlParameter("@p_Ca", ca),
                 new SqlParameter("@p_MayDuc", mayduc)
             )
         .ToListAsync();

            // Kiểm tra và set isCaTruoc
            var currentNgaySX = DateOnly.FromDateTime(ngaySX);
            var currentCa = int.Parse(ca);
            foreach (var item in result)
            {
                if (item.NgaySX != currentNgaySX || item.Ca != currentCa)
                {
                    item.isCaTruoc = true;
                }
                else
                {
                    item.isCaTruoc = false;
                }
                // tính toán ST đã chuyển trong ca
                if (item.isCaTruoc == true)
                {
                    item.ST_CaTruocChuyen = item.TongST_DaChuyen.GetValueOrDefault(0);
                    item.ST_NhapTrongCa = 0;
                    item.ST_CaSauChuyen = item.ST_CaTruocChuyen != 0 ? item.TongSoThanh - item.ST_CaTruocChuyen.GetValueOrDefault(0) : 0;
                }
                else
                {
                    item.ST_CaTruocChuyen = 0;
                    item.ST_NhapTrongCa = item.TongST_DaChuyen.GetValueOrDefault(0);
                    item.ST_CaSauChuyen = item.ST_NhapTrongCa != 0 ? item.TongSoThanh - item.ST_NhapTrongCa : 0;
                }
                item.TinhTrang_Chuyen = item.TongSoThanh - item.TongST_DaChuyen.GetValueOrDefault(0) == 0 ? 1 : 0;
            }

            return result;
        }

        public async Task<List<PhoinhapkhoNhanPhoiDto>> GetPhoiNhapKhoExportRangeAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            return await _context.Database
             .SqlQueryRaw<PhoinhapkhoNhanPhoiDto>(
                 "EXEC sp_CTD_GetPhoiNhapKho_ByExportRange @FromDate, @ToDate",
                 new SqlParameter("@FromDate", fromDate),
                 new SqlParameter("@ToDate", toDate)
             )
         .ToListAsync();
        }

        public async Task<List<InsertSanLuongPhoiDto>> GetSanLuongPhoiChiTietAsync(int ca, string kip, DateTime ngaySX, int? mayDuc = null, Guid? idPhieu = null)
        {
            var query = _context.BM_SanLuongPhoi
                .AsNoTracking()
                .AsQueryable();

            // 1️⃣ Ưu tiên theo IdPhieu (đã chốt)
            if (idPhieu.HasValue)
            {
                query = query.Where(x => x.IdPhieu == idPhieu.Value);
            }
            else
            {
                // 2️⃣ Fallback theo Ngày + Ca + Kíp
                query = query.Where(x =>
                    x.NgaySX.Date == ngaySX.Date &&
                    x.Ca == ca &&
                    x.Kip == kip
                );
            }

            if (mayDuc.HasValue)
            {
                query = query.Where(x => x.MayDuc == mayDuc.Value);
            }

            return await query
                .OrderBy(x => x.MacThep)
                .ThenBy(x => x.KichThuoc)
                .Select(x => new InsertSanLuongPhoiDto
                {
                    KipNgay = $"{x.Ca}{x.Kip}-{x.NgaySX:dd/MM/yyyy}",
                    MacThep = x.MacThep,
                    KichThuoc = x.KichThuoc,

                    StLoai1 = x.StLoai1,
                    KlLoai1 = x.KlLoai1,

                    StPhoiNgan = x.StPhoiNgan,
                    KlPhoiNgan = x.KlPhoiNgan,

                    StLoai2 = x.StLoai2,
                    KlLoai2 = x.KlLoai2,

                    StLoai3 = x.StLoai3,
                    KlLoai3 = x.KlLoai3,

                    TongSoThanh = x.TongSoThanh,
                    TongKhoiLuong = x.TongKhoiLuong,
                    TTHD = true
                })
                .ToListAsync();
        }


        public async Task AddSanLuongPhoiListAsync(List<BM_SanLuongPhoi> entities)
        {
            await _context.BM_SanLuongPhoi.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteSanLuongPhoiByPhieuAsync(Guid idPhieu)
        {
            var entities = await _context.BM_SanLuongPhoi
            .Where(x => x.IdPhieu == idPhieu)
            .ToListAsync();

            if (!entities.Any())
                return;

            _context.BM_SanLuongPhoi.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<List<InsertPhoiNhapKhoDto>> GetPhoiNhapKhoChiTietAsync(int ca, string kip, DateTime ngaySX, int? mayDuc = null, Guid? idPhieu = null)
        {
            var query = _context.BM_PhoiNhapKho
                .AsNoTracking()
                .AsQueryable();

            // // 1️⃣ Ưu tiên theo IdPhieu (đã chốt)
            // if (idPhieu.HasValue)
            // {
            //     query = query.Where(x => x.IdPhieu == idPhieu.Value);
            // }
            // else
            // {

            // }
            // 2️⃣ Fallback theo Ngày + Ca + Kíp
            query = query.Where(x =>
                x.NgaySX.Date == ngaySX.Date &&
                x.Ca == ca
            );
            // check BM_Phieu -> lay thong tin mayDuc de filter
            if (idPhieu.HasValue)
            {
                var phieu = await _context.BmPhieus.FirstOrDefaultAsync(p => p.Idphieu == idPhieu.Value);
                if (phieu != null)
                {
                    int phieuMayDuc = phieu.MayDuc.GetValueOrDefault(0);
                    query = query.Where(x => x.MayDuc == phieuMayDuc);
                }
            }

            // if (mayDuc.HasValue)
            // {
            //     query = query.Where(x => x.MayDuc == mayDuc.Value);
            // }

            // join voi table BK_PhoiThep để lấy ngaySX

            return await query
                .Select(x => new InsertPhoiNhapKhoDto
                {
                    Me = x.Me,
                    Mac = x.Mac,
                    KichThuoc = x.KichThuoc,
                    NgayGiao = x.NgaySX,
                    NgaySX = _context.BkPhoiThep.Where(pt => pt.Me == x.Me && pt.PhanLoaiPhoi == 2).Select(pt => pt.NgaySx).FirstOrDefault().ToDateTime(new TimeOnly(0, 0)), // lay tu bang 

                    StLoai1 = x.StLoai1,
                    KlLoai1 = x.KlLoai1,

                    StLoai2 = x.StLoai2,
                    KlLoai2 = x.KlLoai2,


                    StLoai2TP = x.StLoai2TP,
                    KlLoai2TP = x.KlLoai2TP,

                    StPhoiNgan = x.StPhoiNgan,
                    KlPhoiNgan = x.KlPhoiNgan,
                    CdPhoiNgan = x.CdPhoiNgan,

                    StLoai3 = x.StLoai3,
                    KlLoai3 = x.KlLoai3,

                    TongSoThanh = x.TongSoThanh,
                    TongKhoiLuong = x.TongKhoiLuong,
                    TinhTrangCap0 = x.TinhTrangCap0,
                    TinhTrangCap1 = x.TinhTrangCap1,
                    TinhTrangCap2 = x.TinhTrangCap2
                })
                .ToListAsync();
        }

        public async Task AddPhoiNhapKhoListAsync(List<BM_PhoiNhapKho> entities)
        {
            await _context.BM_PhoiNhapKho.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }
        public async Task DeletePhoiNhapKhoByPhieuAsync(Guid idPhieu)
        {
            var entities = await _context.BM_PhoiNhapKho
            .Where(x => x.IdPhieu == idPhieu)
            .ToListAsync();

            if (!entities.Any())
                return;

            _context.BM_PhoiNhapKho.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
        public async Task<List<BmPhieu>> GetDataAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var query = _context.BmPhieus
                .Where(x => x.IsDelete == 0 &&
                            x.MaBm == "HRC1_BB_GiaoNhanPhoiNhapKho")
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(x => x.NgaySX >= fromDate);

            if (toDate.HasValue)
                query = query.Where(x => x.NgaySX <= toDate);

            query = query.Where(p => !_context.BmPhieus.Any(c => c.ID_PhieuGoc == p.Idphieu));

            return await query.ToListAsync();
        }

        public async Task<List<BmPhieu>> GetDataSanLuongPhoiAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var query = _context.BmPhieus
                .AsNoTracking()
                .Where(x => x.IsDelete == 0 &&
                            x.MaBm == "HRC1_BB_Sanluongphoi")
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(x => x.NgaySX >= fromDate);

            if (toDate.HasValue)
                query = query.Where(x => x.NgaySX <= toDate);

            query = query.Where(p => !_context.BmPhieus.Any(c => c.ID_PhieuGoc == p.Idphieu));


            return await query.ToListAsync();
        }
    }
}