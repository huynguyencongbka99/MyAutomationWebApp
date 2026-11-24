using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AutoTechHub.Models;

namespace AutoTechHub.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Article> FeaturedArticles { get; set; } = new List<Article>();
        public List<Article> LatestArticles { get; set; } = new List<Article>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Tag> PopularTags { get; set; } = new List<Tag>();
        public List<CodeSnippet> FeaturedSnippets { get; set; } = new List<CodeSnippet>();

        public int TotalArticles { get; set; }
        public int TotalViews { get; set; }
        public int TotalCategories { get; set; }
        public int TotalSnippets { get; set; }

        public SiteSetting SiteSetting { get; set; } = new SiteSetting();
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<HomeSectionItem> HomeSections { get; set; } = new List<HomeSectionItem>();
        public List<WebBlock> CustomBlocks { get; set; } = new List<WebBlock>();
    }

    public class ArticleListViewModel
    {
        public List<Article> Articles { get; set; } = new List<Article>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Tag> Tags { get; set; } = new List<Tag>();

        public string SearchQuery { get; set; }
        public string CategorySlug { get; set; }
        public string TagSlug { get; set; }
        public string SortBy { get; set; } = "newest";

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 9;

        public Category CurrentCategory { get; set; }
        public Tag CurrentTag { get; set; }
    }

    public class ArticleDetailViewModel
    {
        public Article Article { get; set; }
        public List<Article> RelatedArticles { get; set; } = new List<Article>();
        public Article PreviousArticle { get; set; }
        public Article NextArticle { get; set; }
        public Comment NewComment { get; set; } = new Comment();
    }

    public class ArticleEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề bài viết là bắt buộc")]
        [StringLength(250)]
        [Display(Name = "Tiêu đề bài viết")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Tóm tắt bài viết là bắt buộc")]
        [StringLength(600)]
        [Display(Name = "Tóm tắt ngắn")]
        public string Summary { get; set; }

        [Required(ErrorMessage = "Nội dung bài viết (Markdown) là bắt buộc")]
        [Display(Name = "Nội dung Markdown")]
        public string ContentMarkdown { get; set; }

        [Display(Name = "Ảnh đại diện (URL)")]
        public string ThumbnailUrl { get; set; }

        [Required(ErrorMessage = "Tên tác giả là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tác giả")]
        public string AuthorName { get; set; } = "Kỹ sư Tự động hóa";

        [StringLength(150)]
        [Display(Name = "Email tác giả")]
        public string AuthorEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chuyên mục")]
        [Display(Name = "Chuyên mục")]
        public int CategoryId { get; set; }

        [Display(Name = "Các thẻ Tag (cách nhau bởi dấu phẩy, ví dụ: PLC, Siemens, Modbus)")]
        public string TagsString { get; set; }

        [Display(Name = "Đánh dấu là bài viết nổi bật (Featured)")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Xuất bản ngay")]
        public bool IsPublished { get; set; } = true;

        public ArticleApprovalStatus ApprovalStatus { get; set; } = ArticleApprovalStatus.Approved;

        public string ReviewNote { get; set; }

        // "publish" | "submit" | "draft"
        public string SubmitAction { get; set; }

        public List<Category> AvailableCategories { get; set; } = new List<Category>();
    }

    public class AdminDashboardViewModel
    {
        public bool IsAdmin { get; set; }
        public int TotalArticles { get; set; }
        public int PublishedArticles { get; set; }
        public int PendingApprovalCount { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public int TotalComments { get; set; }
        public int TotalSnippets { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveAdsCount { get; set; }
        public int ActiveProductsCount { get; set; }
        public int PendingInquiriesCount { get; set; }

        public SiteSetting SiteSetting { get; set; } = new SiteSetting();
        public List<Article> PendingArticles { get; set; } = new List<Article>();
        public List<Article> RecentArticles { get; set; } = new List<Article>();
        public List<Comment> RecentComments { get; set; } = new List<Comment>();
    }
}
