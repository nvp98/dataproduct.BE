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
        public async Task<List<SanLuongPhoiDto>> GetSanLuongPhoiAsync( string ca,string kip,DateTime ngaySX)
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
        public async Task<List<PhoinhapkhoDto>> GetPhoiNhapKhoAsync(string ca,string kip,DateTime ngaySX, int mayduc)
        {
            return await _context.Database
             .SqlQueryRaw<PhoinhapkhoDto>(
                 "EXEC sp_CTD_GetPhoiNhapKho_ByKipNgay @p_Kip, @p_NgaySX, @p_Ca,@p_MayDuc",
                 new SqlParameter("@p_Kip", kip),
                 new SqlParameter("@p_NgaySX", ngaySX),
                 new SqlParameter("@p_Ca", ca),
                 new SqlParameter("@p_MayDuc", mayduc)
             )
         .ToListAsync();
        }

        public async Task<List<InsertSanLuongPhoiDto>> GetSanLuongPhoiChiTietAsync(int ca,string kip,DateTime ngaySX, int? mayDuc = null,Guid? idPhieu = null)
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
                    TTHD =true
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
                .Select(x => new InsertPhoiNhapKhoDto
                {
                    Me = x.Me,
                    Mac = x.Mac,
                    KichThuoc = x.KichThuoc,

                    StLoai1 = x.StLoai1,
                    KlLoai1 = x.KlLoai1,

                    StLoai2 = x.StLoai2,
                    KlLoai2 = x.KlLoai2,


                    StLoai2TP = x.StLoai2TP,
                    KlLoai2TP = x.KlLoai2TP,

                    StPhoiNgan = x.StPhoiNgan,
                    KlPhoiNgan = x.KlPhoiNgan,
                    CdPhoiNgan =x.CdPhoiNgan,

                    StLoai3 = x.StLoai3,
                    KlLoai3 = x.KlLoai3,

                    TongSoThanh = x.TongSoThanh,
                    TongKhoiLuong = x.TongKhoiLuong
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