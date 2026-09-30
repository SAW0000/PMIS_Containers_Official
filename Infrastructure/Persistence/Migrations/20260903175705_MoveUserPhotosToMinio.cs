using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class MoveUserPhotosToMinio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE dbo.Users SET PhotoFileRef = NULL WHERE PhotoFileRef IS NOT NULL AND PhotoFileRef NOT LIKE '%/%';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
