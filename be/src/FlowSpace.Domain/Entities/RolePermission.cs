using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowSpace.Domain.Entities
{
    public class RolePermission
    {
        [Key, Column(Order = 0)]
        public Guid RoleId { get; set; }

        [Key, Column(Order = 1)]
        public Guid PermissionId { get; set; }

        // Navigation properties
        public Role? Role { get; set; }
        public Permission? Permission { get; set; }
    }
}
