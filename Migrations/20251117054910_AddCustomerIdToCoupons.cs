using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeStore.Migrations
{
    public partial class AddCustomerIdToCoupons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "Coupons",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_CustomerId",
                table: "Coupons",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK__Coupons__Custome__1F98B2C1",
                table: "Coupons",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Coupons__Custome__1F98B2C1",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_CustomerId",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Coupons");
        }
    }
}
