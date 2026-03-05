using Library_Management_system.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library_Management_system.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260305110000_AddBorrowingReservationAndReturnMetadata")]
    public partial class AddBorrowingReservationAndReturnMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                table: "BorrowingRecords",
                type: "int",
                nullable: false,
                defaultValue: 14);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "BorrowingRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "BorrowingRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnUserId",
                table: "BorrowingRecords",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowingRecords_ReservationId",
                table: "BorrowingRecords",
                column: "ReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowingRecords_CartItems_ReservationId",
                table: "BorrowingRecords",
                column: "ReservationId",
                principalTable: "CartItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BorrowingRecords_CartItems_ReservationId",
                table: "BorrowingRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowingRecords_ReservationId",
                table: "BorrowingRecords");

            migrationBuilder.DropColumn(
                name: "DurationDays",
                table: "BorrowingRecords");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "BorrowingRecords");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "BorrowingRecords");

            migrationBuilder.DropColumn(
                name: "ReturnUserId",
                table: "BorrowingRecords");
        }
    }
}
