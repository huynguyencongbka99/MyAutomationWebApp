using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoTechHub.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên chuyên mục là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tên chuyên mục")]
        public string Name { get; set; }

        [StringLength(100)]
        public string Slug { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [StringLength(50)]
        [Display(Name = "Biểu tượng Icon (Bootstrap Icons)")]
        public string Icon { get; set; } = "bi-cpu";

        [StringLength(30)]
        [Display(Name = "Màu chủ đạo / Badge")]
        public string BadgeColor { get; set; } = "primary";

        public int DisplayOrder { get; set; } = 0;

        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
    }
}
