using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Data.Entities;

namespace BizSrt.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public virtual DbSet<Account> Accounts { get; set; }
    public virtual DbSet<CompanyProfile> CompanyProfiles { get; set; }
    public virtual DbSet<CompanyOffice> CompanyOffices { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<CompanyProduct> CompanyProducts { get; set; }
    public virtual DbSet<Project> Projects { get; set; }
    public virtual DbSet<CompanyProject> CompanyProjects { get; set; }
    public virtual DbSet<Job> Jobs { get; set; }
    public virtual DbSet<CompanyMedia> CompanyMedia { get; set; }
    public virtual DbSet<ProductMedia> ProductMedia { get; set; }
    public virtual DbSet<ProjectMedia> ProjectMedia { get; set; }
    public virtual DbSet<CommunityMedia> CommunityMedia { get; set; }
    public virtual DbSet<CompanyAffiliation> CompanyAffiliations { get; set; }
    public virtual DbSet<CompanyCommunity> CompanyCommunities { get; set; }
    public virtual DbSet<Community> Communities { get; set; }
    public virtual DbSet<Promotion> Promotions { get; set; }
    public virtual DbSet<Category_Unwound> Categories_Unwound { get; set; }
    public virtual DbSet<Location_Unwound> Locations_Unwound { get; set; }
    public virtual DbSet<Location> Locations { get; set; }
    public virtual DbSet<StreetName> StreetNames { get; set; }
    public virtual DbSet<AreaName> AreaNames { get; set; }
    public virtual DbSet<CategoryProductAttribute> CategoryProductAttributes { get; set; }
    public virtual DbSet<BizSrt.Api.Data.Entities.SecurityProfile> SecurityProfiles { get; set; }
    public virtual DbSet<BizSrt.Api.Data.Entities.SecurityProfilePriviledge> SecurityProfilePriviledges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(50);
            entity.Property(e => e.WebSite).HasMaxLength(250);
            entity.Property(e => e.Alias).HasMaxLength(100);
            entity.Property(e => e.Created).HasColumnType("datetime");
            entity.Property(e => e.Updated).HasColumnType("datetime");
            entity.Property(e => e.Indexed).HasColumnType("datetime");
        });

        modelBuilder.Entity<CompanyOffice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(d => d.CompanyProfile)
                .WithMany(p => p.Offices)
                .HasForeignKey(d => d.Company);
            
            entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.StreetNumber).HasMaxLength(10);
            entity.Property(e => e.Address1).HasMaxLength(50);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Phone1).HasMaxLength(20);
            entity.Property(e => e.Fax).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.WebUrl).HasMaxLength(250);
            entity.Property(e => e.PlaceId).HasMaxLength(50);
            entity.Property(e => e.MetadataCheck).HasColumnType("datetime");
            entity.Property(e => e.GeoLocation).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Keywords).HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.WebUrl).HasMaxLength(250);
            entity.Property(e => e.Created).HasColumnType("datetime");
            entity.Property(e => e.Updated).HasColumnType("datetime");
        });

        modelBuilder.Entity<CompanyProduct>(entity =>
        {
            entity.HasKey(e => e.Product);
            entity.HasOne(d => d.ProductNavigation)
                .WithOne()
                .HasForeignKey<CompanyProduct>(d => d.Product);
            entity.Property(e => e.Alias).HasMaxLength(100);
            entity.Property(e => e.Indexed).HasColumnType("datetime");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.StreetNumber).HasMaxLength(10);
            entity.Property(e => e.Created).HasColumnType("datetime");
            entity.Property(e => e.Updated).HasColumnType("datetime");
            entity.Property(e => e.Indexed).HasColumnType("datetime");
        });

        modelBuilder.Entity<CompanyProject>(entity =>
        {
            entity.HasKey(e => e.Project);
            entity.HasOne(d => d.ProjectNavigation)
                .WithOne()
                .HasForeignKey<CompanyProject>(d => d.Project);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(d => d.ProductNavigation)
                .WithOne()
                .HasForeignKey<Job>(d => d.Id);
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.StreetNumber).HasMaxLength(10);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ExternalId).HasMaxLength(100);
            entity.Property(e => e.PwdHash).IsRequired().HasMaxLength(24).IsFixedLength();
            entity.Property(e => e.PwdSalt).IsRequired().HasMaxLength(24).IsFixedLength();
            entity.Property(e => e.Created).HasColumnType("datetime");
            entity.Property(e => e.Updated).HasColumnType("datetime");
        });

        modelBuilder.Entity<CompanyMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Metadata).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<ProductMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Metadata).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<ProjectMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Metadata).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<CommunityMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Metadata).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<CompanyAffiliation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Text).HasMaxLength(1000);
        });

        modelBuilder.Entity<CompanyCommunity>(entity =>
        {
            entity.HasKey(e => new { e.Company, e.Community });
        });

        modelBuilder.Entity<Community>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Alias).HasMaxLength(100);
            entity.Property(e => e.Created).HasColumnType("datetime");
            entity.Property(e => e.Updated).HasColumnType("datetime");
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.StreetNumber).HasMaxLength(10);
            entity.Property(e => e.Address1).HasMaxLength(50);
            entity.Property(e => e.DefaultCategory).HasMaxLength(50);
            entity.Property(e => e.Password).HasMaxLength(50);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(d => d.CommunityNavigation)
                .WithOne()
                .HasForeignKey<Promotion>(d => d.Id);
            entity.Property(e => e.EffectiveFrom).HasColumnType("datetime");
            entity.Property(e => e.EffectiveTo).HasColumnType("datetime");
        });
    }
}

