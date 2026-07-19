# FlowSpace — Không gian Làm việc & Quản trị Dự án Notion-Style

FlowSpace là nền tảng quản lý công việc và dự án cộng tác tối giản theo phong cách Notion. Hệ thống kết hợp bảng quản lý công việc Kanban, sơ đồ Gantt chart trực quan, lịch biểu FullCalendar, bộ đếm giờ làm việc (Time Tracking), không gian thảo luận nhóm real-time qua SignalR WebSockets, soạn thảo tài liệu WYSIWYG và quy trình phê duyệt công việc tự động 4 cấp.

---

## 📸 Ảnh Giao diện & Video Demo

### 1. Ảnh Giao diện Ứng dụng (Screenshots)
Các hình ảnh giao diện thực tế nằm tại thư mục [`screenshots/`](file:///e:/flowspace-fe/screenshots/):
- **Dashboard Tổng quan**: Thống kê chỉ số KPI, tiến độ dự án, công việc quá hạn & biểu đồ năng suất.
- **Bảng Kanban**: Kéo thả công việc giữa các cột trạng thái (*Chưa bắt đầu*, *Đang làm*, *Chờ duyệt*, *Hoàn thành*).
- **Gantt Chart**: Trực quan hóa timeline dự án, mối quan hệ phụ thuộc công việc và đường găng (Critical Path).
- **Lịch biểu & Time Tracking**: Theo dõi hạn hoàn thành theo ngày/tuần/tháng và bộ đếm giờ tự động.
- **Quy trình Phê duyệt 4 cấp**: Duyệt đơn tự động qua các cấp Trưởng nhóm ➔ Quản lý ➔ Giám đốc.

### 2. Video Demo
- 🎥 **Demo Video YouTube**: [Xem Video Trải nghiệm FlowSpace Platform v1.0.0](https://youtube.com) *(Đang cập nhật link)*

---

## 🏗️ Kiến trúc Hệ thống (Architecture)

FlowSpace được thiết kế theo mô hình tách biệt Frontend và Backend (Clean Architecture):

```
FlowSpace Solution
├── backend/src/
│   ├── FlowSpace.Domain/         # Core Entities, Enums & Interfaces
│   ├── FlowSpace.Application/    # DTOs, Mapping Profiles, Services (Project, Task, TimeLog, Workflow, Dashboard)
│   ├── FlowSpace.Infrastructure/ # JWT Token Generator & BCrypt Password Hashing
│   ├── FlowSpace.Persistence/    # EF Core DbContext, SQL Server Migrations, Generic Repository & DbInitializer
│   └── FlowSpace.Api/            # Web API Controllers, SignalR Hubs, Swagger & Middlewares
└── app/                          # Single Page Application (HTML5, Vanilla CSS System, jQuery, Bootstrap 5)
```

---

## 🛠️ Công nghệ Sử dụng (Tech Stack)

| Phân tầng | Công nghệ / Thư viện | Phiên bản |
|---|---|---|
| **Backend Framework** | .NET Web API, C# 12 | 8.0 (`net8.0`) |
| **ORM & Database** | Entity Framework Core, SQL Server LocalDB | 8.0.* |
| **Real-time Engine** | SignalR WebSockets (`ChatHub`, `NotificationHub`) | 8.0.* |
| **Authentication** | JWT Bearer Token, Refresh Token Rotation, BCrypt.Net | 8.0.* / 4.0.3 |
| **Frontend Core** | Single Page Application (SPA), HTML5, CSS Variables, jQuery | 3.7.1 |
| **Frontend Utilities**| Bootstrap, SortableJS, Chart.js, FullCalendar.js | 5.3.3 / 1.15 / 4.4 |

---

## 🚀 Hướng dẫn Chạy Ứng dụng Dưới Local

### 1. Yêu cầu Tiền đề
- **.NET 8 SDK** hoặc mới hơn.
- **SQL Server LocalDB** (Mặc định `(localdb)\mssqllocaldb`) hoặc SQL Server Express.

### 2. Khởi chạy Backend .NET 8 Web API
1. Mở Terminal và di chuyển tới thư mục backend:
   ```bash
   cd backend/src
   ```
2. Chạy ứng dụng (Tự động tạo Database, Migration và Nạp Seed Data):
   ```bash
   dotnet run --project FlowSpace.Api/FlowSpace.Api.csproj
   ```
3. Truy cập Swagger API Documentation:
   - HTTPS: `https://localhost:7297/swagger`
   - HTTP: `http://localhost:5000/swagger`

### 3. Khởi chạy Frontend SPA
1. Mở file `app/login.html` trực tiếp trong trình duyệt web (hoặc sử dụng VS Code *Live Server*).
2. Chọn một trong các tài khoản demo dưới đây để trải nghiệm ngay.

---

## 🔑 Tài khoản Demo Hệ thống (Demo Credentials)

Hệ thống được khởi tạo sẵn 4 tài khoản đại diện cho 4 vai trò phân quyền (RBAC):

| Vai trò (Role) | Tên người dùng | Username | Mật khẩu | Hạn mức Phê duyệt |
|---|---|---|---|---|
| **Giám đốc (Director)** | Nguyen Director | `director` | `123456` | Duyệt cấp tối cao (Cấp 4) |
| **Quản lý (Manager)** | Tran Manager | `manager` | `123456` | Duyệt dự án & đơn cấp 2, 3 |
| **Trưởng nhóm (Team Lead)** | Le Lead | `team_lead` | `123456` | Duyệt đơn cấp 1 (Hạn mức nhóm) |
| **Nhân viên (Employee)** | Pham Employee | `employee` | `123456` | Tạo đơn, làm task & log giờ |

---

## 🔌 Hệ thống RESTful API & Database Schema

- 📖 **Chi tiết APIs**: Xem bảng 30+ Endpoints RESTful tại [API.md](file:///e:/flowspace-fe/API.md).
- 🗄️ **Sơ đồ Database**: Xem chi tiết 10 bảng SQL Server tại [DATABASE.md](file:///e:/flowspace-fe/DATABASE.md).
- 📦 **Postman Collection**: File Postman đã được đóng gói sẵn tại [`postman/FlowSpace.postman_collection.json`](file:///e:/flowspace-fe/postman/FlowSpace.postman_collection.json).

---

## 🌐 Triển khai Production (Deployment)

Xem chi tiết quy trình đóng gói Container Docker, Deploy Render, IIS Server và Vercel/Nginx tại [DEPLOYMENT.md](file:///e:/flowspace-fe/DEPLOYMENT.md).

```bash
# Lệnh build Docker Container Backend
docker build -t flowspace-api:latest -f backend/Dockerfile backend/
```

---

## 📜 Giấy phép (License)

Dự án được phát hành theo giấy phép **[MIT License](file:///e:/flowspace-fe/LICENSE)**.
