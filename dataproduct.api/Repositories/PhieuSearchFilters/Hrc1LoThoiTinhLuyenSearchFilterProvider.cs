using dataproduct.api.Models;

namespace dataproduct.api.Repositories.PhieuSearchFilters;

/// <summary>
/// HRC1_LoThoi / HRC1_TinhLuyen: phiếu không có Scope (khác các BM thường dùng cặp MaKhuVuc&lt;-&gt;Scope),
/// nên chỉ cần khớp MaBm + QuyenChucNang, không check MaKhuVuc/Scope, không yêu cầu BmPheDuyet.
/// </summary>
public class Hrc1LoThoiTinhLuyenSearchFilterProvider : IPhieuSearchFilterProvider
{
    public IReadOnlyCollection<string> MaBms { get; } = new[] { "HRC1_LoThoi", "HRC1_TinhLuyen" };

    public bool HasScope => false;

    public IQueryable<BmPhieu> BuildQuery(ProductFormContext context, int loaiVung, int userId)
    {
        var baseQuery = context.BmPhieus
            .Where(x => x.IsDelete != 1 && x.IsLock != 1
                     && (x.MaBm == "HRC1_LoThoi" || x.MaBm == "HRC1_TinhLuyen"));

        return loaiVung switch
        {
            // Việc tôi bắt đầu: quyền 1|4
            1 => baseQuery.Where(x => context.BmQuyenXls.Any(q =>
                    q.IdTaiKhoan == userId && q.MaBm == x.MaBm &&
                    (q.QuyenChucNang == 1 || q.QuyenChucNang == 4))),

            // Việc đến tôi: quyền 2|4
            2 => baseQuery.Where(x => context.BmQuyenXls.Any(q =>
                    q.IdTaiKhoan == userId && q.MaBm == x.MaBm &&
                    (q.QuyenChucNang == 2 || q.QuyenChucNang == 4))),

            // Chỉ xem: quyền 5
            3 => baseQuery.Where(x => context.BmQuyenXls.Any(q =>
                    q.IdTaiKhoan == userId && q.MaBm == x.MaBm &&
                    q.QuyenChucNang == 5)),

            _ => baseQuery.Where(_ => false),
        };
    }
}
