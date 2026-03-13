using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;

namespace dataproduct.api.Repositories
{
    public class CtdPhoiNongRepository : ICtdPhoiNongRepository
    {
        private readonly ProductFormContext _context;

        public CtdPhoiNongRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CtdPhoiNong>> GetAllAsync(DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me)
        {
            var query = _context.CtdPhoiNongs.AsQueryable();

            if (NgaySX.HasValue)
                query = query.Where(x => x.NgaySx == NgaySX);

            if (Ca.HasValue)
                query = query.Where(x => x.Ca == Ca.Value);

            if (!string.IsNullOrEmpty(Kip))
                query = query.Where(x => x.Kip == Kip);
            if (Xuong.HasValue)
                query = query.Where(x => x.NmCan == Xuong.Value);
            if (!string.IsNullOrEmpty(Me))
                query = query.Where(x => x.Me == Me);

            var results = await query.ToListAsync();

            var bkIds = results
                .Where(x => x.IdBkPhoiThep.HasValue)
                .Select(x => x.IdBkPhoiThep!.Value)
                .Distinct()
                .ToList();

            if (bkIds.Count > 0)
            {
                var bkMap = await _context.BkPhoiThep
                    .Where(x => bkIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.NgaySx);

                foreach (var item in results)
                {
                    if (item.IdBkPhoiThep.HasValue && bkMap.TryGetValue(item.IdBkPhoiThep.Value, out var ngayDuc))
                        item.NgayDuc = ngayDuc;
                }
            }

            return results;
        }

        public async Task<CtdPhoiNong?> GetByIdAsync(int id)
        {
            return await _context.CtdPhoiNongs.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<CtdPhoiNong>> GetByPhieuIdAsync(Guid phieuId)
        {
            return await _context.CtdPhoiNongs
                .Where(x => x.Idphieu == phieuId)
                .ToListAsync();
        }

        public async Task AddAsync(CtdPhoiNong entity)
        {
            _context.CtdPhoiNongs.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddListAsync(List<CtdPhoiNong> entities)
        {
            await _context.CtdPhoiNongs.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CtdPhoiNong entity)
        {
            _context.CtdPhoiNongs.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<int> UpdateStatusRangeAsync(List<CtdPhoiNongStatusUpdate> items)
        {
            var ids = items.Select(x => x.Id).ToList();
            var map = items.ToDictionary(x => x.Id);
            var entities = await _context.CtdPhoiNongs.Where(e => ids.Contains(e.Id)).ToListAsync();
            foreach (var e in entities)
            {
                var m = map[e.Id];
                if (m.TinhTrangCTD.HasValue) e.TinhTrangCTD = m.TinhTrangCTD;
                if (m.TinhTrangQLCL.HasValue) e.TinhTrangQLCL = m.TinhTrangQLCL;
            }
            await _context.SaveChangesAsync();
            return entities.Count;
        }

        public async Task<int> UpdateStatusDone(DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me, int? status)
        {
            var query = _context.CtdPhoiNongs.Where(x => x.NgaySx == NgaySX && x.Ca == Ca && x.NmCan == Xuong).AsQueryable();

            //if (NgaySX.HasValue)
            //    query = query.Where(x => x.NgaySx == NgaySX);

            //if (Ca.HasValue)
            //    query = query.Where(x => x.Ca == Ca.Value);

            if (!string.IsNullOrEmpty(Kip))
                query = query.Where(x => x.Kip == Kip);
            //if (Xuong.HasValue)
            //    query = query.Where(x => x.NmCan == Xuong.Value);
            if (!string.IsNullOrEmpty(Me))
                query = query.Where(x => x.Me == Me);

            // Xác định trạng thái: 0 = hủy chốt, 1 = chốt
            var statusValue = status ?? 1;

            // 🔥 UPDATE ALL MATCHED ROWS
            var affectedRows = await query.ExecuteUpdateAsync(setters =>
                setters.SetProperty(x => x.TinhTrang, statusValue)
            //.SetProperty(x => x.NgayCapNhat, DateTime.Now) // nếu có
            );

            return affectedRows;
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.CtdPhoiNongs.FindAsync(id);
            if (item != null)
            {
                _context.CtdPhoiNongs.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(int Created, int Updated)> UpsertListAsync(List<CtdPhoiNong> entities)
        {
            if (entities == null || entities.Count == 0) return (0, 0);

            var created = 0;
            var updated = 0;

            foreach (var model in entities)
            {
                var existing = await _context.CtdPhoiNongs
                    .FirstOrDefaultAsync(x => x.NgaySx == model.NgaySx && x.Me == model.Me && x.Kip == model.Kip);

                if (existing == null)
                {
                    _context.CtdPhoiNongs.Add(model);
                    created++;
                }
                else if (existing.TinhTrangCTD != 1 && existing.TinhTrangCTD != 1) // khi mẻ chưa được xử lý thì được chuyển vào thêm 
                {
                    existing.NgaySx = model.NgaySx;
                    existing.Ca = model.Ca;
                    existing.Kip = model.Kip;
                    existing.Mac = model.Mac;
                    existing.KichThuoc = model.KichThuoc;
                    existing.NmCan = model.NmCan;
                    existing.SoThanhLoai1 = existing.SoThanhLoai1 + model.SoThanhLoai1;
                    existing.KhoiLuongLoai1 = existing.KhoiLuongLoai1 + model.KhoiLuongLoai1;
                    existing.SoThanhLoai2 = existing.SoThanhLoai2 + model.SoThanhLoai2;
                    existing.KhoiLuongLoai2 = existing.KhoiLuongLoai2 + model.KhoiLuongLoai2;
                    existing.SoThanhLoai3 = existing.SoThanhLoai3 + model.SoThanhLoai3;
                    existing.KhoiLuongLoai3 = existing.KhoiLuongLoai3 + model.KhoiLuongLoai3;
                    existing.TongSt = existing.TongSt + model.TongSt;
                    existing.TongKl = existing.TongKl + model.TongKl;
                    existing.CaKip = model.CaKip;
                    existing.IdBkPhoiThep = model.IdBkPhoiThep;
                    existing.TinhTrang = model.TinhTrang;
                    existing.GhiChu = model.GhiChu;
                    existing.NgayTao = model.NgayTao;
                    existing.TinhTrangCTD = model.TinhTrangCTD;
                    existing.TinhTrangQLCL = model.TinhTrangQLCL;
                    updated++;
                }
            }

            await _context.SaveChangesAsync();
            return (created, updated);
        }
    }
}