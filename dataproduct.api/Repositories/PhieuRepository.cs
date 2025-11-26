using dataproduct.api.Models;
using dataproduct.api.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace dataproduct.api.Repositories
{
    public class PhieuRepository : IPhieuRepository
    {
        private readonly ProductFormContext _context;

        public PhieuRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BmPhieu>> GetAllAsync(string? MaBM, int? NguoiTaoID)
        {
            var query = _context.BmPhieus.Where(x => x.IsDelete != 1).OrderByDescending(x => x.NgayTao).AsQueryable();


            if (!string.IsNullOrEmpty(MaBM))
                query = query.Where(x => x.MaBm == MaBM);
            if (NguoiTaoID != null)
                query = query.Where(x => x.NguoiTaoId == NguoiTaoID);

            return await query.ToListAsync();
        }

        public async Task<BmPhieu?> GetByIdAsync(Guid id)
        {
            return await _context.BmPhieus.FirstOrDefaultAsync(x => x.Idphieu == id);
        }

        public async Task<BmPhieu> AddAsync([FromBody] JsonElement formData)
        {
            try
            {
                string maBM = formData.GetProperty("maBm").GetString() ?? "UNKNOWN";

                string soPhieu = await SoPhieuHelper.GenerateAutoSoPhieu(_context, prefix: "BBGN");

                var phieu = new BmPhieu
                {
                    Idphieu = Guid.NewGuid(),
                    MaBm = maBM,
                    SoPhieu = soPhieu,
                    NgaySX = formData.TryGetProperty("NgaySX", out var ngaySXProp)
                                ? DateOnly.FromDateTime(ngaySXProp.GetDateTime())
                                : null,
                    Ca = formData.TryGetProperty("ca", out var caProp) ? caProp.GetInt32() : null,
                    MayDuc = formData.TryGetProperty("mayduc", out var mdProp) ? mdProp.GetInt32() : null,
                    NguoiTaoId = formData.TryGetProperty("nguoiTaoId", out var nguoitao) ? nguoitao.GetInt32() : null,
                    XuongId = formData.TryGetProperty("xuongId", out var xuongId) ? xuongId.GetInt32() : null,
                    IdphongBan = formData.TryGetProperty("idphongBan", out var idphongBan) ? idphongBan.GetInt32() : null,
                    DataJson = formData.GetRawText(),
                    NgayTao = DateTime.Now,
                    TinhTrang = 0,
                    IsDelete = 0,
                    IsLock = 0
                };
                _context.BmPhieus.Add(phieu);
                await _context.SaveChangesAsync();
                // Lưu thông tin phê duyệt

                List<BmPheDuyet> pheDuyetList = new();

                if (formData.TryGetProperty("pheDuyet", out var pheDuyetProp) && pheDuyetProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in pheDuyetProp.EnumerateArray())
                    {
                        var phe = new BmPheDuyet
                        {
                            PhieuId = phieu.Idphieu,
                            CapDuyet = item.TryGetProperty("capDuyet", out var capProp) ? capProp.GetInt32() : 0,
                            NguoiDuyetId = item.TryGetProperty("nguoiDuyetId", out var ndProp) ? ndProp.GetInt32() : 0,
                            TinhTrang = item.TryGetProperty("tinhTrang", out var ttProp) ? ttProp.GetInt32() : 0,
                            GhiChu = item.TryGetProperty("ghiChu", out var gcProp) ? gcProp.GetString() : null,
                        };

                        pheDuyetList.Add(phe);
                    }
                }
                if (pheDuyetList.Count > 0) {
                    // xóa dữ liệu phê duyệt cũ
                    var listDuyet = _context.BmPheDuyets.Where(p => p.PhieuId == phieu.Idphieu).ToList();
                    _context.BmPheDuyets.RemoveRange(listDuyet);
                    // Lưu danh sách phê duyệt mới
                    foreach (var item in pheDuyetList)
                    {
                        _context.BmPheDuyets.Add(item);
                    }
                    _context.SaveChanges();
                }

                return phieu;

            }
            catch (Exception ex)
            {
                return null;
                //BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task UpdateAsync(BmPhieu entity)
        {
            // EF tự nhận diện Guid làm khóa chính → Update không lỗi
            _context.BmPhieus.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var item = await _context.BmPhieus.FindAsync(id);
            if (item != null)
            {
                _context.BmPhieus.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.BmPhieus.AnyAsync(e => e.Idphieu == id);
        }
    }
}
