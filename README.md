# Library Management System

Library Management System is an ASP.NET Core 9 web app for managing books, reservations, borrowing, returns, fines, and user accounts with role-based access.

## Core Features

- ASP.NET Core Identity authentication with email confirmation
- Role-based access (`Admin`, `Librarian`, `User`)
- Book catalog management (books, categories, authors, images)
- Reservation workflow with approval/rejection and FIFO priority checks
- Borrowing lifecycle (create, update, return) with fine tracking
- Student-facing pages for search, cart, bookmark, history, and reviews
- Admin reporting (`borrowing`, `returns`, `most-borrowed`, `fine-collection`)
- Contact inbox and feedback/review moderation
- Optional Telegram notifications for admin alerts and OTP delivery

## Tech Stack

| Layer         | Technology                           |
| ------------- | ------------------------------------ |
| Framework     | ASP.NET Core 9 (MVC + Razor Pages)   |
| Language      | C# / .NET 9                          |
| Data          | Entity Framework Core 9 + SQL Server |
| Identity      | ASP.NET Core Identity                |
| UI            | Razor Views, Bootstrap 5, JavaScript |
| Email         | MailKit (SMTP)                       |
| Notifications | Telegram Bot API (optional)          |

## Project Structure

```text
.
|-- Controllers/
|   |-- Admin/
|   |-- User/
|   |-- AccountController.cs
|   |-- HomeController.cs
|-- Data/
|   |-- ApplicationDbContext.cs
|   |-- Migrations/
|-- Models/
|   |-- Admin/
|   |-- ApplicationUser.cs
|-- Services/
|   |-- EmailSender.cs
|   |-- TelegramNotifier.cs
|-- Views/
|-- Areas/Identity/Pages/
|-- wwwroot/
|-- Program.cs
|-- appsettings.json
```

## Prerequisites

- .NET 9 SDK
- SQL Server (2019+ recommended)
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## Quick Start

```bash
git clone https://github.com/Rattnakvisal/Library-Management-system.git
cd Library-Management-system
dotnet restore
dotnet build
dotnet ef database update
dotnet run --launch-profile https
```

Open:

- `https://localhost:7004`
- `http://localhost:5083`

## Configuration

Use `appsettings.Development.json` or user-secrets for local sensitive values.

### Required keys

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Server=YOUR_SERVER;Database=LIBRARY_DB;User ID=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
    },
    "TelegramBot": {
        "Enabled": false,
        "BotToken": "",
        "AdminChatId": "",
        "OtpBotToken": "",
        "OtpChatId": ""
    },
    "SeedAdmin": {
        "Email": "admin@library.com",
        "Password": "Admin@123",
        "ResetPasswordOnStartup": false
    }
}
```

### User-secrets example

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=LIBRARY_DB;User ID=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
dotnet user-secrets set "EmailSettings:SenderEmail" "you@example.com"
dotnet user-secrets set "EmailSettings:Password" "your-app-password"
dotnet user-secrets set "SeedAdmin:Email" "admin@library.com"
dotnet user-secrets set "SeedAdmin:Password" "Admin@123"
```

## Database and Seeding

On startup, the app automatically:

- Applies pending EF Core migrations
- Ensures roles exist: `Admin`, `Librarian`, `User`
- Ensures a seed admin account exists and is in `Admin` role
- Optionally resets seed admin password in Development mode

Manual migration commands:

```bash
dotnet ef migrations add YourMigrationName
dotnet ef database update
dotnet ef migrations list
```

## Roles and Access

| Role      | Access                                                                   |
| --------- | ------------------------------------------------------------------------ |
| Admin     | Full management: users, books, category/author/event, reports, dashboard |
| Librarian | Operational management: dashboard, borrowing, catalog operations         |
| User      | Browse/search books, reserve via cart, bookmark, review, history/profile |

## Main Routes

| Area             | Route                        |
| ---------------- | ---------------------------- |
| Home             | `/`                          |
| Login            | `/login`                     |
| Book list        | `/book`                      |
| Book detail      | `/book/{id}`                 |
| Cart             | `/cart`                      |
| Bookmark         | `/bookmark`                  |
| History          | `/history`                   |
| Admin dashboard  | `/admin/dashboard`           |
| Manage users     | `/admin/manageuser`          |
| Manage borrowing | `/admin/manageborrowingbook` |
| Reports          | `/admin/managereport`        |

## Development Notes

- Default launch profile is configured in `Properties/launchSettings.json`
- Static assets are under `wwwroot/`
- Admin report data endpoint: `GET /admin/managereport/data`
- Fine calculation uses a fixed rate of `$1.00/day` in current logic

## Security Notes

- Do not commit real credentials/tokens in `appsettings.json`
- Prefer environment variables or `dotnet user-secrets` for local development
- Rotate any leaked connection strings, SMTP passwords, or bot tokens immediately

## Contributing

```bash
git checkout -b feature/your-feature
git commit -m "feat: your description"
git push origin feature/your-feature
```

Then open a pull request.

## License

MIT License - see [LICENSE](LICENSE).

## Contact

GitHub: [@Rattnakvisal](https://github.com/Rattnakvisal)
