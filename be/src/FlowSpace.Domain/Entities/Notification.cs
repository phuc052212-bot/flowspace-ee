using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowSpace.Domain.Entities
{
    public class Notification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        // Could be enum or string; using string for flexibility
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Message { get; set; }

        // Optional related entity reference
        public string? RelatedEntityType { get; set; }
        public Guid? RelatedEntityId { get; set; }

        public bool IsRead { get; set; } = false;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        // Soft delete flag
        public bool IsDeleted { get; set; } = false;
    }
}
