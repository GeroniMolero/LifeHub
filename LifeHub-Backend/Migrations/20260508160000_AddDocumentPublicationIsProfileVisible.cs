using LifeHub.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260508160000_AddDocumentPublicationIsProfileVisible")]
    public partial class AddDocumentPublicationIsProfileVisible : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('DocumentPublications', 'IsProfileVisible') IS NULL
BEGIN
    ALTER TABLE [DocumentPublications]
    ADD [IsProfileVisible] BIT NOT NULL
        CONSTRAINT [DF_DocumentPublications_IsProfileVisible] DEFAULT 1;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('DocumentPublications', 'IsProfileVisible') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.default_object_id = dc.object_id
        JOIN sys.tables t ON t.object_id = c.object_id
        WHERE t.name = 'DocumentPublications' AND c.name = 'IsProfileVisible'
    )
    BEGIN
        ALTER TABLE [DocumentPublications]
        DROP CONSTRAINT [DF_DocumentPublications_IsProfileVisible];
    END

    ALTER TABLE [DocumentPublications] DROP COLUMN [IsProfileVisible];
END
");
        }
    }
}
