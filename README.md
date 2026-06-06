 FUNewsManagementSystem

**FUNewsManagementSystem** là một hệ thống quản lý tin tức dành cho trường đại học, cho phép **Admin**, **Staff**, và **Lecturer** đăng tải, quản lý, và phê duyệt tin tức.  
Dự án bao gồm:
- **Backend API**: ASP.NET Core Web API  
- **Frontend**: ASP.NET Core MVC (giao diện web)

---

 1. Cấu hình và chạy dự án

### **Yêu cầu hệ thống**
- .NET 8 SDK hoặc cao hơn  
- SQL Server 2019+  
- Visual Studio 2022 (hoặc VS Code có C# extension)  

---

### **Cấu hình Database**

1. Tạo database:
   chạy file sql kèm theo
2. upgrade-migration trong nuget package
---

### **Chạy Backend API**

```bash
cd FUNewsManagementSystem.BE
dotnet run
```

- API mặc định chạy tại: **https://localhost:7135** hoặc **http://localhost:7134**

---
**Chạy AI APi**

```bash
cd FUNewsManagementSystem.AiApi
dotnet run
```
 **Chạy Analytic APi**

```bash
cd FUNewsManagementSystem.AnalyticsAPI
dotnet run
```
### **Chạy Frontend (MVC)**

```bash
cd FUNewsManagementSystem.FE
dotnet run
```

- Ứng dụng web mặc định chạy tại: **https://localhost:7036** hoặc **http://localhost:7035**



 2. Tài khoản test

| **Role** | **Username / Email** | **Password** |
|-----------|----------------------|---------------|
| 🛠 **Admin** | `admin@FUNewsManagementSystem.org` | `@@abc123@@` |
| 👩‍💼 **Staff** | `MichaelCharlotte@FUNewsManagement.org` | `@1` |
| 👨‍🏫 **Lecturer** | `OliviaJames@FUNewsManagement.org` | `@1` |

---


---

3. Tính năng chính

- Quản lý và duyệt tin tức (theo vai trò)
- Phân quyền người dùng: Admin / Staff / Lecturer
- Giao diện thân thiện, dễ sử dụng
- Xác thực bằng JWT (API) + Cookie Auth (MVC)
- Xem audit log
- Offline mode
---
