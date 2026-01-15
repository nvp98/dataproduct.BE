using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Models.Scaffolded;

public partial class ProductFormContextTmp3 : DbContext
{
    public ProductFormContextTmp3()
    {
    }

    public ProductFormContextTmp3(DbContextOptions<ProductFormContextTmp3> options)
        : base(options)
    {
    }

    public virtual DbSet<CtdPhoiNong> CtdPhoiNongs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=192.168.240.3,1433;Database=PRODUCT_FORM;User Id=sa;Password=HPDQ@1234;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.Property(e => e.Nmcan).HasColumnName("NMCan");
            entity.Property(e => e.SoThanhLoai1).HasColumnName("SoThanh_Loai1");
            entity.Property(e => e.SoThanhLoai2).HasColumnName("SoThanh_Loai2");
            entity.Property(e => e.SoThanhLoai3).HasColumnName("SoThanh_Loai3");
            entity.Property(e => e.TinhTrangCtd).HasColumnName("TinhTrangCTD");
            entity.Property(e => e.TinhTrangQlcl).HasColumnName("TinhTrangQLCL");
            entity.Property(e => e.TongKl).HasColumnName("TongKL");
            entity.Property(e => e.TongSt).HasColumnName("TongST");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
