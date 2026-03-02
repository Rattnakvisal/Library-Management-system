using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library_Management_system.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookTableColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookCode",
                table: "Books",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Isbn",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pages",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Books",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "available");

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookCode",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Isbn",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Pages",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Books");
        }
    }
}
