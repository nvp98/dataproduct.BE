using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class BBGN_ThepLongRepository : IBBGN_ThepLongRepository
    {
        private readonly ProductFormContext _context;

        public BBGN_ThepLongRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task XuLyDuLieuMeThoiGangLongAsync(List<string> data, FetchMeThoiRequest request)
        {
            // listNew có thể null/empty → coi như toRemove = toàn bộ currentSet
            var listNew = data ?? new List<string>();
            var maBm = request.NhaMay == (int)NhaMay.HRC1 ? "HRC1_BBGN_ThepLong" : "HRC2_BBGN_ThepLong";

            // Lấy danh sách phiếu ứng với NgaySX/Ca/scope cho biểu mẫu BBGN thép lỏng
            IQueryable<BmPhieu> phieuQuery = _context.BmPhieus
                .Where(x => x.NgaySX == request.NgaySX && x.Ca == request.Ca && x.MaBm == maBm);

            if (request.NhaMay == (int)NhaMay.HRC1 || request.NhaMay == (int)NhaMay.HRC2)
            {
                phieuQuery = phieuQuery.Where(x => x.Scope == request.NhaMay);
            }

            var phieus = await phieuQuery
                .Select(x => new { x.Idphieu, x.TinhTrang })
                .ToListAsync();

            if (!phieus.Any())
            {
                return;
            }

            // HashSet nguồn mới (unique MaMeThoi, bỏ null/rỗng)
            var newSet = new HashSet<string>(
                listNew.Where(s => !string.IsNullOrWhiteSpace(s)),
                StringComparer.OrdinalIgnoreCase);

            // Lấy toàn bộ bản ghi BBGN_ThepLong cho các phiếu liên quan trong 1 query
            var phieuIds = phieus.Select(p => p.Idphieu).ToList();

            var currentRows = await _context.BBGN_ThepLongs
                .Where(x => phieuIds.Contains(x.IdPhieu))
                .ToListAsync();

            // Group theo IdPhieu để xử lý từng phiếu (đảm bảo tuân thủ TinhTrang từng phiếu)
            foreach (var phieu in phieus)
            {
                var rowsOfPhieu = currentRows
                    .Where(r => r.IdPhieu == phieu.Idphieu)
                    .ToList();

                var currentSet = new HashSet<string>(
                    rowsOfPhieu
                        .Select(r => r.Me)
                        .Where(s => !string.IsNullOrWhiteSpace(s))!,
                    StringComparer.OrdinalIgnoreCase);

                // Bước 2: Nếu DB chưa có dữ liệu cho phiếu này
                if (!rowsOfPhieu.Any())
                {
                    foreach (var meThoi in newSet)
                    {
                        _context.BBGN_ThepLongs.Add(new BBGN_ThepLong
                        {
                            IdPhieu = phieu.Idphieu,
                            Me = meThoi,
                            NgaySX = request.NgaySX.ToDateTime(TimeOnly.MinValue),
                            Ca = request.Ca,
                            BieuMau = maBm,
                            Scope = request.NhaMay,
                            IsGhost = false
                        });
                    }
                    continue;
                }

                // Bước 4.1: mẻ cần thêm = newSet - currentSet
                var toInsert = newSet.Except(currentSet, StringComparer.OrdinalIgnoreCase);
                foreach (var meThoi in toInsert)
                {
                    _context.BBGN_ThepLongs.Add(new BBGN_ThepLong
                    {
                        IdPhieu = phieu.Idphieu,
                        Me = meThoi,
                        NgaySX = request.NgaySX.ToDateTime(TimeOnly.MinValue),
                        Ca = request.Ca,
                        BieuMau = maBm,
                        Scope = request.NhaMay,
                        IsGhost = false
                    });
                }

                // Bước 4.2: mẻ bị thiếu = currentSet - newSet
                var toRemove = currentSet.Except(newSet, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in rowsOfPhieu.Where(r => r.Me != null && toRemove.Contains(r.Me)))
                {
                    if (phieu.TinhTrang == 5)
                    {
                        // Phiếu đã chốt → đánh dấu ghost, không xóa
                        row.IsGhost = true;
                        _context.BBGN_ThepLongs.Update(row);
                    }
                    else
                    {
                        // Chưa chốt → xóa record
                        _context.BBGN_ThepLongs.Remove(row);
                    }
                }

                // Bước 4.3: mẻ vẫn tồn tại (giao) → nếu đang ghost thì clear ghost
                var intersect = currentSet.Intersect(newSet, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var row in rowsOfPhieu.Where(r => r.Me != null && intersect.Contains(r.Me)))
                {
                    if (row.IsGhost == true)
                    {
                        row.IsGhost = false;
                        _context.BBGN_ThepLongs.Update(row);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
