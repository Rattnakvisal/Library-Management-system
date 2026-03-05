using Library_Management_system.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library_Management_system.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260305190000_AddFinesForLateReturns")]
    public partial class AddFinesForLateReturns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fines",
                columns: table => new
                {
                    FineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorrowID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Paid = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fines", x => x.FineID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fines_BorrowID",
                table: "fines",
                column: "BorrowID",
                unique: true);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[BorrowingRecords]', N'U') IS NOT NULL
                BEGIN
                    ALTER TABLE [fines] WITH CHECK
                    ADD CONSTRAINT [FK_fines_BorrowingRecords_BorrowID]
                    FOREIGN KEY([BorrowID]) REFERENCES [BorrowingRecords]([Id]) ON DELETE CASCADE;

                    INSERT INTO fines (BorrowID, Amount, Paid, PaidDate, Remark)
                    SELECT
                        br.Id,
                        CAST(DATEDIFF(DAY, CONVERT(date, br.DueDate), CONVERT(date, br.ReturnDate)) AS decimal(10,2)),
                        CAST(0 AS bit),
                        NULL,
                        N'Backfilled fine for historical late return'
                    FROM BorrowingRecords br
                    WHERE br.ReturnDate IS NOT NULL
                      AND CONVERT(date, br.ReturnDate) > CONVERT(date, br.DueDate)
                      AND NOT EXISTS (
                          SELECT 1
                          FROM fines f
                          WHERE f.BorrowID = br.Id
                      );
                END
                ELSE IF OBJECT_ID(N'[Borrowing]', N'U') IS NOT NULL
                BEGIN
                    ALTER TABLE [fines] WITH CHECK
                    ADD CONSTRAINT [FK_fines_Borrowing_BorrowID]
                    FOREIGN KEY([BorrowID]) REFERENCES [Borrowing]([Id]) ON DELETE CASCADE;

                    INSERT INTO fines (BorrowID, Amount, Paid, PaidDate, Remark)
                    SELECT
                        br.Id,
                        CAST(DATEDIFF(DAY, CONVERT(date, br.DueDate), CONVERT(date, br.ReturnDate)) AS decimal(10,2)),
                        CAST(0 AS bit),
                        NULL,
                        N'Backfilled fine for historical late return'
                    FROM Borrowing br
                    WHERE br.ReturnDate IS NOT NULL
                      AND CONVERT(date, br.ReturnDate) > CONVERT(date, br.DueDate)
                      AND NOT EXISTS (
                          SELECT 1
                          FROM fines f
                          WHERE f.BorrowID = br.Id
                      );
                END
                ELSE
                BEGIN
                    THROW 50001, 'Neither BorrowingRecords nor Borrowing table exists. Cannot create fines foreign key.', 1;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fines");
        }
    }
}
