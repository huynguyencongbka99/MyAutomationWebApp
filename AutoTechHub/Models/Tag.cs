using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoTechHub.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(50)]
        public string Slug { get; set; }

        public virtual ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
    }
}
