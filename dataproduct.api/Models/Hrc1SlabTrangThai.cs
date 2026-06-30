namespace dataproduct.api.Models;

public class Hrc1SlabTrangThai
{
    public int Id { get; set; }
    public int IdSlab { get; set; }

    // ── Chuyển ca ──────────────────────────────────────────────────────────
    public bool IsChuyenCa { get; set; }
    public Guid? IdPhieuBBSL { get; set; }   // phiếu đích (chỉ set khi IsChuyenCa = true)
    public Guid? IdPhieuGoc { get; set; }    // phiếu gốc trước khi chuyển
    public int? NguoiChuyen { get; set; }
    public DateTime? NgayChuyen { get; set; }

    // ── Cán Tấm — XN Đúc ───────────────────────────────────────────────────
    public int TrangThaiDuc { get; set; }
    public int? NguoiXacNhanDuc { get; set; }
    public DateTime? NgayXacNhanDuc { get; set; }

    // ── Cán Tấm — XN Kho (cột DB: TrangThaiCan) ───────────────────────────
    public int TrangThaiCan { get; set; }
    public int? NguoiXacNhanCan { get; set; }
    public DateTime? NgayXacNhanCan { get; set; }

    // ── PKH — Chốt ─────────────────────────────────────────────────────────
    public int TrangThaiPKH { get; set; }
    public int? NguoiChotPKH { get; set; }
    public DateTime? NgayChotPKH { get; set; }

    public DateTime NgayTao { get; set; }
}
