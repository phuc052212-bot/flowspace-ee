using System;
using System.ComponentModel.DataAnnotations;

namespace FlowSpace.Domain.Entities
{
    public class SlaSetting
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium";

        public int ResponseTimeHours { get; set; } = 4;
        public int ResolutionTimeHours { get; set; } = 24;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
