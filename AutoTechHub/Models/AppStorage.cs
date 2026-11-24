using System;
using System.ComponentModel.DataAnnotations;

namespace AutoTechHub.Models
{
    public class DataProtectionKey
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string FriendlyName { get; set; }

        [Required]
        public string Xml { get; set; }
    }

    public class MediaAsset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(120)]
        public string ContentType { get; set; }

        [Required]
        public byte[] Data { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
