using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripTogether.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRequesterIdFromFriendship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_friendships_users_requester_id",
                table: "friendships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_friendships",
                table: "friendships");

            migrationBuilder.DropColumn(
                name: "requester_id",
                table: "friendships");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "friendships",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "friendships",
                newName: "created_by");

            migrationBuilder.AddPrimaryKey(
                name: "PK_friendships",
                table: "friendships",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_friendships_created_by",
                table: "friendships",
                column: "created_by");

            migrationBuilder.AddForeignKey(
                name: "FK_friendships_users_created_by",
                table: "friendships",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_friendships_users_created_by",
                table: "friendships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_friendships",
                table: "friendships");

            migrationBuilder.DropIndex(
                name: "IX_friendships_created_by",
                table: "friendships");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "friendships",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "friendships",
                newName: "CreatedBy");

            migrationBuilder.AddColumn<Guid>(
                name: "requester_id",
                table: "friendships",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_friendships",
                table: "friendships",
                columns: new[] { "requester_id", "addressee_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_friendships_users_requester_id",
                table: "friendships",
                column: "requester_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
