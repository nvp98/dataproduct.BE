using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace dataproduct.api.Models.MasterData;

public partial class ProductDataMasterDbContext : DbContext
{
    public ProductDataMasterDbContext()
    {
    }

    public ProductDataMasterDbContext(DbContextOptions<ProductDataMasterDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaiKhoan> Tbl_TaiKhoan { get; set; }
    public DbSet<PhongBan> Tbl_PhongBan { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<TaiKhoan>(entity =>
        //{
        //    entity.ToTable("Tbl_TaiKhoan");

        //    entity.Property(e => e.NgayTao)
        //          .HasColumnType("date");

        //    entity.Property(e => e.ChuKy)
        //          .HasColumnType("nvarchar(MAX)");

        //    entity.Property(e => e.PhongBan_Them)
        //          .HasColumnType("nvarchar(MAX)");
        //});
        modelBuilder.Entity<TaiKhoan>()
            .HasOne(t => t.PhongBan)
            .WithMany(p => p.TaiKhoans)
            .HasForeignKey(t => t.ID_PhongBan);


        base.OnModelCreating(modelBuilder);
    }
}
