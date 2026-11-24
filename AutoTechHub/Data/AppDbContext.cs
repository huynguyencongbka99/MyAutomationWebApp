using Microsoft.EntityFrameworkCore;
using AutoTechHub.Models;

namespace AutoTechHub.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Article> Articles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ArticleTag> ArticleTags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CodeSnippet> CodeSnippets { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Advertisement> Advertisements { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductInquiry> ProductInquiries { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }
        public DbSet<WebBlock> WebBlocks { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<MediaAsset> MediaAssets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite key for ArticleTag
            modelBuilder.Entity<ArticleTag>()
                .HasKey(at => new { at.ArticleId, at.TagId });

            modelBuilder.Entity<ArticleTag>()
                .HasOne(at => at.Article)
                .WithMany(a => a.ArticleTags)
                .HasForeignKey(at => at.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ArticleTag>()
                .HasOne(at => at.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(at => at.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // Article - Category
            modelBuilder.Entity<Article>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Articles)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Article - Comment
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Article)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Article - AuthorUser / ReviewedByUser
            modelBuilder.Entity<Article>()
                .HasOne(a => a.AuthorUser)
                .WithMany(u => u.AuthoredArticles)
                .HasForeignKey(a => a.AuthorUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Article>()
                .HasOne(a => a.ReviewedByUser)
                .WithMany()
                .HasForeignKey(a => a.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Product - Inquiry
            modelBuilder.Entity<ProductInquiry>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Inquiries)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for fast searching
            modelBuilder.Entity<Article>()
                .HasIndex(a => a.Slug)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            modelBuilder.Entity<Tag>()
                .HasIndex(t => t.Slug)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Slug)
                .IsUnique();
        }
    }
}
