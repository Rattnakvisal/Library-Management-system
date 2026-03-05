using Library_Management_system.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library_Management_system.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260305113000_AddAuthorsAndNormalizeBooks")]
    public partial class AddAuthorsAndNormalizeBooks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Authors_Name",
                table: "Authors",
                column: "Name",
                unique: true);

            migrationBuilder.AddColumn<bool>(
                name: "Availability",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorId",
                table: "Books",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookImage",
                table: "Books",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Books",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summarized",
                table: "Books",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE b
                SET
                    b.BookImage = CASE WHEN b.BookImage IS NULL OR LTRIM(RTRIM(b.BookImage)) = '' THEN b.ImageUrl ELSE b.BookImage END,
                    b.Summarized = CASE WHEN b.Summarized IS NULL OR LTRIM(RTRIM(b.Summarized)) = '' THEN b.Description ELSE b.Summarized END,
                    b.Availability = CASE
                        WHEN b.Quantity <= 0 OR b.Status IN ('unavailable', 'maintenance') THEN 0
                        ELSE 1
                    END
                FROM Books b;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO Authors (Name)
                SELECT DISTINCT LEFT(LTRIM(RTRIM(ISNULL(b.Author, 'Unknown Author'))), 100)
                FROM Books b
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM Authors a
                    WHERE a.Name = LEFT(LTRIM(RTRIM(ISNULL(b.Author, 'Unknown Author'))), 100)
                );
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM Authors WHERE Name = N'Unknown Author')
                    INSERT INTO Authors (Name) VALUES (N'Unknown Author');
                """);

            migrationBuilder.Sql(
                """
                UPDATE b
                SET b.AuthorId = a.Id
                FROM Books b
                INNER JOIN Authors a
                    ON a.Name = LEFT(LTRIM(RTRIM(ISNULL(b.Author, 'Unknown Author'))), 100)
                WHERE b.AuthorId IS NULL;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'Uncategorized')
                    INSERT INTO Categories (Name, CreatedDate) VALUES (N'Uncategorized', GETUTCDATE());
                """);

            migrationBuilder.Sql(
                """
                UPDATE b
                SET b.CategoryId = c.Id
                FROM Books b
                INNER JOIN Categories c
                    ON c.Name = LEFT(LTRIM(RTRIM(ISNULL(b.CategoryName, 'Uncategorized'))), 100)
                WHERE b.CategoryId IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE b
                SET b.CategoryId = c.Id
                FROM Books b
                CROSS APPLY (
                    SELECT TOP 1 Id
                    FROM Categories
                    WHERE Name = N'Uncategorized'
                ) c
                WHERE b.CategoryId IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE b
                SET b.AuthorId = a.Id
                FROM Books b
                CROSS APPLY (
                    SELECT TOP 1 Id
                    FROM Authors
                    WHERE Name = N'Unknown Author'
                ) a
                WHERE b.AuthorId IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Books",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AuthorId",
                table: "Books",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_AuthorId",
                table: "Books",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_CategoryId",
                table: "Books",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Authors_AuthorId",
                table: "Books",
                column: "AuthorId",
                principalTable: "Authors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Categories_CategoryId",
                table: "Books",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Books_Authors_AuthorId",
                table: "Books");

            migrationBuilder.DropForeignKey(
                name: "FK_Books_Categories_CategoryId",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_AuthorId",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_CategoryId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Availability",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "BookImage",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Summarized",
                table: "Books");

            migrationBuilder.DropTable(
                name: "Authors");
        }
    }
}
