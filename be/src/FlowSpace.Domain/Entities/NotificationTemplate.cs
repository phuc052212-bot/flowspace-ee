using System;
using System.ComponentModel.DataAnnotations;

namespace FlowSpace.Domain.Entities
{
    public class NotificationTemplate
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Channel { get; set; } = "InApp";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
