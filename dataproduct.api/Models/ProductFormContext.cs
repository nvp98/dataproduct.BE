using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Models;

public partial class ProductFormContext : DbContext
{
    public ProductFormContext()
    {
    }

    public ProductFormContext(DbContextOptions<ProductFormContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BkNguyenLieu> BkNguyenLieus { get; set; }

    public virtual DbSet<BkPhoiThep> BkPhoiThep { get; set; }

    public virtual DbSet<BmPheDuyet> BmPheDuyets { get; set; }

    public virtual DbSet<BmPhieu> BmPhieus { get; set; }

    public virtual DbSet<BmPhieuChiTiet> BmPhieuChiTiets { get; set; }

    public virtual DbSet<CtdPhoiNguoi> CtdPhoiNguois { get; set; }

    public virtual DbSet<CtdPhoiNong> CtdPhoiNongs { get; set; }
    public virtual DbSet<DLNM_HRC2> DLNM_HRC2s { get; set; }
    public virtual DbSet<Header_Key> Header_Keys { get; set; }
    public virtual DbSet<Header_Mapping> Header_Mappings { get; set; }

    public virtual DbSet<HRC2_NM> HRC2_NMs { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=192.168.240.3,1433;Database=PRODUCT_FORM;User Id=sa;Password=HPDQ@1234;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BkNguyenLieu>(entity =>
        {
            entity.ToTable("BK_NguyenLieu");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DoAm).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.GhiChu).HasMaxLength(50);
            entity.Property(e => e.GioLayMau).HasMaxLength(10);
            entity.Property(e => e.GioNhapBk)
                .HasMaxLength(50)
                .HasColumnName("GioNhap_BK");
            entity.Property(e => e.Kip)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.NgaySx).HasColumnName("NgaySX");
            entity.Property(e => e.Silo).HasMaxLength(20);
            entity.Property(e => e.TenNvl)
                .HasMaxLength(150)
                .HasColumnName("TenNVL");
            entity.Property(e => e.TronId).HasColumnName("Tron_ID");
        });

        modelBuilder.Entity<BkPhoiThep>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BK_NuocThep");

            entity.ToTable("BK_PhoiThep");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GhiChu).HasMaxLength(50);
            entity.Property(e => e.KichThuoc).HasMaxLength(20);
            entity.Property(e => e.Kip)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.LoaiId).HasColumnName("LoaiID");
            entity.Property(e => e.LoaiPhoi).HasMaxLength(50);
            entity.Property(e => e.Mac).HasMaxLength(10);
            entity.Property(e => e.MauThu).HasMaxLength(10);
            entity.Property(e => e.Me).HasMaxLength(20);
            entity.Property(e => e.NgaySx).HasColumnName("NgaySX");
            entity.Property(e => e.NgayTaoBk)
                .HasColumnType("datetime")
                .HasColumnName("NgayTaoBK");
            entity.Property(e => e.StDaChuyen).HasColumnName("ST_DaChuyen");
            entity.Property(e => e.TenLoai).HasMaxLength(10);
            entity.Property(e => e.TenPhanLoai).HasMaxLength(20);
            entity.Property(e => e.VanChuyen).HasMaxLength(50);
            entity.Property(e => e.IsNm).HasColumnName("IsNM");
        });

        modelBuilder.Entity<BmPheDuyet>(entity =>
        {
            entity.ToTable("BM_PheDuyet");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GhiChu).HasMaxLength(250);
            entity.Property(e => e.NgayDuyet).HasColumnType("datetime");
            entity.Property(e => e.NguoiDuyetId).HasColumnName("NguoiDuyetID");
            entity.Property(e => e.PhieuId).HasColumnName("PhieuID");
        });

        modelBuilder.Entity<BmPhieu>(entity =>
        {
            entity.HasKey(e => e.Idphieu);

            entity.ToTable("BM_Phieu");

            entity.Property(e => e.Idphieu)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("IDPhieu");
            entity.Property(e => e.DataJson).HasColumnName("DataJSon");
            entity.Property(e => e.Idkip).HasColumnName("IDKip");
            entity.Property(e => e.IdphongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.IsDelete).HasDefaultValue(0);
            entity.Property(e => e.IsLock).HasDefaultValue(0);
            entity.Property(e => e.Kip)
                .HasMaxLength(1)
                .IsFixedLength();
            entity.Property(e => e.MaBm)
                .HasMaxLength(100)
                .HasColumnName("MaBM");
            entity.Property(e => e.NgaySX).HasColumnName("NgaySX");
            entity.Property(e => e.NgayTao).HasColumnType("datetime");
            entity.Property(e => e.NguoiTaoId).HasColumnName("NguoiTaoID");
            entity.Property(e => e.SoPhieu).HasMaxLength(50);
            entity.Property(e => e.XuongId).HasColumnName("XuongID");
        });

        modelBuilder.Entity<BmPhieuChiTiet>(entity =>
        {
            entity.ToTable("BM_Phieu_ChiTiet");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GiaTri).HasMaxLength(20);
            entity.Property(e => e.PhieuId).HasColumnName("PhieuID");
            entity.Property(e => e.RowId).HasColumnName("RowID");
            entity.Property(e => e.ThongSo).HasMaxLength(20);
        });

        modelBuilder.Entity<CtdPhoiNguoi>(entity =>
        {
            entity.ToTable("CTD_PhoiNguoi");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GhiChu).HasMaxLength(250);
            entity.Property(e => e.KichThuoc).HasMaxLength(50);
            entity.Property(e => e.Mac).HasMaxLength(10);
            entity.Property(e => e.Me).HasMaxLength(10);
            entity.Property(e => e.PhieuId).HasColumnName("PhieuID");
            entity.Property(e => e.TongKl).HasColumnName("TongKL");
        });

        modelBuilder.Entity<CtdPhoiNong>(entity =>
        {
            entity.ToTable("CTD_PhoiNong");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CaKip)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.GhiChu).HasMaxLength(200);
            entity.Property(e => e.IdBkPhoiThep).HasColumnName("ID_BK_PhoiThep");
            entity.Property(e => e.Idphieu).HasColumnName("IDPhieu");
            entity.Property(e => e.KhoiLuongLoai1).HasColumnName("KhoiLuong_Loai1");
            entity.Property(e => e.KhoiLuongLoai2).HasColumnName("KhoiLuong_Loai2");
            entity.Property(e => e.KhoiLuongLoai3).HasColumnName("KhoiLuong_Loai3");
            entity.Property(e => e.KichThuoc).HasMaxLength(50);
            entity.Property(e => e.Kip)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Mac).HasMaxLength(10);
            entity.Property(e => e.Me).HasMaxLength(10);
            entity.Property(e => e.NgaySx).HasColumnName("NgaySX");
            entity.Property(e => e.NgayTao).HasColumnType("datetime");
            entity.Property(e => e.NmCan).HasColumnName("NMCan");
            entity.Property(e => e.SoThanhLoai1).HasColumnName("SoThanh_Loai1");
            entity.Property(e => e.SoThanhLoai2).HasColumnName("SoThanh_Loai2");
            entity.Property(e => e.SoThanhLoai3).HasColumnName("SoThanh_Loai3");
            entity.Property(e => e.TinhTrangCTD).HasColumnName("TinhTrangCTD");
            entity.Property(e => e.TinhTrangQLCL).HasColumnName("TinhTrangQLCL");
            entity.Property(e => e.TongKl).HasColumnName("TongKL");
            entity.Property(e => e.TongSt).HasColumnName("TongST");
        });

        modelBuilder.Entity<DLNM_HRC2>(entity =>
        {
            entity.ToTable("DLNM_HRC2");
            entity.Property(e => e.ID).HasColumnName("ID");
            entity.Property(e => e.REPORT_NO).HasColumnName("REPORT_NO");
            entity.Property(e => e.NgaySx).HasColumnName("NgaySX");
            entity.Property(e => e.Ngay).HasColumnName("Ngay");
            entity.Property(e => e.Ca).HasColumnName("Ca");
            entity.Property(e => e.BieuMau).HasColumnName("BieuMau");
            entity.Property(e => e.Scope).HasColumnName("Scope");
            entity.Property(e => e.MeThoi).HasColumnName("MeThoi");
            entity.Property(e => e.MacThep).HasColumnName("MacThep");
            entity.Property(e => e.O2).HasColumnName("O2");
            entity.Property(e => e.AR_RH).HasColumnName("AR_RH");
            entity.Property(e => e.N2).HasColumnName("N2");
            entity.Property(e => e.AR_BOF).HasColumnName("AR_BOF");
            entity.Property(e => e.AR_LF).HasColumnName("AR_LF");
            entity.Property(e => e.ID_PhuLieu).HasColumnName("ID_PhuLieu");
            entity.Property(e => e.TenPhuLieu).HasColumnName("TenPhuLieu");
            entity.Property(e => e.KLPhuGia).HasColumnName("KLPhuGia");
            entity.Property(e => e.KLGangLong).HasColumnName("KLGangLong");
            entity.Property(e => e.KLThepPhe).HasColumnName("KLThepPhe");
        });

        modelBuilder.Entity<Header_Key>(entity =>
        {
            entity.ToTable("Header_Key");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.KeyGuid).HasColumnName("KeyGuid");
            entity.Property(e => e.TenHienThi).HasColumnName("TenHienThi");
            entity.Property(e => e.Mota).HasColumnName("Mota");
            entity.Property(e => e.LoaiPhieu).HasColumnName("LoaiPhieu");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.NgayTao).HasColumnName("NgayTao");
        });

        modelBuilder.Entity<Header_Mapping>(entity =>
        {
            entity.ToTable("Header_Mapping");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.TenNguonDuLieu).HasColumnName("TenNguonDuLieu");
            entity.Property(e => e.ID_PhuLieu).HasColumnName("ID_PhuLieu");
            entity.Property(e => e.ID_HeaderKey).HasColumnName("ID_HeaderKey");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.NgayTao).HasColumnName("NgayTao");
        });
        modelBuilder.Entity<HRC2_NM>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("HRC2_NM");
            entity.Property(e => e.REPORT_NO).HasColumnName("REPORT_NO");
            entity.Property(e => e.PRODUCTION_DATE).HasColumnName("PRODUCTION_DATE");
            entity.Property(e => e.ShiftDate).HasColumnName("ShiftDate");
            entity.Property(e => e.Shift).HasColumnName("Shift");
            entity.Property(e => e.PLANT).HasColumnName("PLANT");
            entity.Property(e => e.PLANT_NO).HasColumnName("PLANT_NO");
            entity.Property(e => e.PRODUCT_ID).HasColumnName("PRODUCT_ID");
            entity.Property(e => e.GRADE_ID_PLAN).HasColumnName("GRADE_ID_PLAN");
            entity.Property(e => e.O2).HasColumnName("O2");
            entity.Property(e => e.AR_RH).HasColumnName("AR_RH");
            entity.Property(e => e.N2).HasColumnName("N2");
            entity.Property(e => e.AR_BOF).HasColumnName("AR_BOF");
            entity.Property(e => e.AR_LF).HasColumnName("AR_LF");
            entity.Property(e => e.MATERIAL_NO).HasColumnName("MATERIAL_NO");
            entity.Property(e => e.DESCRIPTION_EN).HasColumnName("DESCRIPTION_EN");
            entity.Property(e => e.KLPhuGia).HasColumnName("KLPhuGia");
            entity.Property(e => e.KLGangLong).HasColumnName("KLGangLong");
            entity.Property(e => e.KLThepPhe).HasColumnName("KLThepPhe");
        });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
