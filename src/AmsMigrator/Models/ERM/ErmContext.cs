using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AmsMigrator.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ErmContext : DbContext
    {
        public ErmContext(DbContextOptions<ErmContext> options)
            : base(options)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public virtual DbSet<File> Files { get; set; }

        public virtual DbSet<Note> Notes { get; set; }

        public virtual DbSet<OrderPositionAdvertisement> OrderPositionAdvertisement { get; set; }

        public virtual DbSet<OrderPosition> OrderPositions { get; set; }

        public virtual DbSet<Order> Orders { get; set; }

        public virtual DbSet<OrganizationUnit> OrganizationUnits { get; set; }

        public virtual DbSet<Position> Positions { get; set; }

        public virtual DbSet<PositionChildren> PositionChildren { get; set; }

        public virtual DbSet<PricePosition> PricePositions { get; set; }

        public virtual DbSet<Price> Prices { get; set; }

        public virtual DbSet<OrderFile> OrderFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderFile>(entity =>
            {
                entity.ToTable("OrderFiles", "Billing");

                entity.HasIndex(e => e.FileId)
                    .HasName("NCI_FileId");

                entity.HasIndex(e => new { e.FileId, e.OrderId })
                    .HasName("IX_OrderFiles_OrderId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Comment).HasMaxLength(512);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.IsActive).HasDefaultValueSql("1");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.Timestamp)
                    .IsRequired()
                    .HasColumnType("timestamp")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(d => d.File)
                    .WithMany(p => p.OrderFiles)
                    .HasForeignKey(d => d.FileId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_OrderFiles_Files");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.OrderFiles)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_OrderFiles_Orders");
            });

            modelBuilder.Entity<File>(entity =>
            {
                entity.ToTable("Files", "Shared");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ContentType)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.Data).IsRequired();

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime2(2)");
            });

            modelBuilder.Entity<OrderPositionAdvertisement>(entity =>
            {
                entity.ToTable("OrderPositionAdvertisement", "Billing");

                entity.HasIndex(e => new { e.AdvertisementId, e.OrderPositionId })
                    .HasName("IX_OrderPositionAdvertisement_OrderPositionId");

                entity.HasIndex(e => new { e.OrderPositionId, e.PositionId })
                    .HasName("IX_OrderPositionAdvertisement_OrderPositionId_PositionId");

                entity.HasIndex(e => new { e.OrderPositionId, e.AdvertisementId, e.PositionId })
                    .HasName("IX_OrderPositionAdvertisement_OrderPositionId-AdvertisementId-PositionId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime2(2)");

                entity.HasOne(d => d.Position)
                    .WithMany(p => p.OrderPositionAdvertisement)
                    .HasForeignKey(d => d.PositionId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_OrderPositionAdvertisement_Positions");

                entity.HasOne(d => d.OrderPosition)
                    .WithMany(p => p.OrderPositionAdvertisement)
                    .HasForeignKey(d => d.OrderPositionId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_OrderPositionAdvertisement_OrderPositions");
            });

            modelBuilder.Entity<OrderPosition>(entity =>
            {
                entity.ToTable("OrderPositions", "Billing");

                entity.HasIndex(e => new { e.OrderId, e.IsActive, e.IsDeleted })
                    .HasName("IX_OrderPositions_OrderId_IsActive_IsDeleted");

                entity.HasIndex(e => new { e.PricePositionId, e.IsActive, e.IsDeleted })
                    .HasName("IX_OrderPositions_PricePositionId_IsActive_IsDeleted");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.Extensions).IsRequired();

                entity.Property(e => e.IsActive).IsRequired().HasDefaultValueSql("1");

                entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValueSql("0");

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime2(2)");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.OrderPositions)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_OrderPositions_Orders");

                entity.HasOne(d => d.PricePosition)
                    .WithMany(p => p.OrderPositions)
                    .HasForeignKey(d => d.PricePositionId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_OrderPositions_PricePositions");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders", "Billing");

                entity.HasIndex(e => e.ReplicationCode)
                    .HasName("IX_Orders_ReplicationCode");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ApprovalDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.BeginDistributionDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.Comment).HasMaxLength(300);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.DocumentsComment).HasMaxLength(300);

                entity.Property(e => e.EndDistributionDateFact).HasColumnType("datetime2(2)");

                entity.Property(e => e.EndDistributionDatePlan).HasColumnType("datetime2(2)");

                entity.Property(e => e.HasDocumentsDebt).HasDefaultValueSql("1");

                entity.Property(e => e.IsActive).IsRequired().HasDefaultValueSql("1");

                entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValueSql("0");

                entity.Property(e => e.IsTerminated).IsRequired().HasDefaultValueSql("0");

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.Number)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.RejectionDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.SignupDate).HasColumnType("datetime2(2)");

                entity.HasOne(d => d.DestOrganizationUnit)
                    .WithMany(p => p.OrdersDestOrganizationUnit)
                    .HasForeignKey(d => d.DestOrganizationUnitId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_DestOrgUnitOrder");

                entity.HasOne(d => d.SourceOrganizationUnit)
                    .WithMany(p => p.OrdersSourceOrganizationUnit)
                    .HasForeignKey(d => d.SourceOrganizationUnitId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_SourceOrgUnitOrder");
            });

            modelBuilder.Entity<OrganizationUnit>(entity =>
            {
                entity.ToTable("OrganizationUnits", "Billing");

                entity.HasIndex(e => e.Name)
                    .HasName("IX_OrganizationUnits_Name");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(5);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.ElectronicMedia)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValueSql("N''");

                entity.Property(e => e.ErmLaunchDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.FirstEmitDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.InfoRussiaLaunchDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.IsActive).HasDefaultValueSql("1");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.SyncCode1C).HasMaxLength(50);
            });

            modelBuilder.Entity<Position>(entity =>
            {
                entity.ToTable("Positions", "Billing");

                entity.HasIndex(e => e.Name)
                    .HasName("IX_Positions_Name");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.IsComposite).HasDefaultValueSql("0");

                entity.Property(e => e.IsContentSales).IsRequired();

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(256);
            });

            modelBuilder.Entity<PositionChildren>(entity =>
            {
                entity.HasKey(e => new { e.MasterPositionId, e.ChildPositionId })
                    .HasName("PK_PositionChildren_MasterPositionId_ChildPositionId");

                entity.ToTable("PositionChildren", "Billing");

                entity.HasOne(d => d.ChildPosition)
                    .WithMany(p => p.PositionChildrenChildPosition)
                    .HasForeignKey(d => d.ChildPositionId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_PositionChildren_PositionsChild");

                entity.HasOne(d => d.MasterPosition)
                    .WithMany(p => p.PositionChildrenMasterPosition)
                    .HasForeignKey(d => d.MasterPositionId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_PositionChildren_PositionsMaster");
            });

            modelBuilder.Entity<PricePosition>(entity =>
            {
                entity.ToTable("PricePositions", "Billing");

                entity.HasIndex(e => e.PriceId)
                    .HasName("IX_FK_Price_Position_Price");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Cost).HasColumnType("decimal");

                entity.Property(e => e.CreatedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.IsActive).HasDefaultValueSql("1");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime2(2)");

                entity.HasOne(d => d.Position)
                    .WithMany(p => p.PricePositions)
                    .HasForeignKey(d => d.PositionId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Price_Position_Position");

                entity.HasOne(d => d.Price)
                    .WithMany(p => p.PricePositions)
                    .HasForeignKey(d => d.PriceId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Price_Position_Price");
            });

            modelBuilder.Entity<Price>(entity =>
            {
                entity.ToTable("Prices", "Billing");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.BeginDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.CreateDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.CreatedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.IsActive).HasDefaultValueSql("1");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.Property(e => e.IsPublished).HasDefaultValueSql("0");

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.PublishDate).HasColumnType("datetime2(2)");
            });

            modelBuilder.Entity<Note>(entity =>
            {
                entity.ToTable("Notes", "Shared");

                entity.HasIndex(e => new { e.ParentId, e.ParentType })
                    .HasName("IX_Notes_ParentId_ParentType");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime2(2)");

                entity.Property(e => e.Text).HasColumnType("ntext");

                entity.Property(e => e.Title).HasMaxLength(256);
            });
        }
    }
}