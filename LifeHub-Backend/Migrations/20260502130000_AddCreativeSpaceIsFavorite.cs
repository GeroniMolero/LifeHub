using LifeHub.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260502130000_AddCreativeSpaceIsFavorite")]
    public partial class AddCreativeSpaceIsFavorite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('CreativeSpaces', 'IsFavorite') IS NULL
BEGIN
    ALTER TABLE [CreativeSpaces]
    ADD [IsFavorite] BIT NOT NULL
        CONSTRAINT [DF_CreativeSpaces_IsFavorite] DEFAULT 0;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('CreativeSpaces', 'IsFavorite') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        JOIN sys.columns c
            ON c.default_object_id = dc.object_id
        JOIN sys.tables t
            ON t.object_id = c.object_id
        WHERE t.name = 'CreativeSpaces'
          AND c.name = 'IsFavorite'
    )
    BEGIN
        ALTER TABLE [CreativeSpaces]
        DROP CONSTRAINT [DF_CreativeSpaces_IsFavorite];
    END

    ALTER TABLE [CreativeSpaces] DROP COLUMN [IsFavorite];
END
");
        }
    }
}
