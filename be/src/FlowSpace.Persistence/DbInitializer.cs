using System;
using System.Collections.Generic;
using System.Linq;
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
                Console.Error.WriteLine($"Lỗi kiểm tra trước khi seed: {ex.Message}");
                return;
            }

            try
            {
                // 1. Seed Users (15 users để khớp với frontend)
                var users = GetUsers();
                context.Users.AddRange(users);
                context.SaveChanges();

                // 2. Seed Workflow Rules
                var rules = GetWorkflowRules();
                context.WorkflowRules.AddRange(rules);
                context.SaveChanges();

                // 3. Seed Projects (5 dự án)
                var projects = GetProjects(users);
                context.Projects.AddRange(projects);
                context.SaveChanges();

                // 4. Sinh tự động 85 Tasks, Subtasks & TimeLogs bằng vòng lặp thông minh để giảm dung lượng compile
                var (tasks, subtasks, timeLogs) = GenerateTasksAndLogs(projects, users);
                context.Tasks.AddRange(tasks);
                context.SaveChanges();

                context.Subtasks.AddRange(subtasks);
                context.TimeLogs.AddRange(timeLogs);
                context.SaveChanges();

                // Cập nhật LoggedHours của TaskItem
                foreach (var task in tasks)
                {
                    task.LoggedHours = timeLogs.Where(tl => tl.TaskId == task.Id).Sum(tl => tl.Hours);
                }
                
                // Cập nhật Progress của Projects
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
                var comments = GenerateComments(tasks, users);
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
                Console.Error.WriteLine($"Lỗi trong quá trình seed database: {ex.Message}");
            }
        }

        private static List<User> GetUsers()
        {
            string defaultPasswordHash = "$2a$11$9Q0c4Y1wVp0d1HqLd5S8OeWzE1V0d1HqLd5S8OeWzE1V0d1HqLd5S"; 

            return new List<User>
            {
                new User { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Phạm Thanh Dung", Email = "admin@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "director", Avatar = "PD", Color = "#e74c3c", Department = "Ban giám đốc", Position = "Giám đốc điều hành", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddYears(-3), JoinDate = DateTime.UtcNow.AddYears(-3) },
                new User { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Lê Minh Cường", Email = "truongphong@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "manager", Avatar = "LC", Color = "#e67e22", Department = "Kỹ thuật", Position = "Trưởng phòng Kỹ thuật", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddYears(-2), JoinDate = DateTime.UtcNow.AddYears(-2) },
                new User { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Trần Thị Bình", Email = "truongnhom@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "team_lead", Avatar = "TB", Color = "#9b59b6", Department = "Kỹ thuật", Position = "Trưởng nhóm Phát triển", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddYears(-1), JoinDate = DateTime.UtcNow.AddYears(-1) },
                new User { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Nguyễn Văn An", Email = "nhanvien@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "employee", Avatar = "NV", Color = "#2ecc71", Department = "Kỹ thuật", Position = "Lập trình viên Fullstack", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddMonths(-6), JoinDate = DateTime.UtcNow.AddMonths(-6) },
                new User { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Vũ Hoàng Giang", Email = "giang.vu@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "team_lead", Avatar = "VG", Color = "#1abc9c", Department = "Kinh doanh", Position = "Trưởng nhóm Kinh doanh B2B", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddYears(-1), JoinDate = DateTime.UtcNow.AddYears(-1) },
                new User { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Đỗ Thùy Trang", Email = "trang.do@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "employee", Avatar = "DT", Color = "#e84393", Department = "Kinh doanh", Position = "Chuyên viên Kinh doanh", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddMonths(-8), JoinDate = DateTime.UtcNow.AddMonths(-8) },
                new User { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Bùi Anh Tuấn", Email = "tuan.bui@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "employee", Avatar = "BT", Color = "#0984e3", Department = "Kỹ thuật", Position = "Kỹ sư Cầu nối (BrSE)", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddMonths(-5), JoinDate = DateTime.UtcNow.AddMonths(-5) },
                new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Phan Minh Trí", Email = "tri.phan@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "employee", Avatar = "PT", Color = "#2d3436", Department = "Kỹ thuật", Position = "Lập trình viên Mobile", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddMonths(-3), JoinDate = DateTime.UtcNow.AddMonths(-3) },
                new User { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), Name = "Mai Xuân Bách", Email = "bach.mai@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "employee", Avatar = "MB", Color = "#ffeaa7", Department = "Kỹ thuật", Position = "UI/UX Designer", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddMonths(-9), JoinDate = DateTime.UtcNow.AddMonths(-9) },
                new User { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "Hoàng Kim Ngân", Email = "ngan.hoang@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "team_lead", Avatar = "HN", Color = "#fd79a8", Department = "Marketing", Position = "Trưởng nhóm Marketing", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddYears(-1), JoinDate = DateTime.UtcNow.AddYears(-1) },
                new User { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "Đặng Hồng Nhung", Email = "nhung.dang@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "employee", Avatar = "DN", Color = "#fdcb6e", Department = "Marketing", Position = "Copywriter & SEO", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddMonths(-7), JoinDate = DateTime.UtcNow.AddMonths(-7) },
                new User { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Name = "Vũ Đình Phong", Email = "phong.vu@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "employee", Avatar = "VP", Color = "#00b894", Department = "Marketing", Position = "Designer & Video Editor", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddMonths(-4), JoinDate = DateTime.UtcNow.AddMonths(-4) },
                new User { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Name = "Lâm Kiều Chinh", Email = "chinh.lam@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "team_lead", Avatar = "LC", Color = "#00cec9", Department = "Nhân sự", Position = "Trưởng nhóm Tuyển dụng", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddYears(-1), JoinDate = DateTime.UtcNow.AddYears(-1) },
                new User { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), Name = "Cao Tiến Thành", Email = "thanh.cao@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "employee", Avatar = "CT", Color = "#a29bfe", Department = "Nhân sự", Position = "Chuyên viên Nhân sự", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddMonths(-6), JoinDate = DateTime.UtcNow.AddMonths(-6) },
                new User { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Name = "Nguyễn Khánh Linh", Email = "linh.nguyen@flowspace.demo", PasswordHash = defaultPasswordHash, Role = "employee", Avatar = "KL", Color = "#ffeaa7", Department = "Nhân sự", Position = "Chuyên viên C&B", Active = true, IsEmailVerified = true, EmailVerifiedAt = DateTime.UtcNow.AddMonths(-5), JoinDate = DateTime.UtcNow.AddMonths(-5) }
            };
        }

        private static List<WorkflowRule> GetWorkflowRules()
        {
            return new List<WorkflowRule>
            {
                new WorkflowRule { Id = Guid.NewGuid(), Name = "Duyệt đơn nghỉ phép", RequestType = "leave", SequenceSteps = "team_lead,manager,director", IsActive = true },
                new WorkflowRule { Id = Guid.NewGuid(), Name = "Duyệt chi phí mua sắm", RequestType = "purchase", SequenceSteps = "team_lead,manager,director", IsActive = true },
                new WorkflowRule { Id = Guid.NewGuid(), Name = "Duyệt yêu cầu làm việc từ xa", RequestType = "remote", SequenceSteps = "team_lead,manager,director", IsActive = true }
            };
        }

        private static List<Project> GetProjects(List<User> users)
        {
            var p1 = new Project { Id = Guid.Parse("10101010-1010-1010-1010-101010101010"), Code = "FSP-V2", Name = "FlowSpace Platform v2", Description = "Phát triển phiên bản 2.0 cho FlowSpace", Status = ProjectStatus.Active, Priority = ProjectPriority.High, StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(4), OwnerId = users[1].Id, CreatedAt = DateTime.UtcNow.AddMonths(-2) };
            var p2 = new Project { Id = Guid.Parse("20202020-2020-2020-2020-202020202020"), Code = "CAMP-AUT", Name = "Campaign Marketing Autumn 2026", Description = "Chiến dịch tiếp thị mùa thu", Status = ProjectStatus.Active, Priority = ProjectPriority.Medium, StartDate = DateTime.UtcNow.AddMonths(-1), EndDate = DateTime.UtcNow.AddMonths(2), OwnerId = users[9].Id, CreatedAt = DateTime.UtcNow.AddMonths(-1) };
            var p3 = new Project { Id = Guid.Parse("30303030-3030-3030-3030-303030303030"), Code = "HR-RECR", Name = "HR Recruitment System Expansion", Description = "Hệ thống mở rộng tuyển dụng", Status = ProjectStatus.Active, Priority = ProjectPriority.Medium, StartDate = DateTime.UtcNow.AddMonths(-1).AddDays(-15), EndDate = DateTime.UtcNow.AddMonths(2).AddDays(15), OwnerId = users[12].Id, CreatedAt = DateTime.UtcNow.AddMonths(-1).AddDays(-15) };
            var p4 = new Project { Id = Guid.Parse("40404040-4040-4040-4040-404040404040"), Code = "ISO-COMP", Name = "ISO Security Compliance Certification", Description = "Chứng nhận tuân thủ bảo mật", Status = ProjectStatus.OnHold, Priority = ProjectPriority.High, StartDate = DateTime.UtcNow.AddMonths(-3), EndDate = DateTime.UtcNow.AddMonths(1), OwnerId = users[1].Id, CreatedAt = DateTime.UtcNow.AddMonths(-3) };
            var p5 = new Project { Id = Guid.Parse("50505050-5050-5050-5050-505050505050"), Code = "SaaS-BI", Name = "SaaS Billing Integration", Description = "Tích hợp cổng thanh toán SaaS", Status = ProjectStatus.Done, Priority = ProjectPriority.High, StartDate = DateTime.UtcNow.AddMonths(-4), EndDate = DateTime.UtcNow.AddMonths(-1), OwnerId = users[1].Id, CreatedAt = DateTime.UtcNow.AddMonths(-4), Progress = 100 };

            // Gán thành viên cho từng dự án
            p1.Members = new List<User> { users[1], users[2], users[3], users[6], users[7], users[8] };
            p2.Members = new List<User> { users[9], users[10], users[11], users[8] };
            p3.Members = new List<User> { users[12], users[13], users[14] };
            p4.Members = new List<User> { users[1], users[2], users[3], users[7] };
            p5.Members = new List<User> { users[1], users[2], users[3] };

            return new List<Project> { p1, p2, p3, p4, p5 };
        }

        private static Tuple<List<TaskItem>, List<Subtask>, List<TimeLog>> GenerateTasksAndLogs(List<Project> projects, List<User> users)
        {
            var tasks = new List<TaskItem>();
            var subtasks = new List<Subtask>();
            var timeLogs = new List<TimeLog>();

            string[] taskNames = new[] {
                "Thiết lập kiến trúc cơ sở dữ liệu v2",
                "Phân tích tài liệu yêu cầu khách hàng",
                "Phát triển API Đăng nhập và Đăng ký",
                "Thiết kế giao diện bảng Kanban",
                "Tích hợp biểu đồ Gantt vào dashboard",
                "Xây dựng hệ thống chat real-time",
                "Cấu hình phân quyền dựa trên vai trò",
                "Viết unit test cho các service chính",
                "Tải lên tài liệu hướng dẫn sử dụng",
                "Thiết lập kênh thông báo đẩy",
                "Tối ưu hiệu năng truy vấn Entity Framework",
                "Kiểm tra bảo mật và rò rỉ bộ nhớ",
                "Đồng bộ hóa dữ liệu ngoại tuyến",
                "Sửa lỗi huy hiệu chuông thông báo",
                "Tích hợp cổng thanh toán Stripe & VNPay",
                "Tối ưu giao diện đáp ứng thiết bị di động",
                "Viết tài liệu API Swagger chi tiết"
            };

            int taskIdCounter = 1;
            for (int pIdx = 0; pIdx < projects.Count; pIdx++)
            {
                var proj = projects[pIdx];
                var members = proj.Members.Any() ? proj.Members.ToList() : users;

                for (int i = 0; i < 17; i++) 
                {
                    var status = (TaskStatus)(i % 4); 
                    if (proj.Status == ProjectStatus.Done) status = TaskStatus.Done;

                    var priority = (TaskPriority)(i % 3); 
                    var assignee = members[i % members.Count];

                    var task = new TaskItem
                    {
                        Id = Guid.NewGuid(),
                        Code = $"{proj.Code}-{taskIdCounter++}",
                        Title = taskNames[i % taskNames.Length] + $" (P{pIdx+1}.{i+1})",
                        Description = $"Mô tả chi tiết cho nhiệm vụ sinh tự động thuộc dự án {proj.Name}.",
                        ProjectId = proj.Id,
                        AssigneeId = assignee.Id,
                        Status = status,
                        Priority = priority,
                        StartDate = DateTime.UtcNow.AddDays(-20 + i),
                        DueDate = DateTime.UtcNow.AddDays(i - 5),
                        EstimatedHours = 8 + (i * 2),
                        CreatedAt = DateTime.UtcNow.AddDays(-21 + i)
                    };

                    if (status == TaskStatus.Done)
                    {
                        task.CompletedAt = DateTime.UtcNow.AddDays(i - 6);
                    }

                    tasks.Add(task);

                    subtasks.Add(new Subtask
                    {
                        Id = Guid.NewGuid(),
                        TaskId = task.Id,
                        Title = $"Kiểm tra kỹ thuật bước phụ của: {task.Title}",
                        Done = status == TaskStatus.Done,
                        CreatedAt = task.CreatedAt
                    });

                    if (status == TaskStatus.InProgress || status == TaskStatus.Done)
                    {
                        timeLogs.Add(new TimeLog
                        {
                            Id = Guid.NewGuid(),
                            TaskId = task.Id,
                            UserId = assignee.Id,
                            ProjectId = proj.Id,
                            Hours = 2 + (i % 4),
                            Date = DateTime.UtcNow.AddDays(-(i % 5)),
                            Note = $"Hoàn thành công việc phụ {task.Title}",
                            CreatedAt = DateTime.UtcNow.AddDays(-(i % 5))
                        });
                    }
                }
            }

            return Tuple.Create(tasks, subtasks, timeLogs);
        }

        private static List<Comment> GenerateComments(List<TaskItem> tasks, List<User> users)
        {
            var comments = new List<Comment>();
            string[] commentTexts = new[] {
                "Tôi đã hoàn thành phần core, nhờ leader review giúp.",
                "Cần kiểm tra kỹ lại UI trên trình duyệt Safari nhé.",
                "Đã sửa xong lỗi rò rỉ bộ nhớ, mọi người test thử.",
                "Mọi thứ chạy ổn định, sẵn sàng deploy."
            };

            for (int i = 0; i < Math.Min(tasks.Count, 30); i++)
            {
                var task = tasks[i];
                var user = users[i % users.Count];
                comments.Add(new Comment
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    UserId = user.Id,
                    Text = commentTexts[i % commentTexts.Length],
                    CreatedAt = DateTime.UtcNow.AddHours(-i)
                });
            }
            return comments;
        }

        private static List<Document> GetDocuments(List<User> users)
        {
            return new List<Document>
            {
                new Document { Id = Guid.NewGuid(), Name = "Quy định làm việc và OT.pdf", Type = "pdf", Size = 102450, Url = "https://flowspace.demo/docs/ot.pdf", CreatedBy = users[0].Id, CreatedAt = DateTime.UtcNow.AddMonths(-5) },
                new Document { Id = Guid.NewGuid(), Name = "Kien_truc_he_thong.docx", Type = "docx", Size = 450000, Url = "https://flowspace.demo/docs/architecture.docx", CreatedBy = users[1].Id, CreatedAt = DateTime.UtcNow.AddMonths(-2) }
            };
        }

        private static Tuple<List<ChatChannel>, List<ChatMessage>> GetChatData(List<User> users)
        {
            var channels = new List<ChatChannel>
            {
                new ChatChannel { Id = Guid.Parse("11111111-2222-3333-4444-555555555555"), Name = "Thảo luận chung", Description = "Kênh thảo luận toàn công ty", IsDirectMessage = false, CreatedAt = DateTime.UtcNow.AddYears(-1) }
            };

            var messages = new List<ChatMessage>
            {
                new ChatMessage { Id = Guid.NewGuid(), ChannelId = channels[0].Id, SenderId = users[0].Id, Content = "Chào cả nhà, chúc mọi người tuần mới làm việc vui vẻ!", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new ChatMessage { Id = Guid.NewGuid(), ChannelId = channels[0].Id, SenderId = users[1].Id, Content = "Chào Giám đốc! Team phát triển đang tiến hành code sprint v2.", CreatedAt = DateTime.UtcNow.AddDays(-2).AddMinutes(5) }
            };

            return Tuple.Create(channels, messages);
        }

        private static Tuple<List<Request>, List<Approval>> GetRequestsAndApprovals(List<User> users)
        {
            var requests = new List<Request>
            {
                new Request { Id = Guid.Parse("90909090-9090-9090-9090-909090909090"), Title = "Yêu cầu nghỉ phép - Nguyễn Văn An", Description = "Xin nghỉ phép 2 ngày giải quyết việc gia đình.", Type = RequestType.Leave, Status = RequestStatus.Pending, RequesterId = users[3].Id, CreatedAt = DateTime.UtcNow.AddDays(-1) }
            };

            var approvals = new List<Approval>
            {
                new Approval { Id = Guid.NewGuid(), RequestId = requests[0].Id, Level = 1, Role = "team_lead", ApproverId = users[2].Id, Status = ApprovalStatus.Approved, Note = "Đồng ý chuyển tiếp lên Trưởng phòng.", UpdatedAt = DateTime.UtcNow.AddHours(-5) },
                new Approval { Id = Guid.NewGuid(), RequestId = requests[0].Id, Level = 2, Role = "manager", ApproverId = users[1].Id, Status = ApprovalStatus.Pending }
            };

            return Tuple.Create(requests, approvals);
        }

        private static List<AuditLog> GetAuditLogs(List<User> users)
        {
            return new List<AuditLog>
            {
                new AuditLog { Id = Guid.NewGuid(), UserId = users[0].Id, Action = "LOGIN", Detail = "Đăng nhập hệ thống thành công", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new AuditLog { Id = Guid.NewGuid(), UserId = users[3].Id, Action = "UPDATE", Detail = "Cập nhật tiến độ task FSP-V2-1 lên 80%", CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
                new AuditLog { Id = Guid.NewGuid(), UserId = users[1].Id, Action = "CREATE", Detail = "Tạo dự án mới SaaS Billing Integration", CreatedAt = DateTime.UtcNow.AddHours(-2) }
            };
        }
    }
}
