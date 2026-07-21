using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowSpace.Domain.Entities
{
    public class UserRole
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid RoleId { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Role? Role { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        // Soft delete flag
        public bool IsDeleted { get; set; } = false;
    }
}
