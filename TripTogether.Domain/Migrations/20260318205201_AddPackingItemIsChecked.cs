using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripTogether.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddPackingItemIsChecked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_checked",
                table: "packing_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_checked",
                table: "packing_items");
        }
    }
}
