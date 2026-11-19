using dataproduct.api.Models;

namespace dataproduct.api.Repositories.PhieuSearchFilters;

/// <summary>
/// Phiếu điều chỉnh số liệu (NM.LG + các phòng ban): 1 phiếu chứa nhiều xưởng, header không có Scope
/// → chỉ khớp MaBm + QuyenChucNang, bỏ qua MaKhuVuc/Scope.
/// MaKhuVuc (ID xưởng) của BM_QuyenXL chỉ dùng ở FE để lọc tab xưởng khi tạo phiếu.
/// Thêm phiếu điều chỉnh của phòng ban mới = thêm MaBm vào danh sách dưới đây
/// (khớp PHIEU_DIEU_CHINH_NM ở FE).
/// </summary>
public class PhieuDieuChinhSearchFilterProvider : IPhieuSearchFilterProvider
{
    private static readonly string[] MaBmList =
    {
        "NMLG_BM_PhieuDieuChinh",
        "NMTKVV_BM_PhieuDieuChinh",
        "NMNL_BM_PhieuDieuChinh",
        "NMLC_BM_PhieuDieuChinh",
    };

    public IReadOnlyCollection<string> MaBms => MaBmList;

    public bool HasScope => false;

    public IQueryable<BmPhieu> BuildQuery(ProductFormContext context, int loaiVung, int userId)
    {
        var ds = MaBmList;
        var baseQuery = context.BmPhieus
            .Where(x => x.IsDelete != 1 && x.IsLock != 1
                     && x.MaBm != null && ds.Contains(x.MaBm));

        return loaiVung switch
        {
            // Việc tôi bắt đầu: quyền 1|4
            1 => baseQuery.Where(x => context.BmQuyenXls.Any(q =>
                    q.IdTaiKhoan == userId && q.MaBm == x.MaBm &&
                    (q.QuyenChucNang == 1 || q.QuyenChucNang == 4))),

            // Việc đến tôi: quyền 2|4 + có dòng phê duyệt của user (giữ như luồng chuẩn)
            2 => baseQuery.Where(x =>
                    context.BmQuyenXls.Any(q =>
                        q.IdTaiKhoan == userId && q.MaBm == x.MaBm &&
                        (q.QuyenChucNang == 2 || q.QuyenChucNang == 4)) &&
                    context.BmPheDuyets.Any(pd =>
                        pd.PhieuId == x.Idphieu &&
                        pd.CapDuyet != null && pd.CapDuyet != 0 &&
                        pd.NguoiDuyetId == userId) &&
                    x.TinhTrang != 0),

            // Chỉ xem: quyền 5
            3 => baseQuery.Where(x => context.BmQuyenXls.Any(q =>
                    q.IdTaiKhoan == userId && q.MaBm == x.MaBm &&
                    q.QuyenChucNang == 5)),

            _ => baseQuery.Where(_ => false),
        };
    }
}
