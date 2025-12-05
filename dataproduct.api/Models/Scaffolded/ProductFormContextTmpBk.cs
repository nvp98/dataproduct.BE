using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Models.Scaffolded;

public partial class ProductFormContextTmpBk : DbContext
{
    public ProductFormContextTmpBk()
    {
    }

    public ProductFormContextTmpBk(DbContextOptions<ProductFormContextTmpBk> options)
        : base(options)
    {
    }

    public virtual DbSet<BkPhoiThep> BkPhoiTheps { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=192.168.240.3,1433;Database=PRODUCT_FORM;User Id=sa;Password=HPDQ@1234;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BkPhoiThep>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BK_NuocThep");

            entity.ToTable("BK_PhoiThep", tb => tb.HasTrigger("trg_Update_Phoi_Classification"));

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GhiChu).HasMaxLength(50);
            entity.Property(e => e.IsNm).HasColumnName("IsNM");
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
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
