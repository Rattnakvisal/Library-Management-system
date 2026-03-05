using Library_Management_system.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library_Management_system.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260305170000_AddAuditFieldsToAuthors")]
    public partial class AddAuditFieldsToAuthors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Authors",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Authors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE Authors
                SET
                    CreatedBy = COALESCE(NULLIF(LTRIM(RTRIM(CreatedBy)), ''), N'System Admin'),
                    CreatedDate = COALESCE(CreatedDate, GETUTCDATE());
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Authors");
        }
    }
}
