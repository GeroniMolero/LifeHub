using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorToDocumentPublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "DocumentPublications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Author",
                table: "DocumentPublications");
        }
    }
}
