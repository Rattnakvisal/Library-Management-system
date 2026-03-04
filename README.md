#  Library Management System

A web-based library management system built with **ASP.NET Core 9**, **Entity Framework Core**, and **Bootstrap 5**.

---

##  Features

**Authentication & Security**
- Email verification required for all new accounts
- Role-based access control (Admin, Librarian, Student)
- Secure password enforcement and HTTPS

**Admin**
- Dashboard with borrowing trends, stats, and overdue fines
- Manage users, roles, books, and borrowing records

**Librarian**
- View dashboard statistics
- Manage and update borrowing records

**Student**
- Search books by title, author, or category
- View borrowing history and account info

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 9, Razor Pages |
| Database | SQL Server, Entity Framework Core |
| Auth | ASP.NET Core Identity |
| Frontend | Bootstrap 5, HTML5, CSS3, JS |
| Email | SMTP via Gmail |

---

##  Getting Started

### Prerequisites
- .NET 9 SDK
- SQL Server 2019+
- Visual Studio 2022+
- Gmail account with App Password

### Installation
```bash
git clone https://github.com/Rattnakvisal/Library-Management-system.git
cd Library-Management-system
dotnet restore
dotnet ef database update
dotnet run
```

Then open https://localhost:7004

### Configuration

In `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=LibraryManagementDB;Trusted_Connection=true;"
},
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "Port": 587,
  "SenderEmail": "your-email@gmail.com",
  "Password": "your-16-char-app-password"
}
```

---

##  Default Admin Account

| Field | Value |
|---|---|
| Email | admin@library.com |
| Password | Admin@123 |

>  Change these credentials immediately after first login.

---

## 👥 Roles

| Role | Access |
|---|---|
| Admin | Full access — users, books, borrowing, dashboard |
| Librarian | Dashboard + borrowing records |
| Student | Book search + borrowing history |

---

## 📧 Email Verification Flow

1. User registers or Admin creates an account
2. Verification email is sent automatically
3. User clicks the confirmation link
4. Account is activated and user can log in

---

##  Contributing
```bash
git checkout -b feature/your-feature
git commit -m "feat: your description"
git push origin feature/your-feature
```

Then open a Pull Request on GitHub.

---

##  License

MIT License — see [LICENSE](LICENSE) for details.

---

##  Contact

GitHub: [@Rattnakvisal](https://github.com/Rattnakvisal)
