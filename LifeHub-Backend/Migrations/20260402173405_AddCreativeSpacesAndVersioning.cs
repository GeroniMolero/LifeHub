using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub.Migrations
{
    /// <inheritdoc />
    public partial class AddCreativeSpacesAndVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreativeSpaceId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CreativeSpaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Privacy = table.Column<int>(type: "int", nullable: false),
                    IsPublicProfileVisible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreativeSpaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreativeSpaces_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpacePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreativeSpaceId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionLevel = table.Column<int>(type: "int", nullable: false),
                    GrantedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpacePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpacePermissions_AspNetUsers_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SpacePermissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SpacePermissions_CreativeSpaces_CreativeSpaceId",
                        column: x => x.CreativeSpaceId,
                        principalTable: "CreativeSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreativeSpaceId",
                table: "Documents",
                column: "CreativeSpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId_CreatedAt",
                table: "ActivityLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreativeSpaces_OwnerId_Name",
                table: "CreativeSpaces",
                columns: new[] { "OwnerId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_CreatedByUserId",
                table: "DocumentVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId_VersionNumber",
                table: "DocumentVersions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpacePermissions_CreativeSpaceId_UserId",
                table: "SpacePermissions",
                columns: new[] { "CreativeSpaceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpacePermissions_GrantedByUserId",
                table: "SpacePermissions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpacePermissions_UserId",
                table: "SpacePermissions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_CreativeSpaces_CreativeSpaceId",
                table: "Documents",
                column: "CreativeSpaceId",
                principalTable: "CreativeSpaces",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_CreativeSpaces_CreativeSpaceId",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "SpacePermissions");

            migrationBuilder.DropTable(
                name: "CreativeSpaces");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CreativeSpaceId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CreativeSpaceId",
                table: "Documents");
        }
    }
}
