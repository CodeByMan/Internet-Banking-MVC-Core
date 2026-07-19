using InternetBanking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternetBanking.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationContext))]
    [Migration("20260716000200_EnforceUniqueAccountNumbers")]
    public partial class EnforceUniqueAccountNumbers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT AccountNumber
                    FROM Products
                    GROUP BY AccountNumber
                    HAVING COUNT(*) > 1
                )
                    THROW 50001, 'Duplicate product account numbers must be resolved before applying this migration.', 1;

                IF EXISTS (SELECT 1 FROM Products WHERE LEN(AccountNumber) > 9)
                    THROW 50002, 'Product account numbers longer than 9 characters must be corrected before applying this migration.', 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "AccountNumber",
                table: "Products",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Products_AccountNumber",
                table: "Products",
                column: "AccountNumber",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_AccountNumber",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "AccountNumber",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(9)",
                oldMaxLength: 9);
        }
    }
}
