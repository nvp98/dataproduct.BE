using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using dataproduct.api.Services;
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
        private readonly PheDuyetService _pdservice;

        public PhieuRepository(ProductFormContext context, PheDuyetService pdservice)
        {
            _context = context;
            _pdservice = pdservice;
        }

        public async Task<IEnumerable<BmPhieu>> GetAllAsync(string? MaBM, int? NguoiTaoID)
        {
            var query = _context.BmPhieus.Where(x => x.IsDelete != 1 && x.IsLock != 1).OrderByDescending(x => x.NgayTao).AsQueryable();


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
                string prefix = formData.TryGetProperty("prefix", out var p) ? p.GetString() ?? "UNKNOWN" : "UNKNOWN";
                int Ca = formData.TryGetProperty("ca", out var ca) ? ca.GetInt32() : 0;
                int Scope = formData.TryGetProperty("scope", out var scope) ? scope.GetInt32() : 0;
                DateOnly? NgaySX = formData.TryGetProperty("NgaySX", out var ngaySXProp)
                                ? DateOnly.FromDateTime(ngaySXProp.GetDateTime())
                                : null;
                string soPhieu = await SoPhieuHelper.GenerateAutoSoPhieu(_context, prefix, Scope, Ca, NgaySX);

                if (maBM == "CTD_BB_GiaoNhanPhoiNhapKho")
                {
                    if (NgaySX == null)
                        throw new Exception("Thiếu ngày sản xuất");

                    if (Ca == 0)
                        throw new Exception("Thiếu ca sản xuất");

                    if (!formData.TryGetProperty("mayduc", out var mDProp))
                        throw new Exception("Thiếu máy đúc");

                    int mayDuc = mDProp.GetInt32();

                    // check đã tồn tại phiếu theo ngày + ca + máy chưa
                    var daTonTai = await _context.BmPhieus.AnyAsync(x =>
                        x.MaBm == maBM &&
                        x.NgaySX == NgaySX &&
                        x.Ca == Ca &&
                        x.MayDuc == mayDuc &&
                        x.IsDelete == 0);

                    if (daTonTai)
                    {
                        throw new Exception($"Đã tồn tại phiếu cho máy {mayDuc}, ca {Ca}, ngày {NgaySX}");
                    }
                }
                if (maBM == "CTD_BB_Sanluongphoi")
                {
                    if (NgaySX == null)
                        throw new Exception("Thiếu ngày sản xuất");


                    if (Ca != 1 && Ca != 2)
                        throw new Exception("Ca không hợp lệ (chỉ 1 hoặc 2)");

                    // check đã tồn tại phiếu theo ngày + ca + máy chưa
                    var daTonTai = await _context.BmPhieus.AnyAsync(x =>
                        x.MaBm == maBM &&
                        x.NgaySX == NgaySX &&
                        x.Ca == Ca &&
                        x.IsDelete == 0);

                    if (daTonTai)
                    {
                        throw new Exception($"Đã tồn tại phiếu cho ca {Ca}, ngày {NgaySX}");
                    }
                }
                var phieu = new BmPhieu
                {
                    Idphieu = Guid.NewGuid(),
                    MaBm = maBM,
                    SoPhieu = soPhieu,
                    NgaySX = NgaySX,
                    Ca = Ca,
                    MayDuc = formData.TryGetProperty("mayduc", out var mdProp) ? mdProp.GetInt32() : null,
                    //NguoiTaoId = formData.TryGetProperty("nguoiTaoId", out var nguoitao) ? nguoitao.GetInt32() : null,
                    Scope = Scope,
                    //XuongId = formData.TryGetProperty("xuongId", out var xuongId) ? xuongId.GetInt32() : null,
                    //IdphongBan = formData.TryGetProperty("idphongBan", out var idphongBan) ? idphongBan.GetInt32() : null,
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
                if (pheDuyetList.Count > 0)
                {
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

        public async Task<bool> CheckExistsAsync(string maBm, DateOnly ngaySX, int ca, int? scope, int? mayduc)
        {
            var query = _context.BmPhieus.Where(x =>
                x.MaBm == maBm &&
                x.NgaySX == ngaySX &&
                x.Ca == ca
            );

            if (scope.HasValue)
                query = query.Where(x => x.Scope == scope.Value);

            if (mayduc.HasValue)
                query = query.Where(x => x.MayDuc == mayduc.Value);

            return await query.AnyAsync();
        }

        public async Task<(IEnumerable<SearchPhieuResponseModel> Data, int TotalCount)> SearchWithPagingAsync(SearchPhieuRequest request)
        {
            var query = _context.BmPhieus.Where(x => x.IsDelete != 1 && x.IsLock != 1).OrderByDescending(x => x.NgaySX).ThenByDescending(x => x.Ca).AsQueryable();
            if (request.TuNgay.HasValue)
            {
                query = query.Where(x => x.NgaySX >= DateOnly.FromDateTime(request.TuNgay.Value));
            }
            if (request.DenNgay.HasValue)
            {
                query = query.Where(x => x.NgaySX <= DateOnly.FromDateTime(request.DenNgay.Value));
            }
            if (request.Ca.HasValue)
            {
                query = query.Where(x => x.Ca == request.Ca.Value);
            }
            if (request.Scope.HasValue)
            {
                query = query.Where(x => x.Scope == request.Scope.Value);
            }
            if (request.MayDuc.HasValue)
            {
                query = query.Where(x => x.MayDuc == request.MayDuc.Value);
            }
            if (!string.IsNullOrEmpty(request.MaBm))
            {
                query = query.Where(x => x.MaBm == request.MaBm);
            }
            if (!string.IsNullOrEmpty(request.searchText))
            {
                query = query.Where(x => x.SoPhieu.Contains(request.searchText));
            }
            var totalCount = await query.CountAsync();
            var data = await query.Skip((request.page - 1) * request.pageSize).Take(request.pageSize).ToListAsync();
            var ids = data.Select(x => x.Idphieu).ToList();
            var result = data.Select(x => new SearchPhieuResponseModel
            {
                Idphieu = x.Idphieu,
                SoPhieu = x.SoPhieu,
                MaBm = x.MaBm,
                NgaySX = x.NgaySX.HasValue ? x.NgaySX.Value : DateOnly.MinValue,
                Ca = x.Ca,
                Kip = x.Kip,
                Scope = x.Scope,
                MayDuc = x.MayDuc,
                TinhTrang = x.TinhTrang,
                NguoiTao = x.NguoiTaoId,
            }).ToList();
            foreach (var item in result)
            {
                var pheDuyet = await _pdservice.GetPheDuyetPhieuAsync(item.Idphieu);
                item.PheDuyet = pheDuyet.ToList();
            }

            return (result, totalCount);
        }

    }
}
