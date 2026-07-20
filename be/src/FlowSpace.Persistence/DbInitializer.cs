using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FlowSpace.Domain.Entities;
using FlowSpace.Domain.Enums;
using FlowSpace.Persistence.Contexts;
using TaskStatus = FlowSpace.Domain.Enums.TaskStatus;

namespace FlowSpace.Persistence
{
    public static class DbInitializer
    {
        public static void Initialize(FlowSpaceDbContext context)
        {
            try
            {
                if (context.Users.Any())
                {
                    return; // Cơ sở dữ liệu đã có dữ liệu seed
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Lỗi khi kiểm tra dữ liệu trước khi seed: {ex.Message}");
                return;
            }

            try
            {
                // 1. Seed Users
                var users = GetUsers();
                context.Users.AddRange(users);
                context.SaveChanges();

                // 2. Seed Workflow Rules
                var rules = GetWorkflowRules();
                context.WorkflowRules.AddRange(rules);
                context.SaveChanges();

                // 3. Seed Projects
                var projects = GetProjects(users);
                context.Projects.AddRange(projects);
                context.SaveChanges();

                // 4. Seed Tasks, Subtasks & TimeLogs
                var (tasks, subtasks, timeLogs) = GetTasksAndLogs(projects, users);
                context.Tasks.AddRange(tasks);
                context.SaveChanges();

                context.Subtasks.AddRange(subtasks);
                context.TimeLogs.AddRange(timeLogs);
                context.SaveChanges();

                // Cập nhật lại LoggedHours của các TaskItem
                foreach (var task in tasks)
                {
                    var sumHours = timeLogs.Where(tl => tl.TaskId == task.Id).Sum(tl => tl.Hours);
                    task.LoggedHours = sumHours;
                }
                
                // Cập nhật lại Progress của Projects dựa trên các TaskItem
                foreach (var proj in projects)
                {
                    var projTasks = tasks.Where(t => t.ProjectId == proj.Id).ToList();
                    if (projTasks.Any())
                    {
                        var doneTasks = projTasks.Count(t => t.Status == TaskStatus.Done);
                        proj.Progress = (doneTasks * 100) / projTasks.Count;
                    }
                }
                context.SaveChanges();

                // 5. Seed Comments
                var comments = GetComments(tasks, users);
                context.Comments.AddRange(comments);
                context.SaveChanges();

                // 6. Seed Documents
                var documents = GetDocuments(users);
                context.Documents.AddRange(documents);
                context.SaveChanges();

                // 7. Seed Chat Channels & Chat Messages
                var (channels, messages) = GetChatData(users);
                context.ChatChannels.AddRange(channels);
                context.SaveChanges();

                context.ChatMessages.AddRange(messages);
                context.SaveChanges();

                // 8. Seed Requests & Approvals
                var (requests, approvals) = GetRequestsAndApprovals(users);
                context.Requests.AddRange(requests);
                context.SaveChanges();

                context.Approvals.AddRange(approvals);
                context.SaveChanges();

                // 9. Seed Audit Logs
                var auditLogs = GetAuditLogs(users);
                context.AuditLogs.AddRange(auditLogs);
                context.SaveChanges();

                Console.WriteLine("=== SEED DATA THÀNH CÔNG CHO FLOWSPACE ===");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Bỏ qua lỗi trong quá trình seed database: {ex.Message}");
            }
        }

        private static List<User> GetUsers()
        {
            // Mật khẩu "123456" mã hóa dạng BCrypt
            string defaultPasswordHash = "$2a$11$9Q0c4Y1wVp0d1HqLd5S8OeWzE1V0d1HqLd5S8OeWzE1V0d1HqLd5S"; 

            return new List<User>
            {
                // 4 User chính giữ nguyên Id
                new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Phạm Thanh Dung",
                    Email = "admin@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "director",
                    Avatar = "PD",
                    Color = "#e74c3c",
                    Department = "Ban giám đốc",
                    Position = "Giám đốc điều hành",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddYears(-3),
                    JoinDate = DateTime.UtcNow.AddYears(-3)
                },
                new User
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Lê Minh Cường",
                    Email = "truongphong@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "manager",
                    Avatar = "LC",
                    Color = "#e67e22",
                    Department = "Kỹ thuật",
                    Position = "Trưởng phòng Kỹ thuật",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddYears(-2),
                    JoinDate = DateTime.UtcNow.AddYears(-2)
                },
                new User
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Trần Thị Bình",
                    Email = "truongnhom@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "team_lead",
                    Avatar = "TB",
                    Color = "#9b59b6",
                    Department = "Kỹ thuật",
                    Position = "Trưởng nhóm Phát triển",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddYears(-1),
                    JoinDate = DateTime.UtcNow.AddYears(-1)
                },
                new User
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Nguyễn Văn An",
                    Email = "nhanvien@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "employee",
                    Avatar = "NV",
                    Color = "#2ecc71",
                    Department = "Kỹ thuật",
                    Position = "Lập trình viên Fullstack",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMonths(-6),
                    JoinDate = DateTime.UtcNow.AddMonths(-6)
                },
                
                // Mở rộng thêm 12 Nhân sự ở các phòng ban
                new User
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "Vũ Hoàng Giang",
                    Email = "giang.vu@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "team_lead",
                    Avatar = "VG",
                    Color = "#1abc9c",
                    Department = "Kinh doanh",
                    Position = "Trưởng nhóm Kinh doanh B2B",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddYears(-1),
                    JoinDate = DateTime.UtcNow.AddYears(-1)
                },
                new User
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Name = "Đỗ Thùy Trang",
                    Email = "trang.do@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "employee",
                    Avatar = "DT",
                    Color = "#e84393",
                    Department = "Kinh doanh",
                    Position = "Chuyên viên Kinh doanh",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMonths(-8),
                    JoinDate = DateTime.UtcNow.AddMonths(-8)
                },
                new User
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Name = "Bùi Anh Tuấn",
                    Email = "tuan.bui@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "employee",
                    Avatar = "BT",
                    Color = "#0984e3",
                    Department = "Kỹ thuật",
                    Position = "Kỹ sư Cầu nối (BrSE)",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMonths(-5),
                    JoinDate = DateTime.UtcNow.AddMonths(-5)
                },
                new User
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Name = "Phan Minh Trí",
                    Email = "tri.phan@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "employee",
                    Avatar = "PT",
                    Color = "#2d3436",
                    Department = "Kỹ thuật",
                    Position = "Lập trình viên Mobile",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMonths(-3),
                    JoinDate = DateTime.UtcNow.AddMonths(-3)
                },
                new User
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Name = "Lâm Mỹ Lệ",
                    Email = "le.lam@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "manager",
                    Avatar = "LL",
                    Color = "#fdcb6e",
                    Department = "Nhân sự",
                    Position = "Trưởng phòng Nhân sự",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddYears(-2),
                    JoinDate = DateTime.UtcNow.AddYears(-2)
                },
                new User
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Name = "Hoàng Kim Yến",
                    Email = "yen.hoang@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "employee",
                    Avatar = "HY",
                    Color = "#fd79a8",
                    Department = "Nhân sự",
                    Position = "Chuyên viên Tuyển dụng",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMonths(-4),
                    JoinDate = DateTime.UtcNow.AddMonths(-4)
                },
                new User
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Name = "Nguyễn Hữu Nam",
                    Email = "nam.nguyen@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "manager",
                    Avatar = "HN",
                    Color = "#6c5ce7",
                    Department = "Marketing",
                    Position = "Trưởng phòng Marketing",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddYears(-1).AddMonths(-5),
                    JoinDate = DateTime.UtcNow.AddYears(-1).AddMonths(-5)
                },
                new User
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Name = "Trần Quang Minh",
                    Email = "minh.tran@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "employee",
                    Avatar = "TM",
                    Color = "#00cec9",
                    Department = "Marketing",
                    Position = "Chuyên viên Sáng tạo nội dung",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMonths(-3),
                    JoinDate = DateTime.UtcNow.AddMonths(-3)
                },
                // User đã nghỉ việc (Active = false)
                new User
                {
                    Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    Name = "Lê Thị Thu",
                    Email = "thu.le@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "employee",
                    Avatar = "LT",
                    Color = "#b2bec3",
                    Department = "Kinh doanh",
                    Position = "Cựu nhân viên Kinh doanh",
                    Active = false,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddYears(-1),
                    JoinDate = DateTime.UtcNow.AddYears(-1)
                },
                // User chưa verify Email
                new User
                {
                    Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    Name = "Mai Tiến Dũng",
                    Email = "dung.mai@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "employee",
                    Avatar = "MD",
                    Color = "#ffeaa7",
                    Department = "Kỹ thuật",
                    Position = "Thực tập sinh Lập trình",
                    Active = true,
                    IsEmailVerified = false,
                    EmailVerifiedAt = null,
                    JoinDate = DateTime.UtcNow.AddDays(-10)
                },
                // User bị khóa tạm thời (FailedLoginCount >= 3)
                new User
                {
                    Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    Name = "Trần Minh Quân",
                    Email = "quan.tran@flowspace.demo",
                    PasswordHash = defaultPasswordHash,
                    Role = "employee",
                    Avatar = "MQ",
                    Color = "#ff7675",
                    Department = "Kỹ thuật",
                    Position = "Kỹ sư hệ thống Cloud",
                    Active = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMonths(-2),
                    FailedLoginCount = 4,
                    LockoutEndAt = DateTime.UtcNow.AddMinutes(15),
                    JoinDate = DateTime.UtcNow.AddMonths(-2)
                }
            };
        }

        private static List<WorkflowRule> GetWorkflowRules()
        {
            return new List<WorkflowRule>
            {
                new WorkflowRule
                {
                    Id = Guid.Parse("91111111-9111-9111-9111-911111111111"),
                    RequestType = "leave",
                    Name = "Quy trình xin nghỉ phép thường niên",
                    SequenceSteps = "team_lead,manager",
                    IsActive = true
                },
                new WorkflowRule
                {
                    Id = Guid.Parse("92222222-9222-9222-9222-922222222222"),
                    RequestType = "overtime",
                    Name = "Quy trình phê duyệt làm thêm giờ (OT)",
                    SequenceSteps = "team_lead,manager",
                    IsActive = true
                },
                new WorkflowRule
                {
                    Id = Guid.Parse("93333333-9333-9333-9333-933333333333"),
                    RequestType = "remote",
                    Name = "Quy trình đăng ký làm việc từ xa (Remote)",
                    SequenceSteps = "team_lead",
                    IsActive = true
                },
                new WorkflowRule
                {
                    Id = Guid.Parse("94444444-9444-9444-9444-944444444444"),
                    RequestType = "purchase",
                    Name = "Quy trình mua sắm thiết bị công cụ",
                    SequenceSteps = "team_lead,manager,director",
                    IsActive = true
                }
            };
        }

        private static List<Project> GetProjects(List<User> users)
        {
            var director = users.First(u => u.Role == "director");
            var techManager = users.First(u => u.Email == "truongphong@flowspace.demo");
            var salesLead = users.First(u => u.Email == "giang.vu@flowspace.demo");
            var hrManager = users.First(u => u.Email == "le.lam@flowspace.demo");

            var projects = new List<Project>
            {
                new Project
                {
                    Id = Guid.Parse("a1111111-a111-a111-a111-a11111111111"),
                    Code = "FS-001",
                    Name = "FlowSpace Platform v2",
                    Description = "Nâng cấp toàn diện nền tảng FlowSpace lên phiên bản 2.0 với giao diện Notion-style, Kanban, Gantt chart, và Chat real-time.",
                    Status = ProjectStatus.Active,
                    Priority = ProjectPriority.High,
                    StartDate = DateTime.UtcNow.AddMonths(-3),
                    EndDate = DateTime.UtcNow.AddMonths(3),
                    Progress = 45,
                    OwnerId = techManager.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                },
                new Project
                {
                    Id = Guid.Parse("a2222222-a222-a222-a222-a22222222222"),
                    Code = "MKT-SEO",
                    Name = "Chiến dịch tối ưu SEO & Content Q3",
                    Description = "Mở rộng tiếp cận khách hàng tiềm năng qua kênh tìm kiếm tự nhiên và sản xuất nội dung blog chất lượng cao.",
                    Status = ProjectStatus.Active,
                    Priority = ProjectPriority.Medium,
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow.AddMonths(2),
                    Progress = 30,
                    OwnerId = users.First(u => u.Email == "nam.nguyen@flowspace.demo").Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-1)
                },
                new Project
                {
                    Id = Guid.Parse("a3333333-a333-a333-a333-a33333333333"),
                    Code = "SALE-B2B",
                    Name = "Mở rộng kinh doanh B2B miền Nam",
                    Description = "Tiếp cận các doanh nghiệp sản xuất và Logistics tại Bình Dương và Đồng Nai để cung cấp giải pháp FlowSpace SaaS.",
                    Status = ProjectStatus.Active,
                    Priority = ProjectPriority.High,
                    StartDate = DateTime.UtcNow.AddMonths(-2),
                    EndDate = DateTime.UtcNow.AddMonths(4),
                    Progress = 50,
                    OwnerId = salesLead.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                },
                new Project
                {
                    Id = Guid.Parse("a4444444-a444-a444-a444-a44444444444"),
                    Code = "HR-ONB",
                    Name = "Hệ thống hóa tài liệu Onboarding",
                    Description = "Xây dựng cổng thông tin tài liệu và video đào tạo nhập môn trực tuyến dành cho nhân sự mới.",
                    Status = ProjectStatus.Done,
                    Priority = ProjectPriority.Low,
                    StartDate = DateTime.UtcNow.AddMonths(-4),
                    EndDate = DateTime.UtcNow.AddMonths(-1),
                    Progress = 100,
                    OwnerId = hrManager.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new Project
                {
                    Id = Guid.Parse("a5555555-a555-a555-a555-a55555555555"),
                    Code = "CLOUD-INF",
                    Name = "Chuyển dịch Hạ tầng sang AWS Cloud",
                    Description = "Thiết kế kiến trúc HA (High Availability) trên AWS, tích hợp CI/CD tự động và bảo mật đa lớp.",
                    Status = ProjectStatus.OnHold,
                    Priority = ProjectPriority.High,
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow.AddMonths(5),
                    Progress = 10,
                    OwnerId = techManager.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-1).AddDays(-15)
                }
            };

            // Gán Members cho các Project
            projects[0].Members = new List<User> { users[1], users[2], users[3], users[6], users[7] }; // Kỹ thuật
            projects[1].Members = new List<User> { users[10], users[11] }; // Marketing
            projects[2].Members = new List<User> { users[4], users[5] }; // Kinh doanh
            projects[3].Members = new List<User> { users[8], users[9] }; // Nhân sự
            projects[4].Members = new List<User> { users[1], users[2], users[14] }; // Cloud Kỹ thuật

            return projects;
        }

        private static (List<TaskItem> tasks, List<Subtask> subtasks, List<TimeLog> timeLogs) GetTasksAndLogs(List<Project> projects, List<User> users)
        {
            var tasks = new List<TaskItem>();
            var subtasks = new List<Subtask>();
            var timeLogs = new List<TimeLog>();

            var fsProj = projects[0]; // FS-001
            var mktProj = projects[1]; // MKT-SEO
            var salesProj = projects[2]; // SALE-B2B
            var hrProj = projects[3]; // HR-ONB
            var cloudProj = projects[4]; // CLOUD-INF

            var an = users.First(u => u.Email == "nhanvien@flowspace.demo");
            var binh = users.First(u => u.Email == "truongnhom@flowspace.demo");
            var cuong = users.First(u => u.Email == "truongphong@flowspace.demo");
            var yen = users.First(u => u.Email == "yen.hoang@flowspace.demo");
            var minh = users.First(u => u.Email == "minh.tran@flowspace.demo");
            var trang = users.First(u => u.Email == "trang.do@flowspace.demo");

            // --- PROJECT FS-001: FLOWSPACE PLATFORM V2 ---
            // Task 1: Done
            var t1 = new TaskItem
            {
                Id = Guid.Parse("b1111111-b111-b111-b111-b11111111111"),
                Code = "FS-T1",
                Title = "Thiết kế Kiến trúc Cơ sở dữ liệu PostgreSQL",
                Description = "Chuyển đổi schema database từ SQLite sang PostgreSQL, tối ưu hóa các index cho chat real-time và tài liệu.",
                ProjectId = fsProj.Id,
                AssigneeId = binh.Id,
                Status = TaskStatus.Done,
                Priority = TaskPriority.High,
                StartDate = DateTime.UtcNow.AddMonths(-3),
                DueDate = DateTime.UtcNow.AddMonths(-2).AddDays(-15),
                CompletedAt = DateTime.UtcNow.AddMonths(-2).AddDays(-16),
                EstimatedHours = 24,
                CreatedBy = cuong.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            };
            tasks.Add(t1);

            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t1.Id, Title = "Định nghĩa ER Diagram", Done = true });
            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t1.Id, Title = "Viết migration script", Done = true });
            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t1.Id, Title = "Đo kiểm hiệu năng index", Done = true });

            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t1.Id, UserId = binh.Id, ProjectId = fsProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddMonths(-3).AddDays(2), Note = "Vẽ sơ đồ thực thể ER" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t1.Id, UserId = binh.Id, ProjectId = fsProj.Id, Hours = 8.5m, Date = DateTime.UtcNow.AddMonths(-3).AddDays(5), Note = "Generate migration và test trên PostgreSQL" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t1.Id, UserId = binh.Id, ProjectId = fsProj.Id, Hours = 7.5m, Date = DateTime.UtcNow.AddMonths(-3).AddDays(7), Note = "Tối ưu và gán Index cho cột khóa ngoại" });

            // Task 2: Done
            var t2 = new TaskItem
            {
                Id = Guid.Parse("b2222222-b222-b222-b222-b22222222222"),
                Code = "FS-T2",
                Title = "Xây dựng Core API Authentication & Authorization",
                Description = "Triển khai JWT token, refresh token, băm mật khẩu bằng BCrypt và phân quyền vai trò người dùng (Role-based).",
                ProjectId = fsProj.Id,
                AssigneeId = an.Id,
                Status = TaskStatus.Done,
                Priority = TaskPriority.High,
                StartDate = DateTime.UtcNow.AddMonths(-2).AddDays(-15),
                DueDate = DateTime.UtcNow.AddMonths(-2).AddDays(-5),
                CompletedAt = DateTime.UtcNow.AddMonths(-2).AddDays(-4), // Xong hơi trễ hạn
                EstimatedHours = 32,
                CreatedBy = binh.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-2).AddDays(-15)
            };
            tasks.Add(t2);

            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t2.Id, Title = "Thiết kế Middleware kiểm tra Token", Done = true });
            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t2.Id, Title = "Tích hợp BCrypt.Net", Done = true });

            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t2.Id, UserId = an.Id, ProjectId = fsProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddMonths(-2).AddDays(-13), Note = "Triển khai JWT Generator và Middleware" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t2.Id, UserId = an.Id, ProjectId = fsProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddMonths(-2).AddDays(-10), Note = "Viết controller Login / Register và băm pass" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t2.Id, UserId = an.Id, ProjectId = fsProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddMonths(-2).AddDays(-7), Note = "Xử lý refresh token và thu hồi token khi logout" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t2.Id, UserId = an.Id, ProjectId = fsProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddMonths(-2).AddDays(-4), Note = "Sửa lỗi CORS và Unit Test" });

            // Task 3: InProgress
            var t3 = new TaskItem
            {
                Id = Guid.Parse("b3333333-b333-b333-b333-b33333333333"),
                Code = "FS-T3",
                Title = "Tích hợp real-time Chat sử dụng SignalR",
                Description = "Phát triển bộ Hub gửi nhận tin nhắn tức thời, kết nối nhóm chat theo kênh dự án và chat riêng tư 1-1.",
                ProjectId = fsProj.Id,
                AssigneeId = an.Id,
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.High,
                StartDate = DateTime.UtcNow.AddDays(-15),
                DueDate = DateTime.UtcNow.AddDays(10),
                EstimatedHours = 40,
                CreatedBy = binh.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-16)
            };
            tasks.Add(t3);

            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t3.Id, Title = "Cấu hình SignalR Hub", Done = true });
            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t3.Id, Title = "Xử lý kết nối, ngắt kết nối client", Done = true });
            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t3.Id, Title = "Lưu lịch sử tin nhắn vào database", Done = false });

            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t3.Id, UserId = an.Id, ProjectId = fsProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddDays(-12), Note = "Tạo ChatHub và cấu hình Dependency Injection" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t3.Id, UserId = an.Id, ProjectId = fsProj.Id, Hours = 6.0m, Date = DateTime.UtcNow.AddDays(-8), Note = "Bắt sự kiện ConnectionId map với UserId" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t3.Id, UserId = an.Id, ProjectId = fsProj.Id, Hours = 6.0m, Date = DateTime.UtcNow.AddDays(-3), Note = "Viết API lấy tin nhắn cũ của kênh chat" });

            // Task 4: InProgress (Quá hạn)
            var t4 = new TaskItem
            {
                Id = Guid.Parse("b4444444-b444-b444-b444-b44444444444"),
                Code = "FS-T4",
                Title = "Tối ưu hóa UI/UX Layout Notion-style",
                Description = "Cải tiến thanh Sidebar bên trái hỗ trợ Folder lồng nhau, giao diện kéo thả mượt mà trên Kanban Board.",
                ProjectId = fsProj.Id,
                AssigneeId = binh.Id,
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.Medium,
                StartDate = DateTime.UtcNow.AddDays(-25),
                DueDate = DateTime.UtcNow.AddDays(-3), // Quá hạn 3 ngày
                EstimatedHours = 30,
                CreatedBy = cuong.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            };
            tasks.Add(t4);

            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t4.Id, Title = "Dựng HTML/CSS khung Notion-style", Done = true });
            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t4.Id, Title = "Tích hợp thư viện kéo thả drag-and-drop", Done = false });

            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t4.Id, UserId = binh.Id, ProjectId = fsProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddDays(-20), Note = "Dựng layout Flexbox responsive sidebar" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t4.Id, UserId = binh.Id, ProjectId = fsProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddDays(-15), Note = "Xử lý API load thư mục tài liệu lồng nhau" });

            // Task 5: Review
            var t5 = new TaskItem
            {
                Id = Guid.Parse("b5555555-b555-b555-b555-b55555555555"),
                Code = "FS-T5",
                Title = "Xây dựng cổng Phê duyệt (Approvals) 4 cấp",
                Description = "Triển khai module gửi phiếu duyệt, phân bước phê duyệt động (WorkflowRule) cho Trưởng nhóm, Trưởng phòng, Giám đốc.",
                ProjectId = fsProj.Id,
                AssigneeId = binh.Id,
                Status = TaskStatus.Review,
                Priority = TaskPriority.High,
                StartDate = DateTime.UtcNow.AddDays(-10),
                DueDate = DateTime.UtcNow.AddDays(2),
                EstimatedHours = 20,
                CreatedBy = cuong.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };
            tasks.Add(t5);

            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t5.Id, Title = "Thiết kế các API Request / Approval", Done = true });
            subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = t5.Id, Title = "Đồng bộ logic duyệt tuần tự sequence", Done = true });

            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t5.Id, UserId = binh.Id, ProjectId = fsProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddDays(-7), Note = "Tạo các model Request/Approval và Migrate" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t5.Id, UserId = binh.Id, ProjectId = fsProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddDays(-4), Note = "Hiện thực logic State machine để duyệt tuần tự" });

            // Task 6: Todo
            var t6 = new TaskItem
            {
                Id = Guid.Parse("b6666666-b666-b666-b666-b66666666666"),
                Code = "FS-T6",
                Title = "Phát triển Gantt Chart hiển thị biểu đồ tiến độ",
                Description = "Dựng Gantt Chart biểu diễn lịch trình và các liên kết phụ thuộc (dependencies) giữa các công việc trong dự án.",
                ProjectId = fsProj.Id,
                AssigneeId = users.First(u => u.Email == "tuan.bui@flowspace.demo").Id,
                Status = TaskStatus.Todo,
                Priority = TaskPriority.Medium,
                StartDate = DateTime.UtcNow.AddDays(1),
                DueDate = DateTime.UtcNow.AddDays(15),
                EstimatedHours = 24,
                CreatedBy = binh.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            tasks.Add(t6);
            t6.Dependencies.Add(t4); // Lệ thuộc vào task 4

            // --- PROJECT MKT-SEO: SEO & CONTENT Q3 ---
            var t_mkt1 = new TaskItem
            {
                Id = Guid.Parse("bc111111-bc11-bc11-bc11-bc1111111111"),
                Code = "SEO-T1",
                Title = "Nghiên cứu Từ khóa và Đối thủ cạnh tranh",
                Description = "Phân tích 50 từ khóa thương hiệu chính, 10 đối thủ hàng đầu sử dụng Ahrefs.",
                ProjectId = mktProj.Id,
                AssigneeId = minh.Id,
                Status = TaskStatus.Done,
                Priority = TaskPriority.Medium,
                StartDate = DateTime.UtcNow.AddDays(-25),
                DueDate = DateTime.UtcNow.AddDays(-15),
                CompletedAt = DateTime.UtcNow.AddDays(-16),
                EstimatedHours = 16,
                CreatedBy = users.First(u => u.Email == "nam.nguyen@flowspace.demo").Id,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            };
            tasks.Add(t_mkt1);

            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t_mkt1.Id, UserId = minh.Id, ProjectId = mktProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddDays(-23), Note = "Nghiên cứu từ khóa ngành quản trị dự án" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t_mkt1.Id, UserId = minh.Id, ProjectId = mktProj.Id, Hours = 8.0m, Date = DateTime.UtcNow.AddDays(-20), Note = "Phân tích traffic đối thủ trực tiếp" });

            // --- PROJECT SALE-B2B: SALE MIỀN NAM ---
            var t_sale1 = new TaskItem
            {
                Id = Guid.Parse("c1111111-c111-c111-c111-c11111111111"),
                Code = "SALE-T1",
                Title = "Lập danh sách 100 doanh nghiệp Logistics tiêu biểu",
                Description = "Thu thập thông tin liên hệ của Giám đốc vận hành/IT tại các khu công nghiệp Bình Dương.",
                ProjectId = salesProj.Id,
                AssigneeId = trang.Id,
                Status = TaskStatus.Done,
                Priority = TaskPriority.High,
                StartDate = DateTime.UtcNow.AddDays(-40),
                DueDate = DateTime.UtcNow.AddDays(-30),
                CompletedAt = DateTime.UtcNow.AddDays(-31),
                EstimatedHours = 18,
                CreatedBy = users.First(u => u.Email == "giang.vu@flowspace.demo").Id,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            };
            tasks.Add(t_sale1);

            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t_sale1.Id, UserId = trang.Id, ProjectId = salesProj.Id, Hours = 6.0m, Date = DateTime.UtcNow.AddDays(-38), Note = "Quét thông tin KCN VSIP" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t_sale1.Id, UserId = trang.Id, ProjectId = salesProj.Id, Hours = 6.0m, Date = DateTime.UtcNow.AddDays(-35), Note = "Quét thông tin KCN Sóng Thần" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t_sale1.Id, UserId = trang.Id, ProjectId = salesProj.Id, Hours = 6.0m, Date = DateTime.UtcNow.AddDays(-32), Note = "Tổng hợp danh sách Excel" });

            // --- PROJECT HR-ONB: ONBOARDING DOCUMENTATION ---
            var t_hr1 = new TaskItem
            {
                Id = Guid.Parse("d1111111-d111-d111-d111-d11111111111"),
                Code = "HR-T1",
                Title = "Soạn thảo tài liệu Văn hóa & Quy định Công ty",
                Description = "Viết tài liệu Hướng dẫn nhân sự mới: giờ làm việc, quy định trang phục, phúc lợi nghỉ phép.",
                ProjectId = hrProj.Id,
                AssigneeId = yen.Id,
                Status = TaskStatus.Done,
                Priority = TaskPriority.Low,
                StartDate = DateTime.UtcNow.AddMonths(-3).AddDays(-10),
                DueDate = DateTime.UtcNow.AddMonths(-3),
                CompletedAt = DateTime.UtcNow.AddMonths(-3).AddDays(-1),
                EstimatedHours = 15,
                CreatedBy = users.First(u => u.Email == "le.lam@flowspace.demo").Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-3).AddDays(-10)
            };
            tasks.Add(t_hr1);

            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t_hr1.Id, UserId = yen.Id, ProjectId = hrProj.Id, Hours = 5.0m, Date = DateTime.UtcNow.AddMonths(-3).AddDays(-8), Note = "Phác thảo quy định phúc lợi" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t_hr1.Id, UserId = yen.Id, ProjectId = hrProj.Id, Hours = 5.0m, Date = DateTime.UtcNow.AddMonths(-3).AddDays(-5), Note = "Soạn quy trình xin nghỉ phép nghỉ lễ" });
            timeLogs.Add(new TimeLog { Id = Guid.NewGuid(), TaskId = t_hr1.Id, UserId = yen.Id, ProjectId = hrProj.Id, Hours = 5.0m, Date = DateTime.UtcNow.AddMonths(-3).AddDays(-2), Note = "Hoàn tất kiểm tra lỗi chính tả và format" });

            // Thêm các task phụ khác để làm giàu dữ liệu Kanban (Tổng cộng 85 tasks)
            for (int i = 7; i <= 85; i++)
            {
                var targetProj = (i % 5) switch
                {
                    0 => fsProj,
                    1 => mktProj,
                    2 => salesProj,
                    3 => hrProj,
                    _ => cloudProj
                };

                var status = (i % 4) switch
                {
                    0 => TaskStatus.Todo,
                    1 => TaskStatus.InProgress,
                    2 => TaskStatus.Review,
                    _ => TaskStatus.Done
                };

                var priority = (i % 3) switch
                {
                    0 => TaskPriority.Low,
                    1 => TaskPriority.Medium,
                    _ => TaskPriority.High
                };

                var assignee = users[i % users.Count];
                if (!assignee.Active) assignee = users[0]; // Tránh gán cho user nghỉ việc

                var code = $"{targetProj.Code}-T{i}";
                var titleStr = (i % 5) switch
                {
                    0 => $"Tính năng mở rộng #{i}: Đồng bộ Gantt",
                    1 => $"Viết bài viết nội dung SEO chủ đề #{i}",
                    2 => $"Gặp gỡ và tư vấn khách hàng #{i}",
                    3 => $"Đào tạo hội nhập chuyên môn #{i}",
                    _ => $"Cấu hình tự động hóa CI/CD AWS #{i}"
                };

                var tempTask = new TaskItem
                {
                    Id = Guid.Parse($"b{i:D7}-b{i:D3}-b{i:D3}-b{i:D3}-b{i:D11}"),
                    Code = code,
                    Title = titleStr,
                    Description = $"Mô tả chi tiết của nhiệm vụ {code} phục vụ tiến trình vận hành chuẩn của dự án.",
                    ProjectId = targetProj.Id,
                    AssigneeId = assignee.Id,
                    Status = status,
                    Priority = priority,
                    StartDate = DateTime.UtcNow.AddDays(-i),
                    DueDate = DateTime.UtcNow.AddDays(-i + 15),
                    CompletedAt = status == TaskStatus.Done ? (DateTime?)DateTime.UtcNow.AddDays(-i + 10) : null,
                    EstimatedHours = 8 + (i % 16),
                    CreatedBy = users[0].Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-i - 2)
                };

                tasks.Add(tempTask);

                // Thêm Subtask cho các task phụ này
                subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = tempTask.Id, Title = "Nhiệm vụ con giai đoạn 1", Done = status == TaskStatus.Done });
                subtasks.Add(new Subtask { Id = Guid.NewGuid(), TaskId = tempTask.Id, Title = "Nhiệm vụ con giai đoạn 2", Done = status == TaskStatus.Done });

                // Thêm TimeLogs cho các task đã Done hoặc InProgress để khớp LoggedHours
                if (status == TaskStatus.Done || status == TaskStatus.InProgress)
                {
                    timeLogs.Add(new TimeLog
                    {
                        Id = Guid.NewGuid(),
                        TaskId = tempTask.Id,
                        UserId = assignee.Id,
                        ProjectId = targetProj.Id,
                        Hours = 4.0m + (i % 5),
                        Date = DateTime.UtcNow.AddDays(-2),
                        Note = "Ghi nhận tiến độ làm việc thực tế hàng ngày"
                    });
                }
            }

            return (tasks, subtasks, timeLogs);
        }

        private static List<Comment> GetComments(List<TaskItem> tasks, List<User> users)
        {
            var t3 = tasks.First(t => t.Id == Guid.Parse("b3333333-b333-b333-b333-b33333333333")); // SignalR Task
            var t4 = tasks.First(t => t.Id == Guid.Parse("b4444444-b444-b444-b444-b44444444444")); // UI Notion Task
            
            var an = users.First(u => u.Email == "nhanvien@flowspace.demo");
            var binh = users.First(u => u.Email == "truongnhom@flowspace.demo");
            var cuong = users.First(u => u.Email == "truongphong@flowspace.demo");

            return new List<Comment>
            {
                new Comment
                {
                    Id = Guid.NewGuid(),
                    TaskId = t3.Id,
                    UserId = binh.Id,
                    Text = "An ơi, tiến độ cài đặt SignalR tới đâu rồi? Có gặp khó khăn gì với việc CORS kết nối từ Vercel về không?",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Comment
                {
                    Id = Guid.NewGuid(),
                    TaskId = t3.Id,
                    UserId = an.Id,
                    Text = "Dạ em đã hoàn thành cấu hình CORS ở Backend và kết nối SignalR Hub từ localhost chạy ngon lành rồi. Đang test đẩy lên Vercel xem có bị vướng gì không ạ.",
                    CreatedAt = DateTime.UtcNow.AddDays(-9)
                },
                new Comment
                {
                    Id = Guid.NewGuid(),
                    TaskId = t3.Id,
                    UserId = cuong.Id,
                    Text = "Rất tốt. Hãy nhớ kiểm tra độ trễ kết nối khi deploy container trên Singapore nhé. Nếu cần Redis Backplane để scale thì báo anh.",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new Comment
                {
                    Id = Guid.NewGuid(),
                    TaskId = t4.Id,
                    UserId = cuong.Id,
                    Text = "Bình lưu ý phần kéo thả ở Kanban Board phải chạy mượt cả trên iPad nhé. Bữa trước test thử bản v1 thấy trên máy tính bảng vuốt bị khựng.",
                    CreatedAt = DateTime.UtcNow.AddDays(-14)
                },
                new Comment
                {
                    Id = Guid.NewGuid(),
                    TaskId = t4.Id,
                    UserId = binh.Id,
                    Text = "Dạ em ghi nhận, em đã đổi sang thư viện DragDrop hỗ trợ touch-punch cho thiết bị di động, hiện đang tối ưu CSS transition để vuốt mượt mà hơn ạ.",
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                }
            };
        }

        private static List<Document> GetDocuments(List<User> users)
        {
            var admin = users.First(u => u.Role == "director");
            var manager = users.First(u => u.Email == "truongphong@flowspace.demo");

            var folder1Id = Guid.Parse("f1111111-f111-f111-f111-f11111111111");
            var folder2Id = Guid.Parse("f2222222-f222-f222-f222-f22222222222");

            return new List<Document>
            {
                // Thư mục cha 1
                new Document
                {
                    Id = folder1Id,
                    Name = "Tài liệu Kỹ thuật & Thiết kế",
                    Type = "Folder",
                    Size = 0,
                    Url = "",
                    ParentId = null,
                    CreatedBy = manager.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                },
                // Thư mục con nằm trong Thư mục cha 1
                new Document
                {
                    Id = Guid.Parse("d0111111-d011-d011-d011-d01111111111"),
                    Name = "Đặc tả API v2.docx",
                    Type = "Doc",
                    Size = 1048576, // 1MB
                    Url = "https://flowspace.storage/docs/api-spec-v2.docx",
                    ParentId = folder1Id,
                    CreatedBy = manager.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                },
                new Document
                {
                    Id = Guid.Parse("d0222222-d022-d022-d022-d02222222222"),
                    Name = "Kiến trúc hạ tầng AWS.pdf",
                    Type = "Pdf",
                    Size = 2516582, // 2.4MB
                    Url = "https://flowspace.storage/docs/aws-architecture.pdf",
                    ParentId = folder1Id,
                    CreatedBy = manager.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                },

                // Thư mục cha 2
                new Document
                {
                    Id = folder2Id,
                    Name = "Tài chính & Ngân sách",
                    Type = "Folder",
                    Size = 0,
                    Url = "",
                    ParentId = null,
                    CreatedBy = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new Document
                {
                    Id = Guid.Parse("d0333333-d033-d033-d033-d03311111111"),
                    Name = "Ngân sách phát triển Q3.xlsx",
                    Type = "Sheet",
                    Size = 512000,
                    Url = "https://flowspace.storage/docs/budget-q3.xlsx",
                    ParentId = folder2Id,
                    CreatedBy = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-1)
                },
                new Document
                {
                    Id = Guid.Parse("d0444444-d044-d044-d044-d04411111111"),
                    Name = "Báo cáo doanh thu Q2.pdf",
                    Type = "Pdf",
                    Size = 3145728,
                    Url = "https://flowspace.storage/docs/revenue-q2.pdf",
                    ParentId = folder2Id,
                    CreatedBy = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-1).AddDays(-15)
                }
            };
        }

        private static (List<ChatChannel> channels, List<ChatMessage> messages) GetChatData(List<User> users)
        {
            var channels = new List<ChatChannel>();
            var messages = new List<ChatMessage>();

            var channelTech = new ChatChannel
            {
                Id = Guid.Parse("c0111111-c011-c011-c011-c01111111111"),
                Name = "Kênh Kỹ thuật",
                Description = "Kênh trao đổi chuyên môn kỹ thuật, chia sẻ code và review lỗi hệ thống.",
                IsDirectMessage = false,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            };
            channels.Add(channelTech);

            var channelSales = new ChatChannel
            {
                Id = Guid.Parse("c0222222-c022-c022-c022-c02222222222"),
                Name = "Kênh Kinh doanh B2B",
                Description = "Kênh cập nhật chỉ tiêu doanh số và lịch trình gặp gỡ khách hàng.",
                IsDirectMessage = false,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };
            channels.Add(channelSales);

            var channelDM1 = new ChatChannel
            {
                Id = Guid.Parse("c0333333-c033-c033-c033-c03322222222"),
                Name = "Trần Thị Bình & Nguyễn Văn An",
                Description = "Tin nhắn trực tiếp 1-1",
                IsDirectMessage = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };
            channels.Add(channelDM1);

            // Tin nhắn trong Kênh Kỹ thuật
            var an = users.First(u => u.Email == "nhanvien@flowspace.demo");
            var binh = users.First(u => u.Email == "truongnhom@flowspace.demo");
            var cuong = users.First(u => u.Email == "truongphong@flowspace.demo");

            var m1 = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ChannelId = channelTech.Id,
                SenderId = cuong.Id,
                Content = "Chào cả nhà, chúng ta bắt đầu chuyển dịch dự án FlowSpace sang PostgreSQL nhé. Mọi người chuẩn bị migrate database local.",
                IsPinned = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            };
            messages.Add(m1);

            var m2 = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ChannelId = channelTech.Id,
                SenderId = binh.Id,
                Content = "Dạ vâng anh Cường, em đã tạo sẵn schema trên PostgreSQL và commit file migration lên rồi ạ.",
                ReplyToMessageId = m1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-14)
            };
            messages.Add(m2);

            var m3 = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ChannelId = channelTech.Id,
                SenderId = an.Id,
                Content = "Mọi người chạy lệnh dotnet ef database update để cập nhật cấu trúc database local nhé.",
                CreatedAt = DateTime.UtcNow.AddDays(-13)
            };
            messages.Add(m3);

            // Chat thu hồi
            var m4 = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ChannelId = channelTech.Id,
                SenderId = an.Id,
                Content = "Tin nhắn này đã bị thu hồi.",
                IsRecalled = true,
                CreatedAt = DateTime.UtcNow.AddDays(-12)
            };
            messages.Add(m4);

            return (channels, messages);
        }

        private static (List<Request> requests, List<Approval> approvals) GetRequestsAndApprovals(List<User> users)
        {
            var requests = new List<Request>();
            var approvals = new List<Approval>();

            var an = users.First(u => u.Email == "nhanvien@flowspace.demo");
            var binh = users.First(u => u.Email == "truongnhom@flowspace.demo");
            var cuong = users.First(u => u.Email == "truongphong@flowspace.demo");
            var dung = users.First(u => u.Role == "director");

            // --- REQUEST 1: Leave (Nghỉ phép) - Trạng thái Approved ---
            var r1 = new Request
            {
                Id = Guid.Parse("e1111111-e111-e111-e111-e11111111111"),
                Type = RequestType.Leave,
                Title = "Xin nghỉ phép kết hôn",
                Description = "Tôi xin phép nghỉ 3 ngày phép thường niên từ ngày 25/07 đến hết ngày 27/07 để chuẩn bị công việc gia đình.",
                RequesterId = an.Id,
                Status = RequestStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-8)
            };
            requests.Add(r1);

            // Duyệt cấp 1: Team Lead
            approvals.Add(new Approval
            {
                Id = Guid.NewGuid(),
                RequestId = r1.Id,
                Level = 1,
                Role = "team_lead",
                ApproverId = binh.Id,
                Status = ApprovalStatus.Approved,
                Note = "Chúc mừng hạnh phúc em, công việc ở nhóm đã bàn giao lại cho Tuấn hỗ trợ.",
                UpdatedAt = DateTime.UtcNow.AddDays(-9)
            });
            // Duyệt cấp 2: Manager
            approvals.Add(new Approval
            {
                Id = Guid.NewGuid(),
                RequestId = r1.Id,
                Level = 2,
                Role = "manager",
                ApproverId = cuong.Id,
                Status = ApprovalStatus.Approved,
                Note = "Đã phê duyệt, phòng kỹ thuật chúc mừng hạnh phúc gia đình.",
                UpdatedAt = DateTime.UtcNow.AddDays(-8)
            });

            // --- REQUEST 2: Purchase (Mua sắm) - Trạng thái Rejected ---
            var r2 = new Request
            {
                Id = Guid.Parse("e2222222-e222-e222-e222-e22222222222"),
                Type = RequestType.Purchase,
                Title = "Yêu cầu nâng cấp License Docker Desktop Pro",
                Description = "Yêu cầu chi phí mua sắm 5 account license Docker Desktop Pro phục vụ cho các lập trình viên Kỹ thuật phát triển và đóng gói container local.",
                RequesterId = cuong.Id,
                Status = RequestStatus.Rejected,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            };
            requests.Add(r2);

            // Duyệt cấp 1: Director
            approvals.Add(new Approval
            {
                Id = Guid.NewGuid(),
                RequestId = r2.Id,
                Level = 1,
                Role = "director",
                ApproverId = dung.Id,
                Status = ApprovalStatus.Rejected,
                Note = "Từ chối phê duyệt. Hiện công ty đã chuyển sang phát triển hoàn toàn bằng Rancher Desktop mã nguồn mở miễn phí, không sử dụng Docker Desktop nữa.",
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            });

            // --- REQUEST 3: Remote (Làm việc từ xa) - Trạng thái Pending ---
            var r3 = new Request
            {
                Id = Guid.Parse("e3333333-e333-e333-e333-e33333333333"),
                Type = RequestType.Remote,
                Title = "Đăng ký làm việc từ xa (Remote) do điều trị răng",
                Description = "Xin phép làm việc remote tại nhà ngày 22/07 do có lịch hẹn tiểu phẫu nha khoa, tôi vẫn online đầy đủ để xử lý task đúng hạn.",
                RequesterId = an.Id,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };
            requests.Add(r3);

            // Duyệt cấp 1: Team Lead (Đang Pending chưa xử lý)
            approvals.Add(new Approval
            {
                Id = Guid.NewGuid(),
                RequestId = r3.Id,
                Level = 1,
                Role = "team_lead",
                ApproverId = binh.Id,
                Status = ApprovalStatus.Pending,
                Note = null,
                UpdatedAt = null
            });

            return (requests, approvals);
        }

        private static List<AuditLog> GetAuditLogs(List<User> users)
        {
            var admin = users.First(u => u.Email == "admin@flowspace.demo");
            var employee = users.First(u => u.Email == "nhanvien@flowspace.demo");

            return new List<AuditLog>
            {
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = admin.Id,
                    Action = "Login",
                    IpAddress = "113.161.40.22",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/115.0.0.0",
                    Detail = "Giám đốc đăng nhập hệ thống thành công.",
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = employee.Id,
                    Action = "Login",
                    IpAddress = "118.69.176.12",
                    UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) Safari/16.5",
                    Detail = "Nhân viên đăng nhập hệ thống thành công.",
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                },
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = null,
                    Action = "LoginFailed",
                    IpAddress = "222.252.30.95",
                    UserAgent = "Mozilla/5.0 (Linux; Android 10) Chrome/114.0.0.0",
                    Detail = "Đăng nhập thất bại do sai mật khẩu cho email: hack.attempt@secure.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = employee.Id,
                    Action = "Logout",
                    IpAddress = "118.69.176.12",
                    UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) Safari/16.5",
                    Detail = "Nhân viên đăng xuất hệ thống.",
                    CreatedAt = DateTime.UtcNow.AddDays(-12).AddHours(8)
                }
            };
        }
    }
}
