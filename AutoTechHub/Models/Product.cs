using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoTechHub.Models
{
    public enum ProductStockStatus
    {
        [Display(Name = "Còn hàng")]
        InStock = 0,

        [Display(Name = "Đặt trước")]
        PreOrder = 1,

        [Display(Name = "Tạm hết hàng")]
        OutOfStock = 2,

        [Display(Name = "Liên hệ báo giá")]
        ContactForPrice = 3
    }

    public enum ProductInquiryStatus
    {
        [Display(Name = "Mới tiếp nhận")]
        Pending = 0,

        [Display(Name = "Đã liên hệ")]
        Contacted = 1,

        [Display(Name = "Hoàn tất")]
        Completed = 2,

        [Display(Name = "Đã hủy")]
        Cancelled = 3
    }

    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Tên mặt hàng / sản phẩm")]
        public string Name { get; set; }

        [StringLength(250)]
        public string Slug { get; set; }

        [StringLength(60)]
        [Display(Name = "Mã SKU")]
        public string Sku { get; set; }

        [Required(ErrorMessage = "Danh mục sản phẩm là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Danh mục mặt hàng")]
        public string CategoryName { get; set; } = "Thiết bị Tự động hóa";

        [Required(ErrorMessage = "Mô tả ngắn là bắt buộc")]
        [StringLength(500)]
        [Display(Name = "Mô tả ngắn")]
        public string ShortDescription { get; set; }

        [Display(Name = "Mô tả chi tiết & Thông số kỹ thuật (Markdown)")]
        public string DescriptionMarkdown { get; set; }

        public string DescriptionHtml { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Giá bán (VNĐ)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Giá gốc (nếu giảm giá)")]
        public decimal? OriginalPrice { get; set; }

        [StringLength(20)]
        [Display(Name = "Đơn vị / Tiền tệ")]
        public string Currency { get; set; } = "VNĐ";

        [StringLength(500)]
        [Display(Name = "Ảnh sản phẩm (URL)")]
        public string ImageUrl { get; set; }

        [StringLength(500)]
        [Display(Name = "Link mua ngoài (Shopee/Zalo/Gumroad - tùy chọn)")]
        public string ExternalBuyUrl { get; set; }

        [Display(Name = "Tình trạng hàng")]
        public ProductStockStatus StockStatus { get; set; } = ProductStockStatus.InStock;

        [Display(Name = "Sản phẩm nổi bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Hiển thị công khai")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<ProductInquiry> Inquiries { get; set; } = new List<ProductInquiry>();
    }

    public class ProductInquiry
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ")]
        [StringLength(30)]
        [Display(Name = "Số điện thoại / Zalo")]
        public string Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Range(1, 1000, ErrorMessage = "Số lượng từ 1 đến 1000")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; } = 1;

        [StringLength(1000)]
        [Display(Name = "Ghi chú / Yêu cầu tư vấn")]
        public string Note { get; set; }

        [Display(Name = "Trạng thái")]
        public ProductInquiryStatus Status { get; set; } = ProductInquiryStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
