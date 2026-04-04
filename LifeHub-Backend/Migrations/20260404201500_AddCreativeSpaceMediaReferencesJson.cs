using LifeHub.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260404201500_AddCreativeSpaceMediaReferencesJson")]
    public partial class AddCreativeSpaceMediaReferencesJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent migration: supports environments where the column was
            // added manually as an emergency fix before committing migrations.
            migrationBuilder.Sql(@"
IF COL_LENGTH('CreativeSpaces', 'MediaReferencesJson') IS NULL
BEGIN
    ALTER TABLE [CreativeSpaces]
    ADD [MediaReferencesJson] NVARCHAR(MAX) NOT NULL
        CONSTRAINT [DF_CreativeSpaces_MediaReferencesJson] DEFAULT N'[]';
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('CreativeSpaces', 'MediaReferencesJson') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        JOIN sys.columns c
            ON c.default_object_id = dc.object_id
        JOIN sys.tables t
            ON t.object_id = c.object_id
        WHERE t.name = 'CreativeSpaces'
          AND c.name = 'MediaReferencesJson'
    )
    BEGIN
        ALTER TABLE [CreativeSpaces]
        DROP CONSTRAINT [DF_CreativeSpaces_MediaReferencesJson];
    END

    ALTER TABLE [CreativeSpaces] DROP COLUMN [MediaReferencesJson];
END
");
        }
    }
}