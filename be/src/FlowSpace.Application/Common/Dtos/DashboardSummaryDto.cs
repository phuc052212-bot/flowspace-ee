using System;

namespace FlowSpace.Application.Common.Dtos
{
    public class DashboardSummaryDto
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int PendingApprovalsCount { get; set; }
        public decimal TotalLoggedHours { get; set; }

        // Bổ sung các trường dữ liệu thật
        public List<TaskResponse> Tasks { get; set; } = new List<TaskResponse>();
        public List<ProjectResponse> Projects { get; set; } = new List<ProjectResponse>();
        public List<AuditLogDto> Activities { get; set; } = new List<AuditLogDto>();
        public List<TimeLogDto> WeeklyTimeLogs { get; set; } = new List<TimeLogDto>();
    }

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; } = "System";
        public string Action { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public string Module { get; set; } = "System";
        public DateTime CreatedAt { get; set; }
    }
}
