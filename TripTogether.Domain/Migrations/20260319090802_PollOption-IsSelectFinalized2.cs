using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripTogether.Domain.Migrations
{
    /// <inheritdoc />
    public partial class PollOptionIsSelectFinalized2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_select_finalized",
                table: "poll_options",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_select_finalized",
                table: "poll_options");
        }
    }
}
