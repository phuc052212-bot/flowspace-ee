using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowSpace.Domain.Entities
{
    public class Permission
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Navigation collection
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        // Soft delete flag
        public bool IsDeleted { get; set; } = false;
    }
}
